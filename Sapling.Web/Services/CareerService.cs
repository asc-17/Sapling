using Microsoft.EntityFrameworkCore;
using Sapling.Shared.Contracts;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

public sealed class CareerService(SaplingDbContext db) : ICareerService
{
    public async Task<IReadOnlyList<CareerPathDto>> GetPathsAsync(CancellationToken ct = default)
    {
        var roles = await db.CareerRoles.OrderByDescending(r => r.FitScore).ToListAsync(ct);
        return roles.Select(Map).ToList();
    }

    public async Task<CareerPathDto?> GetPathAsync(int id, CancellationToken ct = default)
    {
        var role = await db.CareerRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
        return role is null ? null : Map(role);
    }

    internal static CareerPathDto Map(CareerRole r) => new(
        r.Id, r.Title, r.Family, r.Tier, r.FitScore,
        r.EntrySalaryMp, r.EntrySalaryMetro, r.DemandTrend, r.FiveYearOutlook,
        Split(r.Employers), Split(r.Reasons), Split(r.CoreSkills), r.CounterCase);

    internal static string[] Split(string value) =>
        string.IsNullOrWhiteSpace(value) ? [] : value.Split('|', StringSplitOptions.RemoveEmptyEntries);
}

public sealed class SkillGapService(SaplingDbContext db, StudentContext ctx) : ISkillGapService
{
    public async Task<IReadOnlyList<SkillGapDto>> GetGapsAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var requirements = await db.RoleSkillRequirements
            .Include(r => r.Skill)
            .Where(r => r.CareerRoleId == profile.TargetRoleId)
            .ToListAsync(ct);

        var levels = profile.Skills.ToDictionary(s => s.SkillId, s => s.Verified ? s.Level : s.Level / 2);

        return requirements
            .Select(r =>
            {
                var current = levels.GetValueOrDefault(r.SkillId);
                var deficit = r.RequiredLevel - current;
                var severity = deficit >= 40 ? "Critical" : deficit >= 18 ? "Important" : deficit > 0 ? "Nice to have" : "Closed";
                var weeks = deficit <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(r.WeeksToClose * deficit / (double)r.RequiredLevel));
                return new SkillGapDto(r.SkillId, r.Skill?.Name ?? "", severity, r.Impact, r.Effort, weeks, current, r.RequiredLevel, r.Rationale);
            })
            .OrderBy(g => SeverityRank(g.Severity))
            .ThenByDescending(g => g.Impact)
            .ThenBy(g => g.Effort)
            .ToList();
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "Critical" => 0,
        "Important" => 1,
        "Nice to have" => 2,
        _ => 3,
    };

    public async Task<SkillRadarDto> GetRadarAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var role = await db.CareerRoles.FirstAsync(r => r.Id == profile.TargetRoleId, ct);
        var requirements = await db.RoleSkillRequirements
            .Include(r => r.Skill)
            .Where(r => r.CareerRoleId == profile.TargetRoleId)
            .OrderByDescending(r => r.Impact)
            .Take(6)
            .ToListAsync(ct);

        var levels = profile.Skills.ToDictionary(s => s.SkillId, s => (double)(s.Verified ? s.Level : s.Level / 2));

        return new SkillRadarDto(
            requirements.Select(r => r.Skill?.Name ?? "").ToList(),
            requirements.Select(r => levels.GetValueOrDefault(r.SkillId)).ToList(),
            requirements.Select(r => (double)r.RequiredLevel).ToList(),
            role.Title);
    }
}

using Microsoft.EntityFrameworkCore;
using Sapling.Shared.Contracts;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

public sealed class ProfileService(SaplingDbContext db, StudentContext ctx, ICollegeCatalogService catalog) : IProfileService
{
    public async Task<StudentProfileDto> GetAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        return Map(profile);
    }

    public async Task<StudentProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        profile.College = request.College;
        profile.Branch = request.Branch;
        profile.GraduationYear = request.GraduationYear;
        profile.State = request.State;
        profile.City = request.City;
        profile.Cgpa = request.Cgpa;
        profile.Backlogs = request.Backlogs;
        profile.PreferredLanguage = request.PreferredLanguage;

        if (profile.User is not null)
        {
            profile.User.FullName = request.FullName;
        }

        await db.SaveChangesAsync(ct);
        return Map(profile);
    }

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        return profile.Skills
            .OrderByDescending(s => s.Verified)
            .ThenByDescending(s => s.Level)
            .Select(s => new SkillDto(s.SkillId, s.Skill?.Name ?? "", s.Skill?.Category ?? "", s.Level, s.Verified, s.Source))
            .ToList();
    }

    public async Task<IReadOnlyList<SkillDto>> SetClaimedSkillsAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var wanted = await db.Skills.Where(s => skillNames.Contains(s.Name)).ToListAsync(ct);

        // Verified skills are evidence-backed and are never removed by an onboarding edit.
        var removable = profile.Skills.Where(s => !s.Verified && !wanted.Any(w => w.Id == s.SkillId)).ToList();
        db.StudentSkills.RemoveRange(removable);
        foreach (var item in removable)
        {
            profile.Skills.Remove(item);
        }

        foreach (var skill in wanted.Where(w => profile.Skills.All(s => s.SkillId != w.Id)))
        {
            profile.Skills.Add(new StudentSkill { SkillId = skill.Id, Level = 35, Verified = false, Source = "Self-claimed", Skill = skill });
        }

        await db.SaveChangesAsync(ct);
        return await GetSkillsAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetSkillSuggestionsAsync(CancellationToken ct = default) =>
        await db.Skills.OrderBy(s => s.Category).ThenBy(s => s.Name).Select(s => s.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<SkillSuggestionDto>> GetSkillCatalogueAsync(CancellationToken ct = default) =>
        await db.Skills.OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new SkillSuggestionDto(s.Name, s.Category)).ToListAsync(ct);

    public Task<IReadOnlyList<CollegeDto>> SearchCollegesAsync(string query, string? state = null, CancellationToken ct = default)
    {
        var results = catalog.Search(query, state, limit: 20);
        return Task.FromResult(results);
    }

    public async Task CompleteOnboardingAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        profile.OnboardingComplete = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task<StudentProfileDto> SetAvatarAsync(string? dataUrl, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        profile.AvatarDataUrl = dataUrl;
        await db.SaveChangesAsync(ct);
        return Map(profile);
    }

    private static StudentProfileDto Map(StudentProfile p) => new(
        p.User?.FullName ?? "Student",
        p.User?.Email ?? "",
        p.College,
        p.Branch,
        p.GraduationYear,
        p.City,
        p.Cgpa,
        p.Backlogs,
        p.PreferredLanguage,
        p.OnboardingComplete,
        p.RiasecCode,
        p.State,
        p.AvatarDataUrl);
}

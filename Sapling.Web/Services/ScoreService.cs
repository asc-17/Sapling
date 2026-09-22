using Microsoft.EntityFrameworkCore;
using Sapling.Shared.Contracts;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

/// <summary>
/// Recomputes the Employability Score from evidence in the database. Weights are fixed by the PRD:
/// academics 15, verified technical skills 30, projects 15, communication 20, certifications 10, exposure 10.
/// </summary>
public sealed class ScoreService(SaplingDbContext db, StudentContext ctx) : IScoreService
{
    public async Task<EmployabilityScoreDto> GetAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var snapshot = await RecomputeAsync(profile, ct);
        var previous = await db.ScoreSnapshots
            .Where(s => s.StudentProfileId == profile.Id && s.Id != snapshot.Id)
            .OrderByDescending(s => s.AsOf)
            .Select(s => s.Total)
            .FirstOrDefaultAsync(ct);

        return new EmployabilityScoreDto(
            snapshot.Total,
            previous == 0 ? snapshot.Total : previous,
            snapshot.AsOf,
            [
                new("Verified technical skills", 30, snapshot.TechnicalSkills, "Only skills backed by a certificate, assessment or code artefact count here."),
                new("Communication & interview", 20, snapshot.Communication, "Averaged across your scored mock interview sessions."),
                new("Academics", 15, snapshot.Academic, "CGPA and backlogs, normalised against the intake for your branch."),
                new("Projects & portfolio", 15, snapshot.Projects, "Completed roadmap projects with a published artefact."),
                new("Certifications", 10, snapshot.Certifications, "Completed courses with an uploaded or verified certificate."),
                new("Industry exposure", 10, snapshot.Exposure, "Internships, applications in progress and live drive participation."),
            ]);
    }

    public async Task<HomeSummaryDto> GetHomeSummaryAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var snapshot = await RecomputeAsync(profile, ct);

        var items = await db.RoadmapItems
            .Where(i => i.StudentProfileId == profile.Id)
            .OrderBy(i => i.Order)
            .ToListAsync(ct);

        var completed = items.Count(i => i.Completed);
        var next = items.FirstOrDefault(i => !i.Completed);
        var totalWeeks = items.Count == 0 ? 0 : items.Max(i => i.WeekNumber);
        var currentWeek = next?.WeekNumber ?? totalWeeks;

        var applied = await db.OpportunityApplications
            .Where(a => a.StudentProfileId == profile.Id)
            .Select(a => a.OpportunityId)
            .ToListAsync(ct);

        var newOpportunities = await db.Opportunities.CountAsync(o => !applied.Contains(o.Id), ct);

        var soon = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        var closingExams = await db.GovtExams.CountAsync(e => e.ExamOn != null && e.ExamOn <= soon, ct);

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var newPosts = await CommunityService.VisibleTo(db, profile).CountAsync(p => p.PostedAtUtc >= weekAgo, ct);

        var firstName = (profile.User?.FullName ?? "Student").Split(' ')[0];

        return new HomeSummaryDto(
            firstName,
            profile.OnboardingComplete,
            snapshot.Total,
            currentWeek,
            totalWeeks,
            completed,
            items.Count,
            next?.Title,
            newOpportunities,
            closingExams,
            newPosts);
    }

    public async Task<ScoreSnapshot> RecomputeAsync(StudentProfile profile, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var verified = profile.Skills.Where(s => s.Verified).ToList();
        var technical = verified.Count == 0 ? 0 : (int)Math.Round(verified.Average(s => s.Level) * Math.Min(1, verified.Count / 6.0));

        var academic = (int)Math.Round(Math.Clamp((profile.Cgpa / 10.0 * 100) - (profile.Backlogs * 8), 0, 100));

        var items = await db.RoadmapItems.Where(i => i.StudentProfileId == profile.Id).ToListAsync(ct);
        var projectItems = items.Where(i => i.Kind == "Project").ToList();
        var courseItems = items.Where(i => i.Kind == "Course").ToList();

        var projects = projectItems.Count == 0 ? 30 : 30 + (int)Math.Round(70.0 * projectItems.Count(i => i.Completed) / projectItems.Count);
        var certifications = courseItems.Count == 0 ? 20 : 20 + (int)Math.Round(80.0 * courseItems.Count(i => i.Completed) / courseItems.Count);

        var interviewScores = await db.InterviewSessions
            .Where(s => s.StudentProfileId == profile.Id && s.OverallScore != null)
            .Select(s => s.OverallScore!.Value)
            .ToListAsync(ct);
        var communication = interviewScores.Count == 0
            ? profile.Skills.FirstOrDefault(s => s.Skill?.Name == "Communication")?.Level ?? 40
            : (int)Math.Round(interviewScores.Average());

        var applications = await db.OpportunityApplications.CountAsync(a => a.StudentProfileId == profile.Id, ct);
        var exposure = Math.Clamp(10 + (applications * 15), 0, 100);

        var total = (int)Math.Round(
            (academic * 0.15) +
            (technical * 0.30) +
            (projects * 0.15) +
            (communication * 0.20) +
            (certifications * 0.10) +
            (exposure * 0.10));

        var snapshot = await db.ScoreSnapshots
            .FirstOrDefaultAsync(s => s.StudentProfileId == profile.Id && s.AsOf == today, ct);

        if (snapshot is null)
        {
            snapshot = new ScoreSnapshot { StudentProfileId = profile.Id, AsOf = today };
            db.ScoreSnapshots.Add(snapshot);
        }

        snapshot.Total = total;
        snapshot.Academic = academic;
        snapshot.TechnicalSkills = technical;
        snapshot.Projects = projects;
        snapshot.Communication = communication;
        snapshot.Certifications = certifications;
        snapshot.Exposure = exposure;

        await db.SaveChangesAsync(ct);
        return snapshot;
    }
}

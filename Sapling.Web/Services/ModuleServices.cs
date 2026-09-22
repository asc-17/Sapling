using Microsoft.EntityFrameworkCore;
using Sapling.Shared.Content;
using Sapling.Shared.Contracts;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

public sealed class RoadmapService(SaplingDbContext db, StudentContext ctx, ScoreService scores) : IRoadmapService
{
    public async Task<RoadmapDto> GetAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var role = await db.CareerRoles.FirstOrDefaultAsync(r => r.Id == profile.TargetRoleId, ct);
        var items = await db.RoadmapItems
            .Where(i => i.StudentProfileId == profile.Id)
            .OrderBy(i => i.Order)
            .ToListAsync(ct);

        var weeks = items
            .GroupBy(i => i.WeekNumber)
            .OrderBy(g => g.Key)
            .Select(g => new RoadmapWeekDto(
                g.Key,
                g.First().WeekFocus,
                DateOnly.FromDateTime(DateTime.Today.AddDays(7 * (g.Key - 1))),
                g.Select(i => new RoadmapItemDto(i.Id, i.Title, i.Kind, i.Detail, i.CourseId, i.Completed, i.EstimatedHours)).ToList()))
            .ToList();

        var currentWeek = items.FirstOrDefault(i => !i.Completed)?.WeekNumber ?? weeks.Count;

        return new RoadmapDto(
            role?.Title ?? "your target role",
            weeks.Count,
            currentWeek,
            items.Count(i => i.Completed),
            items.Count,
            weeks);
    }

    public async Task<RoadmapDto> SetItemCompletedAsync(int itemId, bool completed, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var item = await db.RoadmapItems.FirstOrDefaultAsync(i => i.Id == itemId && i.StudentProfileId == profile.Id, ct);
        if (item is not null)
        {
            item.Completed = completed;
            await db.SaveChangesAsync(ct);
            await scores.RecomputeAsync(profile, ct);
        }

        return await GetAsync(ct);
    }

    public async Task<CourseDto?> GetCourseAsync(int id, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        return course is null ? null : Map(course);
    }

    internal static CourseDto Map(Course c) => new(
        c.Id, c.Title, c.Provider, c.Cost, c.IsFree, c.IsGovernmentSubsidised,
        c.Hours, c.Level, c.Url, CareerService.Split(c.TeachesSkills), c.Summary);
}

public sealed class OpportunityService(SaplingDbContext db, StudentContext ctx, ScoreService scores) : IOpportunityService
{
    public async Task<IReadOnlyList<OpportunityDto>> GetAllAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var applications = await db.OpportunityApplications
            .Where(a => a.StudentProfileId == profile.Id)
            .ToDictionaryAsync(a => a.OpportunityId, a => a.Status, ct);

        var all = await db.Opportunities.OrderByDescending(o => o.MatchScore).ToListAsync(ct);
        return all.Select(o => Map(o, applications.GetValueOrDefault(o.Id))).ToList();
    }

    public async Task<OpportunityDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var opportunity = await db.Opportunities.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (opportunity is null)
        {
            return null;
        }

        var status = await db.OpportunityApplications
            .Where(a => a.StudentProfileId == profile.Id && a.OpportunityId == id)
            .Select(a => a.Status)
            .FirstOrDefaultAsync(ct);

        return Map(opportunity, status);
    }

    public async Task<OpportunityDto> ApplyAsync(int id, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var existing = await db.OpportunityApplications
            .FirstOrDefaultAsync(a => a.StudentProfileId == profile.Id && a.OpportunityId == id, ct);

        if (existing is null)
        {
            db.OpportunityApplications.Add(new OpportunityApplication { StudentProfileId = profile.Id, OpportunityId = id });
            await db.SaveChangesAsync(ct);
            await scores.RecomputeAsync(profile, ct);
        }

        return await GetAsync(id, ct) ?? throw new InvalidOperationException("Opportunity not found.");
    }

    // Readiness is surfaced, never used to hide a role.
    private static OpportunityDto Map(Opportunity o, string? status)
    {
        var missing = CareerService.Split(o.MissingSkills);
        var readiness = missing.Length switch
        {
            0 => "Ready to apply",
            1 => "1 skill away",
            _ => $"{missing.Length} skills away",
        };

        return new OpportunityDto(
            o.Id, o.Title, o.Company, o.Kind, o.Location, o.Remote, o.Stipend, o.ClosesOn,
            o.MatchScore, readiness, missing, CareerService.Split(o.Reasons), o.Description,
            string.IsNullOrEmpty(status) ? null : status);
    }
}

public sealed class GovtService(SaplingDbContext db, StudentContext ctx) : IGovtService
{
    public async Task<IReadOnlyList<GovtExamDto>> GetExamsAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var exams = await db.GovtExams.ToListAsync(ct);
        return exams
            .Select(e => Evaluate(e, profile))
            .OrderBy(e => e.Eligibility == "Eligible" ? 0 : e.Eligibility == "Eligible next year" ? 1 : 2)
            .ThenBy(e => e.ExamOn ?? DateOnly.MaxValue)
            .ToList();
    }

    public async Task<GovtExamDto?> GetExamAsync(int id, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var exam = await db.GovtExams.FirstOrDefaultAsync(e => e.Id == id, ct);
        return exam is null ? null : Evaluate(exam, profile);
    }

    /// <summary>Rule-based eligibility so the answer is auditable rather than generated.</summary>
    private static GovtExamDto Evaluate(GovtExam e, StudentProfile profile)
    {
        var reasons = new List<string>();
        var graduatesThisYear = profile.GraduationYear <= DateTime.Today.Year;
        var status = "Eligible";

        if (e.QualificationRequired.Contains("Postgraduate", StringComparison.OrdinalIgnoreCase))
        {
            status = "Not eligible";
            reasons.Add($"Requires {e.QualificationRequired}; you are completing a bachelor's degree in {profile.GraduationYear}.");
        }
        else if (!graduatesThisYear)
        {
            status = "Eligible next year";
            reasons.Add($"Graduate qualification is required and you finish in {profile.GraduationYear}.");
        }
        else
        {
            reasons.Add($"Your qualification meets the requirement: {e.QualificationRequired}.");
        }

        if (e.RequiresMpDomicile)
        {
            var isMp = string.Equals(profile.State, "Madhya Pradesh", StringComparison.OrdinalIgnoreCase)
                       || LocationCatalog.InferStateFromCity(profile.City).Equals("Madhya Pradesh", StringComparison.OrdinalIgnoreCase);

            if (isMp)
            {
                var loc = !string.IsNullOrEmpty(profile.City) && !string.IsNullOrEmpty(profile.State)
                    ? $"{profile.City}, {profile.State}"
                    : !string.IsNullOrEmpty(profile.State) ? profile.State : profile.City;
                reasons.Add($"Madhya Pradesh domicile is confirmed (recorded location: {loc}).");
            }
            else
            {
                reasons.Add(string.IsNullOrWhiteSpace(profile.State) && string.IsNullOrWhiteSpace(profile.City)
                    ? "Madhya Pradesh domicile is required; select your state and city to confirm."
                    : $"Madhya Pradesh domicile is required and your recorded state is {(!string.IsNullOrEmpty(profile.State) ? profile.State : profile.City)}.");
            }
        }

        reasons.Add($"Age window is {e.MinAge} to {e.MaxAge} years, which a {DateTime.Today.Year} graduate normally falls inside.");

        if (e.NotificationOn is { } notified && notified <= DateOnly.FromDateTime(DateTime.Today))
        {
            reasons.Add($"Notification released on {notified:d MMM yyyy}; applications are open now.");
        }

        return new GovtExamDto(
            e.Id, e.Name, e.Authority, e.Level, e.NotificationOn, e.ExamOn,
            status, reasons, CareerService.Split(e.SyllabusAreas), e.Summary);
    }
}

public sealed class QuizService(SaplingDbContext db, StudentContext ctx) : IQuizService
{
    private static readonly string[] Dimensions =
        ["Realistic", "Investigative", "Artistic", "Social", "Enterprising", "Conventional"];

    public async Task<IReadOnlyList<QuizQuestionDto>> GetQuestionsAsync(CancellationToken ct = default) =>
        await db.QuizQuestions
            .OrderBy(q => q.Order)
            .Select(q => new QuizQuestionDto(q.Id, q.Text, q.Dimension))
            .ToListAsync(ct);

    public async Task<QuizResultDto> SubmitAsync(QuizSubmissionRequest request, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var questions = await db.QuizQuestions.ToDictionaryAsync(q => q.Id, q => q.Dimension, ct);

        var totals = Dimensions.ToDictionary(d => d, _ => 0);
        var counts = Dimensions.ToDictionary(d => d, _ => 0);

        foreach (var answer in request.Answers)
        {
            if (questions.TryGetValue(answer.QuestionId, out var dimension) && totals.ContainsKey(dimension))
            {
                totals[dimension] += answer.Value;
                counts[dimension]++;
            }
        }

        var components = Dimensions
            .Select(d => new ScoreComponentDto(
                d,
                0,
                counts[d] == 0 ? 0 : (int)Math.Round(100.0 * totals[d] / (counts[d] * 5)),
                Describe(d)))
            .OrderByDescending(c => c.Value)
            .ToList();

        var code = string.Concat(components.Take(3).Select(c => c.Name[0]));
        profile.RiasecCode = code;
        await db.SaveChangesAsync(ct);

        return new QuizResultDto(code, components);
    }

    private static string Describe(string dimension) => dimension switch
    {
        "Realistic" => "Hands-on, practical, prefers building and fixing over discussing.",
        "Investigative" => "Analytical, enjoys open problems and understanding why something works.",
        "Artistic" => "Expressive, drawn to design and to work without one correct answer.",
        "Social" => "Teaching, mentoring and collaboration energise you.",
        "Enterprising" => "Persuading, leading and owning outcomes.",
        _ => "Structured, organised, reliable with process and detail.",
    };
}

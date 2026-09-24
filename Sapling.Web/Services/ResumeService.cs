using Microsoft.EntityFrameworkCore;
using Sapling.Shared.Contracts;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

public sealed class ResumeService(SaplingDbContext db, StudentContext ctx) : IResumeService
{
    private static readonly string[] Warnings =
    [
        "Two-column layout: four fields were not extracted in the correct order.",
        "Skills listed inside a graphic, so the parser read none of them.",
        "Dates written as \"Aug'23 – Jun'24\" were not recognised as a date range.",
        "Contact number embedded in a header, which several parsers skip.",
    ];

    private static readonly string[] Sections =
        ["Summary", "Education", "Experience", "Projects", "Skills", "Certifications"];

    public async Task<ResumeDto> GetAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        return await BuildAsync(profile, ct);
    }

    public async Task<ResumeDto> SetSuggestionAcceptedAsync(int suggestionId, bool accepted, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var suggestion = await db.ResumeSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.StudentProfileId == profile.Id, ct);

        if (suggestion is not null)
        {
            suggestion.Accepted = accepted;
            await db.SaveChangesAsync(ct);
        }

        return await BuildAsync(profile, ct);
    }

    public async Task<ResumeDto> TailorAsync(int opportunityId, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var opportunity = await db.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
        profile.ResumeTailoredForRole = opportunity is null ? null : $"{opportunity.Title} at {opportunity.Company}";
        await db.SaveChangesAsync(ct);
        return await BuildAsync(profile, ct);
    }

    private async Task<ResumeDto> BuildAsync(StudentProfile profile, CancellationToken ct)
    {
        var suggestions = await db.ResumeSuggestions
            .Where(s => s.StudentProfileId == profile.Id)
            .OrderBy(s => s.Order)
            .ToListAsync(ct);

        // Each accepted rewrite fixes a concrete parse or specificity problem, so the ATS score moves with it.
        var accepted = suggestions.Count(s => s.Accepted);
        var score = suggestions.Count == 0
            ? profile.AtsScore
            : profile.PreviousAtsScore + (int)Math.Round(27.0 * accepted / suggestions.Count);

        var warnings = Warnings.Skip(accepted).ToList();

        return new ResumeDto(
            score,
            profile.PreviousAtsScore,
            profile.ResumeTailoredForRole,
            warnings,
            suggestions.Select(s => new ResumeSuggestionDto(s.Id, s.Section, s.Original, s.Suggested, s.Rationale, s.Accepted)).ToList(),
            Sections);
    }
}

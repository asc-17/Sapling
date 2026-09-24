using Sapling.Shared.Contracts;
using Sapling.Web.Services;

namespace Sapling.Web.Api;

public static class ApiEndpoints
{
    /// <summary>The web head calls these services in-process; the MAUI head reaches them over HTTP.</summary>
    public static void MapSaplingApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        var profile = api.MapGroup("/profile");
        profile.MapGet("/", (IProfileService s, CancellationToken ct) => s.GetAsync(ct));
        profile.MapPut("/", (UpdateProfileRequest r, IProfileService s, CancellationToken ct) => s.UpdateAsync(r, ct));
        profile.MapGet("/skills", (IProfileService s, CancellationToken ct) => s.GetSkillsAsync(ct));
        profile.MapPut("/skills", (List<string> names, IProfileService s, CancellationToken ct) => s.SetClaimedSkillsAsync(names, ct));
        profile.MapGet("/skill-suggestions", (IProfileService s, CancellationToken ct) => s.GetSkillSuggestionsAsync(ct));
        profile.MapGet("/skill-catalogue", (IProfileService s, CancellationToken ct) => s.GetSkillCatalogueAsync(ct));
        profile.MapGet("/skill-picker", (IProfileService s, CancellationToken ct) => s.GetSkillPickerAsync(ct));
        profile.MapGet("/colleges", (string? q, string? state, ICollegeCatalogService catalog) =>
            catalog.Search(q ?? "", state, 20)).AllowAnonymous();
        profile.MapPost("/complete-onboarding", async (IProfileService s, CancellationToken ct) =>
        {
            await s.CompleteOnboardingAsync(ct);
            return Results.NoContent();
        });
        profile.MapPut("/avatar", (UpdateAvatarRequest r, IProfileService s, CancellationToken ct) =>
            s.SetAvatarAsync(r.AvatarDataUrl, ct));

        api.MapGet("/home", (IHomeService s, CancellationToken ct) => s.GetSummaryAsync(ct));

        var paths = api.MapGroup("/paths");
        paths.MapGet("/", (bool? all, ICareerService s, CancellationToken ct) => s.GetPathsAsync(all == true, ct));
        paths.MapGet("/{id:int}", async (int id, ICareerService s, CancellationToken ct) =>
            await s.GetPathAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());
        paths.MapPost("/{id:int}/target", async (int id, ICareerService s, CancellationToken ct) =>
            await s.SetTargetAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        var skills = api.MapGroup("/skills");
        skills.MapGet("/gaps", (ISkillGapService s, CancellationToken ct) => s.GetGapsAsync(ct));
        skills.MapGet("/coverage", (ISkillGapService s, CancellationToken ct) => s.GetCoverageAsync(ct));

        var roadmap = api.MapGroup("/roadmap");
        roadmap.MapGet("/", (IRoadmapService s, CancellationToken ct) => s.GetAsync(ct));
        roadmap.MapPost("/checkpoints/{id:int}/{done:bool}", (int id, bool done, IRoadmapService s, CancellationToken ct) =>
            s.SetCheckpointAsync(id, done, ct));
        roadmap.MapPost("/skills/{id:int}", (int id, IRoadmapService s, CancellationToken ct) => s.AddSkillAsync(id, ct));
        roadmap.MapPost("/courses/{id:int}/choose", (int id, int[] skillIds, IRoadmapService s, CancellationToken ct) =>
            s.ChooseCourseAsync(id, skillIds, ct));

        var opportunities = api.MapGroup("/opportunities");
        opportunities.MapGet("/", (IOpportunityService s, CancellationToken ct) => s.GetAllAsync(ct));
        opportunities.MapGet("/{id:int}", async (int id, IOpportunityService s, CancellationToken ct) =>
            await s.GetAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());
        opportunities.MapPost("/{id:int}/apply", (int id, IOpportunityService s, CancellationToken ct) => s.ApplyAsync(id, ct));

        var resume = api.MapGroup("/resume");
        resume.MapGet("/", (IResumeService s, CancellationToken ct) => s.GetAsync(ct));
        resume.MapPost("/suggestions/{id:int}/{accepted:bool}", (int id, bool accepted, IResumeService s, CancellationToken ct) =>
            s.SetSuggestionAcceptedAsync(id, accepted, ct));
        resume.MapPost("/tailor/{opportunityId:int}", (int opportunityId, IResumeService s, CancellationToken ct) =>
            s.TailorAsync(opportunityId, ct));

        var interview = api.MapGroup("/interview");
        interview.MapGet("/", (IInterviewService s, CancellationToken ct) => s.GetSessionsAsync(ct));
        interview.MapPost("/", (StartInterviewRequest r, IInterviewService s, CancellationToken ct) => s.StartAsync(r, ct));
        interview.MapGet("/{id:int}", async (int id, IInterviewService s, CancellationToken ct) =>
            await s.GetAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());
        interview.MapPost("/{id:int}/answer", (int id, AnswerInterviewRequest r, IInterviewService s, CancellationToken ct) =>
            s.AnswerAsync(id, r, ct));
        interview.MapPost("/{id:int}/finish", (int id, IInterviewService s, CancellationToken ct) => s.FinishAsync(id, ct));

        var govt = api.MapGroup("/govt");
        govt.MapGet("/", (IGovtService s, CancellationToken ct) => s.GetExamsAsync(ct));
        govt.MapGet("/{id:int}", async (int id, IGovtService s, CancellationToken ct) =>
            await s.GetExamAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        var quiz = api.MapGroup("/quiz");
        quiz.MapGet("/", (IQuizService s, CancellationToken ct) => s.GetQuestionsAsync(ct));
        quiz.MapPost("/", (QuizSubmissionRequest r, IQuizService s, CancellationToken ct) => s.SubmitAsync(r, ct));

        var community = api.MapGroup("/community");
        community.MapGet("/", (string? kind, ICommunityService s, CancellationToken ct) => s.GetFeedAsync(kind, ct));
        community.MapGet("/{id:int}", async (int id, ICommunityService s, CancellationToken ct) =>
            await s.GetPostAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());
        community.MapPost("/{id:int}/upvote/{upvoted:bool}", async (int id, bool upvoted, ICommunityService s, CancellationToken ct) =>
            await s.GetPostAsync(id, ct) is null ? Results.NotFound() : Results.Ok(await s.SetUpvoteAsync(id, upvoted, ct)));
        community.MapGet("/{id:int}/comments", (int id, ICommunityService s, CancellationToken ct) => s.GetCommentsAsync(id, ct));
        community.MapPost("/{id:int}/comments", async (int id, AddCommentRequest r, ICommunityService s, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await s.AddCommentAsync(id, r, ct));
            }
            catch (ArgumentException e)
            {
                return Results.BadRequest(e.Message);
            }
        });
        community.MapDelete("/{id:int}/comments/{commentId:int}", (int id, int commentId, ICommunityService s, CancellationToken ct) =>
            s.DeleteCommentAsync(id, commentId, ct));
    }
}

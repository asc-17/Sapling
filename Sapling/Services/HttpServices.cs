using System.Net.Http.Json;
using Sapling.Shared.Contracts;

namespace Sapling.Services;

public static class SaplingApi
{
    public const string Authenticated = "sapling-api";
    public const string Anonymous = "sapling-api-anon";

    /// <summary>
    /// The PC running Sapling.Web, as a phone on the same Wi-Fi sees it (ipconfig, "Wireless LAN adapter Wi-Fi").
    /// Update it if the router hands the PC a different address.
    /// </summary>
    private const string DevMachineHost = "192.168.1.68";

    // The emulator reaches the host through 10.0.2.2; a physical phone needs the PC's LAN address.
    public static string BaseAddress => DeviceInfo.Platform != DevicePlatform.Android
        ? "http://localhost:5250/"
        : DeviceInfo.DeviceType == DeviceType.Virtual
            ? "http://10.0.2.2:5250/"
            : $"http://{DevMachineHost}:5250/";
}

internal static class HttpExtensions
{
    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url, CancellationToken ct)
        where T : new()
        => await client.GetFromJsonAsync<T>(url, ct) ?? new T();

    public static async Task<T?> GetJsonOrNullAsync<T>(this HttpClient client, string url, CancellationToken ct)
    {
        var response = await client.GetAsync(url, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<T>(ct) : default;
    }

    public static async Task<T> PostJsonAsync<T>(this HttpClient client, string url, object? body, CancellationToken ct)
    {
        var response = body is null
            ? await client.PostAsync(url, null, ct)
            : await client.PostAsJsonAsync(url, body, ct);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}

public sealed class HttpProfileService(IHttpClientFactory factory) : IProfileService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<StudentProfileDto> GetAsync(CancellationToken ct = default) =>
        (await Client.GetFromJsonAsync<StudentProfileDto>("api/profile", ct))!;

    public async Task<StudentProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var response = await Client.PutAsJsonAsync("api/profile", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentProfileDto>(ct))!;
    }

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<SkillDto>>("api/profile/skills", ct);

    public async Task<IReadOnlyList<SkillDto>> SetClaimedSkillsAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default)
    {
        var response = await Client.PutAsJsonAsync("api/profile/skills", skillNames, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<SkillDto>>(ct))!;
    }

    public async Task<IReadOnlyList<string>> GetSkillSuggestionsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<string>>("api/profile/skill-suggestions", ct);

    public async Task<IReadOnlyList<SkillSuggestionDto>> GetSkillCatalogueAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<SkillSuggestionDto>>("api/profile/skill-catalogue", ct);

    public async Task<SkillPickerDto> GetSkillPickerAsync(CancellationToken ct = default) =>
        await Client.GetFromJsonAsync<SkillPickerDto>("api/profile/skill-picker", ct) ?? new SkillPickerDto([], null);

    public async Task<IReadOnlyList<CollegeDto>> SearchCollegesAsync(string query, string? state = null, CancellationToken ct = default)
    {
        var url = $"api/profile/colleges?q={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrWhiteSpace(state))
        {
            url += $"&state={Uri.EscapeDataString(state)}";
        }
        return await Client.GetJsonAsync<List<CollegeDto>>(url, ct);
    }

    public async Task CompleteOnboardingAsync(CancellationToken ct = default) =>
        (await Client.PostAsync("api/profile/complete-onboarding", null, ct)).EnsureSuccessStatusCode();

    public async Task<StudentProfileDto> SetAvatarAsync(string? dataUrl, CancellationToken ct = default)
    {
        var response = await Client.PutAsJsonAsync("api/profile/avatar", new UpdateAvatarRequest(dataUrl), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentProfileDto>(ct))!;
    }
}

public sealed class HttpHomeService(IHttpClientFactory factory) : IHomeService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<HomeSummaryDto> GetSummaryAsync(CancellationToken ct = default) =>
        (await Client.GetFromJsonAsync<HomeSummaryDto>("api/home", ct))!;
}

public sealed class HttpCareerService(IHttpClientFactory factory) : ICareerService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<CareerPathSummaryDto>> GetPathsAsync(bool all = false, CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<CareerPathSummaryDto>>(all ? "api/paths?all=true" : "api/paths", ct);

    public Task<CareerPathDto?> GetPathAsync(int id, CancellationToken ct = default) =>
        Client.GetJsonOrNullAsync<CareerPathDto>($"api/paths/{id}", ct);

    public async Task<CareerPathDto?> SetTargetAsync(int id, CancellationToken ct = default)
    {
        var response = await Client.PostAsync($"api/paths/{id}/target", null, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<CareerPathDto>(ct) : null;
    }
}

public sealed class HttpSkillGapService(IHttpClientFactory factory) : ISkillGapService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<SkillGapDto>> GetGapsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<SkillGapDto>>("api/skills/gaps", ct);

    public async Task<SkillCoverageDto> GetCoverageAsync(CancellationToken ct = default) =>
        (await Client.GetFromJsonAsync<SkillCoverageDto>("api/skills/coverage", ct))!;
}

public sealed class HttpRoadmapService(IHttpClientFactory factory) : IRoadmapService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<RoadmapDto> GetAsync(CancellationToken ct = default) =>
        (await Client.GetFromJsonAsync<RoadmapDto>("api/roadmap", ct))!;

    public Task<RoadmapDto> SetCheckpointAsync(int checkpointId, bool done, CancellationToken ct = default) =>
        Client.PostJsonAsync<RoadmapDto>($"api/roadmap/checkpoints/{checkpointId}/{done.ToString().ToLowerInvariant()}", null, ct);

    public Task<RoadmapDto> AddSkillAsync(int skillId, CancellationToken ct = default) =>
        Client.PostJsonAsync<RoadmapDto>($"api/roadmap/skills/{skillId}", null, ct);

    public Task<RoadmapDto> ChooseCourseAsync(int courseId, IReadOnlyList<int> skillIds, CancellationToken ct = default) =>
        Client.PostJsonAsync<RoadmapDto>($"api/roadmap/courses/{courseId}/choose", skillIds, ct);
}

public sealed class HttpOpportunityService(IHttpClientFactory factory) : IOpportunityService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<OpportunityDto>> GetAllAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<OpportunityDto>>("api/opportunities", ct);

    public Task<OpportunityDto?> GetAsync(int id, CancellationToken ct = default) =>
        Client.GetJsonOrNullAsync<OpportunityDto>($"api/opportunities/{id}", ct);

    public Task<OpportunityDto> ApplyAsync(int id, CancellationToken ct = default) =>
        Client.PostJsonAsync<OpportunityDto>($"api/opportunities/{id}/apply", null, ct);
}

public sealed class HttpResumeService(IHttpClientFactory factory) : IResumeService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<ResumeDto> GetAsync(CancellationToken ct = default) =>
        (await Client.GetFromJsonAsync<ResumeDto>("api/resume", ct))!;

    public Task<ResumeDto> SetSuggestionAcceptedAsync(int suggestionId, bool accepted, CancellationToken ct = default) =>
        Client.PostJsonAsync<ResumeDto>($"api/resume/suggestions/{suggestionId}/{accepted.ToString().ToLowerInvariant()}", null, ct);

    public Task<ResumeDto> TailorAsync(int opportunityId, CancellationToken ct = default) =>
        Client.PostJsonAsync<ResumeDto>($"api/resume/tailor/{opportunityId}", null, ct);
}

public sealed class HttpInterviewService(IHttpClientFactory factory) : IInterviewService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<InterviewSessionDto>> GetSessionsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<InterviewSessionDto>>("api/interview", ct);

    public Task<InterviewSessionDto> StartAsync(StartInterviewRequest request, CancellationToken ct = default) =>
        Client.PostJsonAsync<InterviewSessionDto>("api/interview", request, ct);

    public Task<InterviewSessionDto?> GetAsync(int id, CancellationToken ct = default) =>
        Client.GetJsonOrNullAsync<InterviewSessionDto>($"api/interview/{id}", ct);

    public Task<InterviewSessionDto> AnswerAsync(int id, AnswerInterviewRequest request, CancellationToken ct = default) =>
        Client.PostJsonAsync<InterviewSessionDto>($"api/interview/{id}/answer", request, ct);

    public Task<InterviewSessionDto> FinishAsync(int id, CancellationToken ct = default) =>
        Client.PostJsonAsync<InterviewSessionDto>($"api/interview/{id}/finish", null, ct);
}

public sealed class HttpGovtService(IHttpClientFactory factory) : IGovtService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<GovtExamDto>> GetExamsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<GovtExamDto>>("api/govt", ct);

    public Task<GovtExamDto?> GetExamAsync(int id, CancellationToken ct = default) =>
        Client.GetJsonOrNullAsync<GovtExamDto>($"api/govt/{id}", ct);
}

public sealed class HttpCommunityService(IHttpClientFactory factory) : ICommunityService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<CommunityPostDto>> GetFeedAsync(string? kind = null, CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<CommunityPostDto>>(
            string.IsNullOrWhiteSpace(kind) ? "api/community" : $"api/community?kind={Uri.EscapeDataString(kind)}", ct);

    public Task<CommunityPostDto?> GetPostAsync(int id, CancellationToken ct = default) =>
        Client.GetJsonOrNullAsync<CommunityPostDto>($"api/community/{id}", ct);

    public Task<CommunityPostDto> SetUpvoteAsync(int id, bool upvoted, CancellationToken ct = default) =>
        Client.PostJsonAsync<CommunityPostDto>($"api/community/{id}/upvote/{upvoted.ToString().ToLowerInvariant()}", null, ct);

    public async Task<IReadOnlyList<CommunityCommentDto>> GetCommentsAsync(int postId, CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<CommunityCommentDto>>($"api/community/{postId}/comments", ct);

    public async Task<IReadOnlyList<CommunityCommentDto>> AddCommentAsync(int postId, AddCommentRequest request, CancellationToken ct = default)
    {
        var response = await Client.PostAsJsonAsync($"api/community/{postId}/comments", request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // Surface the server's validation message the same way the in-process service does.
            throw new ArgumentException(await response.Content.ReadFromJsonAsync<string>(ct) ?? "Comment could not be posted.");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<CommunityCommentDto>>(ct))!;
    }

    public async Task<IReadOnlyList<CommunityCommentDto>> DeleteCommentAsync(int postId, int commentId, CancellationToken ct = default)
    {
        var response = await Client.DeleteAsync($"api/community/{postId}/comments/{commentId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<CommunityCommentDto>>(ct))!;
    }
}

public sealed class HttpQuizService(IHttpClientFactory factory) : IQuizService
{
    private HttpClient Client => factory.CreateClient(SaplingApi.Authenticated);

    public async Task<IReadOnlyList<QuizQuestionDto>> GetQuestionsAsync(CancellationToken ct = default) =>
        await Client.GetJsonAsync<List<QuizQuestionDto>>("api/quiz", ct);

    public Task<QuizResultDto> SubmitAsync(QuizSubmissionRequest request, CancellationToken ct = default) =>
        Client.PostJsonAsync<QuizResultDto>("api/quiz", request, ct);
}

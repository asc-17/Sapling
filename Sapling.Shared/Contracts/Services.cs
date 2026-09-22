namespace Sapling.Shared.Contracts;

public interface IProfileService
{
    Task<StudentProfileDto> GetAsync(CancellationToken ct = default);

    Task<StudentProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SkillDto>> SetClaimedSkillsAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetSkillSuggestionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SkillSuggestionDto>> GetSkillCatalogueAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CollegeDto>> SearchCollegesAsync(string query, string? state = null, CancellationToken ct = default);

    Task CompleteOnboardingAsync(CancellationToken ct = default);
}

public interface IScoreService
{
    Task<EmployabilityScoreDto> GetAsync(CancellationToken ct = default);

    Task<HomeSummaryDto> GetHomeSummaryAsync(CancellationToken ct = default);
}

public interface ICareerService
{
    Task<IReadOnlyList<CareerPathDto>> GetPathsAsync(CancellationToken ct = default);

    Task<CareerPathDto?> GetPathAsync(int id, CancellationToken ct = default);
}

public interface ISkillGapService
{
    Task<IReadOnlyList<SkillGapDto>> GetGapsAsync(CancellationToken ct = default);

    Task<SkillRadarDto> GetRadarAsync(CancellationToken ct = default);
}

public interface IRoadmapService
{
    Task<RoadmapDto> GetAsync(CancellationToken ct = default);

    Task<RoadmapDto> SetItemCompletedAsync(int itemId, bool completed, CancellationToken ct = default);

    Task<CourseDto?> GetCourseAsync(int id, CancellationToken ct = default);
}

public interface IOpportunityService
{
    Task<IReadOnlyList<OpportunityDto>> GetAllAsync(CancellationToken ct = default);

    Task<OpportunityDto?> GetAsync(int id, CancellationToken ct = default);

    Task<OpportunityDto> ApplyAsync(int id, CancellationToken ct = default);
}

public interface IResumeService
{
    Task<ResumeDto> GetAsync(CancellationToken ct = default);

    Task<ResumeDto> SetSuggestionAcceptedAsync(int suggestionId, bool accepted, CancellationToken ct = default);

    Task<ResumeDto> TailorAsync(int opportunityId, CancellationToken ct = default);
}

public interface IInterviewService
{
    Task<IReadOnlyList<InterviewSessionDto>> GetSessionsAsync(CancellationToken ct = default);

    Task<InterviewSessionDto> StartAsync(StartInterviewRequest request, CancellationToken ct = default);

    Task<InterviewSessionDto?> GetAsync(int id, CancellationToken ct = default);

    Task<InterviewSessionDto> AnswerAsync(int id, AnswerInterviewRequest request, CancellationToken ct = default);

    Task<InterviewSessionDto> FinishAsync(int id, CancellationToken ct = default);
}

public interface IGovtService
{
    Task<IReadOnlyList<GovtExamDto>> GetExamsAsync(CancellationToken ct = default);

    Task<GovtExamDto?> GetExamAsync(int id, CancellationToken ct = default);
}

public interface IQuizService
{
    Task<IReadOnlyList<QuizQuestionDto>> GetQuestionsAsync(CancellationToken ct = default);

    Task<QuizResultDto> SubmitAsync(QuizSubmissionRequest request, CancellationToken ct = default);
}

/// <summary>Institute-authored posts. Students can read, upvote and comment; only institutions publish.</summary>
public interface ICommunityService
{
    Task<IReadOnlyList<CommunityPostDto>> GetFeedAsync(string? kind = null, CancellationToken ct = default);

    Task<CommunityPostDto?> GetPostAsync(int id, CancellationToken ct = default);

    Task<CommunityPostDto> SetUpvoteAsync(int id, bool upvoted, CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommentDto>> GetCommentsAsync(int postId, CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommentDto>> AddCommentAsync(int postId, AddCommentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CommunityCommentDto>> DeleteCommentAsync(int postId, int commentId, CancellationToken ct = default);
}

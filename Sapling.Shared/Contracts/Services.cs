namespace Sapling.Shared.Contracts;

public interface IProfileService
{
    Task<StudentProfileDto> GetAsync(CancellationToken ct = default);

    Task<StudentProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SkillDto>> SetClaimedSkillsAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetSkillSuggestionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SkillSuggestionDto>> GetSkillCatalogueAsync(CancellationToken ct = default);

    Task<SkillPickerDto> GetSkillPickerAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CollegeDto>> SearchCollegesAsync(string query, string? state = null, CancellationToken ct = default);

    Task CompleteOnboardingAsync(CancellationToken ct = default);

    Task<StudentProfileDto> SetAvatarAsync(string? dataUrl, CancellationToken ct = default);
}

/// <summary>The home dashboard: greeting, target role, roadmap progress and what is new.</summary>
public interface IHomeService
{
    Task<HomeSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

public interface ICareerService
{
    /// <summary>Roles that the student's course leads to, or every role when <paramref name="all"/> is true.</summary>
    Task<IReadOnlyList<CareerPathSummaryDto>> GetPathsAsync(bool all = false, CancellationToken ct = default);

    Task<CareerPathDto?> GetPathAsync(int id, CancellationToken ct = default);

    Task<CareerPathDto?> SetTargetAsync(int id, CancellationToken ct = default);
}

public interface ISkillGapService
{
    Task<IReadOnlyList<SkillGapDto>> GetGapsAsync(CancellationToken ct = default);

    Task<SkillCoverageDto> GetCoverageAsync(CancellationToken ct = default);
}

public interface IRoadmapService
{
    Task<RoadmapDto> GetAsync(CancellationToken ct = default);

    /// <summary>Ticks or unticks a checkpoint. Finishing a direct course adds its skills to the profile.</summary>
    Task<RoadmapDto> SetCheckpointAsync(int checkpointId, bool done, CancellationToken ct = default);

    /// <summary>Follow this course for these skills instead of another provider's; only the followed course counts.</summary>
    Task<RoadmapDto> ChooseCourseAsync(int courseId, IReadOnlyList<int> skillIds, CancellationToken ct = default);

    /// <summary>The student says they can use a skill now, e.g. after a foundation course or learning it elsewhere.</summary>
    Task<RoadmapDto> AddSkillAsync(int skillId, CancellationToken ct = default);
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
    Task<IReadOnlyList<InterviewSummaryDto>> GetHistoryAsync(CancellationToken ct = default);

    Task<InterviewDto?> GetAsync(int id, CancellationToken ct = default);

    Task<InterviewDto> CreateAsync(CreateInterviewRequest request, CancellationToken ct = default);

    Task<InterviewDto> AttachResumeAsync(int id, string fileName, byte[] pdfBytes, CancellationToken ct = default);

    /// <summary>Continues without a resume, removing one already uploaded.</summary>
    Task<InterviewDto> SkipResumeAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Starts the interview, or resumes one that is already live: returns the pending interviewer line,
    /// or generates the next one when the last saved turn is an answer.
    /// </summary>
    Task<InterviewerLineDto> BeginAsync(int id, CancellationToken ct = default);

    Task<InterviewerLineDto> AnswerAsync(int id, SubmitAnswerRequest request, CancellationToken ct = default);

    Task<InterviewDto> FinishAsync(int id, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
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

/// <summary>A problem the student should see as written, such as an unreadable upload or the AI being out of credit.</summary>
public sealed class InterviewProblemException(string message, bool retryable = false) : Exception(message)
{
    public bool Retryable { get; } = retryable;
}

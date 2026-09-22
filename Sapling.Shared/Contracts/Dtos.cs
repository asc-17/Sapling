namespace Sapling.Shared.Contracts;

public sealed record StudentProfileDto(
    string FullName,
    string Email,
    string College,
    string Branch,
    int GraduationYear,
    string City,
    double Cgpa,
    int Backlogs,
    string PreferredLanguage,
    bool OnboardingComplete,
    string? RiasecCode,
    string State = "",
    string? AvatarDataUrl = null);

public sealed record UpdateAvatarRequest(string? AvatarDataUrl);

public sealed record UpdateProfileRequest(
    string FullName,
    string College,
    string Branch,
    int GraduationYear,
    string City,
    double Cgpa,
    int Backlogs,
    string PreferredLanguage,
    string State = "");

public sealed record CollegeDto(
    string Name,
    string State,
    string City,
    string? AicteId = null,
    string? Category = null);

public sealed record SkillDto(
    int Id,
    string Name,
    string Category,
    int Level,
    bool Verified,
    string Source);

public sealed record ScoreComponentDto(
    string Name,
    int Weight,
    int Value,
    string Summary);

public sealed record EmployabilityScoreDto(
    int Total,
    int PreviousTotal,
    DateOnly AsOf,
    IReadOnlyList<ScoreComponentDto> Components);

public sealed record CareerPathDto(
    int Id,
    string Title,
    string Family,
    string Tier,
    int FitScore,
    string EntrySalaryMp,
    string EntrySalaryMetro,
    string DemandTrend,
    string FiveYearOutlook,
    IReadOnlyList<string> Employers,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> CoreSkills,
    string? CounterCase);

public sealed record SkillGapDto(
    int Id,
    string SkillName,
    string Severity,
    int Impact,
    int Effort,
    int WeeksToClose,
    int CurrentLevel,
    int RequiredLevel,
    string Rationale);

public sealed record SkillRadarDto(
    IReadOnlyList<string> Axes,
    IReadOnlyList<double> You,
    IReadOnlyList<double> RoleTarget,
    string RoleName);

public sealed record CourseDto(
    int Id,
    string Title,
    string Provider,
    string Cost,
    bool IsFree,
    bool IsGovernmentSubsidised,
    int Hours,
    string Level,
    string Url,
    IReadOnlyList<string> TeachesSkills,
    string Summary);

public sealed record RoadmapItemDto(
    int Id,
    string Title,
    string Kind,
    string? Detail,
    int? CourseId,
    bool Completed,
    int EstimatedHours);

public sealed record RoadmapWeekDto(
    int WeekNumber,
    string Focus,
    DateOnly StartsOn,
    IReadOnlyList<RoadmapItemDto> Items);

public sealed record RoadmapDto(
    string Goal,
    int TotalWeeks,
    int CurrentWeek,
    int CompletedItems,
    int TotalItems,
    IReadOnlyList<RoadmapWeekDto> Weeks);

public sealed record OpportunityDto(
    int Id,
    string Title,
    string Company,
    string Kind,
    string Location,
    bool Remote,
    string Stipend,
    DateOnly ClosesOn,
    int MatchScore,
    string Readiness,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Reasons,
    string Description,
    string? ApplicationStatus);

public sealed record ResumeSuggestionDto(
    int Id,
    string Section,
    string Original,
    string Suggested,
    string Rationale,
    bool Accepted);

public sealed record ResumeDto(
    int AtsScore,
    int PreviousAtsScore,
    string? TailoredForRole,
    IReadOnlyList<string> ParseWarnings,
    IReadOnlyList<ResumeSuggestionDto> Suggestions,
    IReadOnlyList<string> Sections);

public sealed record InterviewTurnDto(
    int Id,
    string Role,
    string Text,
    DateTimeOffset At);

public sealed record InterviewRubricDto(
    string Name,
    int Score,
    string Comment);

public sealed record InterviewFeedbackDto(
    int Overall,
    IReadOnlyList<InterviewRubricDto> Rubrics,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Improvements);

public sealed record InterviewSessionDto(
    int Id,
    string TargetRole,
    string Kind,
    string Status,
    int QuestionsAsked,
    int QuestionLimit,
    DateTimeOffset StartedAt,
    IReadOnlyList<InterviewTurnDto> Turns,
    InterviewFeedbackDto? Feedback);

public sealed record StartInterviewRequest(string TargetRole, string Kind);

public sealed record AnswerInterviewRequest(string Answer);

public sealed record GovtExamDto(
    int Id,
    string Name,
    string Authority,
    string Level,
    DateOnly? NotificationOn,
    DateOnly? ExamOn,
    string Eligibility,
    IReadOnlyList<string> EligibilityReasons,
    IReadOnlyList<string> SyllabusAreas,
    string Summary);

public sealed record QuizQuestionDto(
    int Id,
    string Text,
    string Dimension);

public sealed record QuizSubmissionRequest(IReadOnlyList<QuizAnswerDto> Answers);

public sealed record QuizAnswerDto(int QuestionId, int Value);

public sealed record QuizResultDto(string RiasecCode, IReadOnlyList<ScoreComponentDto> Dimensions);

public sealed record HomeSummaryDto(
    string FirstName,
    bool OnboardingComplete,
    int Score,
    int CurrentWeek,
    int TotalWeeks,
    int CompletedItems,
    int TotalItems,
    string? NextTask,
    int NewOpportunities,
    int ClosingSoonExams,
    int NewCommunityPosts = 0);

public sealed record SkillSuggestionDto(
    string Name,
    string Category);

public static class PostKinds
{
    public const string Opportunity = "Opportunity";
    public const string Workshop = "Workshop";
    public const string Event = "Event";
    public const string Announcement = "Announcement";
}

public sealed record InstitutionDto(
    int Id,
    string Name,
    string ShortName,
    string City,
    bool Verified);

public sealed record CommunityPostDto(
    int Id,
    InstitutionDto Author,
    string Kind,
    string Title,
    string Body,
    DateTimeOffset PostedAt,
    DateTimeOffset? StartsAt,
    string? Venue,
    string? CtaLabel,
    string? CtaUrl,
    IReadOnlyList<string> Tags,
    int Upvotes,
    bool HasUpvoted,
    int CommentCount,
    string? ImageUrl = null);

public sealed record CommunityCommentDto(
    int Id,
    int? ParentId,
    string AuthorName,
    string? AuthorHeadline,
    bool IsInstitution,
    bool IsVerified,
    bool IsPostAuthor,
    bool IsMine,
    string Body,
    DateTimeOffset PostedAt,
    IReadOnlyList<CommunityCommentDto> Replies);

public sealed record AddCommentRequest(string Body, int? ParentId = null);

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
    string Category);

public sealed record ScoreComponentDto(
    string Name,
    int Weight,
    int Value,
    string Summary);

/// <summary>One card on the career paths list. The fit score is worked out for the signed-in student.</summary>
public sealed record CareerPathSummaryDto(
    int Id,
    string Title,
    string Family,
    string Tier,
    int FitScore,
    string? CourseRelevance,
    string TopReason,
    bool IsTarget,
    IReadOnlyList<string> Outlook);

public sealed record CareerPathDto(
    int Id,
    string OnetCode,
    string Title,
    string OnetTitle,
    string Family,
    string Tier,
    int FitScore,
    int Have,
    int Total,
    FitBreakdownDto Fit,
    string? CourseRelevance,
    bool IsTarget,
    string Description,
    IReadOnlyList<string> Tasks,
    IReadOnlyList<string> AlsoCalled,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> Outlook,
    string Preparation,
    string Education,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Considerations,
    IReadOnlyList<CareerRequirementDto> Requirements,
    IReadOnlyList<RelatedCareerDto> Related,
    CareerSourceDto Source);

/// <summary>Each part is 0 to 100. Interests is null until the quiz is taken; Course is null without a course.</summary>
public sealed record FitBreakdownDto(int Skills, int? Interests, int? Course);

public sealed record CareerRequirementDto(string Skill, string Kind, int Impact, bool Have, string Rationale);

public sealed record RelatedCareerDto(int Id, string Title);

public sealed record CareerSourceDto(
    string Name,
    string Publisher,
    string Url,
    string License,
    string LicenseUrl,
    string OccupationUrl,
    string ImportedOn,
    string OutlookSource,
    string CourseMapping);

/// <summary>One requirement of the target role. Have is true once the student has claimed the skill.</summary>
public sealed record SkillGapDto(
    int Id,
    string SkillName,
    string Kind,
    string Severity,
    int Impact,
    int Effort,
    int WeeksToClose,
    bool Have,
    string Rationale);

/// <summary>How much of the target role the student covers, weighted by O*NET importance.</summary>
public sealed record SkillCoverageDto(
    int RoleId,
    string RoleName,
    int Fit,
    int Have,
    int Total,
    bool TargetChosen);

/// <summary>
/// The student's roadmap to their target role: one step per required skill (or group of skills sharing an NPTEL
/// course), missing skills first and then by importance.
/// </summary>
public sealed record RoadmapDto(
    int RoleId,
    string RoleName,
    int CompletedCheckpoints,
    int TotalCheckpoints,
    IReadOnlyList<RoadmapStepDto> Steps,
    string Source);

/// <summary>
/// Impact is O*NET importance (1-10) for the role; Have is true once the student has every skill in the step.
/// Course is the one the student is following, picked from Options (one per provider that teaches it).
/// </summary>
public sealed record RoadmapStepDto(
    IReadOnlyList<RoadmapSkillDto> Skills,
    string Severity,
    int Impact,
    bool Have,
    RoadmapCourseDto Course,
    IReadOnlyList<RoadmapCourseDto> Options);

public sealed record RoadmapSkillDto(int Id, string Name, bool Have);

/// <summary>
/// Provider is "NPTEL" or "Microsoft Learn". Match is "direct" (the course teaches the skill) or "foundation"
/// (the subject underneath a tool). Lessons are NPTEL lectures or Microsoft Learn modules.
/// </summary>
public sealed record RoadmapCourseDto(
    int Id,
    string Provider,
    string Title,
    string Byline,
    string Url,
    int Lessons,
    int Minutes,
    string Match,
    IReadOnlyList<RoadmapCheckpointDto> Checkpoints);

public sealed record RoadmapCheckpointDto(int Id, string Title, int Lessons, int Minutes, bool Done);

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
    int? TargetRoleId,
    string? TargetRole,
    int? TargetFit,
    int CompletedCheckpoints,
    int TotalCheckpoints,
    string? NextCheckpoint,
    int NewOpportunities,
    int ClosingSoonExams,
    int NewCommunityPosts = 0);

public sealed record SkillSuggestionDto(
    string Name,
    string Category);

/// <summary>
/// Skills to suggest at onboarding, most common first across the roles the student's course leads to.
/// Course is the label the suggestions are for, or null when the profile has no course yet.
/// </summary>
public sealed record SkillPickerDto(IReadOnlyList<string> Recommended, string? Course);

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

using Microsoft.AspNetCore.Identity;

namespace Sapling.Web.Data;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";

    public StudentProfile? Profile { get; set; }
}

public class StudentProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public AppUser? User { get; set; }

    public string College { get; set; } = "";

    public string Branch { get; set; } = "";

    public int GraduationYear { get; set; }

    public string City { get; set; } = "";

    public double Cgpa { get; set; }

    public int Backlogs { get; set; }

    public string PreferredLanguage { get; set; } = "English";

    public bool OnboardingComplete { get; set; }

    public string? RiasecCode { get; set; }

    public int TargetRoleId { get; set; }

    public List<StudentSkill> Skills { get; set; } = [];

    public List<ScoreSnapshot> Scores { get; set; } = [];

    public List<RoadmapItem> RoadmapItems { get; set; } = [];

    public List<OpportunityApplication> Applications { get; set; } = [];

    public List<ResumeSuggestion> ResumeSuggestions { get; set; } = [];

    public List<InterviewSession> InterviewSessions { get; set; } = [];

    public int AtsScore { get; set; }

    public int PreviousAtsScore { get; set; }

    public string? ResumeTailoredForRole { get; set; }
}

public class Skill
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Category { get; set; } = "";
}

public class StudentSkill
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public int SkillId { get; set; }

    public Skill? Skill { get; set; }

    public int Level { get; set; }

    public bool Verified { get; set; }

    public string Source { get; set; } = "Self-claimed";
}

public class CareerRole
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Family { get; set; } = "";

    public string Tier { get; set; } = "Safe";

    public int FitScore { get; set; }

    public string EntrySalaryMp { get; set; } = "";

    public string EntrySalaryMetro { get; set; } = "";

    public string DemandTrend { get; set; } = "";

    public string FiveYearOutlook { get; set; } = "";

    public string Employers { get; set; } = "";

    public string Reasons { get; set; } = "";

    public string CoreSkills { get; set; } = "";

    public string? CounterCase { get; set; }

    public List<RoleSkillRequirement> Requirements { get; set; } = [];
}

public class RoleSkillRequirement
{
    public int Id { get; set; }

    public int CareerRoleId { get; set; }

    public int SkillId { get; set; }

    public Skill? Skill { get; set; }

    public int RequiredLevel { get; set; }

    public int Impact { get; set; }

    public int Effort { get; set; }

    public int WeeksToClose { get; set; }

    public string Rationale { get; set; } = "";
}

public class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Provider { get; set; } = "";

    public string Cost { get; set; } = "Free";

    public bool IsFree { get; set; }

    public bool IsGovernmentSubsidised { get; set; }

    public int Hours { get; set; }

    public string Level { get; set; } = "Beginner";

    public string Url { get; set; } = "";

    public string TeachesSkills { get; set; } = "";

    public string Summary { get; set; } = "";
}

public class RoadmapItem
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public int WeekNumber { get; set; }

    public string WeekFocus { get; set; } = "";

    public string Title { get; set; } = "";

    public string Kind { get; set; } = "Course";

    public string? Detail { get; set; }

    public int? CourseId { get; set; }

    public Course? Course { get; set; }

    public int EstimatedHours { get; set; }

    public bool Completed { get; set; }

    public int Order { get; set; }
}

public class Opportunity
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Company { get; set; } = "";

    public string Kind { get; set; } = "Internship";

    public string Location { get; set; } = "";

    public bool Remote { get; set; }

    public string Stipend { get; set; } = "";

    public DateOnly ClosesOn { get; set; }

    public int MatchScore { get; set; }

    public string MissingSkills { get; set; } = "";

    public string Reasons { get; set; } = "";

    public string Description { get; set; } = "";
}

public class OpportunityApplication
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public int OpportunityId { get; set; }

    public Opportunity? Opportunity { get; set; }

    public string Status { get; set; } = "Applied";

    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ResumeSuggestion
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public string Section { get; set; } = "";

    public string Original { get; set; } = "";

    public string Suggested { get; set; } = "";

    public string Rationale { get; set; } = "";

    public bool Accepted { get; set; }

    public int Order { get; set; }
}

public class InterviewSession
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public string TargetRole { get; set; } = "";

    public string Kind { get; set; } = "Technical";

    public string Status { get; set; } = "Active";

    public int QuestionLimit { get; set; } = 5;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<InterviewTurn> Turns { get; set; } = [];

    public int? OverallScore { get; set; }

    public string? Rubrics { get; set; }

    public string? Strengths { get; set; }

    public string? Improvements { get; set; }
}

public class InterviewTurn
{
    public int Id { get; set; }

    public int InterviewSessionId { get; set; }

    public string Role { get; set; } = "Interviewer";

    public string Text { get; set; } = "";

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}

public class GovtExam
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Authority { get; set; } = "";

    public string Level { get; set; } = "State";

    public DateOnly? NotificationOn { get; set; }

    public DateOnly? ExamOn { get; set; }

    public int MinAge { get; set; }

    public int MaxAge { get; set; }

    public string QualificationRequired { get; set; } = "";

    public bool RequiresMpDomicile { get; set; }

    public double MinCgpa { get; set; }

    public string SyllabusAreas { get; set; } = "";

    public string Summary { get; set; } = "";
}

public class ScoreSnapshot
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }

    public DateOnly AsOf { get; set; }

    public int Total { get; set; }

    public int Academic { get; set; }

    public int TechnicalSkills { get; set; }

    public int Projects { get; set; }

    public int Communication { get; set; }

    public int Certifications { get; set; }

    public int Exposure { get; set; }
}

public class QuizQuestion
{
    public int Id { get; set; }

    public string Text { get; set; } = "";

    public string Dimension { get; set; } = "";

    public int Order { get; set; }
}

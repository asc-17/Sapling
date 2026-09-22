namespace Sapling.Shared.Content;

public sealed record Feature(
    string Key,
    string Title,
    string Blurb,
    string LongBlurb,
    string Icon,
    string Route);

/// <summary>Single source for the module list used by home cards, the nav, and the sign-in showcase.</summary>
public static class FeatureCatalog
{
    public const string CareerPaths = "career-paths";
    public const string SkillGap = "skill-gap";
    public const string Roadmap = "roadmap";
    public const string Opportunities = "opportunities";
    public const string Resume = "resume";
    public const string Interview = "interview";
    public const string GovtTrack = "govt-track";
    public const string Community = "community";
    public const string Score = "score";
    public const string Profile = "profile";

    public static readonly IReadOnlyList<Feature> All =
    [
        new(CareerPaths, "Career paths", "Roles that fit you, and why.",
            "Ranked career options with a fit score, salary bands for Madhya Pradesh and metros, demand trend, and a plain-language reason for every recommendation.",
            "compass", "/paths"),

        new(SkillGap, "Skills & gaps", "What you have vs what roles need.",
            "Your verified skills measured against live job descriptions, with each gap ranked by impact against effort and an honest time-to-close estimate.",
            "radar", "/skills"),

        new(Roadmap, "Roadmap", "A week-by-week plan you can follow.",
            "A time-phased plan built around your semester, putting free and government-subsidised courses first, with milestones and project deliverables.",
            "route", "/roadmap"),

        new(Opportunities, "Opportunities", "Internships and jobs matched to you.",
            "Campus drives, internships and jobs ranked by two-way fit. Nothing is hidden from you: roles you are not ready for show exactly what is missing.",
            "briefcase", "/opportunities"),

        new(Resume, "Resume", "Beat the ATS, then beat the reader.",
            "An ATS compatibility score with parse diagnostics, line-level rewrite suggestions you accept or reject, and a variant tailored to any job description.",
            "file-text", "/resume"),

        new(Interview, "Mock interview", "Practise before it counts.",
            "Role-specific technical, HR and aptitude interviews that probe with follow-up questions and score you on content, structure and communication.",
            "mic", "/interview"),

        new(GovtTrack, "Government track", "Exams you are actually eligible for.",
            "MPPSC, MPESB, SSC, Railways and Banking notifications with an eligibility engine that tells you eligible, eligible next year, or not eligible and why.",
            "landmark", "/govt"),

        new(Community, "Community", "Your college's private feed.",
            "Workshops, events, openings and announcements posted by your own college. Only its students can see them, and only the college can post.",
            "users", "/community"),

        new(Score, "Employability Score", "One number that actually moves.",
            "A 0 to 100 score built from academics, verified skills, projects, communication, certifications and exposure, with the full breakdown always visible.",
            "gauge", "/score"),
    ];

    public static readonly IReadOnlyList<Feature> HomeCards =
        All.Where(f => f.Key != Score).ToList();

    public static Feature? Find(string key) => All.FirstOrDefault(f => f.Key == key);
}

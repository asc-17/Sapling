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
    public const string Roadmap = "roadmap";
    public const string Opportunities = "opportunities";
    public const string Resume = "resume";
    public const string Interview = "interview";
    public const string GovtTrack = "govt-track";
    public const string Community = "community";
    public const string Profile = "profile";

    public static readonly IReadOnlyList<Feature> All =
    [
        new(CareerPaths, "Career & skills", "Pick a role and see what it takes.",
            "Every role your course leads to, ranked by fit. Choose one and see how well you fit it, the work it involves, and exactly which skills you have and which are missing.",
            "compass", "/career"),


        new(Roadmap, "Roadmap", "NPTEL courses for what you're missing.",
            "Free NPTEL courses from the IITs for each skill your target role asks for and you don't have yet, most important first, with checkpoints to tick off as you study.",
            "route", "/roadmap"),

        new(Opportunities, "Opportunities", "Internships and jobs matched to you.",
            "Campus drives, internships and jobs ranked by two-way fit. Nothing is hidden from you: roles you are not ready for show exactly what is missing.",
            "briefcase", "/opportunities"),

        new(Resume, "Resume", "Beat the ATS, then beat the reader.",
            "An ATS compatibility score with parse diagnostics, line-level rewrite suggestions you accept or reject, and a variant tailored to any job description.",
            "file-text", "/resume"),

        new(Interview, "Mock interview", "Practise before it counts.",
            "A spoken interview for your target role with an AI interviewer who asks about your resume and follows up, then a detailed report on your answers, pauses and confidence.",
            "mic", "/interview"),

        new(GovtTrack, "Government track", "Exams you are actually eligible for.",
            "MPPSC, MPESB, SSC, Railways and Banking notifications with an eligibility engine that tells you eligible, eligible next year, or not eligible and why.",
            "landmark", "/govt"),

        new(Community, "Community", "Your college's private feed.",
            "Workshops, events, openings and announcements posted by your own college. Only its students can see them, and only the college can post.",
            "users", "/community"),

    ];

    public static readonly IReadOnlyList<Feature> HomeCards =
        All.ToList();

    public static Feature? Find(string key) => All.FirstOrDefault(f => f.Key == key);
}

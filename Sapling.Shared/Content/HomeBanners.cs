namespace Sapling.Shared.Content;

public sealed record Banner(
    string Title,
    string Subtitle,
    string Image,
    string Route,
    string Cta);

public static class HomeBanners
{
    private const string Dir = "_content/Sapling.Shared/img/banners/";

    public static readonly IReadOnlyList<Banner> All =
    [
        new("See the roles that fit you",
            "Ranked by your skills, interests and course, with the reasoning shown.",
            Dir + "score.svg", "/paths", "See career paths"),

        new("MPESB notification is out",
            "Check your eligibility before the window closes.",
            Dir + "govt.svg", "/govt", "Check eligibility"),

        new("Free IIT courses for your gaps",
            "NPTEL courses matched to what your target role needs, with checkpoints to track.",
            Dir + "courses.svg", "/roadmap", "Open roadmap"),

        new("What your college is running this month",
            "Workshops, events and openings posted by your college. Ask questions in the comments.",
            Dir + "community.svg", "/community", "Open community"),

        new("Practise a mock interview",
            "Speak with an AI interviewer for fifteen minutes, then see where you got stuck.",
            Dir + "interview.svg", "/interview", "Start an interview"),

        new("Build an ATS-ready resume",
            "Upload yours for a score and fixes, or build one from scratch and edit it live.",
            Dir + "resume.svg", "/resume", "Open resume builder"),
    ];
}

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
        new("Your score is a starting line",
            "See the six things that make it up, and which one moves fastest.",
            Dir + "score.svg", "/score", "View breakdown"),

        new("MPESB notification is out",
            "Check your eligibility before the window closes.",
            Dir + "govt.svg", "/govt", "Check eligibility"),

        new("Free NPTEL and SWAYAM courses",
            "Government-subsidised first. Close a gap without spending anything.",
            Dir + "courses.svg", "/roadmap", "Open roadmap"),

        new("Practise a mock interview",
            "Adaptive follow-ups and a scored rubric, in under fifteen minutes.",
            Dir + "interview.svg", "/interview", "Start a session"),

        new("Tailor your resume to a role",
            "One click regenerates your resume for the job description you pick.",
            Dir + "resume.svg", "/resume", "Tailor now"),
    ];
}

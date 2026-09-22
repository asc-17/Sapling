namespace Sapling.Shared.Content;

public sealed record NavEntry(string Label, string Route, string Icon, bool ExactMatch = false);

public static class Navigation
{
    public static readonly IReadOnlyList<NavEntry> Primary =
    [
        new("Home", "/home", "home", true),
        new("Career paths", "/paths", "compass"),
        new("Skills & gaps", "/skills", "radar"),
        new("Roadmap", "/roadmap", "route"),
        new("Opportunities", "/opportunities", "briefcase"),
        new("Community", "/community", "users"),
        new("Resume", "/resume", "file-text"),
        new("Mock interview", "/interview", "mic"),
        new("Government", "/govt", "landmark"),
    ];

    public static readonly IReadOnlyList<NavEntry> Tabs =
    [
        new("Home", "/home", "home", true),
        new("Roadmap", "/roadmap", "route"),
        new("Jobs", "/opportunities", "briefcase"),
        new("Interview", "/interview", "mic"),
    ];

    public static readonly IReadOnlyList<NavEntry> MoreSheet =
    [
        new("Employability Score", "/score", "gauge"),
        new("Community", "/community", "users"),
        new("Career paths", "/paths", "compass"),
        new("Skills & gaps", "/skills", "radar"),
        new("Resume", "/resume", "file-text"),
        new("Government track", "/govt", "landmark"),
        new("Profile & settings", "/profile", "user"),
    ];
}

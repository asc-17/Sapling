namespace Sapling.Shared.Content;

public sealed record NavEntry(string Label, string Route, string Icon, bool ExactMatch = false);

public static class Navigation
{
    public static readonly IReadOnlyList<NavEntry> Primary =
    [
        new("Home", "/home", "home", true),
        new("Career & skills", "/career", "compass"),
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
        new("Community", "/community", "users"),
        new("Career & skills", "/career", "compass"),
        new("Resume", "/resume", "file-text"),
        new("Government track", "/govt", "landmark"),
        new("Profile & settings", "/profile", "user"),
    ];
}

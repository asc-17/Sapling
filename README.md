# Sapling

A career-readiness app for college students in Madhya Pradesh. It gives each student an Employability Score, suggests career paths that fit them, shows their skill gaps, and builds a week-by-week plan to close those gaps.

Sapling runs as a Blazor Server web app and as a .NET MAUI app for Android and Windows. Both share the same pages and components.

## Features

- **Onboarding.** A short setup covering the student's details (college search with state and city filled in automatically, course, branch, graduation year, CGPA, backlogs), their skills, and an interest quiz.
- **Employability Score.** A score from 0 to 100 built from academics, verified skills, projects, communication, certifications and exposure. The full breakdown is always visible.
- **Career paths.** Ranked roles, each with a fit score, salary bands for Madhya Pradesh and for metro cities, the demand trend, and a plain-language reason for the recommendation.
- **Skills and gaps.** The student's skills compared with what their target roles need. Each gap is ranked by impact against effort and comes with an estimate of how long it takes to close.
- **Roadmap.** A week-by-week plan with milestones. Free and government-subsidised courses come first, and each course has its own detail page.
- **Opportunities.** Internships, jobs and campus drives ranked by fit. Roles the student isn't ready for still appear, with a list of what's missing.
- **Resume.** An ATS compatibility score, line-by-line rewrite suggestions the student accepts or rejects, and export.
- **Mock interview.** Technical, HR and aptitude interview sessions with follow-up questions and scored feedback.
- **Government track.** MPPSC, MPESB, SSC, Railways and Banking exams, each with an eligibility check.
- **Community.** A private feed for each college, where it posts workshops, events, opportunities and announcements. Students of that college can upvote posts and comment in threads, while only the college itself can post. Posts carry a verified badge.
- **Home dashboard.** Banners, recently visited features, roadmap progress and the latest community posts.
- **Profile.** Editable details and a profile picture.
- **Accounts.**
  - Sign-up with email verification: the student enters an email, types in a 6-digit code sent to it, then chooses a password.
  - Forgot password, with the same code check.
  - Google sign-in.
  - A demo account with sample data.
- **Themes.** Light and dark.

## Projects

| Project | What it contains |
|---|---|
| `Sapling.Shared` | Razor pages, components, styles and service contracts used by both apps |
| `Sapling.Web` | Blazor Server host: EF Core with SQLite, ASP.NET Identity, the HTTP API, the AI client, and demo data seeding |
| `Sapling` | .NET MAUI Blazor Hybrid app for Android and Windows, which talks to `Sapling.Web` over HTTP |

## Running it

You need the .NET 10 SDK, plus the MAUI workload if you want the mobile app.

```bash
dotnet run --project Sapling.Web/Sapling.Web.csproj
```

The web app runs at `http://localhost:5250`. On first start it creates `sapling.db` and seeds demo data. To try it straight away, sign in with **demo@sapling.app** / **Sapling@2026**.

To run the mobile app, start `Sapling.Web` first, then deploy the `Sapling` project from Visual Studio.
- **Android emulator:** reaches the server at `10.0.2.2`, with no changes needed.
- **Physical phone:** set `DevMachineHost` in `Sapling/Services/HttpServices.cs` to your PC's Wi-Fi IP address, and allow inbound TCP port 5250 through Windows Firewall.

## Configuration

Keep secrets in user secrets, not in `appsettings.json`:

```bash
dotnet user-secrets set "<key>" "<value>" --project Sapling.Web
```

| Key | Used for | If not set |
|---|---|---|
| `Ai:Anthropic:ApiKey` (plus optional `Ai:Anthropic:Model`) | AI replies from Claude | A scripted offline responder is used |
| `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` | Google sign-in | The Google button shows "Not set up" |
| `Email:Azure:ConnectionString`, `Email:Azure:SenderAddress` | Verification emails through Azure Communication Services | In development, codes are written to the server log |

For Google sign-in, add `http://localhost:5250/signin-google` as an authorized redirect URI on the Google OAuth client. Google rejects private IP addresses, so to use Google sign-in from a phone during development, forward the port over USB with `adb reverse tcp:5250 tcp:5250` and point the app at `localhost`.

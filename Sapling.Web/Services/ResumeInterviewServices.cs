using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Sapling.Shared.Contracts;
using Sapling.Web.Ai;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

public sealed class ResumeService(SaplingDbContext db, StudentContext ctx) : IResumeService
{
    private static readonly string[] Warnings =
    [
        "Two-column layout: four fields were not extracted in the correct order.",
        "Skills listed inside a graphic, so the parser read none of them.",
        "Dates written as \"Aug'23 – Jun'24\" were not recognised as a date range.",
        "Contact number embedded in a header, which several parsers skip.",
    ];

    private static readonly string[] Sections =
        ["Summary", "Education", "Experience", "Projects", "Skills", "Certifications"];

    public async Task<ResumeDto> GetAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        return await BuildAsync(profile, ct);
    }

    public async Task<ResumeDto> SetSuggestionAcceptedAsync(int suggestionId, bool accepted, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var suggestion = await db.ResumeSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.StudentProfileId == profile.Id, ct);

        if (suggestion is not null)
        {
            suggestion.Accepted = accepted;
            await db.SaveChangesAsync(ct);
        }

        return await BuildAsync(profile, ct);
    }

    public async Task<ResumeDto> TailorAsync(int opportunityId, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var opportunity = await db.Opportunities.FirstOrDefaultAsync(o => o.Id == opportunityId, ct);
        profile.ResumeTailoredForRole = opportunity is null ? null : $"{opportunity.Title} at {opportunity.Company}";
        await db.SaveChangesAsync(ct);
        return await BuildAsync(profile, ct);
    }

    private async Task<ResumeDto> BuildAsync(StudentProfile profile, CancellationToken ct)
    {
        var suggestions = await db.ResumeSuggestions
            .Where(s => s.StudentProfileId == profile.Id)
            .OrderBy(s => s.Order)
            .ToListAsync(ct);

        // Each accepted rewrite fixes a concrete parse or specificity problem, so the ATS score moves with it.
        var accepted = suggestions.Count(s => s.Accepted);
        var score = suggestions.Count == 0
            ? profile.AtsScore
            : profile.PreviousAtsScore + (int)Math.Round(27.0 * accepted / suggestions.Count);

        var warnings = Warnings.Skip(accepted).ToList();

        return new ResumeDto(
            score,
            profile.PreviousAtsScore,
            profile.ResumeTailoredForRole,
            warnings,
            suggestions.Select(s => new ResumeSuggestionDto(s.Id, s.Section, s.Original, s.Suggested, s.Rationale, s.Accepted)).ToList(),
            Sections);
    }
}

public sealed class InterviewService(
    SaplingDbContext db,
    StudentContext ctx,
    ScoreService scores,
    IChatClient chat) : IInterviewService
{
    private static readonly Dictionary<string, string[]> Openers = new()
    {
        ["Technical"] =
        [
            "Take me through how you would design the database for a library system that three departments share.",
            "You have a query that takes forty seconds. Walk me through how you find out why.",
            "Explain the difference between an interface and an abstract class, and when you would reach for each.",
            "Describe a bug you could not reproduce. What did you do?",
            "How would you expose this as a REST API, and what would you get wrong the first time?",
        ],
        ["HR"] =
        [
            "Tell me about yourself in under ninety seconds.",
            "Why this role, and why now?",
            "Describe a time you disagreed with a teammate. What happened?",
            "What is the hardest feedback you have received?",
            "Where do you want to be in three years, honestly?",
        ],
        ["Aptitude"] =
        [
            "A train covers 180 km in 3 hours. If it speeds up by 25%, how long does the same journey take?",
            "Two pipes fill a tank in 12 and 18 minutes. How long do they take together?",
            "If the code for CAT is DBU, what is the code for DOG?",
            "A shopkeeper marks up by 40% then discounts by 25%. What is the net profit percentage?",
            "In a class of 60, 35 play cricket and 25 play football, 10 play both. How many play neither?",
        ],
        ["Case"] =
        [
            "A college wants to raise placement rates by 15% in one year. Where do you start?",
            "Your team's feature shipped and usage dropped. How do you find out why?",
            "Estimate how many people in Indore need a resume rewritten this month.",
            "You have one week and two engineers. What do you cut?",
            "A client wants a feature you think is wrong. How do you handle it?",
        ],
    };

    public async Task<IReadOnlyList<InterviewSessionDto>> GetSessionsAsync(CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var sessions = await db.InterviewSessions
            .Include(s => s.Turns)
            .Where(s => s.StudentProfileId == profile.Id)
            .OrderByDescending(s => s.Id)
            .ToListAsync(ct);

        return sessions.Select(Map).ToList();
    }

    public async Task<InterviewSessionDto> StartAsync(StartInterviewRequest request, CancellationToken ct = default)
    {
        var profile = await ctx.GetProfileAsync(ct);
        var kind = Openers.ContainsKey(request.Kind) ? request.Kind : "Technical";

        var session = new InterviewSession
        {
            StudentProfileId = profile.Id,
            TargetRole = request.TargetRole,
            Kind = kind,
        };
        session.Turns.Add(new InterviewTurn { Role = "Interviewer", Text = Openers[kind][0] });

        db.InterviewSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<InterviewSessionDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct);
        return session is null ? null : Map(session);
    }

    public async Task<InterviewSessionDto> AnswerAsync(int id, AnswerInterviewRequest request, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct) ?? throw new InvalidOperationException("Session not found.");
        if (session.Status != "Active")
        {
            return Map(session);
        }

        session.Turns.Add(new InterviewTurn { Role = "Candidate", Text = request.Answer });

        var asked = session.Turns.Count(t => t.Role == "Interviewer");
        if (asked >= session.QuestionLimit)
        {
            await db.SaveChangesAsync(ct);
            return await FinishAsync(id, ct);
        }

        var next = await NextQuestionAsync(session, ct);
        session.Turns.Add(new InterviewTurn { Role = "Interviewer", Text = next });
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<InterviewSessionDto> FinishAsync(int id, CancellationToken ct = default)
    {
        var session = await LoadAsync(id, ct) ?? throw new InvalidOperationException("Session not found.");
        if (session.Status == "Complete")
        {
            return Map(session);
        }

        var answers = session.Turns.Where(t => t.Role == "Candidate").ToList();
        var rubrics = Grade(answers);

        session.Status = "Complete";
        session.OverallScore = rubrics.Count == 0 ? 0 : (int)Math.Round(rubrics.Average(r => r.Score));
        session.Rubrics = string.Join('|', rubrics.Select(r => $"{r.Name}~{r.Score}~{r.Comment}"));
        session.Strengths = string.Join('|', Strengths(answers));
        session.Improvements = (await CoachAsync(session, ct)).Replace('|', ' ');

        await db.SaveChangesAsync(ct);

        var profile = await ctx.GetProfileAsync(ct);
        await scores.RecomputeAsync(profile, ct);

        return Map(session);
    }

    private async Task<InterviewSession?> LoadAsync(int id, CancellationToken ct)
    {
        var profile = await ctx.GetProfileAsync(ct);
        return await db.InterviewSessions
            .Include(s => s.Turns)
            .FirstOrDefaultAsync(s => s.Id == id && s.StudentProfileId == profile.Id, ct);
    }

    private async Task<string> NextQuestionAsync(InterviewSession session, CancellationToken ct)
    {
        var asked = session.Turns.Count(t => t.Role == "Interviewer");
        var bank = Openers[session.Kind];

        try
        {
            var profile = await ctx.GetProfileAsync(ct);
            var facts = $"Branch: {profile.Branch}; CGPA: {profile.Cgpa}; verified skills: "
                      + string.Join(", ", profile.Skills.Where(s => s.Verified).Select(s => s.Skill?.Name));
            var transcript = string.Join('\n', session.Turns.Select(t => $"{t.Role}: {t.Text}"));

            var response = await chat.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, Prompts.System),
                    new ChatMessage(ChatRole.User, Prompts.InterviewQuestion(session.TargetRole, session.Kind, facts, transcript)),
                ],
                cancellationToken: ct);

            var text = response.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }
        catch (Exception)
        {
            // Fall through to the question bank so a model outage never blocks a session.
        }

        return bank[asked % bank.Length];
    }

    private async Task<string> CoachAsync(InterviewSession session, CancellationToken ct)
    {
        try
        {
            var transcript = string.Join('\n', session.Turns.Select(t => $"{t.Role}: {t.Text}"));
            var response = await chat.GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, Prompts.System),
                    new ChatMessage(ChatRole.User, Prompts.InterviewFeedback(session.TargetRole, transcript)),
                ],
                cancellationToken: ct);

            var text = response.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }
        catch (Exception)
        {
            // Scripted guidance below.
        }

        return "Lead with one sentence of context, then the decision you made, then the measurable outcome.";
    }

    private static readonly string[] Fillers = ["basically", "actually", "like", "you know", "so yeah", "umm", "uh"];

    /// <summary>Deterministic transcript measurements so the rubric is reproducible and defensible.</summary>
    private static List<InterviewRubricDto> Grade(List<InterviewTurn> answers)
    {
        if (answers.Count == 0)
        {
            return [];
        }

        var words = answers.Sum(a => a.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        var average = words / (double)answers.Count;
        var lower = string.Join(' ', answers.Select(a => a.Text)).ToLowerInvariant();
        var fillerCount = Fillers.Sum(f => CountOccurrences(lower, f));
        var hasNumbers = answers.Count(a => a.Text.Any(char.IsDigit));
        var structured = answers.Count(a =>
            a.Text.Contains(" because ", StringComparison.OrdinalIgnoreCase) ||
            a.Text.Contains(" so ", StringComparison.OrdinalIgnoreCase) ||
            a.Text.Contains(" then ", StringComparison.OrdinalIgnoreCase));

        var content = Clamp(35 + (int)(average * 0.9) + (hasNumbers * 8));
        var structure = Clamp(30 + (int)(100.0 * structured / answers.Count * 0.5) + (average > 40 ? 12 : 0));
        var communication = Clamp(85 - (fillerCount * 6));
        var depth = Clamp(30 + (hasNumbers * 12) + (int)(average * 0.6));

        return
        [
            new("Content correctness", content, content >= 70 ? "Answers were on topic and specific." : "Answers stayed general; name the technology and the decision."),
            new("Structure (STAR)", structure, structure >= 70 ? "Clear situation, action and result." : "Say the situation first, then what you did, then the outcome."),
            new("Communication", communication, fillerCount == 0 ? "Clean delivery with no filler words." : $"{fillerCount} filler words detected. Pause instead of filling silence."),
            new("Evidence & depth", depth, hasNumbers > 0 ? "You backed claims with numbers." : "Add one number to every claim: users, time saved, percentage."),
        ];
    }

    private static List<string> Strengths(List<InterviewTurn> answers)
    {
        var list = new List<string>();
        if (answers.Any(a => a.Text.Any(char.IsDigit)))
        {
            list.Add("You quantified at least one claim, which most candidates never do.");
        }

        if (answers.Count > 0 && answers.Average(a => a.Text.Length) > 180)
        {
            list.Add("You gave answers with enough substance to probe.");
        }

        if (list.Count == 0)
        {
            list.Add("You completed a full session. Attempt two is where the score usually moves.");
        }

        return list;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static int Clamp(int value) => Math.Clamp(value, 10, 96);

    private static InterviewSessionDto Map(InterviewSession s)
    {
        InterviewFeedbackDto? feedback = null;
        if (s.Status == "Complete")
        {
            var rubrics = (s.Rubrics ?? "")
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('~'))
                .Where(parts => parts.Length == 3)
                .Select(parts => new InterviewRubricDto(parts[0], int.Parse(parts[1]), parts[2]))
                .ToList();

            feedback = new InterviewFeedbackDto(
                s.OverallScore ?? 0,
                rubrics,
                CareerService.Split(s.Strengths ?? ""),
                string.IsNullOrWhiteSpace(s.Improvements) ? [] : [s.Improvements]);
        }

        return new InterviewSessionDto(
            s.Id, s.TargetRole, s.Kind, s.Status,
            s.Turns.Count(t => t.Role == "Interviewer"),
            s.QuestionLimit,
            s.StartedAt,
            s.Turns.OrderBy(t => t.Id).Select(t => new InterviewTurnDto(t.Id, t.Role, t.Text, t.At)).ToList(),
            feedback);
    }
}

namespace Sapling.Web.Ai;

/// <summary>
/// Prompt templates. The model phrases facts; it never invents them. Anything the model states must
/// be traceable to the profile block passed in, which is the anti-fabrication contract in the PRD.
/// </summary>
public static class Prompts
{
    public const string InterviewQuestionTag = "[sapling:interview-question]";
    public const string InterviewFeedbackTag = "[sapling:interview-feedback]";
    public const string ExplainTag = "[sapling:explain]";
    public const string ResumeRewriteTag = "[sapling:resume-rewrite]";

    public const string System = """
        You are Sapling, a career readiness coach for students in Madhya Pradesh, India.
        Rules you must not break:
        1. Use only the facts given to you. Never invent a project, a metric, an employer or a certificate.
        2. If a fact is missing, say so plainly instead of filling the gap.
        3. Write in clear, direct English at roughly a class-10 reading level. No motivational filler.
        4. Be honest about trade-offs, including when the recommended path has a real downside.
        """;

    public static string InterviewQuestion(string role, string kind, string profileFacts, string transcript) =>
        $"""
         {InterviewQuestionTag}
         Ask the next interview question.
         Target role: {role}
         Interview type: {kind}
         Candidate facts:
         {profileFacts}
         Transcript so far:
         {transcript}

         Return one question only. If the last answer was thin or unsupported, probe it instead of moving on.
         """;

    public static string InterviewFeedback(string role, string transcript) =>
        $"""
         {InterviewFeedbackTag}
         Review this mock interview for a {role} role and give the candidate two short paragraphs:
         what held up, and the single change that would move their score most.
         Transcript:
         {transcript}
         """;

    public static string Explain(string subject, string evidence) =>
        $"""
         {ExplainTag}
         Explain in three sentences why {subject} was recommended, using only this evidence:
         {evidence}
         """;

    public static string ResumeRewrite(string section, string original, string profileFacts) =>
        $"""
         {ResumeRewriteTag}
         Rewrite this resume line to be specific and quantified.
         Section: {section}
         Original: {original}
         Facts you may use, and nothing else:
         {profileFacts}
         """;
}

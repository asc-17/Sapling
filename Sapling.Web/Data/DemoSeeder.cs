using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Sapling.Web.Data;

/// <summary>Seeds the shared catalogue plus the PRD demo student (score 52, ATS 61).</summary>
public static class DemoSeeder
{
    public const string DemoEmail = "demo@sapling.app";
    public const string DemoPassword = "Sapling@2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaplingDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Skills.AnyAsync())
        {
            await SeedCatalogueAsync(db);
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(DemoEmail) is null)
        {
            await SeedDemoStudentAsync(db, users);
        }
    }

    private static async Task SeedCatalogueAsync(SaplingDbContext db)
    {
        var skills = new[]
        {
            new Skill { Name = "Python", Category = "Programming" },
            new Skill { Name = "Java", Category = "Programming" },
            new Skill { Name = "SQL", Category = "Data" },
            new Skill { Name = "Data structures", Category = "Fundamentals" },
            new Skill { Name = "Git", Category = "Tooling" },
            new Skill { Name = "HTML & CSS", Category = "Web" },
            new Skill { Name = "JavaScript", Category = "Web" },
            new Skill { Name = "React", Category = "Web" },
            new Skill { Name = "REST APIs", Category = "Backend" },
            new Skill { Name = "Cloud fundamentals", Category = "Cloud" },
            new Skill { Name = "Docker", Category = "Cloud" },
            new Skill { Name = "Linux", Category = "Tooling" },
            new Skill { Name = "Pandas", Category = "Data" },
            new Skill { Name = "Machine learning", Category = "Data" },
            new Skill { Name = "Statistics", Category = "Data" },
            new Skill { Name = "Data visualisation", Category = "Data" },
            new Skill { Name = "Excel", Category = "Data" },
            new Skill { Name = "Communication", Category = "Professional" },
            new Skill { Name = "Aptitude", Category = "Professional" },
            new Skill { Name = "System design", Category = "Backend" },
            new Skill { Name = "Testing", Category = "Quality" },
            new Skill { Name = "General studies", Category = "Government" },
        };
        db.Skills.AddRange(skills);
        await db.SaveChangesAsync();

        var byName = skills.ToDictionary(s => s.Name, s => s.Id);

        var roles = new[]
        {
            new CareerRole
            {
                Title = "Backend Developer",
                Family = "Software Engineering",
                Tier = "Safe",
                FitScore = 78,
                EntrySalaryMp = "₹3.6 – 5.5 LPA",
                EntrySalaryMetro = "₹6 – 9 LPA",
                DemandTrend = "Rising",
                FiveYearOutlook = "Senior engineer or tech lead at ₹18 – 28 LPA, with a clear path into architecture.",
                Employers = "TCS|Infosys|Persistent (Indore)|Impetus (Indore)|Yash Technologies (Bhopal)",
                Reasons = "Your Java and SQL are already at working level|Backend roles are the largest single category of openings in Indore and Bhopal|Your CGPA clears the 6.5 cut-off most service companies apply|The two gaps that block you take about nine weeks together",
                CoreSkills = "Java|SQL|REST APIs|Data structures|Git|Cloud fundamentals",
                CounterCase = "Service-company backend roles in MP start lower than the metro figures you see online, and the first two years are often maintenance work rather than new development.",
                Requirements =
                [
                    new() { SkillId = byName["Java"], RequiredLevel = 70, Impact = 9, Effort = 4, WeeksToClose = 3, Rationale = "Named in 82% of backend JDs you matched." },
                    new() { SkillId = byName["SQL"], RequiredLevel = 70, Impact = 8, Effort = 3, WeeksToClose = 2, Rationale = "Every shortlisted JD asks for joins and indexing." },
                    new() { SkillId = byName["REST APIs"], RequiredLevel = 65, Impact = 8, Effort = 4, WeeksToClose = 3, Rationale = "The standard first-round coding task." },
                    new() { SkillId = byName["Data structures"], RequiredLevel = 75, Impact = 10, Effort = 6, WeeksToClose = 6, Rationale = "Decides the online assessment round." },
                    new() { SkillId = byName["Cloud fundamentals"], RequiredLevel = 60, Impact = 9, Effort = 5, WeeksToClose = 4, Rationale = "Blocks 41% of the open roles you otherwise fit." },
                    new() { SkillId = byName["Docker"], RequiredLevel = 45, Impact = 6, Effort = 3, WeeksToClose = 2, Rationale = "Increasingly assumed, rarely taught in the syllabus." },
                    new() { SkillId = byName["Git"], RequiredLevel = 60, Impact = 5, Effort = 2, WeeksToClose = 1, Rationale = "Screened for in the portfolio review." },
                    new() { SkillId = byName["Communication"], RequiredLevel = 65, Impact = 7, Effort = 5, WeeksToClose = 6, Rationale = "The HR round eliminates more candidates than the coding round." },
                ],
            },
            new CareerRole
            {
                Title = "Data Analyst",
                Family = "Data & Analytics",
                Tier = "Stretch",
                FitScore = 66,
                EntrySalaryMp = "₹3.0 – 4.8 LPA",
                EntrySalaryMetro = "₹5.5 – 8 LPA",
                DemandTrend = "Rising fast",
                FiveYearOutlook = "Analytics lead or data scientist at ₹15 – 24 LPA if you add modelling depth.",
                Employers = "Deloitte (Indore)|Infobeans|Mindtree|IDFC First|State analytics cells",
                Reasons = "Your statistics marks are the strongest part of your transcript|Analyst roles accept non-CS branches more readily than engineering roles|Three of your four biggest gaps are shared with the backend track, so effort is not wasted|Remote analyst roles are unusually accessible from tier-2 cities",
                CoreSkills = "SQL|Python|Pandas|Statistics|Data visualisation|Excel",
                CounterCase = "Analyst openings in Madhya Pradesh are fewer than backend openings, so you would be competing for remote roles against metro candidates with internships you do not have yet.",
                Requirements =
                [
                    new() { SkillId = byName["SQL"], RequiredLevel = 80, Impact = 10, Effort = 3, WeeksToClose = 3, Rationale = "The single most tested skill in analyst interviews." },
                    new() { SkillId = byName["Python"], RequiredLevel = 65, Impact = 8, Effort = 4, WeeksToClose = 4, Rationale = "Expected for anything beyond reporting." },
                    new() { SkillId = byName["Pandas"], RequiredLevel = 60, Impact = 7, Effort = 3, WeeksToClose = 3, Rationale = "Appears in the take-home assignment." },
                    new() { SkillId = byName["Statistics"], RequiredLevel = 70, Impact = 8, Effort = 5, WeeksToClose = 4, Rationale = "Separates analysts from report builders." },
                    new() { SkillId = byName["Data visualisation"], RequiredLevel = 60, Impact = 7, Effort = 3, WeeksToClose = 2, Rationale = "Power BI or Tableau named in most JDs." },
                    new() { SkillId = byName["Communication"], RequiredLevel = 70, Impact = 9, Effort = 5, WeeksToClose = 6, Rationale = "You are hired to explain numbers to people who do not like numbers." },
                ],
            },
            new CareerRole
            {
                Title = "Cloud & DevOps Engineer",
                Family = "Infrastructure",
                Tier = "Aspirational",
                FitScore = 54,
                EntrySalaryMp = "₹4.0 – 6.0 LPA",
                EntrySalaryMetro = "₹7 – 11 LPA",
                DemandTrend = "Rising",
                FiveYearOutlook = "Platform or SRE lead at ₹22 – 35 LPA; the steepest salary curve of the three.",
                Employers = "Persistent|TCS iON|Nagarro|Cloud partners in Indore|Remote-first startups",
                Reasons = "The highest entry salary band available to you locally|Your Linux comfort is an unusual head start|Certification-led hiring means a credential can offset a tier-3 college|It shares cloud fundamentals with your backend track",
                CoreSkills = "Linux|Cloud fundamentals|Docker|Git|Python|System design",
                CounterCase = "Almost nobody is hired into DevOps straight from campus. The realistic route is two years of backend work first, so treat this as a 24-month target and not a placement-season plan.",
                Requirements =
                [
                    new() { SkillId = byName["Cloud fundamentals"], RequiredLevel = 75, Impact = 10, Effort = 6, WeeksToClose = 6, Rationale = "A cloud practitioner certification is effectively the entry ticket." },
                    new() { SkillId = byName["Docker"], RequiredLevel = 70, Impact = 9, Effort = 4, WeeksToClose = 4, Rationale = "Containers are the daily unit of work." },
                    new() { SkillId = byName["Linux"], RequiredLevel = 70, Impact = 8, Effort = 3, WeeksToClose = 3, Rationale = "Interviews are hands-on shell exercises." },
                    new() { SkillId = byName["Python"], RequiredLevel = 60, Impact = 7, Effort = 4, WeeksToClose = 4, Rationale = "Automation scripting is most of the job." },
                    new() { SkillId = byName["System design"], RequiredLevel = 55, Impact = 8, Effort = 8, WeeksToClose = 10, Rationale = "Needed for anything above junior level." },
                ],
            },
        };
        db.CareerRoles.AddRange(roles);

        db.Courses.AddRange(
            new Course { Title = "Programming in Java", Provider = "NPTEL", Cost = "Free", IsFree = true, IsGovernmentSubsidised = true, Hours = 36, Level = "Intermediate", Url = "https://nptel.ac.in", TeachesSkills = "Java|Data structures", Summary = "IIT-run twelve-week course with a proctored exam; the certificate is recognised by most MP recruiters." },
            new Course { Title = "Database Management Systems", Provider = "SWAYAM", Cost = "Free", IsFree = true, IsGovernmentSubsidised = true, Hours = 30, Level = "Intermediate", Url = "https://swayam.gov.in", TeachesSkills = "SQL", Summary = "Covers joins, indexing and normalisation, which is exactly what the first interview round tests." },
            new Course { Title = "Cloud Computing Fundamentals", Provider = "NPTEL", Cost = "Free", IsFree = true, IsGovernmentSubsidised = true, Hours = 28, Level = "Beginner", Url = "https://nptel.ac.in", TeachesSkills = "Cloud fundamentals", Summary = "Closes the single gap that blocks the largest share of roles you otherwise fit." },
            new Course { Title = "AWS Certified Cloud Practitioner", Provider = "AWS", Cost = "₹8,300 exam fee", IsFree = false, IsGovernmentSubsidised = false, Hours = 25, Level = "Beginner", Url = "https://aws.amazon.com/certification", TeachesSkills = "Cloud fundamentals", Summary = "Paid, but the credential is named directly in several Indore job descriptions." },
            new Course { Title = "MP Skill Development: Employability Communication", Provider = "MP Skill Mission", Cost = "Subsidised", IsFree = false, IsGovernmentSubsidised = true, Hours = 40, Level = "Beginner", Url = "https://mpskills.mp.gov.in", TeachesSkills = "Communication", Summary = "State-subsidised spoken English and interview communication programme delivered in district centres." },
            new Course { Title = "Docker for Beginners", Provider = "Udemy", Cost = "₹499", IsFree = false, IsGovernmentSubsidised = false, Hours = 12, Level = "Beginner", Url = "https://udemy.com", TeachesSkills = "Docker|Linux", Summary = "Short and practical; enough to containerise your capstone project." },
            new Course { Title = "Data Analysis with Python", Provider = "SWAYAM", Cost = "Free", IsFree = true, IsGovernmentSubsidised = true, Hours = 32, Level = "Intermediate", Url = "https://swayam.gov.in", TeachesSkills = "Python|Pandas|Statistics", Summary = "The fastest route into the analyst track using material you can access without paying." },
            new Course { Title = "Git & GitHub Essentials", Provider = "Microsoft Learn", Cost = "Free", IsFree = true, IsGovernmentSubsidised = false, Hours = 6, Level = "Beginner", Url = "https://learn.microsoft.com", TeachesSkills = "Git", Summary = "One weekend. Recruiters check your GitHub before they check your resume." });

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Opportunities.AddRange(
            new Opportunity { Title = "Backend Engineering Intern", Company = "Persistent Systems", Kind = "Internship", Location = "Indore", Remote = false, Stipend = "₹18,000 / month", ClosesOn = today.AddDays(12), MatchScore = 74, MissingSkills = "Cloud fundamentals|Docker", Reasons = "Java and SQL match the stack|Located in your preferred city|Converts to a full-time offer for 6 in 10 interns", Description = "Six-month internship on payments infrastructure. You will work in a Java and PostgreSQL codebase with a mentor assigned from week one." },
            new Opportunity { Title = "Graduate Trainee — Software", Company = "Yash Technologies", Kind = "Full-time", Location = "Bhopal", Remote = false, Stipend = "₹4.2 LPA", ClosesOn = today.AddDays(3), MatchScore = 68, MissingSkills = "Cloud fundamentals", Reasons = "CGPA clears the 6.5 cut-off|Branch is eligible|One skill away from the stated requirement", Description = $"Campus drive for the {DateTime.Today.Year} batch. Online assessment, then a technical and an HR round on the same day." },
            new Opportunity { Title = "Data Analyst Intern", Company = "InfoBeans", Kind = "Internship", Location = "Indore", Remote = true, Stipend = "₹15,000 / month", ClosesOn = today.AddDays(21), MatchScore = 61, MissingSkills = "Pandas|Data visualisation", Reasons = "Statistics is your strongest subject|Remote, so no relocation cost|Team takes non-CS branches", Description = "Reporting and dashboard work for a retail client. SQL every day, Python two days a week." },
            new Opportunity { Title = "Junior Cloud Support Associate", Company = "Nagarro", Kind = "Full-time", Location = "Remote (India)", Remote = true, Stipend = "₹4.8 LPA", ClosesOn = today.AddDays(30), MatchScore = 48, MissingSkills = "Cloud fundamentals|Docker|Linux", Reasons = "Highest salary band open to you|Remote-first|Certification can substitute for experience", Description = "Support and automation for cloud workloads. A cloud practitioner certification is treated as equivalent to one year of experience." },
            new Opportunity { Title = "Software Engineer Trainee", Company = "TCS (NQT)", Kind = "Full-time", Location = "Indore / Bhopal", Remote = false, Stipend = "₹3.6 LPA", ClosesOn = today.AddDays(45), MatchScore = 71, MissingSkills = "", Reasons = "You meet every stated requirement|Largest single recruiter in the state|Aptitude is a scored round and you have four weeks to prepare", Description = "National Qualifier Test route. Aptitude, programming logic and coding sections, followed by a technical interview." },
            new Opportunity { Title = "Frontend Intern", Company = "Impetus", Kind = "Internship", Location = "Indore", Remote = false, Stipend = "₹12,000 / month", ClosesOn = today.AddDays(9), MatchScore = 52, MissingSkills = "React|JavaScript", Reasons = "HTML and CSS already in place|Short commute from campus|Small team, broad exposure", Description = "Build internal tools in React. Suitable if you want product work rather than services work." });

        db.GovtExams.AddRange(
            new GovtExam { Name = "MPESB Group 4 (Assistant Grade 3)", Authority = "MP Employee Selection Board", Level = "State", NotificationOn = today.AddDays(-6), ExamOn = today.AddDays(84), MinAge = 18, MaxAge = 40, QualificationRequired = "Graduate", RequiresMpDomicile = true, MinCgpa = 0, SyllabusAreas = "General knowledge|Quantitative aptitude|Computer knowledge|Hindi|English", Summary = "High-volume state recruitment with a computer-knowledge section that overlaps your degree directly." },
            new GovtExam { Name = "MPPSC State Service Examination", Authority = "MP Public Service Commission", Level = "State", NotificationOn = today.AddDays(18), ExamOn = today.AddDays(150), MinAge = 21, MaxAge = 40, QualificationRequired = "Graduate", RequiresMpDomicile = false, MinCgpa = 0, SyllabusAreas = "General studies|MP-specific GK|CSAT|Essay|Interview", Summary = "The state's flagship administrative examination. Expect a two to three year preparation commitment." },
            new GovtExam { Name = "SSC Combined Graduate Level", Authority = "Staff Selection Commission", Level = "Central", NotificationOn = today.AddDays(-20), ExamOn = today.AddDays(60), MinAge = 18, MaxAge = 32, QualificationRequired = "Graduate", RequiresMpDomicile = false, MinCgpa = 0, SyllabusAreas = "Quantitative aptitude|Reasoning|English|General awareness", Summary = "Central government posts across ministries. Four tiers, with the first tier in under three months." },
            new GovtExam { Name = "IBPS Probationary Officer", Authority = "Institute of Banking Personnel Selection", Level = "Central", NotificationOn = today.AddDays(40), ExamOn = today.AddDays(120), MinAge = 20, MaxAge = 30, QualificationRequired = "Graduate", RequiresMpDomicile = false, MinCgpa = 0, SyllabusAreas = "Reasoning|Quantitative aptitude|English|Banking awareness", Summary = "Banking track with a well-defined syllabus; the aptitude work overlaps your campus placement preparation." },
            new GovtExam { Name = "RRB Junior Engineer", Authority = "Railway Recruitment Board", Level = "Central", NotificationOn = today.AddDays(-2), ExamOn = today.AddDays(95), MinAge = 18, MaxAge = 33, QualificationRequired = "Diploma or Degree in Engineering", RequiresMpDomicile = false, MinCgpa = 0, SyllabusAreas = "Technical subject|Mathematics|Reasoning|General awareness", Summary = "Engineering-specific technical paper, which favours your branch over general-stream candidates." },
            new GovtExam { Name = "MPPSC Assistant Professor", Authority = "MP Public Service Commission", Level = "State", NotificationOn = null, ExamOn = null, MinAge = 21, MaxAge = 40, QualificationRequired = "Postgraduate with NET", RequiresMpDomicile = false, MinCgpa = 0, SyllabusAreas = "Subject paper|Teaching aptitude", Summary = "Requires a master's degree and NET, so this becomes available only after further study." });

        var quiz = new[]
        {
            ("I enjoy taking apart a device to understand how it works.", "Realistic"),
            ("I would rather fix something physical than write about it.", "Realistic"),
            ("I like working with tools, machines or hardware.", "Realistic"),
            ("I enjoy finding the flaw in an argument or a dataset.", "Investigative"),
            ("I would happily spend an afternoon on one hard problem.", "Investigative"),
            ("I read about how things work even when no exam depends on it.", "Investigative"),
            ("I often think of how something could look or sound better.", "Artistic"),
            ("I prefer open-ended work to work with one correct answer.", "Artistic"),
            ("I enjoy designing interfaces, posters or presentations.", "Artistic"),
            ("People come to me when they need something explained.", "Social"),
            ("I enjoy teaching a junior something I have just learned.", "Social"),
            ("I would choose team work over solo work most of the time.", "Social"),
            ("I am comfortable persuading people to change their minds.", "Enterprising"),
            ("I would like to run something of my own one day.", "Enterprising"),
            ("I enjoy leading a group towards a deadline.", "Enterprising"),
            ("I like clear rules, checklists and predictable processes.", "Conventional"),
            ("I keep my notes and files organised without being told.", "Conventional"),
            ("I would rather improve an existing system than invent a new one.", "Conventional"),
        };
        db.QuizQuestions.AddRange(quiz.Select((q, i) => new QuizQuestion { Text = q.Item1, Dimension = q.Item2, Order = i }));

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoStudentAsync(SaplingDbContext db, UserManager<AppUser> users)
    {
        var user = new AppUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            FullName = "Aarav Verma",
        };

        var created = await users.CreateAsync(user, DemoPassword);
        if (!created.Succeeded)
        {
            return;
        }

        var backend = await db.CareerRoles.FirstAsync(r => r.Title == "Backend Developer");
        var skillIds = await db.Skills.ToDictionaryAsync(s => s.Name, s => s.Id);

        var profile = new StudentProfile
        {
            UserId = user.Id,
            College = "Shri Govindram Seksaria Institute of Technology, Indore",
            Branch = "Computer Science & Engineering",
            GraduationYear = DateTime.Today.Year,
            City = "Indore",
            Cgpa = 7.2,
            Backlogs = 0,
            PreferredLanguage = "English",
            OnboardingComplete = true,
            RiasecCode = "IRC",
            TargetRoleId = backend.Id,
            AtsScore = 61,
            PreviousAtsScore = 61,
        };

        (string Skill, int Level, bool Verified, string Source)[] studentSkills =
        [
            ("Java", 70, true, "NPTEL certificate"),
            ("SQL", 62, true, "Micro-assessment"),
            ("Data structures", 64, true, "Micro-assessment"),
            ("Git", 55, true, "GitHub analysis"),
            ("HTML & CSS", 60, false, "Self-claimed"),
            ("JavaScript", 35, false, "Self-claimed"),
            ("Python", 40, false, "Self-claimed"),
            ("Linux", 45, false, "Self-claimed"),
            ("REST APIs", 30, false, "Self-claimed"),
            ("Cloud fundamentals", 12, false, "Self-claimed"),
            ("Docker", 0, false, "Not started"),
            ("Communication", 48, false, "Interview transcript"),
            ("Aptitude", 65, true, "Micro-assessment"),
            ("Statistics", 72, true, "Academic record"),
        ];

        profile.Skills = studentSkills
            .Select(s => new StudentSkill { SkillId = skillIds[s.Skill], Level = s.Level, Verified = s.Verified, Source = s.Source })
            .ToList();

        profile.Scores =
        [
            new ScoreSnapshot { AsOf = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), Total = 44, Academic = 72, TechnicalSkills = 36, Projects = 30, Communication = 48, Certifications = 20, Exposure = 10 },
        ];

        var courses = await db.Courses.ToDictionaryAsync(c => c.Title, c => c.Id);

        (int Week, string Focus, string Title, string Kind, string? Detail, string? Course, int Hours, bool Done)[] plan =
        [
            (1, "Close the SQL gap", "Database Management Systems — weeks 1 to 2", "Course", "Joins, indexing, normalisation.", "Database Management Systems", 8, true),
            (1, "Close the SQL gap", "Solve 20 SQL problems on joins", "Practice", "Use any free problem set; log your wrong answers.", null, 4, true),
            (2, "Close the SQL gap", "Database Management Systems — weeks 3 to 4", "Course", "Transactions and query plans.", "Database Management Systems", 8, true),
            (2, "Close the SQL gap", "Add a query-optimisation note to your project README", "Project", "Recruiters read the README before the code.", null, 2, true),
            (3, "Version control and portfolio", "Git & GitHub Essentials", "Course", "One weekend, then clean up your profile.", "Git & GitHub Essentials", 6, false),
            (3, "Version control and portfolio", "Publish two existing college projects with proper READMEs", "Project", "Screenshots, setup steps, what you would do differently.", null, 5, false),
            (4, "Cloud fundamentals", "Cloud Computing Fundamentals — weeks 1 to 2", "Course", "The gap that blocks 41% of your matched roles.", "Cloud Computing Fundamentals", 8, false),
            (5, "Cloud fundamentals", "Cloud Computing Fundamentals — weeks 3 to 4", "Course", "Storage, networking, cost models.", "Cloud Computing Fundamentals", 8, false),
            (6, "Cloud fundamentals", "Deploy one project to a free cloud tier", "Project", "A live URL is worth more than a certificate line.", null, 6, false),
            (7, "Containers", "Docker for Beginners", "Course", "Containerise the project you just deployed.", "Docker for Beginners", 12, false),
            (8, "Java depth", "Programming in Java — first half", "Course", "Collections, concurrency, exceptions.", "Programming in Java", 12, false),
            (9, "Java depth", "Programming in Java — second half", "Course", "Streams, JDBC, testing.", "Programming in Java", 12, false),
            (10, "Communication", "MP Skill Development: Employability Communication", "Course", "State-subsidised; attend the Indore centre.", "MP Skill Development: Employability Communication", 10, false),
            (10, "Communication", "Three mock interviews with feedback review", "Practice", "Target a rubric score above 70 on structure.", null, 3, false),
            (11, "Placement sprint", "Aptitude practice for the TCS NQT pattern", "Practice", "Two full mock papers, timed.", null, 8, false),
            (11, "Placement sprint", "Tailor your resume to the Persistent internship JD", "Task", "Accept the rewrite suggestions, then export.", null, 2, false),
            (12, "Placement sprint", "Capstone: REST API with authentication and tests", "Project", "The single artefact that carries your portfolio.", null, 16, false),
            (12, "Placement sprint", "Apply to every matched opportunity above 65% fit", "Task", "Do not wait until you feel ready.", null, 2, false),
        ];

        profile.RoadmapItems = plan.Select((p, i) => new RoadmapItem
        {
            WeekNumber = p.Week,
            WeekFocus = p.Focus,
            Title = p.Title,
            Kind = p.Kind,
            Detail = p.Detail,
            CourseId = p.Course is null ? null : courses[p.Course],
            EstimatedHours = p.Hours,
            Completed = p.Done,
            Order = i,
        }).ToList();

        (string Section, string Original, string Suggested, string Why)[] suggestions =
        [
            ("Experience", "Worked on a college project using Java.",
                "Built a Java and PostgreSQL library management system used by 3 departments, cutting issue-desk time from 6 minutes to under 2.",
                "Weak verb with no scope or outcome. The rewrite uses only facts already in your profile."),
            ("Experience", "Helped the team with database work.",
                "Designed the schema and wrote 14 indexed queries, reducing report generation from 40 seconds to 3.",
                "\"Helped\" hides your actual contribution; name what you personally built."),
            ("Skills", "Languages: Java, Python, HTML, CSS, JavaScript, C, C++, SQL",
                "Core: Java, SQL, Data structures (verified). Working: Python, HTML/CSS. Learning: Cloud fundamentals.",
                "Undifferentiated skill lists score poorly with ATS keyword matching and invite questions you cannot answer."),
            ("Summary", "Hardworking B.Tech student seeking a challenging role in a reputed organisation.",
                "Final-year CSE student at SGSITS Indore, building backend services in Java and SQL, targeting a backend engineering role in Indore or Bhopal.",
                "The original says nothing a recruiter can act on and appears on thousands of resumes verbatim."),
            ("Education", $"B.Tech CSE, {DateTime.Today.Year}, 7.2 CGPA",
                $"B.Tech Computer Science & Engineering, SGSITS Indore, {DateTime.Today.Year} — CGPA 7.2/10, no backlogs.",
                "Spell out the institution and the scale; some ATS parsers drop the score when the scale is missing."),
            ("Formatting", "Two-column layout with a skills sidebar and icons.",
                "Single-column layout, standard section headings, no text inside graphics.",
                "Your current layout is the reason four fields failed to parse in the ATS check."),
        ];

        profile.ResumeSuggestions = suggestions.Select((s, i) => new ResumeSuggestion
        {
            Section = s.Section,
            Original = s.Original,
            Suggested = s.Suggested,
            Rationale = s.Why,
            Order = i,
        }).ToList();

        db.StudentProfiles.Add(profile);
        await db.SaveChangesAsync();
    }
}

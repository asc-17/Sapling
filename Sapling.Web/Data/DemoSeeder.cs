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

        // Ensure State column exists in StudentProfiles table on SQLite
        await EnsureSchemaColumnsAsync(db);

        if (!await db.Skills.AnyAsync())
        {
            await SeedCatalogueAsync(db);
        }
        else
        {
            // Ensure any new catalogue skills are added to an existing DB.
            await EnsureSkillCatalogueAsync(db);
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await users.FindByEmailAsync(DemoEmail) is null)
        {
            await SeedDemoStudentAsync(db, users);
        }

        // Seed colleges directory from AICTE dataset if not already populated
        await EnsureCollegesSeededAsync(db);

        await EnsureCommunitySchemaAsync(db);

        // The first placeholder set was a shared cross-college feed; communities are now private per college.
        if (await db.Institutions.AnyAsync(i => i.Name == "Narmada Skills Academy"))
        {
            await db.PostUpvotes.ExecuteDeleteAsync();
            await db.PostComments.ExecuteDeleteAsync();
            await db.CommunityPosts.ExecuteDeleteAsync();
            await db.Institutions.ExecuteDeleteAsync();
        }

        if (!await db.Institutions.AnyAsync())
        {
            await SeedCommunityAsync(db);
        }
    }

    /// <summary>EnsureCreated skips databases that already exist, so community tables are added here for older demo DBs.</summary>
    private static async Task EnsureCommunitySchemaAsync(SaplingDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(CommunityTablesSql);

        // Added after the first community build, so it may be missing from a table created by it.
        var hasImageUrl = await db.Database
            .SqlQueryRaw<int>("""SELECT COUNT(*) AS "Value" FROM pragma_table_info('CommunityPosts') WHERE name = 'ImageUrl'""")
            .FirstAsync();

        if (hasImageUrl == 0)
        {
            await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "CommunityPosts" ADD COLUMN "ImageUrl" TEXT NULL;""");
        }
    }

    private const string CommunityTablesSql = """
        CREATE TABLE IF NOT EXISTS "Institutions" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Institutions" PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "ShortName" TEXT NOT NULL,
            "City" TEXT NOT NULL,
            "Verified" INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS "CommunityPosts" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_CommunityPosts" PRIMARY KEY AUTOINCREMENT,
            "InstitutionId" INTEGER NOT NULL,
            "Kind" TEXT NOT NULL,
            "Title" TEXT NOT NULL,
            "Body" TEXT NOT NULL,
            "PostedAtUtc" TEXT NOT NULL,
            "StartsAtUtc" TEXT NULL,
            "Venue" TEXT NULL,
            "CtaLabel" TEXT NULL,
            "CtaUrl" TEXT NULL,
            "ImageUrl" TEXT NULL,
            "Tags" TEXT NOT NULL,
            "BaseUpvotes" INTEGER NOT NULL,
            CONSTRAINT "FK_CommunityPosts_Institutions_InstitutionId" FOREIGN KEY ("InstitutionId") REFERENCES "Institutions" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_InstitutionId" ON "CommunityPosts" ("InstitutionId");
        CREATE INDEX IF NOT EXISTS "IX_CommunityPosts_PostedAtUtc" ON "CommunityPosts" ("PostedAtUtc");
        CREATE TABLE IF NOT EXISTS "PostUpvotes" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_PostUpvotes" PRIMARY KEY AUTOINCREMENT,
            "CommunityPostId" INTEGER NOT NULL,
            "StudentProfileId" INTEGER NOT NULL,
            "CreatedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_PostUpvotes_CommunityPosts_CommunityPostId" FOREIGN KEY ("CommunityPostId") REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_PostUpvotes_StudentProfiles_StudentProfileId" FOREIGN KEY ("StudentProfileId") REFERENCES "StudentProfiles" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_PostUpvotes_CommunityPostId_StudentProfileId" ON "PostUpvotes" ("CommunityPostId", "StudentProfileId");
        CREATE INDEX IF NOT EXISTS "IX_PostUpvotes_StudentProfileId" ON "PostUpvotes" ("StudentProfileId");
        CREATE TABLE IF NOT EXISTS "PostComments" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_PostComments" PRIMARY KEY AUTOINCREMENT,
            "CommunityPostId" INTEGER NOT NULL,
            "ParentCommentId" INTEGER NULL,
            "StudentProfileId" INTEGER NULL,
            "InstitutionId" INTEGER NULL,
            "AuthorName" TEXT NOT NULL,
            "AuthorHeadline" TEXT NULL,
            "Body" TEXT NOT NULL,
            "PostedAtUtc" TEXT NOT NULL,
            CONSTRAINT "FK_PostComments_CommunityPosts_CommunityPostId" FOREIGN KEY ("CommunityPostId") REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_PostComments_Institutions_InstitutionId" FOREIGN KEY ("InstitutionId") REFERENCES "Institutions" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_PostComments_PostComments_ParentCommentId" FOREIGN KEY ("ParentCommentId") REFERENCES "PostComments" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_PostComments_StudentProfiles_StudentProfileId" FOREIGN KEY ("StudentProfileId") REFERENCES "StudentProfiles" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_PostComments_CommunityPostId" ON "PostComments" ("CommunityPostId");
        CREATE INDEX IF NOT EXISTS "IX_PostComments_InstitutionId" ON "PostComments" ("InstitutionId");
        CREATE INDEX IF NOT EXISTS "IX_PostComments_ParentCommentId" ON "PostComments" ("ParentCommentId");
        CREATE INDEX IF NOT EXISTS "IX_PostComments_StudentProfileId" ON "PostComments" ("StudentProfileId");
        """;

    /// <summary>
    /// Placeholder college posts until institution accounts exist. Names match the AICTE catalogue, which is how a
    /// student's profile college is linked to its private community. Dates are relative so the feed looks current.
    /// </summary>
    private static async Task SeedCommunityAsync(SaplingDbContext db)
    {
        var sgsits = new Institution { Name = "Shri Govindram Seksaria Institute of Technology and Science", ShortName = "SGSITS", City = "Indore", Verified = true };
        var iiti = new Institution { Name = "Indian Institute of Technology Indore", ShortName = "IIT Indore", City = "Indore", Verified = true };
        var manit = new Institution { Name = "Maulana Azad National Institute of Technology Bhopal", ShortName = "MANIT", City = "Bhopal", Verified = true };
        db.Institutions.AddRange(sgsits, iiti, manit);

        var now = DateTime.UtcNow;

        // A time of day in IST, some days from today.
        DateTime At(int days, double istHour = 10) => now.Date.AddDays(days).AddHours(istHour - 5.5);

        var docker = new CommunityPost
        {
            Institution = sgsits, Kind = "Workshop", PostedAtUtc = now.AddHours(-3), BaseUpvotes = 86,
            Title = "Two-day hands-on workshop: Docker and cloud fundamentals",
            Body = "The Department of Computer Engineering is running a two-day, lab-first workshop on containers and cloud basics. Day one covers Linux essentials, images, containers and Docker Compose. Day two deploys a small Java and PostgreSQL app to a free-tier cloud account.\n\nBring a laptop with at least 8 GB RAM. Seats are limited to 60 and allotted first come, first served.",
            StartsAtUtc = At(5), Venue = "Seminar Hall, CE Block, SGSITS Indore",
            Tags = "Docker|Cloud fundamentals|Linux|Hands-on",
        };
        var hackathon = new CommunityPost
        {
            Institution = manit, Kind = "Event", PostedAtUtc = now.AddHours(-20), BaseUpvotes = 214,
            Title = "Hack Bhopal: 36-hour hackathon for MANIT students",
            Body = "Teams of up to four MANIT students can take part, from any branch or year. Problem statements come from state departments and local startups: agriculture supply chains, public transport, and Hindi-first citizen services.\n\nMeals are covered for all 36 hours. The top three teams receive incubation support from the institute's innovation cell.",
            StartsAtUtc = At(18, 9), Venue = "MANIT campus, Bhopal",
            Tags = "Hackathon|Teams of 4|Incubation",
        };
        var research = new CommunityPost
        {
            Institution = iiti, Kind = "Opportunity", PostedAtUtc = now.AddDays(-1).AddHours(-4), BaseUpvotes = 172,
            Title = "Summer research projects open to pre-final year undergraduates",
            Body = "Eight-week faculty-mentored research projects across computer science, electrical and civil engineering. Pre-final year students may apply with a one-page statement of interest and a faculty reference.\n\nA monthly stipend is paid for the duration of the project.",
            Tags = "Research|Internship|Stipend|Pre-final year",
        };
        var nptel = new CommunityPost
        {
            Institution = sgsits, Kind = "Announcement", PostedAtUtc = now.AddDays(-2), BaseUpvotes = 64,
            Title = "NPTEL local chapter: exam registration closes this Friday",
            Body = "Students enrolled in NPTEL courses through the SGSITS local chapter must register for the proctored exam by Friday. A passed exam gives you a certificate that counts towards verified skills and certifications on most placement portals.",
            CtaLabel = "Open NPTEL", CtaUrl = "https://nptel.ac.in",
            Tags = "NPTEL|Certification|Deadline",
        };
        var mockDay = new CommunityPost
        {
            Institution = sgsits, Kind = "Workshop", PostedAtUtc = now.AddDays(-3), BaseUpvotes = 131,
            Title = "Mock interview day with alumni from product companies",
            Body = "SGSITS alumni working as software engineers will run 30-minute mock technical interviews over video, followed by written feedback. Open to final-year students from any branch. Pick a slot in the morning or afternoon session.",
            StartsAtUtc = At(9, 11), Venue = "Online",
            Tags = "Interviews|Alumni|Online",
        };
        var drive = new CommunityPost
        {
            Institution = sgsits, Kind = "Opportunity", PostedAtUtc = now.AddDays(-4), BaseUpvotes = 98,
            Title = "Pre-placement talk and campus drive for final-year CSE and IT",
            Body = "The Training and Placement Cell is hosting a pre-placement talk followed by an online assessment for the software trainee role. Eligibility: 6.5 CGPA and no active backlogs. Carry two printed copies of your resume.",
            StartsAtUtc = At(6, 14), Venue = "Main Auditorium, SGSITS Indore",
            Tags = "Placement|Final year|CSE|IT",
        };
        var aicte = new CommunityPost
        {
            Institution = manit, Kind = "Opportunity", PostedAtUtc = now.AddDays(-5), BaseUpvotes = 57,
            Title = "New Madhya Pradesh listings on the AICTE internship portal",
            Body = "Our placement office has shortlisted internships on the AICTE portal that are based in Indore, Bhopal and Jabalpur, or fully remote. Most are open to second and third-year students and several are paid.",
            CtaLabel = "Browse internships", CtaUrl = "https://internship.aicte-india.org",
            Tags = "AICTE|Internship|Remote",
        };
        var lecture = new CommunityPost
        {
            Institution = iiti, Kind = "Event", PostedAtUtc = now.AddDays(-6), BaseUpvotes = 143,
            Title = "Guest lecture: what recruiters actually look at in your GitHub",
            Body = "An engineering manager walks through real student profiles, anonymised, and explains what gets a resume shortlisted: README quality, commit history, tests, and one project done properly rather than ten tutorials.",
            StartsAtUtc = At(3, 17), Venue = "Online",
            Tags = "GitHub|Portfolio|Guest lecture",
        };
        var english = new CommunityPost
        {
            Institution = sgsits, Kind = "Workshop", PostedAtUtc = now.AddDays(-8), BaseUpvotes = 47,
            Title = "Spoken English and interview confidence batch",
            Body = "The Training and Placement Cell is running a free four-week evening batch for students who are more comfortable in Hindi. Practise answering common HR questions in English, in small groups of ten.",
            StartsAtUtc = At(12, 17), Venue = "Room 204, Main Building, SGSITS Indore",
            Tags = "Communication|Free|Hindi-friendly",
        };

        var sprint = new CommunityPost
        {
            Institution = sgsits, Kind = "Event", PostedAtUtc = now.AddHours(-26), BaseUpvotes = 121,
            Title = "Code Sprint: 24-hour campus hackathon",
            Body = "Teams of up to four SGSITS students, from any branch or year. Problem statements come from Indore startups and the municipal corporation: waste collection routing, bus arrival times, and a Hindi-first grievance app.\n\nThe top three teams present to the startups, and two of them have offered internships to winning teams in past editions.",
            StartsAtUtc = At(16, 9), Venue = "Central Library, SGSITS Indore",
            Tags = "Hackathon|Teams of 4|Internships",
        };

        db.CommunityPosts.AddRange(docker, sprint, hackathon, research, nptel, mockDay, drive, aicte, lecture, english);
        await db.SaveChangesAsync();

        PostComment Student(CommunityPost post, string name, string headline, string body, double hoursAfter, PostComment? parent = null) => new()
        {
            CommunityPostId = post.Id, ParentCommentId = parent?.Id, AuthorName = name, AuthorHeadline = headline, Body = body,
            PostedAtUtc = post.PostedAtUtc.AddHours(hoursAfter),
        };

        PostComment Reply(CommunityPost post, PostComment parent, string body, double hoursAfter) => new()
        {
            CommunityPostId = post.Id, ParentCommentId = parent.Id, InstitutionId = post.InstitutionId,
            AuthorName = post.Institution!.Name, Body = body, PostedAtUtc = post.PostedAtUtc.AddHours(hoursAfter),
        };

        var dockerYears = Student(docker, "Riya Sharma", "Information Technology · 2027 batch", "Is this open to second-year students or only final year?", 0.5);
        var dockerCert = Student(docker, "Mohit Patel", "Computer Science & Engineering · 2026 batch", "Will there be a certificate? It would help for cloud support roles.", 1.2);
        var hackCross = Student(hackathon, "Ananya Tiwari", "Electrical Engineering · 2027 batch", "Can first-year students take part?", 2);
        var sprintBranch = Student(sprint, "Sneha Joshi", "Electronics & Communication · 2026 batch", "Can teams mix branches?", 2);
        var hackTeam = Student(hackathon, "Arjun Singh", "Computer Science & Engineering · 2027 batch", "Looking for a frontend developer to join our team. We are taking on the public transport problem.", 6);
        var mockRecord = Student(mockDay, "Kavya Rao", "Mechanical Engineering · 2026 batch", "Please share a recording of a sample interview for those who cannot get a slot.", 3);
        var englishFee = Student(english, "Pooja Yadav", "Civil Engineering · 2026 batch", "Is there a weekend batch as well? I have labs on weekday evenings.", 10);
        var researchCgpa = Student(research, "Devansh Mishra", "Electrical Engineering · 2027 batch", "Is there a minimum CGPA to apply?", 5);

        db.PostComments.AddRange(dockerYears, dockerCert, sprintBranch, hackCross, hackTeam, mockRecord, englishFee, researchCgpa);
        await db.SaveChangesAsync();

        db.PostComments.AddRange(
            Reply(docker, dockerYears, "Open to all years. Second-year students are welcome as long as they bring a laptop that meets the requirement.", 1),
            Student(docker, "Aditi Chouhan", "Information Technology · 2027 batch", "Thanks, registering now.", 1.5, dockerYears),
            Reply(docker, dockerCert, "Yes. Everyone who completes both days' labs receives a participation certificate from the department.", 2),
            Reply(sprint, sprintBranch, "Yes, and we encourage it. At most two members of a team can be from the same branch.", 3),
            Reply(english, englishFee, "A Saturday morning batch starts the week after. We will post the details here.", 14),
            Reply(hackathon, hackCross, "Yes, all years are welcome. First-year teams are judged in their own category.", 3),
            Reply(mockDay, mockRecord, "A recorded sample interview will be shared on this post within 48 hours of the event.", 5),
            Reply(research, researchCgpa, "There is no hard cut-off. The statement of interest and faculty reference carry more weight than CGPA.", 9));
        await db.SaveChangesAsync();
    }

    /// <summary>Ensures any missing columns (such as State) are added to existing SQLite database.</summary>
    private static async Task EnsureSchemaColumnsAsync(SaplingDbContext db)
    {
        try
        {
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var conn = db.Database.GetDbConnection();
            var shouldClose = conn.State != System.Data.ConnectionState.Open;
            if (shouldClose)
            {
                await conn.OpenAsync();
            }

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(StudentProfiles);";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingCols.Add(reader.GetString(1));
                }

                if (!existingCols.Contains("State"))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = "ALTER TABLE StudentProfiles ADD COLUMN State TEXT NOT NULL DEFAULT '';";
                    await alterCmd.ExecuteNonQueryAsync();
                }

                // Ensure Colleges table exists with Category and Aliases
                using var tableCmd = conn.CreateCommand();
                tableCmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS "Colleges" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Colleges" PRIMARY KEY AUTOINCREMENT,
                        "Name" TEXT NOT NULL,
                        "State" TEXT NOT NULL,
                        "City" TEXT NOT NULL,
                        "AicteId" TEXT NULL,
                        "Category" TEXT NOT NULL DEFAULT 'AICTE Approved',
                        "Aliases" TEXT NOT NULL DEFAULT ''
                    );
                    CREATE INDEX IF NOT EXISTS "IX_Colleges_Name" ON "Colleges" ("Name");
                    CREATE INDEX IF NOT EXISTS "IX_Colleges_State" ON "Colleges" ("State");
                    CREATE INDEX IF NOT EXISTS "IX_Colleges_City" ON "Colleges" ("City");
                    """;
                await tableCmd.ExecuteNonQueryAsync();

                // Ensure Category and Aliases columns exist if table was already created
                var collegeCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var colCmd = conn.CreateCommand();
                colCmd.CommandText = "PRAGMA table_info(Colleges);";
                using var colReader = await colCmd.ExecuteReaderAsync();
                while (await colReader.ReadAsync())
                {
                    collegeCols.Add(colReader.GetString(1));
                }

                if (!collegeCols.Contains("Category"))
                {
                    using var addCat = conn.CreateCommand();
                    addCat.CommandText = "ALTER TABLE Colleges ADD COLUMN Category TEXT NOT NULL DEFAULT 'AICTE Approved';";
                    await addCat.ExecuteNonQueryAsync();
                }

                if (!collegeCols.Contains("Aliases"))
                {
                    using var addAliases = conn.CreateCommand();
                    addAliases.CommandText = "ALTER TABLE Colleges ADD COLUMN Aliases TEXT NOT NULL DEFAULT '';";
                    await addAliases.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await conn.CloseAsync();
                }
            }
        }
        catch
        {
            // Non-SQLite or already applied
        }
    }

    /// <summary>Seeds all Indian colleges from the AICTE/AISHE dataset if the table is empty.</summary>
    private static async Task EnsureCollegesSeededAsync(SaplingDbContext db)
    {
        try
        {
            if (await db.Colleges.AnyAsync())
                return;

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "aicte_colleges.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "aicte_colleges.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Sapling.Web", "Data", "aicte_colleges.json"),
                @"C:\Users\Abhijeet\source\repos\Sapling\Sapling.Web\Data\aicte_colleges.json",
            };

            var jsonPath = candidates.FirstOrDefault(File.Exists);
            if (jsonPath is null)
                return;

            await using var stream = File.OpenRead(jsonPath);
            var items = await System.Text.Json.JsonSerializer.DeserializeAsync<List<CollegeSeedItem>>(stream);
            if (items is not null && items.Count > 0)
            {
                const int chunkSize = 1500;
                for (int i = 0; i < items.Count; i += chunkSize)
                {
                    var chunk = items.Skip(i).Take(chunkSize)
                        .Select(c => new College 
                        { 
                            Name = c.name, 
                            State = c.state, 
                            City = c.city, 
                            AicteId = c.aicte_id,
                            Category = c.category ?? "AICTE Approved",
                            Aliases = c.aliases ?? ""
                        })
                        .ToList();
                    db.Colleges.AddRange(chunk);
                    await db.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // Avoid startup failure if dataset file is inaccessible
        }
    }

    private sealed record CollegeSeedItem(string name, string state, string city, string? aicte_id, string? category = null, string? aliases = null);

    /// <summary>Adds any catalogue skills that are missing from an existing database (idempotent).</summary>
    private static async Task EnsureSkillCatalogueAsync(SaplingDbContext db)
    {
        var existingNames = await db.Skills.Select(s => s.Name).ToHashSetAsync();
        var toAdd = CatalogueSkills
            .Where(s => !existingNames.Contains(s.Name))
            .Select(s => new Skill { Name = s.Name, Category = s.Category })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.Skills.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>All skills that should exist in the catalogue. Called on first run and also by EnsureSkillCatalogueAsync for existing DBs.</summary>
    private static readonly (string Name, string Category)[] CatalogueSkills =
    [
        // ── Programming Languages ──
        ("Python", "Programming"), ("Java", "Programming"), ("C", "Programming"),
        ("C++", "Programming"), ("C#", "Programming"), ("TypeScript", "Programming"),
        ("Kotlin", "Programming"), ("Go", "Programming"), ("R", "Programming"),
        ("PHP", "Programming"), ("Ruby", "Programming"), ("Swift", "Programming"),
        ("Dart", "Programming"), ("MATLAB", "Programming"),

        // ── Web Development ──
        ("HTML & CSS", "Web"), ("JavaScript", "Web"), ("React", "Web"),
        ("Angular", "Web"), ("Vue.js", "Web"), ("Next.js", "Web"),
        ("Node.js", "Web"), ("Tailwind CSS", "Web"), ("Bootstrap", "Web"),

        // ── Backend & APIs ──
        ("REST APIs", "Backend"), ("System design", "Backend"),
        ("Spring Boot", "Backend"), ("Django", "Backend"), ("Flask", "Backend"),
        ("Express.js", "Backend"), (".NET", "Backend"), ("GraphQL", "Backend"),
        ("Microservices", "Backend"),

        // ── Data & Analytics ──
        ("SQL", "Data"), ("Pandas", "Data"), ("Machine learning", "Data"),
        ("Statistics", "Data"), ("Data visualisation", "Data"), ("Excel", "Data"),
        ("Power BI", "Data"), ("Tableau", "Data"), ("NumPy", "Data"),
        ("TensorFlow", "Data"), ("Deep learning", "Data"), ("NLP", "Data"),
        ("Big Data (Spark)", "Data"),

        // ── Cloud & DevOps ──
        ("Cloud fundamentals", "Cloud"), ("Docker", "Cloud"),
        ("Kubernetes", "Cloud"), ("AWS", "Cloud"), ("Azure", "Cloud"),
        ("GCP", "Cloud"), ("CI/CD", "Cloud"), ("Terraform", "Cloud"),
        ("Jenkins", "Cloud"), ("Ansible", "Cloud"),

        // ── Tooling ──
        ("Git", "Tooling"), ("Linux", "Tooling"), ("VS Code", "Tooling"),
        ("IntelliJ", "Tooling"), ("Postman", "Tooling"), ("Jira", "Tooling"),
        ("Figma", "Tooling"),

        // ── Fundamentals ──
        ("Data structures", "Fundamentals"), ("Algorithms", "Fundamentals"),
        ("OOP", "Fundamentals"), ("DBMS", "Fundamentals"),
        ("Computer networks", "Fundamentals"), ("Operating systems", "Fundamentals"),

        // ── Professional Skills ──
        ("Communication", "Professional"), ("Aptitude", "Professional"),
        ("Leadership", "Professional"), ("Teamwork", "Professional"),
        ("Problem solving", "Professional"), ("Time management", "Professional"),
        ("Presentation skills", "Professional"),

        // ── Quality & Testing ──
        ("Testing", "Quality"), ("Unit testing", "Quality"), ("Selenium", "Quality"),
        ("JUnit", "Quality"), ("Manual testing", "Quality"), ("Performance testing", "Quality"),

        // ── Mobile Development ──
        ("Android", "Mobile"), ("iOS", "Mobile"), ("Flutter", "Mobile"),
        ("React Native", "Mobile"),

        // ── Cybersecurity ──
        ("Network security", "Cybersecurity"), ("Ethical hacking", "Cybersecurity"),
        ("Cryptography", "Cybersecurity"),

        // ── Commerce & Finance ──
        ("Tally", "Commerce"), ("GST", "Commerce"), ("Accounting", "Commerce"),
        ("Financial analysis", "Commerce"), ("SAP", "Commerce"),
        ("Business analytics", "Commerce"),

        // ── Government ──
        ("General studies", "Government"),
    ];

    private static async Task SeedCatalogueAsync(SaplingDbContext db)
    {
        var skills = CatalogueSkills.Select(s => new Skill { Name = s.Name, Category = s.Category }).ToArray();
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
            College = "Shri Govindram Seksaria Institute of Technology and Science",
            Branch = "Computer Science & Engineering",
            GraduationYear = DateTime.Today.Year,
            City = "Indore",
            State = "Madhya Pradesh",
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

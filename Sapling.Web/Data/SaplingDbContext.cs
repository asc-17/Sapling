using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sapling.Web.Data;

public class SaplingDbContext(DbContextOptions<SaplingDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<StudentSkill> StudentSkills => Set<StudentSkill>();

    public DbSet<CareerRole> CareerRoles => Set<CareerRole>();

    public DbSet<RoleSkillRequirement> RoleSkillRequirements => Set<RoleSkillRequirement>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<RoadmapItem> RoadmapItems => Set<RoadmapItem>();

    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    public DbSet<OpportunityApplication> OpportunityApplications => Set<OpportunityApplication>();

    public DbSet<ResumeSuggestion> ResumeSuggestions => Set<ResumeSuggestion>();

    public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();

    public DbSet<InterviewTurn> InterviewTurns => Set<InterviewTurn>();

    public DbSet<GovtExam> GovtExams => Set<GovtExam>();

    public DbSet<ScoreSnapshot> ScoreSnapshots => Set<ScoreSnapshot>();

    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<StudentProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.Skills)
            .WithOne()
            .HasForeignKey(s => s.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.RoadmapItems)
            .WithOne()
            .HasForeignKey(i => i.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.Applications)
            .WithOne()
            .HasForeignKey(a => a.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.ResumeSuggestions)
            .WithOne()
            .HasForeignKey(s => s.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.InterviewSessions)
            .WithOne()
            .HasForeignKey(s => s.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasMany(p => p.Scores)
            .WithOne()
            .HasForeignKey(s => s.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InterviewSession>()
            .HasMany(s => s.Turns)
            .WithOne()
            .HasForeignKey(t => t.InterviewSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CareerRole>()
            .HasMany(r => r.Requirements)
            .WithOne()
            .HasForeignKey(r => r.CareerRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Skill>().HasIndex(s => s.Name).IsUnique();
    }
}

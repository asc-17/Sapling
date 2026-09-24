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

    public DbSet<CourseRoleLink> CourseRoleLinks => Set<CourseRoleLink>();


    public DbSet<LearningCourse> LearningCourses => Set<LearningCourse>();

    public DbSet<CourseCheckpoint> CourseCheckpoints => Set<CourseCheckpoint>();

    public DbSet<SkillCourse> SkillCourses => Set<SkillCourse>();

    public DbSet<CheckpointProgress> CheckpointProgress => Set<CheckpointProgress>();

    public DbSet<StudentCourseChoice> StudentCourseChoices => Set<StudentCourseChoice>();

    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    public DbSet<OpportunityApplication> OpportunityApplications => Set<OpportunityApplication>();

    public DbSet<ResumeSuggestion> ResumeSuggestions => Set<ResumeSuggestion>();

    public DbSet<MockInterview> MockInterviews => Set<MockInterview>();

    public DbSet<MockInterviewTurn> MockInterviewTurns => Set<MockInterviewTurn>();

    public DbSet<GovtExam> GovtExams => Set<GovtExam>();

    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    public DbSet<College> Colleges => Set<College>();

    public DbSet<Institution> Institutions => Set<Institution>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();

    public DbSet<PostUpvote> PostUpvotes => Set<PostUpvote>();

    public DbSet<PostComment> PostComments => Set<PostComment>();

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

        builder.Entity<LearningCourse>()
            .HasMany(c => c.Checkpoints)
            .WithOne()
            .HasForeignKey(c => c.LearningCourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<LearningCourse>().HasIndex(c => c.ExternalId).IsUnique();

        builder.Entity<SkillCourse>()
            .HasOne<LearningCourse>()
            .WithMany()
            .HasForeignKey(s => s.LearningCourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentCourseChoice>()
            .HasOne<LearningCourse>()
            .WithMany()
            .HasForeignKey(c => c.LearningCourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentCourseChoice>()
            .HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(c => c.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentCourseChoice>().HasIndex(c => new { c.StudentProfileId, c.SkillId }).IsUnique();

        builder.Entity<CheckpointProgress>()
            .HasOne<CourseCheckpoint>()
            .WithMany()
            .HasForeignKey(p => p.CourseCheckpointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CheckpointProgress>()
            .HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(p => p.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CheckpointProgress>().HasIndex(p => new { p.StudentProfileId, p.CourseCheckpointId }).IsUnique();

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
            .HasMany(p => p.MockInterviews)
            .WithOne()
            .HasForeignKey(i => i.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MockInterview>()
            .HasMany(i => i.Turns)
            .WithOne()
            .HasForeignKey(t => t.MockInterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MockInterview>().HasIndex(i => new { i.StudentProfileId, i.CreatedAt });

        builder.Entity<CareerRole>()
            .HasMany(r => r.Requirements)
            .WithOne()
            .HasForeignKey(r => r.CareerRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CareerRole>()
            .HasMany(r => r.CourseLinks)
            .WithOne()
            .HasForeignKey(l => l.CareerRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CareerRole>().HasIndex(r => r.OnetCode);

        builder.Entity<Skill>().HasIndex(s => s.Name).IsUnique();

        builder.Entity<College>().HasIndex(c => c.Name);

        builder.Entity<Institution>()
            .HasMany(i => i.Posts)
            .WithOne(p => p.Institution)
            .HasForeignKey(p => p.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommunityPost>()
            .HasMany(p => p.Upvotes)
            .WithOne()
            .HasForeignKey(u => u.CommunityPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommunityPost>()
            .HasMany(p => p.Comments)
            .WithOne()
            .HasForeignKey(c => c.CommunityPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CommunityPost>().HasIndex(p => p.PostedAtUtc);

        builder.Entity<PostUpvote>().HasIndex(u => new { u.CommunityPostId, u.StudentProfileId }).IsUnique();

        builder.Entity<PostUpvote>()
            .HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(u => u.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComment>()
            .HasOne<PostComment>()
            .WithMany()
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComment>()
            .HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(c => c.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComment>()
            .HasOne(c => c.Institution)
            .WithMany()
            .HasForeignKey(c => c.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

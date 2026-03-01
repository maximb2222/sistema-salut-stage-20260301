using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Data;

/// <summary>
/// Контекст EF Core для системы учета посещаемости.
/// </summary>
public class ClubAttendanceContext(DbContextOptions<ClubAttendanceContext> options) : DbContext(options)
{
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClubMember>()
            .HasIndex(member => new { member.LastName, member.FirstName });

        modelBuilder.Entity<ClubMember>()
            .Property(member => member.MembershipType)
            .HasDefaultValue("Стандарт");

        modelBuilder.Entity<Visit>()
            .HasOne(visit => visit.ClubMember)
            .WithMany(member => member.Visits)
            .HasForeignKey(visit => visit.ClubMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Visit>()
            .HasOne(visit => visit.WorkoutSession)
            .WithMany(session => session.Visits)
            .HasForeignKey(visit => visit.WorkoutSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Visit>()
            .HasIndex(visit => visit.CheckInAt);

        modelBuilder.Entity<WorkoutSession>()
            .HasIndex(session => session.StartsAt);
    }
}

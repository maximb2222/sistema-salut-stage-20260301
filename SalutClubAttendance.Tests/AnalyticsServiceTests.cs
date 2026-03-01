using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;
using SalutClubAttendance.Web.Services;

namespace SalutClubAttendance.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task BuildDashboardAsync_ReturnsBasicCounters()
    {
        await using var context = CreateContext(nameof(BuildDashboardAsync_ReturnsBasicCounters));
        SeedTestData(context);
        var service = new AnalyticsService(context);

        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(2, dashboard.TotalMembers);
        Assert.Equal(1, dashboard.ActiveMembers);
        Assert.True(dashboard.VisitsToday >= 1);
        Assert.True(dashboard.VisitsThisMonth >= 1);
    }

    [Fact]
    public async Task BuildDashboardAsync_ReturnsLast14DaysTrend()
    {
        await using var context = CreateContext(nameof(BuildDashboardAsync_ReturnsLast14DaysTrend));
        SeedTestData(context);
        var service = new AnalyticsService(context);

        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(14, dashboard.DailyTrendLabels.Count);
        Assert.Equal(14, dashboard.DailyTrendData.Count);
        Assert.NotEmpty(dashboard.TopSessionsLabels);
    }

    [Fact]
    public async Task BuildDashboardAsync_ReturnsZeroCounters_WhenVisitsAbsent()
    {
        await using var context = CreateContext(nameof(BuildDashboardAsync_ReturnsZeroCounters_WhenVisitsAbsent));
        context.ClubMembers.Add(new ClubMember
        {
            FirstName = "Мария",
            LastName = "Соколова",
            MembershipType = "Премиум",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(1),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(1, dashboard.TotalMembers);
        Assert.Equal(1, dashboard.ActiveMembers);
        Assert.Equal(0, dashboard.VisitsToday);
        Assert.Equal(0, dashboard.VisitsThisMonth);
        Assert.All(dashboard.DailyTrendData, item => Assert.Equal(0, item));
    }

    [Fact]
    public async Task BuildDashboardAsync_LimitsTopSessionsToSixItems()
    {
        await using var context = CreateContext(nameof(BuildDashboardAsync_LimitsTopSessionsToSixItems));

        var member = new ClubMember
        {
            FirstName = "Никита",
            LastName = "Орехов",
            MembershipType = "Стандарт",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(2),
            IsActive = true
        };

        context.ClubMembers.Add(member);
        await context.SaveChangesAsync();

        var sessions = Enumerable.Range(1, 8)
            .Select(index => new WorkoutSession
            {
                Title = $"Тренировка {index}",
                TrainerName = "Тренер",
                StartsAt = DateTime.Today.AddDays(-index).AddHours(10),
                Capacity = 20
            })
            .ToList();

        context.WorkoutSessions.AddRange(sessions);
        await context.SaveChangesAsync();

        foreach (var session in sessions)
        {
            context.Visits.Add(new Visit
            {
                ClubMemberId = member.Id,
                WorkoutSessionId = session.Id,
                CheckInAt = DateTime.Today.AddDays(-1).AddHours(9),
                CheckInMethod = "Турникет"
            });
        }

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);
        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(6, dashboard.TopSessionsLabels.Count);
        Assert.Equal(6, dashboard.TopSessionsData.Count);
    }

    private static ClubAttendanceContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ClubAttendanceContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ClubAttendanceContext(options);
    }

    private static void SeedTestData(ClubAttendanceContext context)
    {
        var member1 = new ClubMember
        {
            FirstName = "Иван",
            LastName = "Иванов",
            MembershipType = "Стандарт",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(2),
            IsActive = true
        };

        var member2 = new ClubMember
        {
            FirstName = "Петр",
            LastName = "Петров",
            MembershipType = "Утренний",
            MembershipStartDate = DateTime.Today.AddMonths(-6),
            MembershipEndDate = DateTime.Today.AddMonths(-2),
            IsActive = false
        };

        var session = new WorkoutSession
        {
            Title = "Кроссфит",
            TrainerName = "Тренер Тест",
            StartsAt = DateTime.Today.AddHours(18),
            Capacity = 15
        };

        context.ClubMembers.AddRange(member1, member2);
        context.WorkoutSessions.Add(session);
        context.SaveChanges();

        context.Visits.AddRange(
            new Visit
            {
                ClubMemberId = member1.Id,
                WorkoutSessionId = session.Id,
                CheckInAt = DateTime.Today.AddHours(10),
                CheckInMethod = "Администратор"
            },
            new Visit
            {
                ClubMemberId = member1.Id,
                WorkoutSessionId = session.Id,
                CheckInAt = DateTime.Today.AddDays(-3).AddHours(9),
                CheckInMethod = "Турникет"
            },
            new Visit
            {
                ClubMemberId = member2.Id,
                CheckInAt = DateTime.Today.AddDays(-2).AddHours(8),
                CheckInMethod = "Турникет"
            });

        context.SaveChanges();
    }
}

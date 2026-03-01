using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;
using SalutClubAttendance.Web.Services;

namespace SalutClubAttendance.Tests;

public class VisitValidationServiceTests
{
    [Fact]
    public async Task ValidateForCreateAsync_ReturnsValidResult_WhenVisitDataIsCorrect()
    {
        await using var context = await CreateContextWithMemberAndSessionAsync(nameof(ValidateForCreateAsync_ReturnsValidResult_WhenVisitDataIsCorrect));
        var service = new VisitValidationService(context);
        var sessionId = await context.WorkoutSessions.Select(item => item.Id).SingleAsync();
        var memberId = await context.ClubMembers.Select(item => item.Id).SingleAsync();

        var visit = new Visit
        {
            ClubMemberId = memberId,
            WorkoutSessionId = sessionId,
            CheckInAt = DateTime.Today.AddHours(10),
            CheckInMethod = "Администратор"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenMemberMissing()
    {
        await using var context = await CreateContextWithMemberAndSessionAsync(nameof(ValidateForCreateAsync_ReturnsIssue_WhenMemberMissing));
        var service = new VisitValidationService(context);
        var sessionId = await context.WorkoutSessions.Select(item => item.Id).SingleAsync();

        var visit = new Visit
        {
            ClubMemberId = 99999,
            WorkoutSessionId = sessionId,
            CheckInAt = DateTime.Today.AddHours(11),
            CheckInMethod = "Турникет"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.ClubMemberId));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenMembershipExpired()
    {
        await using var context = CreateContext(nameof(ValidateForCreateAsync_ReturnsIssue_WhenMembershipExpired));
        var member = new ClubMember
        {
            FirstName = "Игорь",
            LastName = "Соколов",
            MembershipType = "Стандарт",
            MembershipStartDate = DateTime.Today.AddMonths(-6),
            MembershipEndDate = DateTime.Today.AddDays(-2),
            IsActive = false
        };

        var session = new WorkoutSession
        {
            Title = "Йога",
            TrainerName = "Тренер",
            StartsAt = DateTime.Today.AddHours(9),
            Capacity = 20
        };

        context.ClubMembers.Add(member);
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        var service = new VisitValidationService(context);

        var visit = new Visit
        {
            ClubMemberId = member.Id,
            WorkoutSessionId = session.Id,
            CheckInAt = DateTime.Today.AddHours(9),
            CheckInMethod = "Администратор"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.ClubMemberId));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenSessionMissing()
    {
        await using var context = CreateContext(nameof(ValidateForCreateAsync_ReturnsIssue_WhenSessionMissing));
        var member = new ClubMember
        {
            FirstName = "Анна",
            LastName = "Карпова",
            MembershipType = "Премиум",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(1),
            IsActive = true
        };

        context.ClubMembers.Add(member);
        await context.SaveChangesAsync();

        var service = new VisitValidationService(context);
        var visit = new Visit
        {
            ClubMemberId = member.Id,
            WorkoutSessionId = 777,
            CheckInAt = DateTime.Today.AddHours(13),
            CheckInMethod = "Турникет"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.WorkoutSessionId));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenSessionDateMismatched()
    {
        await using var context = await CreateContextWithMemberAndSessionAsync(nameof(ValidateForCreateAsync_ReturnsIssue_WhenSessionDateMismatched));
        var service = new VisitValidationService(context);
        var sessionId = await context.WorkoutSessions.Select(item => item.Id).SingleAsync();
        var memberId = await context.ClubMembers.Select(item => item.Id).SingleAsync();

        var visit = new Visit
        {
            ClubMemberId = memberId,
            WorkoutSessionId = sessionId,
            CheckInAt = DateTime.Today.AddDays(1).AddHours(10),
            CheckInMethod = "Администратор"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.WorkoutSessionId));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenSessionCapacityExceeded()
    {
        await using var context = CreateContext(nameof(ValidateForCreateAsync_ReturnsIssue_WhenSessionCapacityExceeded));
        var member1 = new ClubMember
        {
            FirstName = "Иван",
            LastName = "Романов",
            MembershipType = "Стандарт",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(2),
            IsActive = true
        };
        var member2 = new ClubMember
        {
            FirstName = "Олег",
            LastName = "Серов",
            MembershipType = "Стандарт",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(2),
            IsActive = true
        };
        var session = new WorkoutSession
        {
            Title = "Функциональная тренировка",
            TrainerName = "Тренер",
            StartsAt = DateTime.Today.AddHours(19),
            Capacity = 1
        };

        context.ClubMembers.AddRange(member1, member2);
        context.WorkoutSessions.Add(session);
        await context.SaveChangesAsync();

        context.Visits.Add(new Visit
        {
            ClubMemberId = member1.Id,
            WorkoutSessionId = session.Id,
            CheckInAt = DateTime.Today.AddHours(19),
            CheckInMethod = "Турникет"
        });
        await context.SaveChangesAsync();

        var service = new VisitValidationService(context);

        var nextVisit = new Visit
        {
            ClubMemberId = member2.Id,
            WorkoutSessionId = session.Id,
            CheckInAt = DateTime.Today.AddHours(19),
            CheckInMethod = "Администратор"
        };

        var result = await service.ValidateForCreateAsync(nextVisit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.WorkoutSessionId));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenCheckInMethodInvalid()
    {
        await using var context = await CreateContextWithMemberAndSessionAsync(nameof(ValidateForCreateAsync_ReturnsIssue_WhenCheckInMethodInvalid));
        var service = new VisitValidationService(context);
        var sessionId = await context.WorkoutSessions.Select(item => item.Id).SingleAsync();
        var memberId = await context.ClubMembers.Select(item => item.Id).SingleAsync();

        var visit = new Visit
        {
            ClubMemberId = memberId,
            WorkoutSessionId = sessionId,
            CheckInAt = DateTime.Today.AddHours(10),
            CheckInMethod = "QR"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.CheckInMethod));
    }

    [Fact]
    public async Task ValidateForCreateAsync_ReturnsIssue_WhenDateOutOfAllowedRange()
    {
        await using var context = await CreateContextWithMemberAndSessionAsync(nameof(ValidateForCreateAsync_ReturnsIssue_WhenDateOutOfAllowedRange));
        var service = new VisitValidationService(context);
        var sessionId = await context.WorkoutSessions.Select(item => item.Id).SingleAsync();
        var memberId = await context.ClubMembers.Select(item => item.Id).SingleAsync();

        var visit = new Visit
        {
            ClubMemberId = memberId,
            WorkoutSessionId = sessionId,
            CheckInAt = DateTime.Today.AddDays(-400),
            CheckInMethod = "Мобильное приложение"
        };

        var result = await service.ValidateForCreateAsync(visit);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, item => item.PropertyName == nameof(Visit.CheckInAt));
    }

    private static ClubAttendanceContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ClubAttendanceContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ClubAttendanceContext(options);
    }

    private static async Task<ClubAttendanceContext> CreateContextWithMemberAndSessionAsync(string databaseName)
    {
        var context = CreateContext(databaseName);

        context.ClubMembers.Add(new ClubMember
        {
            FirstName = "Максим",
            LastName = "Орлов",
            MembershipType = "Премиум",
            MembershipStartDate = DateTime.Today.AddMonths(-1),
            MembershipEndDate = DateTime.Today.AddMonths(3),
            IsActive = true
        });

        context.WorkoutSessions.Add(new WorkoutSession
        {
            Title = "Силовой класс",
            TrainerName = "Тренер Тест",
            StartsAt = DateTime.Today.AddHours(10),
            Capacity = 12
        });

        await context.SaveChangesAsync();
        return context;
    }
}

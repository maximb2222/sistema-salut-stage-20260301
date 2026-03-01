using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Services;

/// <summary>
/// Проверяет бизнес-правила перед созданием записи посещения.
/// </summary>
public class VisitValidationService(ClubAttendanceContext context) : IVisitValidationService
{
    private static readonly HashSet<string> AllowedCheckInMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Турникет",
        "Администратор",
        "Мобильное приложение"
    };

    public async Task<VisitValidationResult> ValidateForCreateAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        var result = new VisitValidationResult();

        if (visit.CheckInAt < DateTime.Today.AddDays(-365) || visit.CheckInAt > DateTime.Today.AddDays(7))
        {
            result.AddIssue(nameof(visit.CheckInAt), "Дата посещения должна быть в диапазоне от 365 дней назад до 7 дней вперед.");
        }

        if (string.IsNullOrWhiteSpace(visit.CheckInMethod) || !AllowedCheckInMethods.Contains(visit.CheckInMethod))
        {
            result.AddIssue(nameof(visit.CheckInMethod), "Выберите корректный способ регистрации посещения.");
        }

        var member = await context.ClubMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == visit.ClubMemberId, cancellationToken);

        if (member is null)
        {
            result.AddIssue(nameof(visit.ClubMemberId), "Выбранный клиент не найден.");
            return result;
        }

        if (member.MembershipEndDate.Date < visit.CheckInAt.Date)
        {
            result.AddIssue(nameof(visit.ClubMemberId), "Абонемент клиента истек на дату посещения.");
        }

        if (visit.WorkoutSessionId is null)
        {
            return result;
        }

        var session = await context.WorkoutSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == visit.WorkoutSessionId.Value, cancellationToken);

        if (session is null)
        {
            result.AddIssue(nameof(visit.WorkoutSessionId), "Выбранная тренировка не найдена.");
            return result;
        }

        if (session.StartsAt.Date != visit.CheckInAt.Date)
        {
            result.AddIssue(nameof(visit.WorkoutSessionId), "Посещение с тренировкой должно совпадать по дате с расписанием тренировки.");
        }

        var bookedPlaces = await context.Visits
            .CountAsync(item => item.WorkoutSessionId == session.Id, cancellationToken);

        if (bookedPlaces >= session.Capacity)
        {
            result.AddIssue(nameof(visit.WorkoutSessionId), "Превышена вместимость выбранной тренировки.");
        }

        return result;
    }
}

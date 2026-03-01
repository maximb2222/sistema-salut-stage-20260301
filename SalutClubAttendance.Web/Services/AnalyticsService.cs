using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models.ViewModels;

namespace SalutClubAttendance.Web.Services;

/// <summary>
/// Сервис подготовки аналитики для дашборда.
/// </summary>
public class AnalyticsService(ClubAttendanceContext context) : IAnalyticsService
{
    public async Task<DashboardViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalMembersTask = context.ClubMembers.CountAsync(cancellationToken);
        var activeMembersTask = context.ClubMembers.CountAsync(member => member.IsActive, cancellationToken);
        var visitsTodayTask = context.Visits.CountAsync(visit => visit.CheckInAt.Date == today, cancellationToken);
        var visitsMonthTask = context.Visits.CountAsync(visit => visit.CheckInAt >= monthStart, cancellationToken);

        await Task.WhenAll(totalMembersTask, activeMembersTask, visitsTodayTask, visitsMonthTask);

        var trendStart = today.AddDays(-13);
        var trendRaw = await context.Visits
            .Where(visit => visit.CheckInAt >= trendStart)
            .GroupBy(visit => visit.CheckInAt.Date)
            .Select(group => new
            {
                Day = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var trendDictionary = trendRaw.ToDictionary(item => item.Day, item => item.Count);
        var trendLabels = new List<string>();
        var trendData = new List<int>();

        for (var cursor = trendStart; cursor <= today; cursor = cursor.AddDays(1))
        {
            trendLabels.Add(cursor.ToString("dd.MM"));
            trendData.Add(trendDictionary.GetValueOrDefault(cursor, 0));
        }

        var membershipRaw = await context.Visits
            .Where(visit => visit.CheckInAt >= monthStart)
            .Include(visit => visit.ClubMember)
            .GroupBy(visit => visit.ClubMember!.MembershipType)
            .Select(group => new
            {
                MembershipType = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        var topSessionsRaw = await context.Visits
            .Where(visit => visit.CheckInAt >= today.AddDays(-30) && visit.WorkoutSessionId != null)
            .Include(visit => visit.WorkoutSession)
            .GroupBy(visit => visit.WorkoutSession!.Title)
            .Select(group => new
            {
                Session = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .Take(6)
            .ToListAsync(cancellationToken);

        return new DashboardViewModel
        {
            TotalMembers = totalMembersTask.Result,
            ActiveMembers = activeMembersTask.Result,
            VisitsToday = visitsTodayTask.Result,
            VisitsThisMonth = visitsMonthTask.Result,
            DailyTrendLabels = trendLabels,
            DailyTrendData = trendData,
            MembershipLabels = membershipRaw.Select(item => item.MembershipType).ToList(),
            MembershipData = membershipRaw.Select(item => item.Count).ToList(),
            TopSessionsLabels = topSessionsRaw.Select(item => item.Session).ToList(),
            TopSessionsData = topSessionsRaw.Select(item => item.Count).ToList()
        };
    }
}

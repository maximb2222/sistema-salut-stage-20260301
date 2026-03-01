namespace SalutClubAttendance.Web.Models.ViewModels;

/// <summary>
/// Сводные показатели для главной аналитической панели.
/// </summary>
public class DashboardViewModel
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public int VisitsToday { get; set; }
    public int VisitsThisMonth { get; set; }

    public IReadOnlyList<string> DailyTrendLabels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> DailyTrendData { get; set; } = Array.Empty<int>();

    public IReadOnlyList<string> MembershipLabels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> MembershipData { get; set; } = Array.Empty<int>();

    public IReadOnlyList<string> TopSessionsLabels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> TopSessionsData { get; set; } = Array.Empty<int>();
}

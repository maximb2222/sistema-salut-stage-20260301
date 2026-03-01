using SalutClubAttendance.Web.Models.ViewModels;

namespace SalutClubAttendance.Web.Services;

public interface IAnalyticsService
{
    Task<DashboardViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default);
}

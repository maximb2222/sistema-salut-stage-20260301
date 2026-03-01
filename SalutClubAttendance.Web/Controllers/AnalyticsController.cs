using Microsoft.AspNetCore.Mvc;
using SalutClubAttendance.Web.Models.ViewModels;
using SalutClubAttendance.Web.Services;

namespace SalutClubAttendance.Web.Controllers;

/// <summary>
/// Отвечает за вывод аналитики посещаемости.
/// </summary>
public class AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var model = await analyticsService.BuildDashboardAsync(cancellationToken);
            return View(model);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Dashboard analytics load failed.");
            TempData["ErrorMessage"] = "Не удалось загрузить аналитические данные. Попробуйте обновить страницу.";
            return View(new DashboardViewModel());
        }
    }
}

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;
using SalutClubAttendance.Web.Services;

namespace SalutClubAttendance.Web.Controllers;

/// <summary>
/// Управление фактами посещения клуба.
/// </summary>
public class VisitsController(
    ClubAttendanceContext context,
    ILogger<VisitsController> logger,
    IVisitValidationService visitValidationService) : Controller
{
    /// <summary>
    /// Отображает журнал посещений с фильтрацией по клиенту и периоду.
    /// </summary>
    public async Task<IActionResult> Index(int? memberId, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        if (!IsDateRangeValid(dateFrom, dateTo))
        {
            TempData["ErrorMessage"] = "Период фильтра задан некорректно: дата \"с\" не может быть позже даты \"по\".";
            dateFrom = null;
            dateTo = null;
        }

        var visits = await BuildFilteredQuery(memberId, dateFrom, dateTo)
            .OrderByDescending(visit => visit.CheckInAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        ViewData["MemberId"] = memberId;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        await PopulateMembersAsync(memberId, cancellationToken);

        return View(visits);
    }

    /// <summary>
    /// Выгружает отфильтрованный журнал посещений в CSV-файл.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(int? memberId, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        if (!IsDateRangeValid(dateFrom, dateTo))
        {
            TempData["ErrorMessage"] = "CSV не сформирован: период фильтра задан некорректно.";
            return RedirectToAction(nameof(Index), new { memberId, dateFrom, dateTo });
        }

        try
        {
            var visits = await BuildFilteredQuery(memberId, dateFrom, dateTo)
                .OrderByDescending(visit => visit.CheckInAt)
                .Take(5000)
                .ToListAsync(cancellationToken);

            var csv = BuildCsv(visits);
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv))
                .ToArray();

            var fileName = $"visits_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to export visits CSV.");
            TempData["ErrorMessage"] = "Ошибка при формировании CSV-отчета.";
            return RedirectToAction(nameof(Index), new { memberId, dateFrom, dateTo });
        }
    }

    /// <summary>
    /// Форма ручной регистрации посещения.
    /// </summary>
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateDropDownsAsync(null, null, cancellationToken);
        return View(new Visit { CheckInAt = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Visit visit, CancellationToken cancellationToken)
    {
        var validationResult = await visitValidationService.ValidateForCreateAsync(visit, cancellationToken);
        foreach (var issue in validationResult.Issues)
        {
            ModelState.AddModelError(issue.PropertyName, issue.ErrorMessage);
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(visit.ClubMemberId, visit.WorkoutSessionId, cancellationToken);
            return View(visit);
        }

        try
        {
            context.Add(visit);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Посещение успешно зарегистрировано.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create visit.");
            TempData["ErrorMessage"] = "Ошибка регистрации посещения.";
            await PopulateDropDownsAsync(visit.ClubMemberId, visit.WorkoutSessionId, cancellationToken);
            return View(visit);
        }
    }

    /// <summary>
    /// Подтверждение удаления записи посещения.
    /// </summary>
    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var visit = await context.Visits
            .Include(item => item.ClubMember)
            .Include(item => item.WorkoutSession)
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (visit is null)
        {
            return NotFound();
        }

        return View(visit);
    }

    /// <summary>
    /// Удаляет запись посещения после подтверждения.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var visit = await context.Visits.FindAsync([id], cancellationToken);
            if (visit is null)
            {
                return NotFound();
            }

            context.Visits.Remove(visit);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Запись посещения удалена.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to delete visit #{VisitId}.", id);
            TempData["ErrorMessage"] = "Ошибка при удалении записи посещения.";
            return RedirectToAction(nameof(Index));
        }
    }

    private IQueryable<Visit> BuildFilteredQuery(int? memberId, DateTime? dateFrom, DateTime? dateTo)
    {
        IQueryable<Visit> query = context.Visits
            .Include(visit => visit.ClubMember)
            .Include(visit => visit.WorkoutSession);

        if (memberId.HasValue)
        {
            query = query.Where(visit => visit.ClubMemberId == memberId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(visit => visit.CheckInAt >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var exclusiveEnd = dateTo.Value.Date.AddDays(1);
            query = query.Where(visit => visit.CheckInAt < exclusiveEnd);
        }

        return query;
    }

    private static bool IsDateRangeValid(DateTime? dateFrom, DateTime? dateTo)
    {
        return !dateFrom.HasValue || !dateTo.HasValue || dateFrom.Value.Date <= dateTo.Value.Date;
    }

    private static string BuildCsv(IEnumerable<Visit> visits)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id;Дата и время;Клиент;Тренировка;Способ регистрации;Комментарий");

        foreach (var visit in visits)
        {
            var fullName = visit.ClubMember?.FullName ?? string.Empty;
            var sessionName = visit.WorkoutSession?.Title ?? "Самостоятельная тренировка";
            var notes = visit.Notes ?? string.Empty;

            builder.Append(visit.Id).Append(';')
                .Append(EscapeCsv(visit.CheckInAt.ToString("dd.MM.yyyy HH:mm"))).Append(';')
                .Append(EscapeCsv(fullName)).Append(';')
                .Append(EscapeCsv(sessionName)).Append(';')
                .Append(EscapeCsv(visit.CheckInMethod)).Append(';')
                .Append(EscapeCsv(notes))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private async Task PopulateMembersAsync(int? selectedMemberId, CancellationToken cancellationToken)
    {
        var members = await context.ClubMembers
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .Select(member => new
            {
                member.Id,
                Display = member.LastName + " " + member.FirstName
            })
            .ToListAsync(cancellationToken);

        ViewBag.Members = new SelectList(members, "Id", "Display", selectedMemberId);
    }

    private async Task PopulateDropDownsAsync(int? selectedMemberId, int? selectedSessionId, CancellationToken cancellationToken)
    {
        await PopulateMembersAsync(selectedMemberId, cancellationToken);

        var sessions = await context.WorkoutSessions
            .Where(session => session.StartsAt >= DateTime.Today.AddDays(-7) &&
                              session.StartsAt <= DateTime.Today.AddDays(30))
            .OrderBy(session => session.StartsAt)
            .Select(session => new
            {
                session.Id,
                Display = session.StartsAt.ToString("dd.MM.yyyy HH:mm") + " - " + session.Title
            })
            .ToListAsync(cancellationToken);

        ViewBag.Sessions = new SelectList(sessions, "Id", "Display", selectedSessionId);
    }
}

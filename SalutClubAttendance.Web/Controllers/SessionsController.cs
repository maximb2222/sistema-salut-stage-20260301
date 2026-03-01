using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Controllers;

/// <summary>
/// Управление расписанием тренировок.
/// </summary>
public class SessionsController(ClubAttendanceContext context, ILogger<SessionsController> logger) : Controller
{
    public async Task<IActionResult> Index(string? search, bool onlyUpcoming = false, CancellationToken cancellationToken = default)
    {
        IQueryable<WorkoutSession> query = context.WorkoutSessions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(session =>
                EF.Functions.Like(session.Title, $"%{normalizedSearch}%") ||
                EF.Functions.Like(session.TrainerName, $"%{normalizedSearch}%"));
        }

        if (onlyUpcoming)
        {
            query = query.Where(session => session.StartsAt >= DateTime.Today);
        }

        var sessions = await query
            .OrderBy(session => session.StartsAt)
            .ToListAsync(cancellationToken);

        ViewData["Search"] = search;
        ViewData["OnlyUpcoming"] = onlyUpcoming;
        return View(sessions);
    }

    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var session = await context.WorkoutSessions
            .Include(item => item.Visits)
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        return View(session);
    }

    public IActionResult Create()
    {
        return View(new WorkoutSession
        {
            StartsAt = DateTime.Now.AddHours(2),
            Capacity = 20
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkoutSession session, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(session);
        }

        try
        {
            context.Add(session);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Тренировка успешно добавлена.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create session.");
            TempData["ErrorMessage"] = "Ошибка при создании тренировки.";
            return View(session);
        }
    }

    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var session = await context.WorkoutSessions.FindAsync([id.Value], cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkoutSession session, CancellationToken cancellationToken)
    {
        if (id != session.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(session);
        }

        try
        {
            context.Update(session);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Тренировка обновлена.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SessionExistsAsync(session.Id, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to update session #{SessionId}.", session.Id);
            TempData["ErrorMessage"] = "Ошибка при обновлении тренировки.";
            return View(session);
        }
    }

    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var session = await context.WorkoutSessions
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        return View(session);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await context.WorkoutSessions.FindAsync([id], cancellationToken);
            if (session is null)
            {
                return NotFound();
            }

            context.WorkoutSessions.Remove(session);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Тренировка удалена.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to delete session #{SessionId}.", id);
            TempData["ErrorMessage"] = "Ошибка удаления тренировки.";
            return RedirectToAction(nameof(Index));
        }
    }

    private Task<bool> SessionExistsAsync(int id, CancellationToken cancellationToken)
    {
        return context.WorkoutSessions.AnyAsync(session => session.Id == id, cancellationToken);
    }
}

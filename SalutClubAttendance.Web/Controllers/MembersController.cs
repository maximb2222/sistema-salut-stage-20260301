using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalutClubAttendance.Web.Data;
using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Controllers;

/// <summary>
/// CRUD-операции по клиентам клуба.
/// </summary>
public class MembersController(ClubAttendanceContext context, ILogger<MembersController> logger) : Controller
{
    public async Task<IActionResult> Index(string? search, bool? isActive, CancellationToken cancellationToken)
    {
        IQueryable<ClubMember> query = context.ClubMembers;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(member =>
                EF.Functions.Like(member.FirstName, $"%{normalizedSearch}%") ||
                EF.Functions.Like(member.LastName, $"%{normalizedSearch}%") ||
                EF.Functions.Like(member.PhoneNumber!, $"%{normalizedSearch}%"));
        }

        if (isActive.HasValue)
        {
            query = query.Where(member => member.IsActive == isActive.Value);
        }

        var members = await query
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync(cancellationToken);

        ViewData["Search"] = search;
        ViewData["IsActive"] = isActive;
        return View(members);
    }

    public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var member = await context.ClubMembers
            .Include(item => item.Visits)
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (member is null)
        {
            return NotFound();
        }

        return View(member);
    }

    public IActionResult Create()
    {
        return View(new ClubMember
        {
            MembershipStartDate = DateTime.Today,
            MembershipEndDate = DateTime.Today.AddMonths(3),
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClubMember member, CancellationToken cancellationToken)
    {
        ValidateMembershipDates(member);

        if (!ModelState.IsValid)
        {
            return View(member);
        }

        try
        {
            member.RegisteredAt = DateTime.UtcNow;
            context.Add(member);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Клиент успешно добавлен.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create a member record.");
            TempData["ErrorMessage"] = "Ошибка сохранения клиента.";
            return View(member);
        }
    }

    public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var member = await context.ClubMembers.FindAsync([id.Value], cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        return View(member);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClubMember member, CancellationToken cancellationToken)
    {
        if (id != member.Id)
        {
            return NotFound();
        }

        ValidateMembershipDates(member);

        if (!ModelState.IsValid)
        {
            return View(member);
        }

        try
        {
            context.Update(member);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Изменения по клиенту сохранены.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await MemberExistsAsync(member.Id, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to update member #{MemberId}.", member.Id);
            TempData["ErrorMessage"] = "Ошибка при обновлении клиента.";
            return View(member);
        }
    }

    public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return NotFound();
        }

        var member = await context.ClubMembers
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (member is null)
        {
            return NotFound();
        }

        return View(member);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var member = await context.ClubMembers.FindAsync([id], cancellationToken);
            if (member is null)
            {
                return NotFound();
            }

            context.ClubMembers.Remove(member);
            await context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Клиент удален.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to delete member #{MemberId}.", id);
            TempData["ErrorMessage"] = "Ошибка удаления клиента.";
            return RedirectToAction(nameof(Index));
        }
    }

    private void ValidateMembershipDates(ClubMember member)
    {
        if (member.MembershipEndDate < member.MembershipStartDate)
        {
            ModelState.AddModelError(nameof(member.MembershipEndDate), "Дата окончания не может быть раньше даты начала.");
        }
    }

    private Task<bool> MemberExistsAsync(int id, CancellationToken cancellationToken)
    {
        return context.ClubMembers.AnyAsync(member => member.Id == id, cancellationToken);
    }
}

using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Services;

/// <summary>
/// Бизнес-валидация входящих данных по посещениям.
/// </summary>
public interface IVisitValidationService
{
    Task<VisitValidationResult> ValidateForCreateAsync(Visit visit, CancellationToken cancellationToken = default);
}

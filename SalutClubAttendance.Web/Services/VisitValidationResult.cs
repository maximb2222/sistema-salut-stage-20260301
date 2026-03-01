namespace SalutClubAttendance.Web.Services;

/// <summary>
/// Результат проверки данных для регистрации посещения.
/// </summary>
public sealed class VisitValidationResult
{
    private readonly List<ValidationIssue> issues = [];

    public IReadOnlyList<ValidationIssue> Issues => issues;

    public bool IsValid => issues.Count == 0;

    public void AddIssue(string propertyName, string errorMessage)
    {
        issues.Add(new ValidationIssue(propertyName, errorMessage));
    }
}

/// <summary>
/// Отдельная ошибка валидации.
/// </summary>
public sealed record ValidationIssue(string PropertyName, string ErrorMessage);

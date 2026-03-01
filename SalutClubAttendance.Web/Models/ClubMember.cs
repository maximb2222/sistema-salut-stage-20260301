using System.ComponentModel.DataAnnotations;

namespace SalutClubAttendance.Web.Models;

/// <summary>
/// Карточка клиента спортивного клуба.
/// </summary>
public class ClubMember
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите имя")]
    [StringLength(80)]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите фамилию")]
    [StringLength(80)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }

    [StringLength(120)]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Укажите тип абонемента")]
    [StringLength(50)]
    [Display(Name = "Тип абонемента")]
    public string MembershipType { get; set; } = "Стандарт";

    [DataType(DataType.Date)]
    [Display(Name = "Начало абонемента")]
    public DateTime MembershipStartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Окончание абонемента")]
    public DateTime MembershipEndDate { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Дата регистрации")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public string FullName => $"{LastName} {FirstName}".Trim();
}

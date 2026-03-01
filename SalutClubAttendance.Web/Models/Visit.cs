using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalutClubAttendance.Web.Models;

/// <summary>
/// Факт посещения клуба клиентом.
/// </summary>
public class Visit
{
    public int Id { get; set; }

    [Display(Name = "Клиент")]
    public int ClubMemberId { get; set; }

    [Display(Name = "Тренировка")]
    public int? WorkoutSessionId { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Время входа")]
    public DateTime CheckInAt { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Укажите способ регистрации")]
    [StringLength(50)]
    [Display(Name = "Способ регистрации")]
    public string CheckInMethod { get; set; } = "Администратор";

    [StringLength(250)]
    [Display(Name = "Комментарий")]
    public string? Notes { get; set; }

    [ForeignKey(nameof(ClubMemberId))]
    public ClubMember? ClubMember { get; set; }

    [ForeignKey(nameof(WorkoutSessionId))]
    public WorkoutSession? WorkoutSession { get; set; }
}

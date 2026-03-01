using System.ComponentModel.DataAnnotations;

namespace SalutClubAttendance.Web.Models;

/// <summary>
/// Тренировочная сессия, на которую могут записываться клиенты.
/// </summary>
public class WorkoutSession
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название тренировки")]
    [StringLength(100)]
    [Display(Name = "Тренировка")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите тренера")]
    [StringLength(120)]
    [Display(Name = "Тренер")]
    public string TrainerName { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    [Display(Name = "Начало")]
    public DateTime StartsAt { get; set; }

    [Range(1, 500, ErrorMessage = "Вместимость должна быть от 1 до 500")]
    [Display(Name = "Вместимость")]
    public int Capacity { get; set; } = 20;

    [StringLength(200)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}

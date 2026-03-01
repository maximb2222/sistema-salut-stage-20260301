using SalutClubAttendance.Web.Models;

namespace SalutClubAttendance.Web.Data;

/// <summary>
/// Наполняет БД стартовыми данными для демонстрации.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(ClubAttendanceContext context)
    {
        context.Database.EnsureCreated();

        if (context.ClubMembers.Any())
        {
            return;
        }

        var random = new Random(2026);

        var firstNames = new[]
        {
            "Алексей", "Иван", "Дмитрий", "Максим", "Павел", "Сергей", "Андрей", "Николай",
            "Екатерина", "Ольга", "Марина", "Светлана", "Анна", "Ирина", "Елена", "Дарья"
        };

        var lastNames = new[]
        {
            "Иванов", "Петров", "Сидоров", "Кузнецов", "Попов", "Смирнов", "Волков", "Федоров",
            "Лебедев", "Михайлов", "Новиков", "Морозов", "Соловьев", "Васильев", "Зайцев", "Павлов"
        };

        var membershipTypes = new[] { "Стандарт", "Премиум", "Утренний", "Безлимит" };

        var members = new List<ClubMember>();
        for (var index = 0; index < 120; index++)
        {
            var startDate = DateTime.Today.AddDays(-random.Next(5, 260));
            var type = membershipTypes[random.Next(membershipTypes.Length)];
            var durationMonths = type == "Премиум" ? 6 : 3;

            members.Add(new ClubMember
            {
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                PhoneNumber = $"+7 900 {random.Next(100, 999)} {random.Next(10, 99)} {random.Next(10, 99)}",
                Email = $"member{index + 1}@salutclub.local",
                MembershipType = type,
                MembershipStartDate = startDate,
                MembershipEndDate = startDate.AddMonths(durationMonths),
                IsActive = startDate.AddMonths(durationMonths) >= DateTime.Today,
                RegisteredAt = startDate.AddDays(-random.Next(1, 10))
            });
        }

        context.ClubMembers.AddRange(members);
        context.SaveChanges();

        var templates = new[]
        {
            new { Title = "Функциональный тренинг", Trainer = "Семен Чернов", Capacity = 18, Hour = 8 },
            new { Title = "Йога", Trainer = "Мария Орлова", Capacity = 16, Hour = 10 },
            new { Title = "Кроссфит", Trainer = "Егор Виноградов", Capacity = 14, Hour = 18 },
            new { Title = "Силовой класс", Trainer = "Никита Соловьев", Capacity = 20, Hour = 20 }
        };

        var sessions = new List<WorkoutSession>();
        for (var dayOffset = -30; dayOffset <= 30; dayOffset++)
        {
            var currentDay = DateTime.Today.AddDays(dayOffset);
            foreach (var template in templates)
            {
                sessions.Add(new WorkoutSession
                {
                    Title = template.Title,
                    TrainerName = template.Trainer,
                    StartsAt = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, template.Hour, 0, 0),
                    Capacity = template.Capacity,
                    Description = "Плановая групповая тренировка по расписанию клуба \"Салют\"."
                });
            }
        }

        context.WorkoutSessions.AddRange(sessions);
        context.SaveChanges();

        var visitMethods = new[] { "Турникет", "Администратор", "Мобильное приложение" };
        var visits = new List<Visit>();

        var memberIds = context.ClubMembers.Select(member => member.Id).ToArray();
        var sessionsByDate = context.WorkoutSessions
            .ToList()
            .GroupBy(session => session.StartsAt.Date)
            .ToDictionary(group => group.Key, group => group.Select(session => session.Id).ToList());

        for (var dayOffset = -60; dayOffset <= 0; dayOffset++)
        {
            var date = DateTime.Today.AddDays(dayOffset).Date;
            var visitsPerDay = random.Next(25, 60);

            for (var count = 0; count < visitsPerDay; count++)
            {
                var memberId = memberIds[random.Next(memberIds.Length)];

                sessionsByDate.TryGetValue(date, out var sessionIdsForDay);
                int? sessionId = sessionIdsForDay is { Count: > 0 } && random.NextDouble() > 0.2
                    ? sessionIdsForDay[random.Next(sessionIdsForDay.Count)]
                    : null;

                var hour = random.Next(7, 22);
                var minute = random.Next(0, 60);

                visits.Add(new Visit
                {
                    ClubMemberId = memberId,
                    WorkoutSessionId = sessionId,
                    CheckInAt = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0),
                    CheckInMethod = visitMethods[random.Next(visitMethods.Length)],
                    Notes = random.NextDouble() > 0.92 ? "Индивидуальная консультация с тренером" : null
                });
            }
        }

        context.Visits.AddRange(visits);
        context.SaveChanges();
    }
}

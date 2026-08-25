using Microsoft.EntityFrameworkCore;
using Tasks.App.Data;
using Tasks.App.Models;

namespace Tasks.App.Services;

public class DailyTaskWithStatus
{
    public DailyTask Task { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<DailyChecklistWithStatus> Checklists { get; set; } = new();
}

public class DailyChecklistWithStatus
{
    public DailyChecklistItem Item { get; set; } = null!;
    public bool IsCompleted { get; set; }
}

public class DayOverview
{
    public DateTime Date { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public bool IsAllCompleted => TotalTasks > 0 && TotalTasks == CompletedTasks;
    public bool HasTasks => TotalTasks > 0;
}

public interface IDailyTaskService
{
    Task InitializeForUserAsync(int userId);
    Task<List<DailyTaskWithStatus>> GetTasksForDateAsync(DateTime targetDate, int userId);
    Task<Dictionary<DateTime, DayOverview>> GetMonthOverviewAsync(int year, int month, int userId);
    Task<DailyTask> SaveDailyTaskAsync(DailyTask task, List<string> checklistTitles, int userId);
    Task ToggleTaskCompletionAsync(int taskId, DateTime date, bool isCompleted);
    Task ToggleChecklistCompletionAsync(int checklistItemId, DateTime date, bool isCompleted);
    Task DeleteDailyTaskAsync(int taskId);
}

public class DailyTaskService : IDailyTaskService
{
    private readonly AppDbContext _context;

    public DailyTaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeForUserAsync(int userId)
    {
        await _context.EnsureTablesCreatedAsync();

        if (!await _context.DailyTasks.AnyAsync(t => t.UserId == userId))
        {
            var today = DateTime.Today;

            var welcomeDailyTask = new DailyTask
            {
                UserId = userId,
                Title = "Planejar o dia e revisar prioridades 📝",
                Description = "Organize suas principais tarefas e objetivos de hoje.",
                Priority = TaskPriority.Alta,
                Recurrence = RecurrenceType.TodosOsDias,
                StartDate = today,
                CreatedAt = DateTime.UtcNow,
                Checklists = new List<DailyChecklistItem>
                {
                    new() { Title = "Definir a meta principal do dia", Order = 0 },
                    new() { Title = "Checar mensagens ou e-mails pendentes", Order = 1 },
                    new() { Title = "Revisar o calendário de entregas", Order = 2 }
                }
            };

            var hydrationTask = new DailyTask
            {
                UserId = userId,
                Title = "Beber 2L de água 💧",
                Description = "Manter-se hidratado durante a jornada de trabalho.",
                Priority = TaskPriority.Media,
                Recurrence = RecurrenceType.TodosOsDias,
                StartDate = today,
                CreatedAt = DateTime.UtcNow
            };

            var workoutTask = new DailyTask
            {
                UserId = userId,
                Title = "Treino / Caminhada de 30 minutos 🏃",
                Description = "Atividade física para manter o corpo e a mente saudáveis.",
                Priority = TaskPriority.Media,
                Recurrence = RecurrenceType.DiasUteis,
                StartDate = today,
                CreatedAt = DateTime.UtcNow
            };

            await _context.DailyTasks.AddRangeAsync(welcomeDailyTask, hydrationTask, workoutTask);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<DailyTaskWithStatus>> GetTasksForDateAsync(DateTime targetDate, int userId)
    {
        var dateOnly = targetDate.Date;

        var allTasks = await _context.DailyTasks
            .Include(t => t.Checklists.OrderBy(c => c.Order))
            .Include(t => t.Completions.Where(c => c.Date == dateOnly))
            .Where(t => t.UserId == userId && t.StartDate.Date <= dateOnly)
            .AsNoTracking()
            .ToListAsync();

        var validTasks = allTasks.Where(t => IsTaskActiveOnDate(t, dateOnly)).ToList();

        var checklistCompletions = await _context.DailyChecklistCompletions
            .Where(c => c.Date == dateOnly)
            .AsNoTracking()
            .ToListAsync();

        var checklistStatusLookup = checklistCompletions.ToDictionary(c => c.DailyChecklistItemId, c => c.IsCompleted);

        var result = new List<DailyTaskWithStatus>();
        foreach (var task in validTasks)
        {
            var taskCompletion = task.Completions.FirstOrDefault();
            var isTaskCompleted = taskCompletion?.IsCompleted ?? false;

            var checklistItemsWithStatus = task.Checklists.Select(item => new DailyChecklistWithStatus
            {
                Item = item,
                IsCompleted = checklistStatusLookup.TryGetValue(item.Id, out var comp) && comp
            }).ToList();

            result.Add(new DailyTaskWithStatus
            {
                Task = task,
                IsCompleted = isTaskCompleted,
                CompletedAt = taskCompletion?.CompletedAt,
                Checklists = checklistItemsWithStatus
            });
        }

        return result.OrderBy(r => r.IsCompleted).ThenByDescending(r => r.Task.Priority).ToList();
    }

    public async Task<Dictionary<DateTime, DayOverview>> GetMonthOverviewAsync(int year, int month, int userId)
    {
        var firstDay = new DateTime(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var allTasks = await _context.DailyTasks
            .Where(t => t.UserId == userId && t.StartDate.Date <= lastDay)
            .AsNoTracking()
            .ToListAsync();

        var completions = await _context.DailyTaskCompletions
            .Where(c => c.DailyTask!.UserId == userId && c.Date >= firstDay && c.Date <= lastDay && c.IsCompleted)
            .AsNoTracking()
            .ToListAsync();

        var completedLookup = completions.GroupBy(c => c.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new Dictionary<DateTime, DayOverview>();

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var dateOnly = day.Date;
            var activeCount = allTasks.Count(t => IsTaskActiveOnDate(t, dateOnly));
            int completedCount = completedLookup.TryGetValue(dateOnly, out var c) ? c : 0;

            result[dateOnly] = new DayOverview
            {
                Date = dateOnly,
                TotalTasks = activeCount,
                CompletedTasks = completedCount
            };
        }

        return result;
    }

    public static bool IsTaskActiveOnDate(DailyTask task, DateTime date)
    {
        var start = task.StartDate.Date;
        if (date < start) return false;
        if (task.EndDate.HasValue && date > task.EndDate.Value.Date) return false;

        return task.Recurrence switch
        {
            RecurrenceType.ApenasNesteDia => date == start,
            RecurrenceType.Proximos3Dias => date >= start && date < start.AddDays(3),
            RecurrenceType.Proximos15Dias => date >= start && date < start.AddDays(15),
            RecurrenceType.Proximos30Dias => date >= start && date < start.AddDays(30),
            RecurrenceType.DiasUteis => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday,
            RecurrenceType.TodosOsDias => true,
            _ => true
        };
    }

    public async Task<DailyTask> SaveDailyTaskAsync(DailyTask task, List<string> checklistTitles, int userId)
    {
        task.UserId = userId;

        if (task.Id == 0)
        {
            task.CreatedAt = DateTime.UtcNow;

            for (int i = 0; i < checklistTitles.Count; i++)
            {
                task.Checklists.Add(new DailyChecklistItem
                {
                    Title = checklistTitles[i],
                    Order = i
                });
            }

            await _context.DailyTasks.AddAsync(task);
        }
        else
        {
            var existing = await _context.DailyTasks
                .Include(t => t.Checklists)
                .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == userId);

            if (existing != null)
            {
                existing.Title = task.Title;
                existing.Description = task.Description;
                existing.Priority = task.Priority;
                existing.Recurrence = task.Recurrence;
                existing.StartDate = task.StartDate;
                existing.EndDate = task.EndDate;

                _context.DailyChecklistItems.RemoveRange(existing.Checklists);
                for (int i = 0; i < checklistTitles.Count; i++)
                {
                    existing.Checklists.Add(new DailyChecklistItem
                    {
                        Title = checklistTitles[i],
                        Order = i,
                        DailyTaskId = existing.Id
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task ToggleTaskCompletionAsync(int taskId, DateTime date, bool isCompleted)
    {
        var dateOnly = date.Date;
        var completion = await _context.DailyTaskCompletions
            .FirstOrDefaultAsync(c => c.DailyTaskId == taskId && c.Date == dateOnly);

        if (completion == null)
        {
            completion = new DailyTaskCompletion
            {
                DailyTaskId = taskId,
                Date = dateOnly,
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.UtcNow : null
            };
            await _context.DailyTaskCompletions.AddAsync(completion);
        }
        else
        {
            completion.IsCompleted = isCompleted;
            completion.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task ToggleChecklistCompletionAsync(int checklistItemId, DateTime date, bool isCompleted)
    {
        var dateOnly = date.Date;
        var completion = await _context.DailyChecklistCompletions
            .FirstOrDefaultAsync(c => c.DailyChecklistItemId == checklistItemId && c.Date == dateOnly);

        if (completion == null)
        {
            completion = new DailyChecklistCompletion
            {
                DailyChecklistItemId = checklistItemId,
                Date = dateOnly,
                IsCompleted = isCompleted
            };
            await _context.DailyChecklistCompletions.AddAsync(completion);
        }
        else
        {
            completion.IsCompleted = isCompleted;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteDailyTaskAsync(int taskId)
    {
        var task = await _context.DailyTasks.FindAsync(taskId);
        if (task != null)
        {
            _context.DailyTasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Tasks.App.Data;
using Tasks.App.Models;

namespace Tasks.App.Services;

public interface ITaskService
{
    Task InitializeDatabaseForUserAsync(int userId);
    Task<List<TaskColumn>> GetColumnsWithTasksAsync(int userId);
    Task<TaskItem> SaveTaskAsync(TaskItem task);
    Task DeleteTaskAsync(int taskId);
    Task MoveTaskAsync(int taskId, int targetColumnId, int newOrderIndex);
    Task<ChecklistItem> AddChecklistItemAsync(int taskId, string title);
    Task ToggleChecklistItemAsync(int itemId, bool isCompleted);
    Task DeleteChecklistItemAsync(int itemId);
    Task<TaskColumn> AddColumnAsync(string title, string colorHex, int userId);
    Task DeleteColumnAsync(int columnId);
}

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;

    public TaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeDatabaseForUserAsync(int userId)
    {
        await _context.EnsureTablesCreatedAsync();

        if (!await _context.Columns.AnyAsync(c => c.UserId == userId))
        {
            var defaultColumns = new List<TaskColumn>
            {
                new() { UserId = userId, Title = "Planejado", Order = 0, ColorHex = "#6366F1" },       // Indigo
                new() { UserId = userId, Title = "Em Execução", Order = 1, ColorHex = "#3B82F6" },     // Blue
                new() { UserId = userId, Title = "Em Revisão", Order = 2, ColorHex = "#F59E0B" },       // Amber
                new() { UserId = userId, Title = "Alcançado", Order = 3, ColorHex = "#10B981" }         // Emerald
            };

            await _context.Columns.AddRangeAsync(defaultColumns);
            await _context.SaveChangesAsync();
            // Contas iniciam com o quadro Kanban 100% limpo de tarefas/cards
        }
    }

    public async Task<List<TaskColumn>> GetColumnsWithTasksAsync(int userId)
    {
        return await _context.Columns
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Order)
            .Include(c => c.Tasks.OrderBy(t => t.Order))
                .ThenInclude(t => t.Checklist.OrderBy(cl => cl.Order))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TaskItem> SaveTaskAsync(TaskItem task)
    {
        if (task.Id == 0)
        {
            var maxOrder = await _context.Tasks
                .Where(t => t.ColumnId == task.ColumnId)
                .Select(t => (int?)t.Order)
                .MaxAsync() ?? -1;

            task.Order = maxOrder + 1;
            task.CreatedAt = System.DateTime.UtcNow;
            await _context.Tasks.AddAsync(task);
        }
        else
        {
            var existing = await _context.Tasks
                .Include(t => t.Checklist)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            if (existing != null)
            {
                existing.Title = task.Title;
                existing.Description = task.Description;
                existing.Priority = task.Priority;
                existing.DueDate = task.DueDate;
                existing.ColumnId = task.ColumnId;

                // Sincroniza e salva o status de conclusão dos itens do checklist
                _context.ChecklistItems.RemoveRange(existing.Checklist);
                existing.Checklist.Clear();

                for (int i = 0; i < task.Checklist.Count; i++)
                {
                    var item = task.Checklist[i];
                    existing.Checklist.Add(new ChecklistItem
                    {
                        TaskItemId = existing.Id,
                        Title = item.Title,
                        IsCompleted = item.IsCompleted,
                        Order = i
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task MoveTaskAsync(int taskId, int targetColumnId, int newOrderIndex)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return;

        int oldColumnId = task.ColumnId;
        task.ColumnId = targetColumnId;

        var targetTasks = await _context.Tasks
            .Where(t => t.ColumnId == targetColumnId && t.Id != taskId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        if (newOrderIndex < 0) newOrderIndex = 0;
        if (newOrderIndex > targetTasks.Count) newOrderIndex = targetTasks.Count;

        targetTasks.Insert(newOrderIndex, task);

        for (int i = 0; i < targetTasks.Count; i++)
        {
            targetTasks[i].Order = i;
        }

        if (oldColumnId != targetColumnId)
        {
            var oldTasks = await _context.Tasks
                .Where(t => t.ColumnId == oldColumnId && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            for (int i = 0; i < oldTasks.Count; i++)
            {
                oldTasks[i].Order = i;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ChecklistItem> AddChecklistItemAsync(int taskId, string title)
    {
        var maxOrder = await _context.ChecklistItems
            .Where(c => c.TaskItemId == taskId)
            .Select(c => (int?)c.Order)
            .MaxAsync() ?? -1;

        var item = new ChecklistItem
        {
            TaskItemId = taskId,
            Title = title,
            IsCompleted = false,
            Order = maxOrder + 1
        };

        await _context.ChecklistItems.AddAsync(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task ToggleChecklistItemAsync(int itemId, bool isCompleted)
    {
        var item = await _context.ChecklistItems.FindAsync(itemId);
        if (item != null)
        {
            item.IsCompleted = isCompleted;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteChecklistItemAsync(int itemId)
    {
        var item = await _context.ChecklistItems.FindAsync(itemId);
        if (item != null)
        {
            _context.ChecklistItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TaskColumn> AddColumnAsync(string title, string colorHex, int userId)
    {
        var maxOrder = await _context.Columns
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Order)
            .MaxAsync() ?? -1;

        var column = new TaskColumn
        {
            UserId = userId,
            Title = title,
            ColorHex = colorHex,
            Order = maxOrder + 1
        };

        await _context.Columns.AddAsync(column);
        await _context.SaveChangesAsync();
        return column;
    }

    public async Task DeleteColumnAsync(int columnId)
    {
        var column = await _context.Columns.FindAsync(columnId);
        if (column != null)
        {
            _context.Columns.Remove(column);
            await _context.SaveChangesAsync();
        }
    }
}

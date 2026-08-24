namespace Tasks.App.Models;

public enum TaskPriority
{
    Baixa = 0,
    Media = 1,
    Alta = 2,
    Urgente = 3
}

public class TaskColumn
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string ColorHex { get; set; } = "#3B82F6"; // Cor padrão

    public List<TaskItem> Tasks { get; set; } = new();
}

public class ChecklistItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }

    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Media;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Order { get; set; }

    public int ColumnId { get; set; }
    public TaskColumn? Column { get; set; }

    public List<ChecklistItem> Checklist { get; set; } = new();
}

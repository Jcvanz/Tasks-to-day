using System;
using System.Collections.Generic;

namespace Tasks.App.Models;

public enum TaskPriority
{
    Baixa = 0,
    Media = 1,
    Alta = 2,
    Urgente = 3
}

public enum RecurrenceType
{
    ApenasNesteDia = 0,
    Proximos3Dias = 1,
    Proximos15Dias = 2,
    Proximos30Dias = 3,
    DiasUteis = 4,
    TodosOsDias = 5
}

// ----------------------------------------------------
// AUTENTICAÇÃO E USUÁRIOS
// ----------------------------------------------------

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DailyTask> DailyTasks { get; set; } = new();
    public List<TaskColumn> GoalColumns { get; set; } = new();
}

public class EmailVerificationCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ----------------------------------------------------
// MEU DIA A DIA (Tarefas Diárias com Recorrência)
// ----------------------------------------------------

public class DailyTask
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Media;
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.ApenasNesteDia;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DailyChecklistItem> Checklists { get; set; } = new();
    public List<DailyTaskCompletion> Completions { get; set; } = new();
}

public class DailyChecklistItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public int DailyTaskId { get; set; }
    public DailyTask? DailyTask { get; set; }

    public List<DailyChecklistCompletion> Completions { get; set; } = new();
}

public class DailyTaskCompletion
{
    public int Id { get; set; }
    public int DailyTaskId { get; set; }
    public DailyTask? DailyTask { get; set; }
    public DateTime Date { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DailyChecklistCompletion
{
    public int Id { get; set; }
    public int DailyChecklistItemId { get; set; }
    public DailyChecklistItem? DailyChecklistItem { get; set; }
    public DateTime Date { get; set; }
    public bool IsCompleted { get; set; }
}

// ----------------------------------------------------
// QUADRO DE OBJETIVOS & METAS (Kanban)
// ----------------------------------------------------

public class TaskColumn
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string ColorHex { get; set; } = "#3B82F6";

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

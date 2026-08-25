using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class ChecklistItemViewModel : ObservableObject
{
    private readonly ITaskService _taskService;

    public int Id { get; }
    public int TaskItemId { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;

    public ChecklistItemViewModel(ChecklistItem item, ITaskService taskService)
    {
        _taskService = taskService;
        Id = item.Id;
        TaskItemId = item.TaskItemId;
        _title = item.Title;
        _isCompleted = item.IsCompleted;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _ = _taskService.ToggleChecklistItemAsync(Id, value);
    }
}

public partial class TaskItemViewModel : ObservableObject
{
    private readonly ITaskService _taskService;

    public int Id { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private TaskPriority _priority;

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private int _columnId;

    [ObservableProperty]
    private int _order;

    public ObservableCollection<ChecklistItemViewModel> Checklist { get; } = new();

    public int TotalChecklistCount => Checklist.Count;
    public int CompletedChecklistCount => Checklist.Count(c => c.IsCompleted);
    public bool HasChecklist => Checklist.Count > 0;
    public double ProgressPercentage => TotalChecklistCount == 0 ? 0 : (double)CompletedChecklistCount / TotalChecklistCount * 100;
    public string ChecklistProgressText => $"{CompletedChecklistCount}/{TotalChecklistCount}";

    public TaskItemViewModel(TaskItem item, ITaskService taskService)
    {
        _taskService = taskService;
        Id = item.Id;
        _title = item.Title;
        _description = item.Description;
        _priority = item.Priority;
        _dueDate = item.DueDate;
        _columnId = item.ColumnId;
        _order = item.Order;

        foreach (var check in item.Checklist.OrderBy(c => c.Order))
        {
            var vm = new ChecklistItemViewModel(check, _taskService);
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChecklistItemViewModel.IsCompleted))
                {
                    OnPropertyChanged(nameof(CompletedChecklistCount));
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ChecklistProgressText));
                }
            };
            Checklist.Add(vm);
        }
    }

    public void RefreshChecklistStats()
    {
        OnPropertyChanged(nameof(TotalChecklistCount));
        OnPropertyChanged(nameof(CompletedChecklistCount));
        OnPropertyChanged(nameof(HasChecklist));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(ChecklistProgressText));
    }
}

public partial class ColumnViewModel : ObservableObject
{
    public int Id { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _colorHex = "#3B82F6";

    [ObservableProperty]
    private int _order;

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();

    public int TaskCount => Tasks.Count;

    public ColumnViewModel(TaskColumn column)
    {
        Id = column.Id;
        _title = column.Title;
        _colorHex = column.ColorHex;
        _order = column.Order;

        Tasks.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TaskCount));
        };
    }
}

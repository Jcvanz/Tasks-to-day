using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class TaskEditorChecklistDraft : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}

public partial class TaskEditorViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly int _taskId;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private TaskPriority _priority = TaskPriority.Media;

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private int _columnId;

    [ObservableProperty]
    private string _newChecklistTitle = string.Empty;

    public ObservableCollection<TaskEditorChecklistDraft> ChecklistDrafts { get; } = new();

    public Array Priorities => Enum.GetValues(typeof(TaskPriority));

    public TaskEditorViewModel(TaskItemViewModel? existingTask, int columnId, ITaskService taskService)
    {
        _taskService = taskService;
        _columnId = columnId;

        if (existingTask != null)
        {
            _taskId = existingTask.Id;
            _title = existingTask.Title;
            _description = existingTask.Description;
            _priority = existingTask.Priority;
            _dueDate = existingTask.DueDate;
            _columnId = existingTask.ColumnId;

            foreach (var item in existingTask.Checklist)
            {
                ChecklistDrafts.Add(new TaskEditorChecklistDraft
                {
                    Id = item.Id,
                    Title = item.Title,
                    IsCompleted = item.IsCompleted
                });
            }
        }
    }

    [RelayCommand]
    public void AddChecklistDraft()
    {
        if (string.IsNullOrWhiteSpace(NewChecklistTitle)) return;

        ChecklistDrafts.Add(new TaskEditorChecklistDraft
        {
            Title = NewChecklistTitle.Trim(),
            IsCompleted = false
        });

        NewChecklistTitle = string.Empty;
    }

    [RelayCommand]
    public void RemoveChecklistDraft(TaskEditorChecklistDraft? draft)
    {
        if (draft != null)
        {
            ChecklistDrafts.Remove(draft);
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title)) return false;

        var entity = new TaskItem
        {
            Id = _taskId,
            Title = Title.Trim(),
            Description = Description?.Trim(),
            Priority = Priority,
            DueDate = DueDate,
            ColumnId = ColumnId,
            Checklist = ChecklistDrafts.Select((d, index) => new ChecklistItem
            {
                Id = d.Id,
                Title = d.Title,
                IsCompleted = d.IsCompleted,
                Order = index
            }).ToList()
        };

        await _taskService.SaveTaskAsync(entity);
        return true;
    }
}

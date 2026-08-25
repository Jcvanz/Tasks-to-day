using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public class RecurrenceOptionItem
{
    public RecurrenceType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public partial class DailyTaskEditorViewModel : ObservableObject
{
    private readonly IDailyTaskService _taskService;
    private readonly int _taskId = 0;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private TaskPriority _priority = TaskPriority.Media;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private RecurrenceOptionItem _selectedRecurrence;

    [ObservableProperty]
    private string _newChecklistTitle = string.Empty;

    public ObservableCollection<RecurrenceOptionItem> RecurrenceOptions { get; } = new();
    public ObservableCollection<string> ChecklistDrafts { get; } = new();
    public Array Priorities => Enum.GetValues(typeof(TaskPriority));

    public DailyTaskEditorViewModel(DateTime initialDate, IDailyTaskService taskService)
    {
        _taskService = taskService;
        _startDate = initialDate;

        RecurrenceOptions.Add(new() { Type = RecurrenceType.ApenasNesteDia, DisplayName = "Apenas na data selecionada (1 dia)" });
        RecurrenceOptions.Add(new() { Type = RecurrenceType.Proximos3Dias, DisplayName = "Próximos 3 dias" });
        RecurrenceOptions.Add(new() { Type = RecurrenceType.Proximos15Dias, DisplayName = "Próximos 15 dias" });
        RecurrenceOptions.Add(new() { Type = RecurrenceType.Proximos30Dias, DisplayName = "Próximos 30 dias" });
        RecurrenceOptions.Add(new() { Type = RecurrenceType.DiasUteis, DisplayName = "Apenas dias de semana (Segunda a Sexta)" });
        RecurrenceOptions.Add(new() { Type = RecurrenceType.TodosOsDias, DisplayName = "Todos os dias do ano (Diário contínuo)" });

        _selectedRecurrence = RecurrenceOptions[0];
    }

    [RelayCommand]
    public void AddChecklistDraft()
    {
        if (string.IsNullOrWhiteSpace(NewChecklistTitle)) return;

        ChecklistDrafts.Add(NewChecklistTitle.Trim());
        NewChecklistTitle = string.Empty;
    }

    [RelayCommand]
    public void RemoveChecklistDraft(string? title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            ChecklistDrafts.Remove(title);
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title)) return false;

        var dailyTask = new DailyTask
        {
            Id = _taskId,
            Title = Title.Trim(),
            Description = Description?.Trim(),
            Priority = Priority,
            StartDate = StartDate.Date,
            Recurrence = SelectedRecurrence.Type
        };

        await _taskService.SaveDailyTaskAsync(dailyTask, ChecklistDrafts.ToList());
        return true;
    }
}

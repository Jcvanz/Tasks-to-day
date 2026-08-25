using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class DailyChecklistItemViewModel : ObservableObject
{
    private readonly IDailyTaskService _service;
    private readonly DateTime _currentDate;

    public int Id { get; }
    public string Title { get; }

    [ObservableProperty]
    private bool _isCompleted;

    public event Action? CompletionChanged;

    public DailyChecklistItemViewModel(DailyChecklistWithStatus itemWithStatus, DateTime currentDate, IDailyTaskService service)
    {
        _service = service;
        _currentDate = currentDate;
        Id = itemWithStatus.Item.Id;
        Title = itemWithStatus.Item.Title;
        _isCompleted = itemWithStatus.IsCompleted;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _ = _service.ToggleChecklistCompletionAsync(Id, _currentDate, value);
        CompletionChanged?.Invoke();
    }
}

public partial class DailyTaskItemViewModel : ObservableObject
{
    private readonly IDailyTaskService _service;
    private readonly DateTime _currentDate;

    public int Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public TaskPriority Priority { get; }
    public RecurrenceType Recurrence { get; }
    public DateTime StartDate { get; }
    public string RecurrenceText => GetRecurrenceDescription(Recurrence);

    [ObservableProperty]
    private bool _isCompleted;

    public ObservableCollection<DailyChecklistItemViewModel> Checklists { get; } = new();

    public int TotalChecklistCount => Checklists.Count;
    public int CompletedChecklistCount => Checklists.Count(c => c.IsCompleted);
    public bool HasChecklist => Checklists.Count > 0;
    public double ChecklistProgress => TotalChecklistCount == 0 ? 0 : (double)CompletedChecklistCount / TotalChecklistCount * 100;
    public string ChecklistProgressText => $"{CompletedChecklistCount}/{TotalChecklistCount}";

    public event Action? TaskCompletionChanged;

    public DailyTaskItemViewModel(DailyTaskWithStatus taskWithStatus, DateTime currentDate, IDailyTaskService service)
    {
        _service = service;
        _currentDate = currentDate;
        Id = taskWithStatus.Task.Id;
        Title = taskWithStatus.Task.Title;
        Description = taskWithStatus.Task.Description;
        Priority = taskWithStatus.Task.Priority;
        Recurrence = taskWithStatus.Task.Recurrence;
        StartDate = taskWithStatus.Task.StartDate;
        _isCompleted = taskWithStatus.IsCompleted;

        foreach (var check in taskWithStatus.Checklists)
        {
            var checkVm = new DailyChecklistItemViewModel(check, _currentDate, _service);
            checkVm.CompletionChanged += () =>
            {
                OnPropertyChanged(nameof(CompletedChecklistCount));
                OnPropertyChanged(nameof(ChecklistProgress));
                OnPropertyChanged(nameof(ChecklistProgressText));
            };
            Checklists.Add(checkVm);
        }
    }

    partial void OnIsCompletedChanged(bool value)
    {
        _ = _service.ToggleTaskCompletionAsync(Id, _currentDate, value);
        TaskCompletionChanged?.Invoke();
    }

    private static string GetRecurrenceDescription(RecurrenceType recurrence) => recurrence switch
    {
        RecurrenceType.ApenasNesteDia => "Apenas neste dia",
        RecurrenceType.Proximos3Dias => "3 dias",
        RecurrenceType.Proximos15Dias => "15 dias",
        RecurrenceType.Proximos30Dias => "30 dias",
        RecurrenceType.DiasUteis => "Dias úteis (Seg-Sex)",
        RecurrenceType.TodosOsDias => "Todos os dias",
        _ => "Diário"
    };
}

public partial class CalendarDayViewModel : ObservableObject
{
    public DateTime Date { get; }
    public int DayNumber => Date.Day;
    public bool IsCurrentMonth { get; }
    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsPast => Date.Date < DateTime.Today;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _totalTasks;

    [ObservableProperty]
    private int _completedTasks;

    public bool HasTasks => TotalTasks > 0;
    public bool IsAllCompleted => TotalTasks > 0 && TotalTasks == CompletedTasks;
    public string ProgressText => $"{CompletedTasks}/{TotalTasks}";

    public CalendarDayViewModel(DateTime date, bool isCurrentMonth, DayOverview? overview = null)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        if (overview != null)
        {
            _totalTasks = overview.TotalTasks;
            _completedTasks = overview.CompletedTasks;
        }
    }

    public void UpdateOverview(DayOverview overview)
    {
        TotalTasks = overview.TotalTasks;
        CompletedTasks = overview.CompletedTasks;
        OnPropertyChanged(nameof(HasTasks));
        OnPropertyChanged(nameof(IsAllCompleted));
        OnPropertyChanged(nameof(ProgressText));
    }
}

public partial class DailyTasksViewModel : ObservableObject
{
    private readonly IDailyTaskService _taskService;
    private readonly IAuthService _authService;
    private readonly Func<DateTime, Task<bool>> _openTaskEditorFunc;

    [ObservableProperty]
    private DateTime _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<CalendarDayViewModel> CalendarDays { get; } = new();
    public ObservableCollection<DailyTaskItemViewModel> TasksForSelectedDate { get; } = new();

    public string CurrentMonthHeader => DisplayMonth.ToString("MMMM yyyy", new CultureInfo("pt-BR")).ToUpperInvariant();
    public string FormattedSelectedDate => SelectedDate.ToString("dddd, dd 'de' MMMM", new CultureInfo("pt-BR"));

    // Regra: Bloquear criação para datas anteriores a hoje
    public bool CanAddTaskToSelectedDate => SelectedDate.Date >= DateTime.Today;
    public bool IsPastDate => SelectedDate.Date < DateTime.Today;

    public int TotalTasksCount => TasksForSelectedDate.Count;
    public int CompletedTasksCount => TasksForSelectedDate.Count(t => t.IsCompleted);
    public double DayProgressPercentage => TotalTasksCount == 0 ? 0 : (double)CompletedTasksCount / TotalTasksCount * 100;
    public string DayProgressSummary => TotalTasksCount == 0 ? "Nenhuma tarefa agendada" : $"{CompletedTasksCount} de {TotalTasksCount} tarefas concluídas ({DayProgressPercentage:F0}%)";

    private int CurrentUserId => _authService.CurrentUser?.Id ?? 1;

    public DailyTasksViewModel(IDailyTaskService taskService, IAuthService authService, Func<DateTime, Task<bool>> openTaskEditorFunc)
    {
        _taskService = taskService;
        _authService = authService;
        _openTaskEditorFunc = openTaskEditorFunc;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await _taskService.InitializeForUserAsync(CurrentUserId);
            await RefreshCalendarAsync();
            await LoadTasksForSelectedDateAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task PreviousMonthAsync()
    {
        DisplayMonth = DisplayMonth.AddMonths(-1);
        OnPropertyChanged(nameof(CurrentMonthHeader));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        DisplayMonth = DisplayMonth.AddMonths(1);
        OnPropertyChanged(nameof(CurrentMonthHeader));
        await RefreshCalendarAsync();
    }

    [RelayCommand]
    public async Task GoToTodayAsync()
    {
        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;
        OnPropertyChanged(nameof(CurrentMonthHeader));
        OnPropertyChanged(nameof(CanAddTaskToSelectedDate));
        OnPropertyChanged(nameof(IsPastDate));
        await RefreshCalendarAsync();
        await LoadTasksForSelectedDateAsync();
    }

    [RelayCommand]
    public async Task SelectDateAsync(CalendarDayViewModel? dayVm)
    {
        if (dayVm == null) return;

        SelectedDate = dayVm.Date;
        foreach (var d in CalendarDays)
        {
            d.IsSelected = d.Date.Date == SelectedDate.Date;
        }

        OnPropertyChanged(nameof(FormattedSelectedDate));
        OnPropertyChanged(nameof(CanAddTaskToSelectedDate));
        OnPropertyChanged(nameof(IsPastDate));
        await LoadTasksForSelectedDateAsync();
    }

    public async Task RefreshCalendarAsync()
    {
        var overviews = await _taskService.GetMonthOverviewAsync(DisplayMonth.Year, DisplayMonth.Month, CurrentUserId);

        CalendarDays.Clear();
        var firstDayOfMonth = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(DisplayMonth.Year, DisplayMonth.Month);

        int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        for (int i = startDayOfWeek - 1; i >= 0; i--)
        {
            var prevDate = firstDayOfMonth.AddDays(-i - 1);
            CalendarDays.Add(new CalendarDayViewModel(prevDate, false));
        }

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(DisplayMonth.Year, DisplayMonth.Month, day);
            overviews.TryGetValue(date, out var ov);
            var dayVm = new CalendarDayViewModel(date, true, ov)
            {
                IsSelected = date.Date == SelectedDate.Date
            };
            CalendarDays.Add(dayVm);
        }

        int remainingCells = (7 - (CalendarDays.Count % 7)) % 7;
        var lastDayOfMonth = new DateTime(DisplayMonth.Year, DisplayMonth.Month, daysInMonth);
        for (int i = 1; i <= remainingCells; i++)
        {
            var nextDate = lastDayOfMonth.AddDays(i);
            CalendarDays.Add(new CalendarDayViewModel(nextDate, false));
        }
    }

    public async Task LoadTasksForSelectedDateAsync()
    {
        var tasks = await _taskService.GetTasksForDateAsync(SelectedDate, CurrentUserId);

        TasksForSelectedDate.Clear();
        foreach (var t in tasks)
        {
            var taskVm = new DailyTaskItemViewModel(t, SelectedDate, _taskService);
            taskVm.TaskCompletionChanged += () =>
            {
                UpdateDayProgressStats();
                _ = RefreshDayBadgeAsync(SelectedDate);
            };
            TasksForSelectedDate.Add(taskVm);
        }

        UpdateDayProgressStats();
    }

    private void UpdateDayProgressStats()
    {
        OnPropertyChanged(nameof(TotalTasksCount));
        OnPropertyChanged(nameof(CompletedTasksCount));
        OnPropertyChanged(nameof(DayProgressPercentage));
        OnPropertyChanged(nameof(DayProgressSummary));
    }

    private async Task RefreshDayBadgeAsync(DateTime date)
    {
        var overviews = await _taskService.GetMonthOverviewAsync(date.Year, date.Month, CurrentUserId);
        if (overviews.TryGetValue(date.Date, out var ov))
        {
            var dayVm = CalendarDays.FirstOrDefault(d => d.Date.Date == date.Date);
            dayVm?.UpdateOverview(ov);
        }
    }

    [RelayCommand]
    public async Task AddNewDailyTaskAsync()
    {
        if (SelectedDate.Date < DateTime.Today)
        {
            MessageBox.Show(
                "Não é permitido agendar tarefas para datas passadas. Selecione o dia de hoje ou uma data futura.",
                "Data Inválida",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        bool saved = await _openTaskEditorFunc(SelectedDate);
        if (saved)
        {
            await RefreshCalendarAsync();
            await LoadTasksForSelectedDateAsync();
        }
    }

    [RelayCommand]
    public async Task DeleteDailyTaskAsync(DailyTaskItemViewModel? taskVm)
    {
        if (taskVm == null) return;

        var result = MessageBox.Show(
            $"Deseja excluir a tarefa \"{taskVm.Title}\"?\nIsso removerá a tarefa de todos os dias vinculados à sua regra de recorrência.",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _taskService.DeleteDailyTaskAsync(taskVm.Id);
            await RefreshCalendarAsync();
            await LoadTasksForSelectedDateAsync();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Tasks.App.Models;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class KanbanViewModel : ObservableObject, IDropTarget
{
    private readonly ITaskService _taskService;
    private readonly Func<TaskItemViewModel?, int, Task<bool>> _openTaskEditorFunc;

    public ObservableCollection<ColumnViewModel> Columns { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public KanbanViewModel(ITaskService taskService, Func<TaskItemViewModel?, int, Task<bool>> openTaskEditorFunc)
    {
        _taskService = taskService;
        _openTaskEditorFunc = openTaskEditorFunc;
    }

    [RelayCommand]
    public async Task LoadBoardAsync()
    {
        IsLoading = true;
        try
        {
            await _taskService.InitializeDatabaseAsync();
            var columns = await _taskService.GetColumnsWithTasksAsync();

            Columns.Clear();
            foreach (var col in columns)
            {
                var colVm = new ColumnViewModel(col);
                foreach (var task in col.Tasks)
                {
                    colVm.Tasks.Add(new TaskItemViewModel(task, _taskService));
                }
                Columns.Add(colVm);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddNewTaskAsync(ColumnViewModel? column)
    {
        int columnId = column?.Id ?? Columns.FirstOrDefault()?.Id ?? 0;
        if (columnId == 0) return;

        bool saved = await _openTaskEditorFunc(null, columnId);
        if (saved)
        {
            await LoadBoardAsync();
        }
    }

    [RelayCommand]
    public async Task EditTaskAsync(TaskItemViewModel? taskVm)
    {
        if (taskVm == null) return;

        bool saved = await _openTaskEditorFunc(taskVm, taskVm.ColumnId);
        if (saved)
        {
            await LoadBoardAsync();
        }
    }

    [RelayCommand]
    public async Task DeleteTaskAsync(TaskItemViewModel? taskVm)
    {
        if (taskVm == null) return;

        var result = MessageBox.Show(
            $"Deseja realmente excluir a tarefa \"{taskVm.Title}\"?",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _taskService.DeleteTaskAsync(taskVm.Id);
            await LoadBoardAsync();
        }
    }

    // GongSolutions Drag & Drop Implementation
    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is TaskItemViewModel && dropInfo.TargetCollection != null)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not TaskItemViewModel sourceTask) return;

        // Identificar a coluna de destino
        ColumnViewModel? targetColumn = null;
        foreach (var col in Columns)
        {
            if (col.Tasks == dropInfo.TargetCollection)
            {
                targetColumn = col;
                break;
            }
        }

        if (targetColumn == null) return;

        // Remover do local de origem na UI
        foreach (var col in Columns)
        {
            if (col.Tasks.Contains(sourceTask))
            {
                col.Tasks.Remove(sourceTask);
                break;
            }
        }

        // Inserir na posição de destino
        int insertIndex = dropInfo.InsertIndex;
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > targetColumn.Tasks.Count) insertIndex = targetColumn.Tasks.Count;

        sourceTask.ColumnId = targetColumn.Id;
        targetColumn.Tasks.Insert(insertIndex, sourceTask);

        // Persistir no banco assincronamente
        _ = _taskService.MoveTaskAsync(sourceTask.Id, targetColumn.Id, insertIndex);
    }
}

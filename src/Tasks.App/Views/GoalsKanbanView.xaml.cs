using System.Windows.Controls;
using System.Windows.Input;
using Tasks.App.ViewModels;

namespace Tasks.App.Views;

public partial class GoalsKanbanView : UserControl
{
    public GoalsKanbanView()
    {
        InitializeComponent();
    }

    private void TaskCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is KanbanViewModel kanbanVm && sender is ListBox listBox && listBox.SelectedItem is TaskItemViewModel selectedTask)
        {
            kanbanVm.EditTaskCommand.Execute(selectedTask);
        }
    }
}

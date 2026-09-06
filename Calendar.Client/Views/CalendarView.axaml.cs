using Avalonia.Controls;

namespace Calendar.Client.Views;

/// <summary>
/// Month view: the day grid plus the controls that navigate through months and years.
/// Rendering only — the displayed month and every command live in the view model.
/// </summary>
public partial class CalendarView : UserControl
{
    public CalendarView()
    {
        InitializeComponent();
    }
}

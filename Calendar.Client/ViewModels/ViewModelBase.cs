using CommunityToolkit.Mvvm.ComponentModel;

namespace Calendar.Client.ViewModels;

/// <summary>
/// Base class for every view model in the client. Inherits change notification from
/// <see cref="ObservableObject"/> and acts as the marker
/// <see cref="Calendar.Client.ViewLocator"/> uses to decide whether it can resolve a view
/// for an object.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Calendar.Client.ViewModels;

namespace Calendar.Client;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Resolves the view that matches a view model, by convention: the view model's type name
    /// with "ViewModel" replaced by "View".
    /// </summary>
    /// <param name="param">The view model instance to resolve a view for.</param>
    /// <returns>
    /// A new instance of the matching view; <c>null</c> when <paramref name="param"/> is
    /// <c>null</c>; or a <see cref="TextBlock"/> reporting the missing type when no view
    /// matches the convention.
    /// </returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Tells Avalonia whether this locator can build a view for the given object.
    /// </summary>
    /// <param name="data">The object bound to the control, normally a view model.</param>
    /// <returns><c>true</c> when it is a <see cref="ViewModelBase"/>; otherwise <c>false</c>.</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}

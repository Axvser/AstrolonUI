using System;

namespace AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Models;

public readonly struct ListenerMedium<T>(T source, Func<T, bool> listener)
{
    public T Source { get; } = source;
    public Func<T, bool> Listener { get; } = listener;
}

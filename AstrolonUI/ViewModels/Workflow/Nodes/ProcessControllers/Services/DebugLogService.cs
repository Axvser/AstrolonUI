using System;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;

public class DebugLogService : NodeHelper<DebugLogViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        var text = parameter?.ToString() ?? "<null>";
        Component.LastValue = text;
        Component.Entries.Add($"{DateTimeOffset.Now:HH:mm:ss}  {text}");

        while (Component.Entries.Count > Math.Max(1, Component.MaxEntries))
        {
            Component.Entries.RemoveAt(0);
        }

        await BroadcastAsync(parameter, ct);
    }
}

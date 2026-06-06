using System;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;

public class DelayService : NodeHelper<DelayViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        Component.IsWaiting = true;
        try
        {
            await Task.Delay(Math.Max(0, Component.DelayMilliseconds), ct);
            await BroadcastAsync(parameter, ct);
        }
        finally
        {
            Component.IsWaiting = false;
        }
    }
}

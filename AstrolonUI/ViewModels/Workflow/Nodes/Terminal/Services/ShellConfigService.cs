using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;

public class ShellConfigService : NodeHelper<ShellConfigViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        var medium = parameter as ShellMedium ?? new ShellMedium();
        medium.Append(Component);
        await BroadcastAsync(medium, ct);
    }
}

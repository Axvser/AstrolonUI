using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;

public class ShellControllorService : NodeHelper<ShellControllorViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        ShellMedium medium = new()
        {
            Pipeline = Component.Pipeline
        };

        await BroadcastAsync(medium, ct);
    }
}

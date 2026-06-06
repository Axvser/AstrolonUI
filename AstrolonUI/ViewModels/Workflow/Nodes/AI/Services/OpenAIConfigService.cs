using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.AI.Services;

public class OpenAIConfigService : NodeHelper<OpenAIConfigViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        await BroadcastAsync(OpenAIConfig.From(Component), ct);
    }
}

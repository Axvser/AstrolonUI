using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;

public class NumberValueService : NodeHelper<NumberValueViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        if (parameter is CalculationMedium medium)
        {
            var next = medium.CloneSnapshot();
            next.Append(
                string.IsNullOrWhiteSpace(Component.ParameterName) ? Component.Name : Component.ParameterName,
                Component.Value);
            await BroadcastAsync(next, ct);
            return;
        }

        await BroadcastAsync(Component.Value, ct);
    }
}

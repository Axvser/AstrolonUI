using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;

public class CalculationStartService : NodeHelper<CalculationStartViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        var id = string.IsNullOrWhiteSpace(Component.CalculationId)
            ? Guid.NewGuid().ToString("N")
            : Component.CalculationId.Trim();

        Component.LastCalculationId = id;
        await BroadcastAsync(new CalculationMedium
        {
            Id = id,
            RequiredParameterCount = Math.Max(1, Component.RequiredParameterCount)
        }, ct);
    }
}

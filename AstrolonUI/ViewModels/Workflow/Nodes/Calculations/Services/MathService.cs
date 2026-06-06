using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;

public class MathService : NodeHelper<MathViewModel>
{
    private readonly Dictionary<string, CalculationMedium> pendingCalculations = [];

    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        if (parameter is not CalculationMedium incoming)
        {
            var result = Calculate([ToDouble(parameter)], Component.Operation);
            Component.LastResult = result;
            await BroadcastAsync(result, ct);
            return;
        }

        var medium = Merge(incoming);
        Component.LastCalculationId = medium.Id;
        Component.WaitingParameterCount = medium.Parameters.Count;

        if (medium.Parameters.Count < Math.Max(1, medium.RequiredParameterCount))
        {
            return;
        }

        var values = medium.Parameters
            .OrderBy(parameter => parameter.CreatedAt)
            .Take(Math.Max(1, medium.RequiredParameterCount))
            .Select(parameter => parameter.Value)
            .ToArray();

        var calculation = medium.CloneSnapshot();
        calculation.Result = Calculate(values, Component.Operation);
        calculation.IsCompleted = true;
        calculation.OperationName = Component.Operation.ToString();
        Component.LastResult = calculation.Result.Value;

        pendingCalculations.Remove(calculation.Id);
        await BroadcastAsync(calculation, ct);
    }

    private CalculationMedium Merge(CalculationMedium incoming)
    {
        if (!pendingCalculations.TryGetValue(incoming.Id, out var current) || incoming.IsCompleted)
        {
            current = new CalculationMedium
            {
                Id = incoming.Id,
                RequiredParameterCount = incoming.RequiredParameterCount
            };
            pendingCalculations[incoming.Id] = current;
        }

        current.RequiredParameterCount = Math.Max(current.RequiredParameterCount, incoming.RequiredParameterCount);
        foreach (var parameter in incoming.Parameters)
        {
            if (current.Parameters.Any(existing =>
                    existing.Source == parameter.Source &&
                    existing.Value == parameter.Value &&
                    existing.CreatedAt == parameter.CreatedAt))
            {
                continue;
            }

            current.Parameters.Add(parameter.CloneSnapshot());
        }

        return current;
    }

    private static double Calculate(IReadOnlyList<double> values, NumericOperation operation)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return operation switch
        {
            NumericOperation.Add => values.Sum(),
            NumericOperation.Subtract => values.Skip(1).Aggregate(values[0], (current, value) => current - value),
            NumericOperation.Multiply => values.Aggregate(1d, (current, value) => current * value),
            NumericOperation.Divide => values.Skip(1).Aggregate(values[0], (current, value) => value == 0 ? double.NaN : current / value),
            NumericOperation.Modulo => values.Skip(1).Aggregate(values[0], (current, value) => value == 0 ? double.NaN : current % value),
            NumericOperation.Power => values.Skip(1).Aggregate(values[0], Math.Pow),
            _ => values[0]
        };
    }

    private static double ToDouble(object? value)
    {
        if (value is CalculationMedium { Result: not null } medium)
        {
            return medium.Result.Value;
        }

        if (value is null)
        {
            return 0;
        }

        if (value is double d)
        {
            return d;
        }

        if (value is IConvertible convertible)
        {
            try
            {
                return convertible.ToDouble(CultureInfo.InvariantCulture);
            }
            catch
            {
            }
        }

        return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }
}

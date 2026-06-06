using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;

public class CompareService : NodeHelper<CompareViewModel>
{
    public override Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return Task.CompletedTask;
        }

        var result = Compare(parameter, Component.TargetValue, Component.Operation);
        Component.LastResult = result;
        Route(result, result);
        return Task.CompletedTask;
    }

    private void Route(object condition, object parameter)
    {
        if (Component is not IConditionSlotProvider router ||
            !router.TryGetSlot(condition, out var slot) ||
            slot is null)
        {
            return;
        }

        foreach (var target in slot.Targets)
        {
            target.Parent?.WorkCommand.Execute(parameter);
        }
    }

    private static bool Compare(object? source, string target, ComparisonOperation operation)
    {
        var left = source?.ToString() ?? string.Empty;
        var right = target ?? string.Empty;

        if (operation == ComparisonOperation.IsNullOrEmpty)
        {
            return string.IsNullOrEmpty(left);
        }

        if (TryToDouble(source, out var leftNumber) &&
            double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out var rightNumber))
        {
            return operation switch
            {
                ComparisonOperation.Equal => leftNumber == rightNumber,
                ComparisonOperation.NotEqual => leftNumber != rightNumber,
                ComparisonOperation.GreaterThan => leftNumber > rightNumber,
                ComparisonOperation.GreaterThanOrEqual => leftNumber >= rightNumber,
                ComparisonOperation.LessThan => leftNumber < rightNumber,
                ComparisonOperation.LessThanOrEqual => leftNumber <= rightNumber,
                _ => CompareText(left, right, operation)
            };
        }

        return CompareText(left, right, operation);
    }

    private static bool CompareText(string left, string right, ComparisonOperation operation)
        => operation switch
        {
            ComparisonOperation.Equal => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            ComparisonOperation.NotEqual => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            ComparisonOperation.Contains => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            ComparisonOperation.StartsWith => left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            ComparisonOperation.EndsWith => left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
            ComparisonOperation.GreaterThan => string.Compare(left, right, StringComparison.OrdinalIgnoreCase) > 0,
            ComparisonOperation.GreaterThanOrEqual => string.Compare(left, right, StringComparison.OrdinalIgnoreCase) >= 0,
            ComparisonOperation.LessThan => string.Compare(left, right, StringComparison.OrdinalIgnoreCase) < 0,
            ComparisonOperation.LessThanOrEqual => string.Compare(left, right, StringComparison.OrdinalIgnoreCase) <= 0,
            _ => false
        };

    private static bool TryToDouble(object? value, out double result)
    {
        if (value is CalculationMedium { Result: not null } medium)
        {
            result = medium.Result.Value;
            return true;
        }

        if (value is null)
        {
            result = 0;
            return false;
        }

        if (value is double d)
        {
            result = d;
            return true;
        }

        if (value is IConvertible convertible)
        {
            try
            {
                result = convertible.ToDouble(CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
            }
        }

        return double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}

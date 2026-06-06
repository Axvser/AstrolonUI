using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using CliWrap;
using CliWrap.EventStream;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;

public class ShellAggregateService : NodeHelper<ShellAggregateViewModel>
{
    private const int DefaultMaxConcurrency = 100;
    private static readonly SemaphoreSlim ConcurrencyGate = new(DefaultMaxConcurrency, DefaultMaxConcurrency);
    private int activeExecutions;

    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        if (parameter is not ShellMedium medium || medium.Shells.Count == 0)
        {
            Debug.WriteLine("[ ERROR ] ShellAggregate requires ShellMedium with at least one shell segment.");
            return;
        }

        await ConcurrencyGate.WaitAsync(ct);
        BeginExecution();

        var result = CreateResult(medium);
        RouteCommandResult(medium.Pipeline, result);

        try
        {
            if (!medium.Shells.All(IsConfigured))
            {
                result.SetError("Shell chain requires Wrap and a valid ExecutionPath before running.");
                CompleteResult(result);
                RouteCommandResult(medium.Pipeline, result);
                return;
            }

            var command = medium.Shells
                .Skip(1)
                .Aggregate(BuildCommand(medium.Shells[0]), (current, next) => current | BuildCommand(next));

            await foreach (var cmdEvent in command.ListenAsync(ct))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        result.ProcessId = started.ProcessId;
                        RouteCommandResult(medium.Pipeline, result);
                        break;
                    case StandardOutputCommandEvent stdOut:
                        result.AddStandardOutput(stdOut.Text);
                        RouteCommandResult(medium.Pipeline, result);
                        break;
                    case StandardErrorCommandEvent stdErr:
                        result.AddStandardError(stdErr.Text);
                        RouteCommandResult(medium.Pipeline, result);
                        break;
                    case ExitedCommandEvent exited:
                        result.ExitCode = exited.ExitCode;
                        CompleteResult(result);
                        RouteCommandResult(medium.Pipeline, result);
                        break;
                }
            }

            if (!result.IsCompleted)
            {
                CompleteResult(result);
                RouteCommandResult(medium.Pipeline, result);
            }
        }
        catch (OperationCanceledException)
        {
            result.SetError("ShellAggregate canceled.");
            CompleteResult(result);
            RouteCommandResult(medium.Pipeline, result);
            Debug.WriteLine("[ LOG ] ShellAggregate canceled.");
        }
        catch (Exception ex)
        {
            result.SetError(ex.Message);
            CompleteResult(result);
            RouteCommandResult(medium.Pipeline, result);
            Debug.WriteLine($"[ ERROR ] {ex.Message}");
        }
        finally
        {
            EndExecution();
            ConcurrencyGate.Release();
        }
    }

    private static ShellExecutionResultViewModel CreateResult(ShellMedium medium)
    {
        var shells = medium.Shells.Select(CloneCommand).ToArray();
        return new ShellExecutionResultViewModel
        {
            Id = medium.Id,
            Pipeline = medium.Pipeline,
            Shells = new ObservableCollection<Shell>(shells),
            Entries = new ObservableCollection<ShellExecutionEntryViewModel>(
                shells.Select(shell => new ShellExecutionEntryViewModel { Command = CloneCommand(shell) })),
            StartedAt = DateTimeOffset.Now,
            IsCompleted = false
        };
    }

    private void BeginExecution()
    {
        if (Interlocked.Increment(ref activeExecutions) == 1)
        {
            Component?.SetRunningState(true);
        }
    }

    private void EndExecution()
    {
        if (Interlocked.Decrement(ref activeExecutions) <= 0)
        {
            Interlocked.Exchange(ref activeExecutions, 0);
            Component?.SetRunningState(false);
        }
    }

    private static void CompleteResult(ShellExecutionResultViewModel result)
    {
        result.FinishedAt = DateTimeOffset.Now;
        result.IsCompleted = true;
    }

    private static bool IsConfigured(Shell shell)
        => !string.IsNullOrWhiteSpace(shell.Wrap)
           && !string.IsNullOrWhiteSpace(shell.ExecutionPath)
           && Directory.Exists(shell.ExecutionPath);

    private static Command BuildCommand(Shell shell)
        => Cli.Wrap(shell.Wrap)
            .WithArguments(shell.Arguments)
            .WithWorkingDirectory(shell.ExecutionPath);

    private static Shell CloneCommand(Shell command)
        => new()
        {
            ExecutionPath = command.ExecutionPath,
            Wrap = command.Wrap,
            Arguments = [.. command.Arguments]
        };

    private void RouteCommandResult(ShellResultPipelines selectedPipeline, ShellExecutionResultViewModel result)
    {
        var snapshot = result.CloneSnapshot();
        if (selectedPipeline == ShellResultPipelines.Mixed)
        {
            Route(ShellResultPipelines.Mixed, snapshot);
            return;
        }

        Route(selectedPipeline, snapshot);
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
}

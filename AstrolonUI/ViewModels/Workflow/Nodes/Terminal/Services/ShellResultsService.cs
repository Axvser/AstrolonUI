using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;

public class ShellResultsService : NodeHelper<ShellResultsViewModel>
{
    public override async Task WorkAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        switch (parameter)
        {
            case IOpenAIProvider provider:
                Component.ShowAgentReady(OpenAIConfig.From(provider));
                return;
            case ShellExecutionResultViewModel result:
                await AnalyzeShellResultAsync(result, ct);
                return;
            default:
                Component.AppendSystemNote("AISummary requires ShellExecutionResultViewModel or IOpenAIProvider input.");
                return;
        }
    }

    private async Task AnalyzeShellResultAsync(ShellExecutionResultViewModel result, CancellationToken ct)
    {
        if (Component is null)
        {
            return;
        }

        Component.ShowShellResult(result);

        if (!result.IsCompleted || !Component.EnableAISummary)
        {
            return;
        }

        if (Component.HasRequestedAISummary(result.Id))
        {
            return;
        }

        if (Component.AgentProvider is null || !CanUseRemoteAi(Component.AgentProvider))
        {
            Component.AppendSystemNote("AI summary is enabled, but no usable OpenAI config is available.");
            return;
        }

        Component.BeginAISummary(result.Id);

        try
        {
            await AgentServices.RunOpenAIAsync(
                Component.AgentProvider,
                new ChatMedium
                {
                    Prompt = Component.Instructions,
                    Message = result.ToPromptMarkdown(),
                    AllowStreaming = Component.AllowStreaming
                },
                ct,
                Component,
                result.Id);
        }
        catch (OperationCanceledException)
        {
            Component.AppendSystemNote("AI summary canceled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ ERROR ] {ex.Message}");
            Component.AppendSystemNote($"AI summary failed: {ex.Message}");
        }
    }

    private static bool CanUseRemoteAi(IOpenAIProvider provider)
        => !string.IsNullOrWhiteSpace(provider.EnvironmentVariableName)
           && !string.IsNullOrWhiteSpace(provider.Endpoint)
           && !string.IsNullOrWhiteSpace(provider.Model);
}



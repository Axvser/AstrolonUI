using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using Microsoft.Agents.AI;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.AI;
using VeloxDev.AI.Workflow;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Trees.Services;

public class AgentHelper() : TreeHelper<TreeViewModel>(200), IChatCallBackProvider
{
    private ChatClientAgent? agent;
    private AgentSession? session;
    private string? providerSignature;

    public EventHandler<string>? OnSyncResponse { get; set; }
    public EventHandler<string>? OnStreamChunk { get; set; }

    public override void Install(IWorkflowTreeViewModel tree)
    {
        base.Install(tree);
        OnSyncResponse = (_, text) => Component?.CompleteAssistantMessage(text);
        OnStreamChunk = (_, text) => Component?.AppendAssistantChunk(text);
    }

    public override void Uninstall(IWorkflowTreeViewModel tree)
    {
        Component?.CancelPendingAgentInteractions();
        agent = null;
        session = null;
        providerSignature = null;
        OnSyncResponse = null;
        OnStreamChunk = null;
        base.Uninstall(tree);
    }

    public async Task AskAsync(object? parameter, CancellationToken ct)
    {
        if (Component is null || parameter is not string message || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var treeSnapshot = await Component.CaptureAgentTreeSnapshotAsync(ct);
        Component.AppendUserMessage(message);
        Component.BeginAssistantMessage();

        ChatClientAgent? currentAgent = null;
        try
        {
            var medium = BuildChatMedium(Component, message);
            currentAgent = await EnsureAgentAsync(Component, medium);
            session ??= await currentAgent.CreateSessionAsync();
            medium.Session = session;

            await AgentServices.RunAgentAsync(
                currentAgent,
                medium,
                ct,
                this,
                Component);
        }
        catch (OperationCanceledException)
        {
            Component.CompleteAssistantMessage("_Canceled._");
        }
        catch (Exception ex)
        {
            Component.CompleteAssistantMessage($"_Error: {ex.Message}_");
        }

        var serializedSession = await SerializeSessionAsync(currentAgent, session);
        await Component.CreateAgentCheckpointAsync(
            $"Agent 对话：{Summarize(message)}",
            treeSnapshot,
            serializedSession,
            Component.CaptureAgentTranscript(),
            CancellationToken.None);
    }

    private async Task<ChatClientAgent> EnsureAgentAsync(TreeViewModel tree, ChatMedium medium)
    {
        var signature = $"{tree.EnvironmentVariableName}|{tree.Endpoint}|{tree.Model}";
        if (agent is not null && string.Equals(providerSignature, signature, StringComparison.Ordinal))
        {
            return agent;
        }

        agent = await AgentServices.BuildAgentAsync(tree, medium);
        session = null;
        providerSignature = signature;
        return agent;
    }

    private ChatMedium BuildChatMedium(TreeViewModel tree, string message)
    {
        var scope = tree.AsAgentScope()
            .WithPromptLanguage(AgentLanguages.Chinese)
            .WithOutputLanguage(AgentLanguages.Chinese)
            .WithAutoDiscovery(assemblyName: "AstrolonUI")
            .WithData([typeof(ShellElement)])
            .WithAutoMarkDirty(false)
            .WithMaxToolCalls(200)
            .WithToolCallCallback(_ => Task.CompletedTask)
            .WithSelectionHandler(tree.HandleAgentSelectionAsync)
            .WithConfirmationHandler(tree.HandleAgentConfirmationAsync)
            .WithInteractionSafety(3);

        return new ChatMedium
        {
            Prompt = scope.ProvideProgressiveContextPrompt(),
            Message = message,
            Tools = scope.ProvideTools(),
            AllowStreaming = tree.UseStreamingAgentResponse
        };
    }

    internal async Task RestoreSessionAsync(string serializedSession, CancellationToken cancellationToken = default)
    {
        if (Component is null || string.IsNullOrWhiteSpace(serializedSession))
        {
            session = null;
            return;
        }

        var medium = BuildChatMedium(Component, string.Empty);
        var currentAgent = await EnsureAgentAsync(Component, medium);
        using var document = JsonDocument.Parse(serializedSession);
        session = await currentAgent.DeserializeSessionAsync(
            document.RootElement,
            jsonSerializerOptions: null,
            cancellationToken: cancellationToken);
    }

    private static async Task<string> SerializeSessionAsync(
        ChatClientAgent? currentAgent,
        AgentSession? currentSession)
    {
        if (currentAgent is null || currentSession is null)
        {
            return string.Empty;
        }

        var serialized = await currentAgent.SerializeSessionAsync(
            currentSession,
            jsonSerializerOptions: null,
            cancellationToken: CancellationToken.None);
        return serialized.GetRawText();
    }

    private static string Summarize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "无附加信息。";
        }

        var normalized = string.Join(
            " ",
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 160
            ? normalized
            : $"{normalized[..157]}...";
    }
}

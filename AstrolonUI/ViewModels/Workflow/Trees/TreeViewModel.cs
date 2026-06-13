using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using AstrolonUI.ViewModels.Workflow.Trees.Services;
using Avalonia.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.MVVM.Serialization;
using VeloxDev.TimeLine;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[WorkflowBuilder.Tree<AgentHelper>]
[MonoBehaviour(channel: "AstrolonAITextRefresh", fps: 12)]
public partial class TreeViewModel : IOpenAIProvider
{
    private const string AiTextRefreshChannel = "AstrolonAITextRefresh";

    private readonly StringBuilder transcriptBuilder = new();
    private readonly StringBuilder streamingAssistantBuffer = new();
    private readonly object transcriptRefreshGate = new();
    private string pendingTranscriptText = string.Empty;
    private bool hasPendingTranscriptText;
    private TaskCompletionSource<string?>? pendingAgentSelection;
    private TaskCompletionSource<AgentConfirmationResult>? pendingAgentConfirmation;
    private CancellationTokenSource? agentRequestCancellation;

    public TreeViewModel()
    {
        InitializeWorkflow();
        EnsureRefreshLoop();
        InitializeMonoBehaviour();
        WorkspaceName = "Untitled";
        LocalPath = string.Empty;
        EnvironmentVariableName = "API_KEY_DEEPSEEK";
        Endpoint = "https://api.deepseek.com";
        Model = "deepseek-v4-flash";
        UseStreamingAgentResponse = true;
        AgentTranscriptDocument = new MarkdownDocumentViewModel { Text = "_Waiting for agent message._" };
        AgentSelectionPrompt = string.Empty;
        AgentSelectionOptions = [];
        AgentConfirmationOperationKey = string.Empty;
        AgentConfirmationDescription = string.Empty;
        AgentConfirmationDenySelected = true;
        AgentCheckpoints = [];
        NodeToolGroups = DiscoverNodeTools();
    }

    [VeloxProperty] private bool isWorkflowRunning = false;

    [VeloxProperty] public partial string WorkspaceName { get; set; }

    [VeloxProperty] public partial string LocalPath { get; set; }

    [VeloxProperty] public partial bool UseStreamingAgentResponse { get; set; }

    [VeloxProperty] public partial string EnvironmentVariableName { get; set; }

    [VeloxProperty] public partial string Endpoint { get; set; }

    [VeloxProperty] public partial string Model { get; set; }

    [VeloxProperty] public partial MarkdownDocumentViewModel AgentTranscriptDocument { get; set; }

    [VeloxProperty] public partial bool IsAgentRequestRunning { get; set; }

    [VeloxProperty] public partial bool HasPendingAgentInteraction { get; set; }

    [VeloxProperty] public partial bool HasPendingAgentSelection { get; set; }

    [VeloxProperty] public partial string AgentSelectionPrompt { get; set; }

    [VeloxProperty] public partial ObservableCollection<AgentSelectionOptionViewModel> AgentSelectionOptions { get; set; }

    [VeloxProperty] public partial bool HasPendingAgentConfirmation { get; set; }

    [VeloxProperty] public partial string AgentConfirmationOperationKey { get; set; }

    [VeloxProperty] public partial string AgentConfirmationDescription { get; set; }

    [VeloxProperty] public partial bool AgentConfirmationDenySelected { get; set; }

    [VeloxProperty] public partial bool AgentConfirmationAllowOnceSelected { get; set; }

    [VeloxProperty] public partial bool AgentConfirmationAllowAlwaysSelected { get; set; }

    public ObservableCollection<AgentCheckpointViewModel> AgentCheckpoints { get; }

    [VeloxProperty] public partial ObservableCollection<NodeToolGroupViewModel> NodeToolGroups { get; set; }

    internal Task<string> CaptureAgentTreeSnapshotAsync(CancellationToken cancellationToken)
    {
        return this.SerializeAsync(
            SerializationOptions.Create().WithCompact(),
            cancellationToken);
    }

    internal async Task<string> CreateAgentCheckpointAsync(
        string? summary,
        string treeSnapshot,
        string agentSession,
        string conversation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var checkpoint = new AgentCheckpointViewModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Summary = string.IsNullOrWhiteSpace(summary) ? "Agent checkpoint" : summary.Trim(),
            CreatedAt = DateTimeOffset.Now,
            Snapshot = treeSnapshot,
            AgentSession = agentSession,
            Conversation = conversation
        };

        if (Dispatcher.UIThread.CheckAccess())
        {
            AgentCheckpoints.Insert(0, checkpoint);
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => AgentCheckpoints.Insert(0, checkpoint));
        }

        return $"Checkpoint created: {checkpoint.Id} at {checkpoint.CreatedAt:O}. Summary: {checkpoint.Summary}";
    }

    internal string CaptureAgentTranscript()
    {
        lock (transcriptRefreshGate)
        {
            return hasPendingTranscriptText
                ? pendingTranscriptText
                : AgentTranscriptDocument.Text;
        }
    }

    internal void ShowCheckpointRestoreNotice(string checkpointTime)
    {
        streamingAssistantBuffer.Clear();
        transcriptBuilder.Clear();
        transcriptBuilder.Append($"已切换回 {checkpointTime} 检查点。");
        SetTranscriptText(transcriptBuilder.ToString(), immediate: true);
    }

    internal void PrepareAfterCheckpointRestore(IEnumerable<AgentCheckpointViewModel> checkpoints)
    {
        agentRequestCancellation?.Cancel();
        agentRequestCancellation?.Dispose();
        agentRequestCancellation = null;
        pendingAgentSelection = null;
        pendingAgentConfirmation = null;
        IsAgentRequestRunning = false;
        HasPendingAgentSelection = false;
        HasPendingAgentConfirmation = false;
        HasPendingAgentInteraction = false;
        AgentSelectionPrompt = string.Empty;
        AgentSelectionOptions.Clear();
        AgentConfirmationOperationKey = string.Empty;
        AgentConfirmationDescription = string.Empty;
        AgentConfirmationDenySelected = true;
        AgentConfirmationAllowOnceSelected = false;
        AgentConfirmationAllowAlwaysSelected = false;
        NodeToolGroups = DiscoverNodeTools();

        AgentCheckpoints.Clear();
        foreach (var checkpoint in checkpoints)
        {
            AgentCheckpoints.Add(checkpoint);
        }

        transcriptBuilder.Clear();
        if (!string.IsNullOrWhiteSpace(AgentTranscriptDocument?.Text))
        {
            transcriptBuilder.Append(AgentTranscriptDocument.Text);
        }
    }

    public bool TryCreateNodeTool(
        NodeToolViewModel tool,
        Anchor anchor,
        out IWorkflowNodeViewModel? node)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(anchor);

        node = Activator.CreateInstance(tool.NodeType)
            as IWorkflowNodeViewModel;
        if (node is null)
        {
            return false;
        }

        node.Anchor = anchor;
        if (node.Size.Width <= 0 || node.Size.Height <= 0)
        {
            node.Size = new Size { Width = tool.DefaultWidth, Height = tool.DefaultHeight };
        }

        var nameProperty = tool.NodeType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
        if (nameProperty is not null && nameProperty.PropertyType == typeof(string))
        {
            var currentName = nameProperty.GetValue(node) as string;
            if (string.IsNullOrWhiteSpace(currentName))
            {
                nameProperty.SetValue(node, tool.Name);
            }
        }

        return true;
    }

    private static ObservableCollection<NodeToolGroupViewModel> DiscoverNodeTools()
    {
        var tools = typeof(TreeViewModel).Assembly.GetTypes()
            .Where(IsWorkflowNodeType)
            .Select(CreateNodeToolDescriptor)
            .OrderBy(tool => GetGroupName(tool.NodeType))
            .ThenBy(tool => tool.Name)
            .GroupBy(tool => GetGroupName(tool.NodeType))
            .Select(group => new NodeToolGroupViewModel
            {
                Name = group.Key,
                Tools = new ObservableCollection<NodeToolViewModel>(group)
            });

        return new ObservableCollection<NodeToolGroupViewModel>(tools);
    }

    private static bool IsWorkflowNodeType(Type type)
        => type is { IsAbstract: false, IsClass: true } &&
           type.GetConstructor(Type.EmptyTypes) is not null &&
           type.GetCustomAttributes(inherit: false)
               .Any(attribute => attribute.GetType().IsGenericType &&
                                 attribute.GetType().GetGenericTypeDefinition().Name.StartsWith("NodeAttribute", StringComparison.Ordinal));

    private static NodeToolViewModel CreateNodeToolDescriptor(Type type)
    {
        var contexts = type.GetCustomAttributes<AgentContextAttribute>(inherit: false)
            .Where(attr => attr.Language == AgentLanguages.Chinese)
            .Select(attr => attr.Context)
            .ToArray();

        var (width, height) = ParseDefaultSize(contexts);
        return new NodeToolViewModel
        {
            Name = TrimViewModelSuffix(type.Name),
            Description = contexts.FirstOrDefault(context => !context.StartsWith("默认大小：", StringComparison.Ordinal)) ?? string.Empty,
            NodeType = type,
            DefaultWidth = width,
            DefaultHeight = height
        };
    }

    private static (double Width, double Height) ParseDefaultSize(IEnumerable<string> contexts)
    {
        foreach (var context in contexts)
        {
            var match = Regex.Match(context, @"默认大小：(?<width>\d+(?:\.\d+)?)x(?<height>\d+(?:\.\d+)?)");
            if (match.Success &&
                double.TryParse(match.Groups["width"].Value, out var width) &&
                double.TryParse(match.Groups["height"].Value, out var height))
            {
                return (width, height);
            }
        }

        return (240, 160);
    }

    private static string GetGroupName(Type nodeType)
    {
        var name = nodeType.Name;
        if (name.Contains("Shell", StringComparison.OrdinalIgnoreCase))
        {
            return "Shell";
        }

        if (name.Contains("AI", StringComparison.OrdinalIgnoreCase) || name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return "AI";
        }

        if (name.Contains("Math", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Calculation", StringComparison.OrdinalIgnoreCase))
        {
            return "Math";
        }

        if (name.Contains("Compare", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Delay", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Debug", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Listener", StringComparison.OrdinalIgnoreCase))
        {
            return "Control";
        }

        return "General";
    }

    private static string TrimViewModelSuffix(string name)
        => name.EndsWith("ViewModel", StringComparison.Ordinal)
            ? name[..^"ViewModel".Length]
            : name;
    [VeloxCommand]
    public async Task AskAsync(object? parameter, CancellationToken ct)
    {
        if (IsAgentRequestRunning || GetHelper() is not AgentHelper helper)
        {
            return;
        }

        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        agentRequestCancellation = requestCancellation;
        IsAgentRequestRunning = true;

        try
        {
            await helper.AskAsync(parameter, requestCancellation.Token);
        }
        finally
        {
            CancelPendingAgentInteractions();
            if (ReferenceEquals(agentRequestCancellation, requestCancellation))
            {
                agentRequestCancellation = null;
            }

            IsAgentRequestRunning = false;
            requestCancellation.Dispose();
        }
    }

    [VeloxCommand]
    private void CancelAgentRequest()
    {
        agentRequestCancellation?.Cancel();
        CancelPendingAgentInteractions();
    }

    internal async Task HandleAgentSelectionAsync(AgentSelectionEventArgs args)
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRequest()
        {
            CompleteAgentSelection(null);
            CompleteAgentConfirmation(AgentConfirmationResult.Deny);
            pendingAgentSelection = completion;
            AgentSelectionPrompt = args.Prompt;
            AgentSelectionOptions.Clear();
            foreach (var option in args.Options)
            {
                AgentSelectionOptions.Add(new AgentSelectionOptionViewModel { Text = option });
            }

            HasPendingAgentSelection = true;
            UpdateAgentInteractionState();
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            BeginRequest();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(BeginRequest);
        }

        args.SelectedOption = await completion.Task.ConfigureAwait(false);
    }

    internal async Task HandleAgentConfirmationAsync(AgentConfirmationEventArgs args)
    {
        var completion = new TaskCompletionSource<AgentConfirmationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRequest()
        {
            CompleteAgentSelection(null);
            CompleteAgentConfirmation(AgentConfirmationResult.Deny);
            pendingAgentConfirmation = completion;
            AgentConfirmationOperationKey = args.OperationKey;
            AgentConfirmationDescription = args.Description;
            AgentConfirmationDenySelected = true;
            AgentConfirmationAllowOnceSelected = false;
            AgentConfirmationAllowAlwaysSelected = false;
            HasPendingAgentConfirmation = true;
            UpdateAgentInteractionState();
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            BeginRequest();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(BeginRequest);
        }

        args.Result = await completion.Task.ConfigureAwait(false);
    }

    [VeloxCommand]
    private void SubmitAgentSelection()
    {
        var selected = AgentSelectionOptions.FirstOrDefault(static option => option.IsSelected)?.Text;
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        CompleteAgentSelection(selected);
    }

    [VeloxCommand]
    private void CancelAgentSelection()
    {
        CompleteAgentSelection(null);
    }

    [VeloxCommand]
    private void SubmitAgentConfirmation()
    {
        var result = AgentConfirmationAllowAlwaysSelected
            ? AgentConfirmationResult.AllowAlways
            : AgentConfirmationAllowOnceSelected
                ? AgentConfirmationResult.AllowOnce
                : AgentConfirmationResult.Deny;

        CompleteAgentConfirmation(result);
    }

    internal void CancelPendingAgentInteractions()
    {
        void Cancel()
        {
            CompleteAgentSelection(null);
            CompleteAgentConfirmation(AgentConfirmationResult.Deny);
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Cancel();
        }
        else
        {
            Dispatcher.UIThread.Post(Cancel);
        }
    }

    private void CompleteAgentSelection(string? selected)
    {
        var completion = pendingAgentSelection;
        pendingAgentSelection = null;
        HasPendingAgentSelection = false;
        AgentSelectionPrompt = string.Empty;
        AgentSelectionOptions.Clear();
        UpdateAgentInteractionState();
        completion?.TrySetResult(selected);
    }

    private void CompleteAgentConfirmation(AgentConfirmationResult result)
    {
        var completion = pendingAgentConfirmation;
        pendingAgentConfirmation = null;
        HasPendingAgentConfirmation = false;
        AgentConfirmationOperationKey = string.Empty;
        AgentConfirmationDescription = string.Empty;
        UpdateAgentInteractionState();
        completion?.TrySetResult(result);
    }

    private void UpdateAgentInteractionState()
    {
        HasPendingAgentInteraction = HasPendingAgentSelection || HasPendingAgentConfirmation;
    }

    private static void EnsureRefreshLoop()
    {
        if (!MonoBehaviourManager.IsRunning(AiTextRefreshChannel))
        {
            MonoBehaviourManager.Start(AiTextRefreshChannel);
        }
    }

    partial void Update(FrameEventArgs e)
    {
        FlushPendingTranscriptText();
    }

    internal void AppendUserMessage(string message)
    {
        streamingAssistantBuffer.Clear();
        transcriptBuilder.AppendLine("## User");
        transcriptBuilder.AppendLine();
        transcriptBuilder.AppendLine(message.Trim());
        transcriptBuilder.AppendLine();
        SetTranscriptText(transcriptBuilder.ToString(), immediate: true);
    }

    internal void BeginAssistantMessage()
    {
        streamingAssistantBuffer.Clear();
        SetTranscriptText($"{transcriptBuilder}\n## Assistant\n\n_Thinking..._", immediate: true);
    }

    internal void CompleteAssistantMessage(string? text)
    {
        streamingAssistantBuffer.Clear();
        var message = string.IsNullOrWhiteSpace(text) ? "_No response._" : text.Trim();
        AppendAssistantMessage(message);
    }

    internal void AppendAssistantChunk(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            streamingAssistantBuffer.Append(text);
        }

        var message = streamingAssistantBuffer.Length == 0
            ? "_Thinking..._"
            : streamingAssistantBuffer.ToString();

        SetTranscriptText($"{transcriptBuilder}\n## Assistant\n\n{message}");
    }

    private void AppendAssistantMessage(string message)
    {
        transcriptBuilder.AppendLine("## Assistant");
        transcriptBuilder.AppendLine();
        transcriptBuilder.AppendLine(message);
        transcriptBuilder.AppendLine();
        SetTranscriptText(transcriptBuilder.ToString(), immediate: true);
    }

    private void SetTranscriptText(string text, bool immediate = false)
    {
        lock (transcriptRefreshGate)
        {
            pendingTranscriptText = text;
            hasPendingTranscriptText = true;
        }

        if (immediate)
        {
            FlushPendingTranscriptText();
        }
    }

    private void FlushPendingTranscriptText()
    {
        string text;
        lock (transcriptRefreshGate)
        {
            if (!hasPendingTranscriptText)
            {
                return;
            }

            text = pendingTranscriptText;
            hasPendingTranscriptText = false;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            AgentTranscriptDocument.Text = text;
            return;
        }

        Dispatcher.UIThread.Post(() => AgentTranscriptDocument.Text = text);
    }
}




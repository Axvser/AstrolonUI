using AstrolonUI.ViewModels.Workflow.Nodes.AI.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.TimeLine;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "Shell结果分析节点，接收ShellExecutionResultViewModel并通过可选AI配置节点生成Markdown分析")]
[AgentContext(AgentLanguages.Chinese, "默认大小：420x320")]
[WorkflowBuilder.Node<ShellResultsService>]
[MonoBehaviour(channel: "AstrolonAITextRefresh", fps: 12)]
public partial class ShellResultsViewModel : IChatCallBackProvider
{
    private const string AiTextRefreshChannel = "AstrolonAITextRefresh";

    private readonly Dictionary<string, ShellExecution> shellResults = [];
    private readonly Dictionary<string, StringBuilder> aiSummaryBuffers = [];
    private readonly HashSet<string> completedAiRequests = [];
    private readonly object documentRefreshGate = new();
    private string pendingDocumentText = string.Empty;
    private bool hasPendingDocumentText;

    public EventHandler<string>? OnSyncResponse { get; set; }
    public EventHandler<string>? OnStreamChunk { get; set; }

    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "分析提示词")]
    [VeloxProperty] public partial string Instructions { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否启用AI汇总；关闭时仅呈现原始Shell执行结果")]
    [VeloxProperty] public partial bool EnableAISummary { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否使用流式AI响应")]
    [VeloxProperty] public partial bool AllowStreaming { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近接收到的OpenAI配置；为空时无法执行AI汇总")]
    [VeloxProperty] public partial OpenAIConfig? AgentProvider { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近一次Shell执行链路结果")]
    [VeloxProperty] public partial ShellExecution? LastResult { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Markdown分析文档")]
    [VeloxProperty] public partial MarkdownDocumentViewModel SummaryDocument { get; set; }

    [AgentContext(AgentLanguages.Chinese, "按执行Id分组的Shell结果")]
    [VeloxProperty] public partial ObservableCollection<ShellResultGroupViewModel> ResultGroups { get; set; }

    [VeloxProperty] public partial bool HasResults { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Shell结果输入槽")]
    [VeloxProperty] public partial SlotViewModel ResultInputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "OpenAI配置输入槽")]
    [VeloxProperty] public partial SlotViewModel AgentInputSlot { get; set; }

    public ShellResultsViewModel()
    {
        InitializeWorkflow();
        EnsureRefreshLoop();
        InitializeMonoBehaviour();
        Name = "Shell Results";
        Instructions = "你是一个代码测试专家，专注于分析 Shell 执行链路，指出是否成功、关键输出、错误原因、风险和下一步建议。输出简洁的 Markdown。";
        EnableAISummary = true;
        AllowStreaming = true;
        SummaryDocument = new MarkdownDocumentViewModel { Text = "_Waiting for shell result._" };
        ResultGroups = [];
        ResultInputSlot = new SlotViewModel();
        AgentInputSlot = new SlotViewModel();

        OnSyncResponse = (sender, text) => SetAISummary(sender?.ToString(), text);
        OnStreamChunk = (sender, text) => AppendAISummary(sender?.ToString(), text);
    }

    public bool HasRequestedAISummary(string id) => completedAiRequests.Contains(id);

    public void ShowShellResult(ShellExecution result)
    {
        LastResult = result;
        shellResults[result.Id] = result;
        RefreshResultGroup(result.Id);
        SetDocumentText(BuildDocumentMarkdown(), immediate: true);
    }

    public void ShowAgentReady(OpenAIConfig provider)
    {
        AgentProvider = provider;
        if (shellResults.Count == 0)
        {
            SetDocumentText($"_OpenAI config ready: `{provider.Model}`._", immediate: true);
            return;
        }

        SetDocumentText(BuildDocumentMarkdown(), immediate: true);
    }

    public void BeginAISummary(string id)
    {
        completedAiRequests.Add(id);
        aiSummaryBuffers[id] = new StringBuilder("_Analyzing..._");
        RefreshResultGroup(id);
        SetDocumentText(BuildDocumentMarkdown(), immediate: true);
    }

    public void SetAISummary(string? id, string? text)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var buffer = GetAISummaryBuffer(id);
        buffer.Clear();
        buffer.Append(string.IsNullOrWhiteSpace(text) ? "_No AI summary._" : text.Trim());
        RefreshResultGroup(id);
        SetDocumentText(BuildDocumentMarkdown(), immediate: true);
    }

    public void AppendAISummary(string? id, string? text)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var buffer = GetAISummaryBuffer(id);
        if (buffer.ToString() == "_Analyzing..._")
        {
            buffer.Clear();
        }

        if (!string.IsNullOrEmpty(text))
        {
            buffer.Append(text);
        }

        RefreshResultGroup(id);
        SetDocumentText(BuildDocumentMarkdown());
    }

    public void AppendSystemNote(string message)
    {
        var id = LastResult?.Id ?? "system";
        var buffer = GetAISummaryBuffer(id);
        buffer.Clear();
        buffer.Append($"> {message}");
        RefreshResultGroup(id);
        SetDocumentText(BuildDocumentMarkdown(), immediate: true);
    }

    private StringBuilder GetAISummaryBuffer(string id)
    {
        if (!aiSummaryBuffers.TryGetValue(id, out var buffer))
        {
            buffer = new StringBuilder();
            aiSummaryBuffers[id] = buffer;
        }

        return buffer;
    }

    private string BuildDocumentMarkdown()
    {
        if (shellResults.Count == 0)
        {
            return "_Waiting for shell result._";
        }

        var builder = new StringBuilder();
        foreach (var result in shellResults.Values.OrderBy(result => result.StartedAt))
        {
            builder.Append(result.ToDisplayMarkdown());
            if (aiSummaryBuffers.TryGetValue(result.Id, out var aiBuffer))
            {
                builder.AppendLine($"### AI Summary `{result.Id}`");
                builder.AppendLine();
                builder.AppendLine(aiBuffer.Length == 0 ? "_No AI summary._" : aiBuffer.ToString());
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private void RefreshResultGroup(string id)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => RefreshResultGroup(id));
            return;
        }

        if (!shellResults.TryGetValue(id, out var result))
        {
            return;
        }

        var group = ResultGroups.FirstOrDefault(item => item.Id == id);
        if (group is null)
        {
            group = new ShellResultGroupViewModel
            {
                Id = id,
                IsExpanded = false
            };
            ResultGroups.Add(group);
        }

        group.Header = $"{result.StartedAt:yyyy-MM-dd HH:mm:ss}  {id[..Math.Min(8, id.Length)]}";
        group.Status = result.IsCompleted
            ? $"Completed · Exit {(result.ExitCode?.ToString() ?? "-")}"
            : "Running";
        group.Document.Text = BuildResultMarkdown(result);
        HasResults = ResultGroups.Count > 0;
    }

    private string BuildResultMarkdown(ShellExecution result)
    {
        var builder = new StringBuilder(result.ToDisplayMarkdown());
        if (aiSummaryBuffers.TryGetValue(result.Id, out var aiBuffer))
        {
            builder.AppendLine($"### AI Summary `{result.Id}`");
            builder.AppendLine();
            builder.AppendLine(aiBuffer.Length == 0 ? "_No AI summary._" : aiBuffer.ToString());
            builder.AppendLine();
        }

        return builder.ToString();
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
        FlushPendingDocumentText();
    }

    private void SetDocumentText(string text, bool immediate = false)
    {
        lock (documentRefreshGate)
        {
            pendingDocumentText = text;
            hasPendingDocumentText = true;
        }

        if (immediate)
        {
            FlushPendingDocumentText();
        }
    }

    private void FlushPendingDocumentText()
    {
        string text;
        lock (documentRefreshGate)
        {
            if (!hasPendingDocumentText)
            {
                return;
            }

            text = pendingDocumentText;
            hasPendingDocumentText = false;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            SummaryDocument.Text = text;
            return;
        }

        Dispatcher.UIThread.Post(() => SummaryDocument.Text = text);
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using VeloxDev.AI;
using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

[AgentContext(AgentLanguages.Chinese, "一次完整Shell链执行记录，按唯一Id聚合命令、输出、错误和完成状态")]
public partial class ShellExecution
{
    [AgentContext(AgentLanguages.Chinese, "本次Shell链执行的唯一标识")]
    [VeloxProperty] public partial string Id { get; set; }

    [AgentContext(AgentLanguages.Chinese, "结果路由管线")]
    [VeloxProperty] public partial ShellResultPipelines Pipeline { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Shell命令链")]
    [VeloxProperty] public partial ObservableCollection<Shell> Shells { get; set; }

    [AgentContext(AgentLanguages.Chinese, "按命令分组的执行记录")]
    [VeloxProperty] public partial ObservableCollection<ShellExecutionEntryViewModel> Entries { get; set; }

    [AgentContext(AgentLanguages.Chinese, "进程Id")]
    [VeloxProperty] public partial int ProcessId { get; set; }

    [AgentContext(AgentLanguages.Chinese, "退出码")]
    [VeloxProperty] public partial int? ExitCode { get; set; }

    [AgentContext(AgentLanguages.Chinese, "开始时间")]
    [VeloxProperty] public partial DateTimeOffset StartedAt { get; set; }

    [AgentContext(AgentLanguages.Chinese, "结束时间")]
    [VeloxProperty] public partial DateTimeOffset? FinishedAt { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否已经完成")]
    [VeloxProperty] public partial bool IsCompleted { get; set; }

    [AgentContext(AgentLanguages.Chinese, "执行错误")]
    [VeloxProperty] public partial string Error { get; set; }

    public ShellExecution()
    {
        Id = Guid.NewGuid().ToString("N");
        Shells = [];
        Entries = [];
        Error = string.Empty;
        StartedAt = DateTimeOffset.Now;
    }

    public void AddStandardOutput(string text)
    {
        foreach (var line in SplitLines(text))
        {
            EnsureCurrentEntry().AddStandardOutput(line);
        }
    }

    public void AddStandardError(string text)
    {
        foreach (var line in SplitLines(text))
        {
            EnsureCurrentEntry().AddStandardError(line);
        }
    }

    public void SetError(string message)
    {
        Error = message ?? string.Empty;
        AddStandardError(Error);
    }

    public ShellExecutionResultViewModel CloneSnapshot()
    {
        return new ShellExecutionResultViewModel
        {
            Id = Id,
            Pipeline = Pipeline,
            Shells = new ObservableCollection<Shell>(Shells.Select(CloneShell)),
            Entries = new ObservableCollection<ShellExecutionEntryViewModel>(Entries.Select(entry => entry.CloneSnapshot())),
            ProcessId = ProcessId,
            ExitCode = ExitCode,
            StartedAt = StartedAt,
            FinishedAt = FinishedAt,
            IsCompleted = IsCompleted,
            Error = Error
        };
    }

    public string ToDisplayMarkdown()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"## Shell Execution `{Id}`");
        builder.AppendLine();
        builder.AppendLine($"- Pipeline: `{Pipeline}`");
        builder.AppendLine($"- Status: {(IsCompleted ? "Completed" : "Running")}");
        builder.AppendLine($"- Exit Code: `{(ExitCode.HasValue ? ExitCode.Value.ToString() : "-")}`");
        builder.AppendLine($"- Started: `{StartedAt:yyyy-MM-dd HH:mm:ss}`");

        if (FinishedAt is not null)
        {
            builder.AppendLine($"- Finished: `{FinishedAt:yyyy-MM-dd HH:mm:ss}`");
        }

        if (!string.IsNullOrWhiteSpace(Error))
        {
            builder.AppendLine($"- Error: `{Error}`");
        }

        builder.AppendLine();

        foreach (var entry in Entries)
        {
            builder.Append(entry.ToDisplayMarkdown());
        }

        return builder.ToString();
    }

    public string ToPromptMarkdown() => ToDisplayMarkdown();

    private ShellExecutionEntryViewModel EnsureCurrentEntry()
    {
        if (Entries.Count == 0)
        {
            Entries.Add(new ShellExecutionEntryViewModel());
        }

        return Entries[^1];
    }

    private static string[] SplitLines(string text)
        => string.IsNullOrEmpty(text)
            ? []
            : text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static Shell CloneShell(Shell shell)
        => new()
        {
            ExecutionPath = shell.ExecutionPath,
            Wrap = shell.Wrap,
            Arguments = [.. shell.Arguments]
        };
}

[AgentContext(AgentLanguages.Chinese, "ShellAggregate传给下游节点的Shell执行结果快照")]
public partial class ShellExecutionResultViewModel : ShellExecution
{
}

[AgentContext(AgentLanguages.Chinese, "单段Shell命令的执行记录，包含命令文本、标准输出和标准错误")]
public partial class ShellExecutionEntryViewModel
{
    [AgentContext(AgentLanguages.Chinese, "命令")]
    [VeloxProperty] public partial Shell Command { get; set; }

    [AgentContext(AgentLanguages.Chinese, "标准输出")]
    [VeloxProperty] public partial ObservableCollection<string> StandardOutput { get; set; }

    [AgentContext(AgentLanguages.Chinese, "标准错误")]
    [VeloxProperty] public partial ObservableCollection<string> StandardError { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否在结果节点展开")]
    [VeloxProperty] public partial bool IsExpanded { get; set; }

    public ShellExecutionEntryViewModel()
    {
        Command = new Shell();
        StandardOutput = [];
        StandardError = [];
    }

    public void AddStandardOutput(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            StandardOutput.Add(text);
        }
    }

    public void AddStandardError(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            StandardError.Add(text);
        }
    }

    public ShellExecutionEntryViewModel CloneSnapshot()
    {
        return new ShellExecutionEntryViewModel
        {
            Command = new Shell
            {
                ExecutionPath = Command.ExecutionPath,
                Wrap = Command.Wrap,
                Arguments = [.. Command.Arguments]
            },
            StandardOutput = [.. StandardOutput],
            StandardError = [.. StandardError],
            IsExpanded = IsExpanded
        };
    }

    public string ToDisplayMarkdown()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"### `$ {Command.ToCommandLine()}`");
        builder.AppendLine();

        if (StandardOutput.Count > 0)
        {
            builder.AppendLine("**stdout**");
            builder.AppendLine();
            builder.AppendLine("```text");
            foreach (var line in StandardOutput)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine("```");
            builder.AppendLine();
        }

        if (StandardError.Count > 0)
        {
            builder.AppendLine("**stderr**");
            builder.AppendLine();
            builder.AppendLine("```text");
            foreach (var line in StandardError)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine("```");
            builder.AppendLine();
        }

        if (StandardOutput.Count == 0 && StandardError.Count == 0)
        {
            builder.AppendLine("_Waiting for output._");
            builder.AppendLine();
        }

        return builder.ToString();
    }
}

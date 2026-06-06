using System.Collections.ObjectModel;
using System.Linq;
using VeloxDev.AI;
using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

[AgentContext(AgentLanguages.Chinese, "单个Shell命令")]
public partial class Shell
{
    public Shell()
    {
        ExecutionPath = string.Empty;
        Wrap = string.Empty;
        Arguments = [];
    }

    [AgentContext(AgentLanguages.Chinese, "命令执行路径，必须是有效工作目录")]
    [VeloxProperty] public partial string ExecutionPath { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令头，例如：git / python / npm / dotnet")]
    [VeloxProperty] public partial string Wrap { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令参数列表")]
    [VeloxProperty] public partial ObservableCollection<string> Arguments { get; set; }

    public static Shell From(IShellProvider provider)
        => new()
        {
            ExecutionPath = provider.ExecutionPath,
            Wrap = provider.Wrap.Value,
            Arguments = [.. provider.Arguments.Select(argument => argument.Value)]
        };

    public string ToCommandLine()
    {
        return string.Join(" ", new[] { Wrap }.Concat(Arguments.Select(EscapeArgument)));
    }

    private static string EscapeArgument(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace)
            ? $"\"{value.Replace("\"", "\\\"")}\""
            : value;
    }
}

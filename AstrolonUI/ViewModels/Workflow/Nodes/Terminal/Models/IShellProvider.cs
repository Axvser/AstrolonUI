using System.Collections.ObjectModel;
using VeloxDev.AI;

namespace AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;

public interface IShellProvider
{
    [AgentContext(AgentLanguages.Chinese, "命令执行路径，必须配置为有效工作目录，例如：E:/VisualStudio/Projects/AstrolonUI")]
    public string ExecutionPath { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令集，例如：git / python / npm / dotnet")]
    public ShellElement Wrap { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令行参数，例如: --version / -p:PublishSingleFile=true / -o E://")]
    public ObservableCollection<ShellElement> Arguments { get; set; }
}

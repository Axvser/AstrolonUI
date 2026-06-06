using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "Shell配置节点，只描述单段命令，不负责执行CliWrap任务")]
[AgentContext(AgentLanguages.Chinese, "默认大小：330x260")]
[WorkflowBuilder.Node<ShellConfigService>]
public partial class ShellConfigViewModel : IShellProvider
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令执行路径，必须配置为有效的绝对路径，ShellAggregate最终会在这个路径下构建CliWrap命令")]
    [VeloxProperty] public partial string ExecutionPath { get; set; }

    [AgentContext(AgentLanguages.Chinese, "[ShellElement] 命令头，例如：git / python / npm / dotnet")]
    [VeloxProperty] public partial ShellElement Wrap { get; set; }

    [AgentContext(AgentLanguages.Chinese, "[ObservableCollection<ShellElement>] 命令参数，例如: --version / -p:PublishSingleFile=true / -c Release")]
    [VeloxProperty] public partial ObservableCollection<ShellElement> Arguments { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Shell链输入槽，接收ShellMedium后追加本节点配置")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Shell链输出槽，将追加后的ShellMedium传给下游Shell或ShellAggregate")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public ShellConfigViewModel()
    {
        InitializeWorkflow();
        Name = string.Empty;
        ExecutionPath = string.Empty;
        Wrap = new();
        Arguments = [];
        InputSlot = new SlotViewModel();
        OutputSlot = new SlotViewModel();
    }

    partial void OnItemAddedToArguments(IEnumerable<ShellElement> items)
    {
        foreach (var shell in items)
        {
            shell.Parent = Arguments;
        }
    }

    partial void OnItemRemovedFromArguments(IEnumerable<ShellElement> items)
    {
        foreach (var shell in items)
        {
            shell.Parent = null;
        }
    }

    [VeloxCommand]
    [AgentContext(AgentLanguages.Chinese, "清空命令参数")]
    private void ClearArguments()
    {
        Arguments = [];
    }

    [VeloxCommand]
    [AgentContext(AgentLanguages.Chinese, "增加一个或多个命令参数")]
    [AgentCommandParameter(typeof(IEnumerable<string>))]
    private void AddArguments(object? parameter)
    {
        var args = (parameter as IEnumerable<string>)?.Select(str => new ShellElement
        {
            Value = str
        }).ToArray();

        foreach (var arg in args ?? [new ShellElement { Value = string.Empty }])
        {
            Arguments.Add(arg);
        }
    }

    [VeloxCommand]
    [AgentContext(AgentLanguages.Chinese, "移除特定索引处的命令参数")]
    [AgentCommandParameter(typeof(IEnumerable<int>))]
    private void RemoveArguments(object? parameter)
    {
        List<ShellElement> items = [];

        foreach (var index in parameter as IEnumerable<int> ?? [])
        {
            if (index >= 0 && index < Arguments.Count)
            {
                items.Add(Arguments[index]);
            }
        }

        foreach (var shell in items)
        {
            shell.DeleteCommand.Execute(null);
        }
    }
}

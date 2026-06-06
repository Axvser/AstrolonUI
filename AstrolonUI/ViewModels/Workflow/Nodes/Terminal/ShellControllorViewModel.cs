using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;
using System;
using System.Collections.Generic;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "Shell链控制器，只负责发起ShellMedium构建与传播，不配置具体命令，不构建CliWrap任务")]
[AgentContext(AgentLanguages.Chinese, "默认大小：220x120")]
[WorkflowBuilder.Node<ShellControllorService>]
public partial class ShellControllorViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "ShellAggregate监听CliWrap事件后转发的结果管线")]
    [VeloxProperty] public partial ShellResultPipelines Pipeline { get; set; }

    [AgentContext(AgentLanguages.Chinese, "可选择的CliWrap结果管线")]
    public IReadOnlyList<ShellResultPipelines> PipelineOptions { get; } =
        [.. Enum.GetValues<ShellResultPipelines>()];

    [AgentContext(AgentLanguages.Chinese, "Shell链输出槽，向第一个Shell配置节点发起ShellMedium")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public ShellControllorViewModel()
    {
        InitializeWorkflow();
        Name = "Shell Controller";
        Pipeline = ShellResultPipelines.Std_Output;
        OutputSlot = new SlotViewModel();
    }
}




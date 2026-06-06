using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;
using System;
using System.Collections.Generic;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "数值计算节点，按计算上下文Id等待指定数量的参数，参数到齐后计算")]
[AgentContext(AgentLanguages.Chinese, "默认大小：260x160")]
[WorkflowBuilder.Node<MathService>]
public partial class MathViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算操作")]
    [VeloxProperty] public partial NumericOperation Operation { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近一次计算结果")]
    [VeloxProperty] public partial double LastResult { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近一次计算Id")]
    [VeloxProperty] public partial string LastCalculationId { get; set; }

    [AgentContext(AgentLanguages.Chinese, "当前已等待到的参数数量")]
    [VeloxProperty] public partial int WaitingParameterCount { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算上下文输入槽")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算结果输出槽")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public IReadOnlyList<NumericOperation> OperationOptions { get; } =
        [.. Enum.GetValues<NumericOperation>()];

    public MathViewModel()
    {
        InitializeWorkflow();
        Name = "Math";
        Operation = NumericOperation.Add;
        LastCalculationId = string.Empty;
        InputSlot = new SlotViewModel();
        OutputSlot = new SlotViewModel();
    }
}

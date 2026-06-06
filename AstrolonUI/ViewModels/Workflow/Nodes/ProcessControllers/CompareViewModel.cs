using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;
using System;
using System.Collections.Generic;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "比较分流节点，将输入与配置值比较后从True或False槽输出布尔结果")]
[AgentContext(AgentLanguages.Chinese, "默认大小：280x170")]
[WorkflowBuilder.Node<CompareService>]
public partial class CompareViewModel : IConditionSlotProvider
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "比较操作")]
    [VeloxProperty] public partial ComparisonOperation Operation { get; set; }

    [AgentContext(AgentLanguages.Chinese, "比较目标值")]
    [VeloxProperty] public partial string TargetValue { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近一次比较结果")]
    [VeloxProperty] public partial bool LastResult { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输入槽")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "True/False输出槽")]
    [SlotSelectors(typeof(bool))]
    [VeloxProperty] public partial SlotEnumerator<SlotViewModel> OutputSlots { get; set; }

    public IReadOnlyList<ComparisonOperation> OperationOptions { get; } =
        [.. Enum.GetValues<ComparisonOperation>()];

    public CompareViewModel()
    {
        InitializeWorkflow();
        Name = "Compare";
        Operation = ComparisonOperation.Equal;
        TargetValue = string.Empty;
        InputSlot = new SlotViewModel();
        OutputSlots = new SlotEnumerator<SlotViewModel>();
        OutputSlots.SetSelector(typeof(bool));
    }

    public bool TryGetSlot(object? condition, out IWorkflowSlotViewModel? slot)
    {
        if (condition is bool value &&
            OutputSlots.TrySelect(value, out SlotViewModel? result))
        {
            slot = result;
            return true;
        }

        slot = null;
        return false;
    }
}

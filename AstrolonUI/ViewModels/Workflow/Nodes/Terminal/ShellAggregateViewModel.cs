using AstrolonUI.ViewModels.Workflow.Common;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Models;
using AstrolonUI.ViewModels.Workflow.Nodes.Terminal.Services;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "Shell聚合执行节点，接收ShellMedium中的Shell链并构建完整CliWrap管道任务")]
[AgentContext(AgentLanguages.Chinese, "默认大小：260x140")]
[WorkflowBuilder.Node<ShellAggregateService>(10)]
public partial class ShellAggregateViewModel : IShellExecutionState, IConditionSlotProvider
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "命令链是否正在运行；由ShellAggregateService维护，外部只读")]
    [VeloxProperty] public partial bool IsRunning { get; internal set; }

    [AgentContext(AgentLanguages.Chinese, "Shell链输入槽，接收由Shell配置节点累积的ShellMedium")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "Shell命令行结果处理路由")]
    [SlotSelectors(typeof(ShellResultPipelines), typeof(bool))]
    [VeloxProperty] public partial SlotEnumerator<SlotViewModel> OutputSlots { get; set; }

    public ShellAggregateViewModel()
    {
        InitializeWorkflow();
        Name = "Shell Aggregate";
        InputSlot = new SlotViewModel();
        OutputSlots = new SlotEnumerator<SlotViewModel>();
        OutputSlots.SetSelector(typeof(ShellResultPipelines));
    }

    public void SetRunningState(bool isRunning)
    {
        IsRunning = isRunning;
    }

    public bool TryGetSlot(object? condition, out IWorkflowSlotViewModel? slot)
    {
        if (condition is not null &&
            OutputSlots.TrySelect(condition, out SlotViewModel? result))
        {
            slot = result;
            return true;
        }

        slot = null;
        return false;
    }

    void IShellExecutionState.SetRunningState(bool isRunning)
    {
        SetRunningState(isRunning);
    }
}

using AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "延迟节点，等待指定毫秒后继续传递原始输入")]
[AgentContext(AgentLanguages.Chinese, "默认大小：220x130")]
[WorkflowBuilder.Node<DelayService>]
public partial class DelayViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "延迟毫秒数")]
    [VeloxProperty] public partial int DelayMilliseconds { get; set; }

    [AgentContext(AgentLanguages.Chinese, "是否正在等待")]
    [VeloxProperty] public partial bool IsWaiting { get; internal set; }

    [AgentContext(AgentLanguages.Chinese, "输入槽")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输出槽")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public DelayViewModel()
    {
        InitializeWorkflow();
        Name = "Delay";
        DelayMilliseconds = 1000;
        InputSlot = new SlotViewModel();
        OutputSlot = new SlotViewModel();
    }
}

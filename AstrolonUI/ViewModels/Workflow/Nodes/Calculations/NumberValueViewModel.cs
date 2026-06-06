using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "数值参数节点，接收计算上下文后向其中追加一个参数")]
[AgentContext(AgentLanguages.Chinese, "默认大小：240x140")]
[WorkflowBuilder.Node<NumberValueService>]
public partial class NumberValueViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输出数值")]
    [VeloxProperty] public partial double Value { get; set; }

    [AgentContext(AgentLanguages.Chinese, "参数名称；为空时使用节点名称")]
    [VeloxProperty] public partial string ParameterName { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算上下文输入槽")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "数值输出槽")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public NumberValueViewModel()
    {
        InitializeWorkflow();
        Name = "Number";
        ParameterName = string.Empty;
        Value = 0;
        InputSlot = new SlotViewModel();
        OutputSlot = new SlotViewModel();
    }
}

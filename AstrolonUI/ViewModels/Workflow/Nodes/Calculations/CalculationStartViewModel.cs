using AstrolonUI.ViewModels.Workflow.Nodes.Calculations.Services;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "计算始发节点，创建携带唯一Id和期望参数数量的计算上下文")]
[AgentContext(AgentLanguages.Chinese, "默认大小：260x150")]
[WorkflowBuilder.Node<CalculationStartService>]
public partial class CalculationStartViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算链路Id；为空时每次触发自动生成")]
    [VeloxProperty] public partial string CalculationId { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算节点需要等待的参数数量")]
    [VeloxProperty] public partial int RequiredParameterCount { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近一次实际发出的计算Id")]
    [VeloxProperty] public partial string LastCalculationId { get; set; }

    [AgentContext(AgentLanguages.Chinese, "计算上下文输出槽")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public CalculationStartViewModel()
    {
        InitializeWorkflow();
        Name = "Calculation Start";
        CalculationId = string.Empty;
        LastCalculationId = string.Empty;
        RequiredParameterCount = 2;
        OutputSlot = new SlotViewModel();
    }
}

using AstrolonUI.ViewModels.Workflow.Nodes.ProcessControllers.Services;
using System.Collections.ObjectModel;
using VeloxDev.AI;
using VeloxDev.MVVM;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[AgentContext(AgentLanguages.Chinese, "调试日志节点，记录输入值并继续透传")]
[AgentContext(AgentLanguages.Chinese, "默认大小：300x180")]
[WorkflowBuilder.Node<DebugLogService>]
public partial class DebugLogViewModel
{
    [AgentContext(AgentLanguages.Chinese, "节点名称")]
    [VeloxProperty] public partial string Name { get; set; }

    [AgentContext(AgentLanguages.Chinese, "日志最多保留行数")]
    [VeloxProperty] public partial int MaxEntries { get; set; }

    [AgentContext(AgentLanguages.Chinese, "最近输入值")]
    [VeloxProperty] public partial string LastValue { get; set; }

    [AgentContext(AgentLanguages.Chinese, "日志记录")]
    [VeloxProperty] public partial ObservableCollection<string> Entries { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输入槽")]
    [VeloxProperty] public partial SlotViewModel InputSlot { get; set; }

    [AgentContext(AgentLanguages.Chinese, "输出槽")]
    [VeloxProperty] public partial SlotViewModel OutputSlot { get; set; }

    public DebugLogViewModel()
    {
        InitializeWorkflow();
        Name = "Debug Log";
        MaxEntries = 20;
        LastValue = string.Empty;
        Entries = [];
        InputSlot = new SlotViewModel();
        OutputSlot = new SlotViewModel();
    }
}

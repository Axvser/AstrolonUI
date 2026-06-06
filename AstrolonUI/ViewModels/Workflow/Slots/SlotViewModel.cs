using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[WorkflowBuilder.Slot<SlotHelper>]
public partial class SlotViewModel
{
    public SlotViewModel() => InitializeWorkflow();
}

using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels.Workflow.Common;

public interface IConditionSlotProvider
{
    public bool TryGetSlot(object? condition, out IWorkflowSlotViewModel? slot);
}

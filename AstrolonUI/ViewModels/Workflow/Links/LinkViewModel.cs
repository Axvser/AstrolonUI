using VeloxDev.WorkflowSystem;

namespace AstrolonUI.ViewModels;

[WorkflowBuilder.Link<LinkHelper>]
public partial class LinkViewModel
{
    public LinkViewModel() => InitializeWorkflow();
}

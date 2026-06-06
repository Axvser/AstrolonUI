using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels;

public partial class AgentSelectionOptionViewModel
{
    [VeloxProperty] public partial string Text { get; set; }

    [VeloxProperty] public partial bool IsSelected { get; set; }

    public AgentSelectionOptionViewModel()
    {
        Text = string.Empty;
    }
}

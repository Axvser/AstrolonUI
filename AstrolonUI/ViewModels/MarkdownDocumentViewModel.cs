using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels;

public partial class MarkdownDocumentViewModel
{
    [VeloxProperty] public partial string Text { get; set; }

    public MarkdownDocumentViewModel()
    {
        Text = string.Empty;
    }
}

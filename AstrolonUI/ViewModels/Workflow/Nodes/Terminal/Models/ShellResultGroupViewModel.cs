using VeloxDev.MVVM;

namespace AstrolonUI.ViewModels;

public partial class ShellResultGroupViewModel
{
    [VeloxProperty] public partial string Id { get; set; }

    [VeloxProperty] public partial string Header { get; set; }

    [VeloxProperty] public partial string Status { get; set; }

    [VeloxProperty] public partial bool IsExpanded { get; set; }

    [VeloxProperty] public partial MarkdownDocumentViewModel Document { get; set; }

    public ShellResultGroupViewModel()
    {
        Id = string.Empty;
        Header = string.Empty;
        Status = string.Empty;
        Document = new MarkdownDocumentViewModel();
    }
}

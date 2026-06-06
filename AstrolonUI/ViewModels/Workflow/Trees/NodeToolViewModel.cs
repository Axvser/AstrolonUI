using System;
using System.Collections.ObjectModel;

namespace AstrolonUI.ViewModels;

public class NodeToolGroupViewModel
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<NodeToolViewModel> Tools { get; set; } = [];
}

public class NodeToolViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type NodeType { get; set; } = typeof(object);
    public double DefaultWidth { get; set; } = 240;
    public double DefaultHeight { get; set; } = 160;
}

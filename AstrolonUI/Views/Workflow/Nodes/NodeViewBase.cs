using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using VeloxDev.DynamicTheme;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(HeadBackground), ["#CCffffff"], ["#CC1e1e1e"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(HeadForeground), ["#1e1e1e"], ["#ffffff"])]
public partial class NodeViewBase : UserControl
{
    public NodeViewBase()
    {
        InitializeTheme();
    }

    public static readonly StyledProperty<IBrush> HeadBackgroundProperty =
        AvaloniaProperty.Register<NodeViewBase, IBrush>(nameof(HeadBackground));

    public IBrush HeadBackground
    {
        get => this.GetValue(HeadBackgroundProperty);
        set => SetValue(HeadBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush> HeadForegroundProperty =
        AvaloniaProperty.Register<NodeViewBase, IBrush>(nameof(HeadForeground));

    public IBrush HeadForeground
    {
        get => this.GetValue(HeadForegroundProperty);
        set => SetValue(HeadForegroundProperty, value);
    }

}

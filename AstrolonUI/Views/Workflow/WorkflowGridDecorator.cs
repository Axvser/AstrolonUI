using AstrolonUI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using VeloxDev.DynamicTheme;
using VeloxDev.WorkflowSystem.AttachedBehaviors;
using WorkflowAnchor = VeloxDev.WorkflowSystem.Anchor;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(SurfaceBackgroundBrush), ["#1E1E1E"], ["#FFFFFF"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(RulerBackgroundBrush), ["#252526"], ["#F5F5F5"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(LabelBrush), ["#D4D4D4"], ["#666666"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(MinorGridBrush), ["#2A2D2E"], ["#EEEEEE"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(MajorGridBrush), ["#3A3D40"], ["#CCCCCC"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(AxisBrush), ["#4D4D4D"], ["#333333"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(TickBrush), ["#808080"], ["#999999"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(DividerBrush), ["#454545"], ["#BBBBBB"])]
public partial class WorkflowGridDecorator : Decorator, IWorkflowGridDecorator
{
    public WorkflowGridDecorator()
    {
        ClipToBounds = true;
        InitializeTheme();
        RegisterDecorator(this);
        AddHandler(PointerMovedEvent, OnToolPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnToolPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        PointerEntered += OnToolPointerEntered;
        PointerExited += OnToolPointerExited;
    }
    private const double MajorLineEpsilon = 0.001;
    private static readonly List<WeakReference<WorkflowGridDecorator>> Decorators = [];
    private static ToolDragSession? toolDragSession;

    private Point previewPoint;
    private bool canRenderToolPreview;

    public static NodeToolViewModel? DraggedTool => toolDragSession?.Tool;

    public static void BeginToolDrag(NodeToolViewModel tool)
    {
        toolDragSession = new ToolDragSession(tool);
        ClearAllToolPreviews();
    }

    public static void CancelToolDrag()
    {
        toolDragSession = null;
        ClearAllToolPreviews();
    }

    private sealed class ToolDragSession(NodeToolViewModel tool)
    {
        public NodeToolViewModel Tool { get; } = tool;
        public WorkflowGridDecorator? ActiveDecorator { get; set; }
        public Point PreviewPoint { get; set; }
    }

    private static void RegisterDecorator(WorkflowGridDecorator decorator)
    {
        Decorators.RemoveAll(static reference => !reference.TryGetTarget(out _));
        if (!Decorators.Exists(reference => reference.TryGetTarget(out var target) && ReferenceEquals(target, decorator)))
        {
            Decorators.Add(new WeakReference<WorkflowGridDecorator>(decorator));
        }
    }

    private static void InvalidateDecorators()
    {
        Decorators.RemoveAll(static reference => !reference.TryGetTarget(out _));
        foreach (var reference in Decorators)
        {
            if (reference.TryGetTarget(out var decorator))
            {
                decorator.InvalidateVisual();
            }
        }
    }

    private static void ClearAllToolPreviews()
    {
        Decorators.RemoveAll(static reference => !reference.TryGetTarget(out _));
        foreach (var reference in Decorators)
        {
            if (reference.TryGetTarget(out var decorator))
            {
                decorator.previewPoint = default;
                decorator.canRenderToolPreview = false;
                decorator.InvalidateVisual();
            }
        }
    }
    public static readonly StyledProperty<double> RulerThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(RulerThickness), 28d);

    public static readonly StyledProperty<double> GridSpacingProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(GridSpacing), 40d);

    public static readonly StyledProperty<int> MajorLineEveryProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, int>(nameof(MajorLineEvery), 5);

    public static readonly StyledProperty<double> ScrollOffsetXProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(ScrollOffsetX));

    public static readonly StyledProperty<double> ScrollOffsetYProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(ScrollOffsetY));

    public static readonly StyledProperty<double> ContentOffsetXProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(ContentOffsetX));

    public static readonly StyledProperty<double> ContentOffsetYProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(ContentOffsetY));

    // 颜色属性
    public static readonly StyledProperty<IBrush> SurfaceBackgroundBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(SurfaceBackgroundBrush));

    public static readonly StyledProperty<IBrush> RulerBackgroundBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(RulerBackgroundBrush));

    public static readonly StyledProperty<IBrush> LabelBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(LabelBrush));

    public static readonly StyledProperty<IBrush> MinorGridBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(MinorGridBrush));

    public static readonly StyledProperty<IBrush> MajorGridBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(MajorGridBrush));

    public static readonly StyledProperty<IBrush> AxisBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(AxisBrush));

    public static readonly StyledProperty<IBrush> TickBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(TickBrush));

    public static readonly StyledProperty<IBrush> DividerBrushProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, IBrush>(nameof(DividerBrush));

    // 新增笔触厚度属性
    public static readonly StyledProperty<double> MinorGridPenThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(MinorGridPenThickness), 2d);

    public static readonly StyledProperty<double> MajorGridPenThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(MajorGridPenThickness), 2d);

    public static readonly StyledProperty<double> AxisPenThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(AxisPenThickness), 2.4d);

    public static readonly StyledProperty<double> TickPenThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(TickPenThickness), 2d);

    public static readonly StyledProperty<double> DividerPenThicknessProperty =
        AvaloniaProperty.Register<WorkflowGridDecorator, double>(nameof(DividerPenThickness), 2d);

    static WorkflowGridDecorator()
    {
        AffectsMeasure<WorkflowGridDecorator>(RulerThicknessProperty);
        AffectsArrange<WorkflowGridDecorator>(RulerThicknessProperty);
        AffectsRender<WorkflowGridDecorator>(
            RulerThicknessProperty,
            GridSpacingProperty,
            MajorLineEveryProperty,
            ScrollOffsetXProperty,
            ScrollOffsetYProperty,
            ContentOffsetXProperty,
            ContentOffsetYProperty,

            // 新增的颜色属性
            SurfaceBackgroundBrushProperty,
            RulerBackgroundBrushProperty,
            LabelBrushProperty,
            MinorGridBrushProperty,
            MajorGridBrushProperty,
            AxisBrushProperty,
            TickBrushProperty,
            DividerBrushProperty,

            // 新增的厚度属性
            MinorGridPenThicknessProperty,
            MajorGridPenThicknessProperty,
            AxisPenThicknessProperty,
            TickPenThicknessProperty,
            DividerPenThicknessProperty);
    }

    // 原有属性实现
    public double RulerThickness
    {
        get => GetValue(RulerThicknessProperty);
        set => SetValue(RulerThicknessProperty, value);
    }

    public double GridSpacing
    {
        get => GetValue(GridSpacingProperty);
        set => SetValue(GridSpacingProperty, value);
    }

    public int MajorLineEvery
    {
        get => GetValue(MajorLineEveryProperty);
        set => SetValue(MajorLineEveryProperty, value);
    }

    public double ScrollOffsetX
    {
        get => GetValue(ScrollOffsetXProperty);
        set => SetValue(ScrollOffsetXProperty, value);
    }

    public double ScrollOffsetY
    {
        get => GetValue(ScrollOffsetYProperty);
        set => SetValue(ScrollOffsetYProperty, value);
    }

    public double ContentOffsetX
    {
        get => GetValue(ContentOffsetXProperty);
        set => SetValue(ContentOffsetXProperty, value);
    }

    public double ContentOffsetY
    {
        get => GetValue(ContentOffsetYProperty);
        set => SetValue(ContentOffsetYProperty, value);
    }

    // 新增颜色属性实现
    public IBrush SurfaceBackgroundBrush
    {
        get => GetValue(SurfaceBackgroundBrushProperty);
        set => SetValue(SurfaceBackgroundBrushProperty, value);
    }

    public IBrush RulerBackgroundBrush
    {
        get => GetValue(RulerBackgroundBrushProperty);
        set => SetValue(RulerBackgroundBrushProperty, value);
    }

    public IBrush LabelBrush
    {
        get => GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    public IBrush MinorGridBrush
    {
        get => GetValue(MinorGridBrushProperty);
        set => SetValue(MinorGridBrushProperty, value);
    }

    public IBrush MajorGridBrush
    {
        get => GetValue(MajorGridBrushProperty);
        set => SetValue(MajorGridBrushProperty, value);
    }

    public IBrush AxisBrush
    {
        get => GetValue(AxisBrushProperty);
        set => SetValue(AxisBrushProperty, value);
    }

    public IBrush TickBrush
    {
        get => GetValue(TickBrushProperty);
        set => SetValue(TickBrushProperty, value);
    }

    public IBrush DividerBrush
    {
        get => GetValue(DividerBrushProperty);
        set => SetValue(DividerBrushProperty, value);
    }

    public double MinorGridPenThickness
    {
        get => GetValue(MinorGridPenThicknessProperty);
        set => SetValue(MinorGridPenThicknessProperty, value);
    }

    public double MajorGridPenThickness
    {
        get => GetValue(MajorGridPenThicknessProperty);
        set => SetValue(MajorGridPenThicknessProperty, value);
    }

    public double AxisPenThickness
    {
        get => GetValue(AxisPenThicknessProperty);
        set => SetValue(AxisPenThicknessProperty, value);
    }

    public double TickPenThickness
    {
        get => GetValue(TickPenThicknessProperty);
        set => SetValue(TickPenThicknessProperty, value);
    }

    public double DividerPenThickness
    {
        get => GetValue(DividerPenThicknessProperty);
        set => SetValue(DividerPenThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var ruler = Math.Max(0, RulerThickness);
        Size childAvailable = new(
            Math.Max(0, availableSize.Width - ruler),
            Math.Max(0, availableSize.Height - ruler));

        Child?.Measure(childAvailable);

        if (Child is null)
        {
            return new Size(ruler, ruler);
        }

        return new Size(
            Child.DesiredSize.Width + ruler,
            Child.DesiredSize.Height + ruler);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var ruler = Math.Max(0, RulerThickness);
        Rect childBounds = new(
            ruler,
            ruler,
            Math.Max(0, finalSize.Width - ruler),
            Math.Max(0, finalSize.Height - ruler));

        Child?.Arrange(childBounds);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var ruler = Math.Max(0, RulerThickness);
        Rect contentRect = new(
            ruler,
            ruler,
            Math.Max(0, bounds.Width - ruler),
            Math.Max(0, bounds.Height - ruler));

        // 使用属性值创建画笔
        var surfaceBgBrush = SurfaceBackgroundBrush;
        var rulerBgBrush = RulerBackgroundBrush;

        context.DrawRectangle(surfaceBgBrush, null, bounds);
        context.DrawRectangle(rulerBgBrush, null, new Rect(0, 0, bounds.Width, ruler));
        context.DrawRectangle(rulerBgBrush, null, new Rect(0, 0, ruler, bounds.Height));

        if (contentRect.Width > 0 && contentRect.Height > 0)
        {
            using (context.PushClip(contentRect))
            {
                context.DrawRectangle(surfaceBgBrush, null, contentRect);
                DrawGrid(context, contentRect);
            }
        }

        DrawRulers(context, bounds, contentRect);
        DrawToolPreview(context, contentRect);
    }

    private void OnToolPointerEntered(object? sender, PointerEventArgs e)
    {
        if (toolDragSession is null)
        {
            return;
        }

        UpdateToolPreview(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnToolPointerMoved(object? sender, PointerEventArgs e)
    {
        if (toolDragSession is null)
        {
            canRenderToolPreview = false;
            return;
        }

        UpdateToolPreview(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnToolPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (toolDragSession is null)
        {
            return;
        }

        e.Handled = true;
        var session = toolDragSession;
        var point = e.GetPosition(this);
        var contentRect = GetContentRect();
        if (ReferenceEquals(session.ActiveDecorator, this) && contentRect.Contains(point) && DataContext is TreeViewModel tree)
        {
            var anchor = ToWorkflowAnchor(point, tree);
            if (tree.TryCreateNodeTool(
                    session.Tool,
                    anchor,
                    out var node) &&
                node is not null)
            {
                tree.CreateNodeCommand.Execute(node);
            }
        }

        CancelToolDrag();
    }

    private void OnToolPointerExited(object? sender, PointerEventArgs e)
    {
        if (toolDragSession is not null && ReferenceEquals(toolDragSession.ActiveDecorator, this))
        {
            CancelToolDrag();
        }
    }

    private void UpdateToolPreview(Point point)
    {
        var session = toolDragSession;
        if (session is null)
        {
            return;
        }

        if (!GetContentRect().Contains(point))
        {
            if (ReferenceEquals(session.ActiveDecorator, this))
            {
                CancelToolDrag();
                return;
            }

            canRenderToolPreview = false;
            InvalidateVisual();
            return;
        }

        session.ActiveDecorator = this;
        session.PreviewPoint = point;
        previewPoint = point;
        canRenderToolPreview = true;
        InvalidateVisual();
    }
    private Rect GetContentRect()
    {
        var ruler = Math.Max(0, RulerThickness);
        return new Rect(
            ruler,
            ruler,
            Math.Max(0, Bounds.Width - ruler),
            Math.Max(0, Bounds.Height - ruler));
    }

    private WorkflowAnchor ToWorkflowAnchor(Point point, TreeViewModel tree)
    {
        var ruler = Math.Max(0, RulerThickness);
        return new WorkflowAnchor
        {
            Horizontal = point.X - ruler + ScrollOffsetX - ContentOffsetX,
            Vertical = point.Y - ruler + ScrollOffsetY - ContentOffsetY,
            Layer = tree.Nodes.Count + 1
        };
    }

    private void DrawToolPreview(DrawingContext context, Rect contentRect)
    {
        var session = toolDragSession;
        if (session is null || !ReferenceEquals(session.ActiveDecorator, this) || !canRenderToolPreview)
        {
            return;
        }

        var point = session.PreviewPoint;
        if (!contentRect.Contains(point))
        {
            return;
        }

        var tool = session.Tool;
        var width = Math.Max(80, tool.DefaultWidth);
        var height = Math.Max(48, tool.DefaultHeight);
        var rect = new Rect(point.X, point.Y, width, height);

        using (context.PushClip(contentRect))
        {
            var fill = new SolidColorBrush(Color.FromArgb(72, 126, 200, 255));
            var border = new Pen(new SolidColorBrush(Color.FromArgb(210, 126, 200, 255)), 2);
            context.DrawRectangle(fill, border, rect, 6, 6);

            var title = new FormattedText(
                tool.Name,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                13,
                LabelBrush);
            context.DrawText(title, new Point(rect.X + 10, rect.Y + 8));
        }
    }
    private void DrawGrid(DrawingContext context, Rect contentRect)
    {
        var spacing = Math.Max(8, GridSpacing);
        var majorStep = spacing * Math.Max(1, MajorLineEvery);
        var worldLeft = ScrollOffsetX - ContentOffsetX;
        var worldTop = ScrollOffsetY - ContentOffsetY;
        var worldRight = worldLeft + contentRect.Width;
        var worldBottom = worldTop + contentRect.Height;

        Pen minorPen = new(MinorGridBrush, MinorGridPenThickness);
        Pen majorPen = new(MajorGridBrush, MajorGridPenThickness);
        Pen axisPen = new(AxisBrush, AxisPenThickness);

        var firstVertical = Math.Floor(worldLeft / spacing) * spacing;
        for (var value = firstVertical; value <= worldRight + spacing; value += spacing)
        {
            var x = contentRect.X + (value - worldLeft);
            var pen = IsNearZero(value) ? axisPen : IsMajorLine(value, majorStep) ? majorPen : minorPen;
            context.DrawLine(pen, new Point(x, contentRect.Y), new Point(x, contentRect.Bottom));
        }

        var firstHorizontal = Math.Floor(worldTop / spacing) * spacing;
        for (var value = firstHorizontal; value <= worldBottom + spacing; value += spacing)
        {
            var y = contentRect.Y + (value - worldTop);
            var pen = IsNearZero(value) ? axisPen : IsMajorLine(value, majorStep) ? majorPen : minorPen;
            context.DrawLine(pen, new Point(contentRect.X, y), new Point(contentRect.Right, y));
        }
    }

    private void DrawRulers(DrawingContext context, Rect bounds, Rect contentRect)
    {
        var ruler = Math.Max(0, RulerThickness);
        var spacing = Math.Max(8, GridSpacing);
        var majorStep = spacing * Math.Max(1, MajorLineEvery);
        var worldLeft = ScrollOffsetX - ContentOffsetX;
        var worldTop = ScrollOffsetY - ContentOffsetY;
        var worldRight = worldLeft + contentRect.Width;
        var worldBottom = worldTop + contentRect.Height;

        Pen dividerPen = new(DividerBrush, DividerPenThickness);
        Pen tickPen = new(TickBrush, TickPenThickness);
        Pen axisPen = new(AxisBrush, AxisPenThickness);
        var labelBrush = LabelBrush;

        context.DrawLine(dividerPen, new Point(ruler, 0), new Point(ruler, bounds.Height));
        context.DrawLine(dividerPen, new Point(0, ruler), new Point(bounds.Width, ruler));

        using (context.PushClip(new Rect(ruler, 0, contentRect.Width, ruler)))
        {
            var firstVertical = Math.Floor(worldLeft / spacing) * spacing;
            for (var value = firstVertical; value <= worldRight + spacing; value += spacing)
            {
                var x = contentRect.X + (value - worldLeft);
                var isMajor = IsMajorLine(value, majorStep);
                var tickLength = isMajor ? ruler - 6 : Math.Max(6, ruler * 0.35);
                var pen = IsNearZero(value) ? axisPen : tickPen;
                context.DrawLine(pen, new Point(x, ruler), new Point(x, ruler - tickLength));

                if (isMajor)
                {
                    DrawLabel(context, value, new Point(x + 3, 2), labelBrush);
                }
            }
        }

        using (context.PushClip(new Rect(0, ruler, ruler, contentRect.Height)))
        {
            var firstHorizontal = Math.Floor(worldTop / spacing) * spacing;
            for (var value = firstHorizontal; value <= worldBottom + spacing; value += spacing)
            {
                var y = contentRect.Y + (value - worldTop);
                var isMajor = IsMajorLine(value, majorStep);
                var tickLength = isMajor ? ruler - 6 : Math.Max(6, ruler * 0.35);
                var pen = IsNearZero(value) ? axisPen : tickPen;
                context.DrawLine(pen, new Point(ruler, y), new Point(ruler - tickLength, y));

                if (isMajor)
                {
                    DrawLabel(context, value, new Point(3, y + 2), labelBrush);
                }
            }
        }
    }

    private static void DrawLabel(DrawingContext context, double value, Point point, IBrush labelBrush)
    {
        var text = Math.Round(value).ToString(CultureInfo.InvariantCulture);
        FormattedText formattedText = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            10,
            labelBrush);

        context.DrawText(formattedText, point);
    }

    private static bool IsMajorLine(double value, double majorStep)
    {
        if (majorStep <= 0)
        {
            return false;
        }

        var remainder = value % majorStep;
        return Math.Abs(remainder) < MajorLineEpsilon
               || Math.Abs(remainder - majorStep) < MajorLineEpsilon
               || Math.Abs(remainder + majorStep) < MajorLineEpsilon;
    }

    private static bool IsNearZero(double value)
        => Math.Abs(value) < MajorLineEpsilon;
}



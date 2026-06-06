using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using VeloxDev.DynamicTheme;
using VeloxDev.WorkflowSystem;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(LineBrush), ["#00ffff"], ["#000000"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(SelectedLineBrush), ["Red"], ["Red"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(SelectionGlowBrush), ["#26B66A12"], ["#33F2B84B"])]
public partial class PolylineCurveView : Control
{
    public PolylineCurveView()
    {
        InitializeTheme();
        InitializeComponent();
        IsHitTestVisible = true;
        Focusable = true;
    }

    #region Styled properties

    public static readonly StyledProperty<double> StartLeftProperty =
        AvaloniaProperty.Register<PolylineCurveView, double>(nameof(StartLeft));
    public static readonly StyledProperty<double> StartTopProperty =
        AvaloniaProperty.Register<PolylineCurveView, double>(nameof(StartTop));
    public static readonly StyledProperty<double> EndLeftProperty =
        AvaloniaProperty.Register<PolylineCurveView, double>(nameof(EndLeft));
    public static readonly StyledProperty<double> EndTopProperty =
        AvaloniaProperty.Register<PolylineCurveView, double>(nameof(EndTop));
    public static readonly StyledProperty<bool> CanRenderProperty =
        AvaloniaProperty.Register<PolylineCurveView, bool>(nameof(CanRender), true);
    public static readonly StyledProperty<bool> IsVirtualProperty =
        AvaloniaProperty.Register<PolylineCurveView, bool>(nameof(IsVirtual), false);
    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<PolylineCurveView, IBrush>(nameof(LineBrush), Brushes.Cyan);
    public static readonly StyledProperty<IBrush> SelectedLineBrushProperty =
        AvaloniaProperty.Register<PolylineCurveView, IBrush>(nameof(SelectedLineBrush), Brushes.OrangeRed);
    public static readonly StyledProperty<IBrush> SelectionGlowBrushProperty =
        AvaloniaProperty.Register<PolylineCurveView, IBrush>(nameof(SelectionGlowBrush), new SolidColorBrush(Color.FromArgb(64, 255, 112, 67)));
    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<PolylineCurveView, double>(nameof(LineThickness), 2.0);
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<PolylineCurveView, bool>(nameof(IsSelected), false);

    public double StartLeft { get => GetValue(StartLeftProperty); set => SetValue(StartLeftProperty, value); }
    public double StartTop { get => GetValue(StartTopProperty); set => SetValue(StartTopProperty, value); }
    public double EndLeft { get => GetValue(EndLeftProperty); set => SetValue(EndLeftProperty, value); }
    public double EndTop { get => GetValue(EndTopProperty); set => SetValue(EndTopProperty, value); }
    public bool CanRender { get => GetValue(CanRenderProperty); set => SetValue(CanRenderProperty, value); }
    public bool IsVirtual { get => GetValue(IsVirtualProperty); set => SetValue(IsVirtualProperty, value); }
    public IBrush LineBrush { get => GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }
    public IBrush SelectedLineBrush { get => GetValue(SelectedLineBrushProperty); set => SetValue(SelectedLineBrushProperty, value); }
    public IBrush SelectionGlowBrush { get => GetValue(SelectionGlowBrushProperty); set => SetValue(SelectionGlowBrushProperty, value); }
    public double LineThickness { get => GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    private bool CanHighlight
        => DataContext is IWorkflowLinkViewModel
        {
            Sender: { Parent: not null },
            Receiver: { Parent: not null }
        };

    static PolylineCurveView()
    {
        AffectsRender<PolylineCurveView>(
            StartLeftProperty, StartTopProperty, EndLeftProperty, EndTopProperty,
            CanRenderProperty, IsVirtualProperty, LineBrushProperty,
            SelectedLineBrushProperty, SelectionGlowBrushProperty,
            LineThicknessProperty, IsSelectedProperty);
    }

    #endregion

    #region Render

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!CanRender) return;

        var points = BuildPoints();
        if (points.Count < 2) return;

        var isHighlighted = IsSelected && CanHighlight;
        var lineBrush = isHighlighted ? SelectedLineBrush : LineBrush;
        var thickness = isHighlighted ? LineThickness + 1.5 : LineThickness;

        if (isHighlighted)
        {
            Pen glowPen = new(SelectionGlowBrush, thickness + 6);
            for (int i = 0; i < points.Count - 1; i++)
                context.DrawLine(glowPen, points[i], points[i + 1]);
        }

        Pen pen;
        if (IsVirtual)
            pen = new Pen(lineBrush, thickness) { DashStyle = new DashStyle([4.0, 2.0], 0) };
        else
            pen = new Pen(lineBrush, thickness);

        for (int i = 0; i < points.Count - 1; i++)
            context.DrawLine(pen, points[i], points[i + 1]);

        if (!IsVirtual)
            DrawArrowhead(context, points[^2], points[^1], lineBrush, thickness);
    }

    private List<Point> BuildPoints()
    {
        Point s = new(StartLeft, StartTop);
        Point e = new(EndLeft, EndTop);

        // Golden ratio short stub on each side (1-φ ≈ 0.382 of half-dx)
        double dx = EndLeft - StartLeft;
        const double phi = 0.6180339887;
        double stub = dx / 2.0 * (1.0 - phi); // ≈ dx × 0.191

        Point p1 = new(s.X + stub, s.Y); // end of start stub
        Point p4 = new(e.X - stub, e.Y); // start of end stub

        // p1 → p4 is a single diagonal/vertical connector
        return [s, p1, p4, e];
    }

    private static void DrawArrowhead(DrawingContext ctx, Point from, Point tip, IBrush brush, double thickness)
    {
        Vector tangent = new(tip.X - from.X, tip.Y - from.Y);
        if (tangent.Length < 0.001) return;
        tangent = tangent.Normalize();

        double arrowLength = 12;
        double arrowWidth = 8;
        Vector perp = new(-tangent.Y, tangent.X);
        Point basePt = new(tip.X - tangent.X * arrowLength, tip.Y - tangent.Y * arrowLength);
        Point wing1 = new(basePt.X + perp.X * (arrowWidth / 2), basePt.Y + perp.Y * (arrowWidth / 2));
        Point wing2 = new(basePt.X - perp.X * (arrowWidth / 2), basePt.Y - perp.Y * (arrowWidth / 2));

        StreamGeometry geo = new();
        using (var c = geo.Open())
        {
            c.BeginFigure(tip, true);
            c.LineTo(wing1);
            c.LineTo(wing2);
        }
        ctx.DrawGeometry(brush, null, geo);
    }

    #endregion

    #region Interaction

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (CanHighlight)
            IsSelected = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        IsSelected = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Delete && IsSelected && CanHighlight)
        {
            if (DataContext is IWorkflowLinkViewModel vm)
                vm.DeleteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private bool HitTestLine(Point pt)
    {
        const double hitRadius = 6.0;
        var points = BuildPoints();
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (DistanceToSegment(pt, points[i], points[i + 1]) <= hitRadius)
                return true;
        }
        return false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        bool over = HitTestLine(pt);
        if (over && !IsSelected && CanHighlight)
        {
            IsSelected = true;
            Focus();
        }
        else if ((!over || !CanHighlight) && IsSelected)
        {
            IsSelected = false;
        }
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var ab = b - a;
        double len2 = ab.X * ab.X + ab.Y * ab.Y;
        if (len2 < 0.0001) return new Vector(p.X - a.X, p.Y - a.Y).Length;
        double t = ((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / len2;
        t = Math.Clamp(t, 0.0, 1.0);
        Point proj = new(a.X + t * ab.X, a.Y + t * ab.Y);
        return new Vector(p.X - proj.X, p.Y - proj.Y).Length;
    }

    #endregion
}

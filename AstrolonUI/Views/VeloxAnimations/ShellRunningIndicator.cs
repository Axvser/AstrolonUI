using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using VeloxDev.DynamicTheme;
using VeloxDev.TransitionSystem;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(TrackBrush), ["#D8E0E6"], ["#27323A"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(ActiveBrush), ["#2F9461"], ["#5EE37A"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(GlowBrush), ["#262F9461"], ["#335EE37A"])]
public partial class ShellRunningIndicator : Control
{
    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, bool>(nameof(IsRunning));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, double>(nameof(Progress));

    public static readonly StyledProperty<double> ActiveOpacityProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, double>(nameof(ActiveOpacity));

    public static readonly StyledProperty<IBrush> TrackBrushProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, IBrush>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush> ActiveBrushProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, IBrush>(nameof(ActiveBrush));

    public static readonly StyledProperty<IBrush> GlowBrushProperty =
        AvaloniaProperty.Register<ShellRunningIndicator, IBrush>(nameof(GlowBrush));

    private static readonly Transition<ShellRunningIndicator>.StateSnapshot RunningAnimation =
        Transition<ShellRunningIndicator>.Create()
            .Property(x => x.Progress, 1d)
            .Effect(new TransitionEffect
            {
                Duration = TimeSpan.FromMilliseconds(1100),
                FPS = 60,
                LoopTime = int.MaxValue
            });

    public ShellRunningIndicator()
    {
        InitializeTheme();
        Height = 4;
        ClipToBounds = true;
        IsHitTestVisible = false;
    }

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double ActiveOpacity
    {
        get => GetValue(ActiveOpacityProperty);
        set => SetValue(ActiveOpacityProperty, value);
    }

    public IBrush TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush ActiveBrush
    {
        get => GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    public IBrush GlowBrush
    {
        get => GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    static ShellRunningIndicator()
    {
        IsRunningProperty.Changed.AddClassHandler<ShellRunningIndicator>((x, e) => x.OnIsRunningChanged((bool)e.NewValue!));
        AffectsRender<ShellRunningIndicator>(
            IsRunningProperty,
            ProgressProperty,
            ActiveOpacityProperty,
            TrackBrushProperty,
            ActiveBrushProperty,
            GlowBrushProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (IsRunning)
        {
            StartAnimation();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Transition.Exit(this);
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(TrackBrush, null, bounds);

        if (!IsRunning && ActiveOpacity <= 0)
        {
            return;
        }

        var width = bounds.Width;
        var height = bounds.Height;
        var segmentWidth = Math.Clamp(width * 0.32, 36, Math.Max(36, width * 0.58));
        var travel = width + segmentWidth;
        var left = -segmentWidth + Math.Clamp(Progress, 0, 1) * travel;
        Rect activeRect = new(left, 0, segmentWidth, height);
        Rect glowRect = new(left - segmentWidth * 0.18, 0, segmentWidth * 1.36, height);

        using (context.PushOpacity(Math.Clamp(ActiveOpacity, 0, 1)))
        {
            context.DrawRectangle(GlowBrush, null, glowRect);
            context.DrawRectangle(ActiveBrush, null, activeRect);
        }
    }

    private void OnIsRunningChanged(bool isRunning)
    {
        if (isRunning)
        {
            StartAnimation();
        }
        else
        {
            StopAnimation();
        }
    }

    private void StartAnimation()
    {
        Transition.Exit(this);
        Progress = 0;
        ActiveOpacity = 1;
        RunningAnimation.Execute(this);
    }

    private void StopAnimation()
    {
        Transition.Exit(this);

        var exitProgress = Math.Clamp(Progress + 0.18, 0, 1);
        Transition<ShellRunningIndicator>.Create()
            .Property(x => x.Progress, exitProgress)
            .Property(x => x.ActiveOpacity, 0d)
            .Effect(new TransitionEffect
            {
                Duration = TimeSpan.FromMilliseconds(260),
                FPS = 60,
                Ease = Eases.Cubic.Out
            })
            .Execute(this);
    }
}

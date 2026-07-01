using AstrolonUI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.ComponentModel;
using VeloxDev.DynamicTheme;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(ThemeBackground), ["#1e1e1e"], ["#ffffff"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(ThemeTextForeground), ["#ffffff"], ["#1e1e1e"])]
public partial class MarkdownView : UserControl
{
    private MarkdownDocumentViewModel? document;
    private bool ready;
    private IBrush themeBackground = Brushes.Transparent;
    private IBrush themeTextForeground = Brushes.White;

    public static readonly StyledProperty<bool> AutoScrollToEndProperty =
        AvaloniaProperty.Register<MarkdownView, bool>(nameof(AutoScrollToEnd));

    public bool AutoScrollToEnd
    {
        get => GetValue(AutoScrollToEndProperty);
        set => SetValue(AutoScrollToEndProperty, value);
    }

    public IBrush ThemeBackground
    {
        get => themeBackground;
        set { themeBackground = value; if (UseThemeColors) Background = value; }
    }

    public IBrush ThemeTextForeground
    {
        get => themeTextForeground;
        set { themeTextForeground = value; if (UseThemeColors) TextForeground = value; }
    }

    public bool UseThemeColors { get; set; } = true;

    public static readonly StyledProperty<IBrush> TextForegroundProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(TextForeground));

    public IBrush TextForeground
    {
        get => GetValue(TextForegroundProperty);
        set => SetValue(TextForegroundProperty, value);
    }

    public MarkdownView()
    {
        InitializeTheme();
        InitializeComponent();

        // NativeWebView 已由 XAML 声明，只需配置事件和导航
        webView.NavigationCompleted += OnNavComplete;

#if BROWSER
        webView.Source = new Uri("http://localhost:5235/standalone-md.html");
#else
        LoadHtml();
#endif
        DataContextChanged += (_, _) => AttachDocument();
        AttachDocument();
    }

    private void AttachDocument()
    {
        if (document is not null)
            document.PropertyChanged -= DocumentPropertyChanged;
        document = DataContext as MarkdownDocumentViewModel;
        if (document is not null)
            document.PropertyChanged += DocumentPropertyChanged;
        if (document is not null)
            PushMarkdown();
    }

    private void DocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarkdownDocumentViewModel.Text))
            PushMarkdown();
    }

    private void PushMarkdown()
    {
        if (!ready) return;
        var text = document?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            try { webView.InvokeScript(@"renderMarkdown('')"); } catch { }
            return;
        }
        var escaped = text.Replace("\\", "\\\\").Replace("'", "\\'")
                          .Replace("\n", "\\n").Replace("\r", "\\r");
        try { webView.InvokeScript($"renderMarkdown('{escaped}')"); }
        catch (Exception ex) { Console.WriteLine($"[MarkdownView] ❌ {ex.Message}"); }
    }

#if !BROWSER
    private void LoadHtml()
    {
        var webDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "web");
        var indexPath = System.IO.Path.Combine(webDir, "index.html");
        if (System.IO.File.Exists(indexPath))
            webView.Source = new Uri("file:///" + indexPath.Replace("\\", "/"));
    }
#endif

    private void OnNavComplete(object? sender, EventArgs e)
    {
        ready = true;
        PushMarkdown();
    }
}

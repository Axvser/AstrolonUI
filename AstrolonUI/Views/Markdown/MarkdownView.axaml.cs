using AstrolonUI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using VeloxDev.DynamicTheme;

#if BROWSER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(ThemeBackground), ["#1e1e1e"], ["#ffffff"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(ThemeTextForeground), ["#ffffff"], ["#1e1e1e"])]
public partial class MarkdownView : UserControl
{
    private MarkdownDocumentViewModel? document;
    private bool ready;
    private IBrush themeBackground = Brushes.Transparent;
    private IBrush themeTextForeground = Brushes.White;
    private NativeWebView? webView;

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

        // 所有平台：NativeWebView 渲染 Markdown
        webView = new NativeWebView { Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e)) };
        ContentRoot.Children.Add(webView);
        webView.NavigationCompleted += OnNavComplete;

#if BROWSER
        // Browser: JS 端会在应用启动后自动导航 iframe
#else
        // Desktop: 用 file:// 加载本地页面
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
#if BROWSER
        // Browser: 通过 JSImport 渲染
        var text = document?.Text;
        if (!string.IsNullOrWhiteSpace(text))
            RenderMarkdown(text);
#else
        if (!ready || webView is null) return;
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
#endif
    }

#if !BROWSER
    private void LoadHtml()
    {
        var webDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "web");
        var indexPath = System.IO.Path.Combine(webDir, "index.html");
        if (System.IO.File.Exists(indexPath))
            webView!.Source = new Uri("file:///" + indexPath.Replace("\\", "/"));
    }
#endif

    private void OnNavComplete(object? sender, EventArgs e)
    {
        ready = true;
        PushMarkdown();
    }

#if BROWSER
    // === JSImport: 渲染 Markdown 到浮动预览面板 ===
    [JSImport("renderMarkdown", "main.js")]
    internal static partial void RenderMarkdown(string text);
#endif
}

using AstrolonUI.ViewModels;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.ComponentModel;
using System.Linq;
using VeloxDev.DynamicTheme;
using MdInline = Markdig.Syntax.Inlines.Inline;

namespace AstrolonUI;

[ThemeConfig<ObjectConverter, Dark, Light>(nameof(Background), ["#1e1e1e"], ["#ffffff"])]
[ThemeConfig<ObjectConverter, Dark, Light>(nameof(TextForeground), ["#ffffff"], ["#1e1e1e"])]
public partial class MarkdownView : UserControl
{
    private static readonly FontFamily MonospaceFontFamily = new("Cascadia Code,Cascadia Mono,Consolas,Microsoft YaHei UI,Segoe UI,monospace");
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private readonly AvaloniaList<Control> displayBlocks = [];
    private MarkdownDocumentViewModel? document;

    public static readonly StyledProperty<IBrush> TextForegroundProperty =
        AvaloniaProperty.Register<MarkdownView, IBrush>(nameof(TextForeground));

    public IBrush TextForeground
    {
        get => GetValue(TextForegroundProperty);
        set => SetValue(TextForegroundProperty, value);
    }

    static MarkdownView()
    {
        TextForegroundProperty.Changed.AddClassHandler<MarkdownView>(
            static (view, _) => view.ApplyTextForeground());
    }

    public MarkdownView()
    {
        InitializeTheme();
        InitializeComponent();
        DisplayItems.ItemsSource = displayBlocks;
        DataContextChanged += (_, _) => AttachDocument();
        AttachDocument();
    }

    private void AttachDocument()
    {
        if (document is not null)
        {
            document.PropertyChanged -= DocumentPropertyChanged;
        }

        document = DataContext as MarkdownDocumentViewModel;
        if (document is not null)
        {
            document.PropertyChanged += DocumentPropertyChanged;
        }

        RebuildDisplay();
    }

    private void DocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarkdownDocumentViewModel.Text))
        {
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                RebuildDisplay();
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(RebuildDisplay);
            }
        }
    }

    private void RebuildDisplay()
    {
        displayBlocks.Clear();
        var markdown = document?.Text;
        if (string.IsNullOrWhiteSpace(markdown))
        {
            var emptyText = new TextBlock { Text = "No markdown", FontStyle = FontStyle.Italic, Opacity = 0.45, FontSize = 13 };
            ApplyTextForeground(emptyText);
            displayBlocks.Add(emptyText);
            return;
        }

        var parsed = Markdown.Parse(markdown, MarkdownPipeline);
        foreach (var block in parsed)
        {
            if (CreateControl(block) is { } control)
            {
                ApplyTextForeground(control);
                displayBlocks.Add(control);
            }
        }
    }

    private void ApplyTextForeground()
    {
        foreach (var block in displayBlocks)
        {
            ApplyTextForeground(block);
        }
    }

    private void ApplyTextForeground(Control control)
    {
        switch (control)
        {
            case SelectableTextBlock selectableTextBlock:
                selectableTextBlock.Foreground = TextForeground;
                break;
            case TextBlock textBlock:
                textBlock.Foreground = TextForeground;
                break;
        }

        switch (control)
        {
            case Border { Child: Control child }:
                ApplyTextForeground(child);
                break;
            case Panel panel:
                foreach (var child in panel.Children)
                {
                    ApplyTextForeground(child);
                }
                break;
            case ContentControl { Content: Control child }:
                ApplyTextForeground(child);
                break;
        }
    }

    private Control? CreateControl(Block block)
        => block switch
        {
            HeadingBlock heading => CreateHeading(heading),
            ParagraphBlock paragraph => CreateInlineTextControl(paragraph.Inline, 14, 22),
            QuoteBlock quote => CreateQuote(quote),
            FencedCodeBlock fenced => CreateCodeBlock(ExtractCode(fenced), fenced.Info),
            CodeBlock code => CreateCodeBlock(ExtractCode(code), null),
            ListBlock list => CreateList(list),
            Table table => CreateTable(table),
            ThematicBreakBlock => new Border { Height = 1, Background = new SolidColorBrush(Color.Parse("#33888888")), Margin = new Thickness(0, 6) },
            _ => CreateFallback(block)
        };

    private Control CreateHeading(HeadingBlock heading)
    {
        var level = Math.Clamp(heading.Level, 1, 6);
        var size = level switch { 1 => 22, 2 => 19, 3 => 16, 4 => 14, _ => 13 };
        var control = CreateInlineTextControl(heading.Inline, size, size + 8, FontWeight.SemiBold, fallbackText: ExtractInlineText(heading.Inline));
        control.Margin = new Thickness(0, level == 1 ? 0 : 8, 0, 4);
        return control;
    }

    private Control CreateQuote(QuoteBlock quote)
    {
        var host = new StackPanel { Spacing = 4 };
        foreach (var child in quote)
        {
            if (CreateControl(child) is { } control)
                host.Children.Add(control);
        }

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#4D9EF5")),
            BorderThickness = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 2, 0, 8),
            Child = host
        };
    }

    private static Control CreateCodeBlock(string code, string? info)
        => new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1F000000")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 4, 0, 8),
            Child = new SelectableTextBlock
            {
                Text = code,
                FontFamily = MonospaceFontFamily,
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap
            }
        };

    private Control CreateList(ListBlock list)
    {
        var host = new StackPanel { Spacing = 3, Margin = new Thickness(8, 0, 0, 8) };
        var index = int.TryParse(list.OrderedStart?.ToString(), out var start) && start > 0 ? start : 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            row.Children.Add(new TextBlock { Text = list.IsOrdered ? $"{index}." : "-", Margin = new Thickness(0, 0, 8, 0), FontSize = 14, LineHeight = 22 });
            var content = CreateInlineTextControl(FindInline(item), 14, 22, fallbackText: ExtractBlockText(item));
            Grid.SetColumn(content, 1);
            row.Children.Add(content);
            host.Children.Add(row);
            if (list.IsOrdered) index++;
        }
        return host;
    }

    private Control CreateTable(Table table)
    {
        var rows = table.OfType<TableRow>().ToList();
        var columnCount = Math.Max(1, rows.Select(r => r.OfType<TableCell>().Count()).DefaultIfEmpty(1).Max());
        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0, Margin = new Thickness(0, 4, 0, 8) };
        for (var col = 0; col < columnCount; col++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (var row = 0; row < rows.Count; row++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var cells = rows[rowIndex].OfType<TableCell>().ToList();
            for (var col = 0; col < columnCount; col++)
            {
                var content = col < cells.Count ? CreateTableCellContent(cells[col]) : new TextBlock();
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse("#66888888")),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = rows[rowIndex].IsHeader ? new SolidColorBrush(Color.Parse("#22888888")) : Brushes.Transparent,
                    Padding = new Thickness(6),
                    Child = content
                };
                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, col);
                grid.Children.Add(border);
            }
        }
        return new Border { BorderBrush = new SolidColorBrush(Color.Parse("#66888888")), BorderThickness = new Thickness(1, 1, 0, 0), Child = grid };
    }

    private Control CreateTableCellContent(TableCell cell)
    {
        var host = new StackPanel { Spacing = 4 };
        foreach (var block in cell)
            if (CreateControl(block) is { } control) host.Children.Add(control);
        return host.Children.Count == 0 ? new TextBlock() : host;
    }

    private static Control? CreateFallback(Block block)
    {
        var text = ExtractBlockText(block);
        return string.IsNullOrWhiteSpace(text) ? null : new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 14, LineHeight = 22 };
    }

    private Control CreateInlineTextControl(ContainerInline? inline, double fontSize, double lineHeight = 0, FontWeight? fontWeight = null, FontStyle? fontStyle = null, string? fallbackText = null)
    {
        var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = fontSize };
        if (lineHeight > 0) textBlock.LineHeight = lineHeight;
        if (fontWeight is { } weight) textBlock.FontWeight = weight;
        if (fontStyle is { } style) textBlock.FontStyle = style;

        var inlines = textBlock.Inlines;
        if (inline is not null && inlines is not null)
            AppendInlines(inlines, inline.FirstChild, new InlineRenderStyle(textBlock.FontWeight, textBlock.FontStyle, false));
        if (inlines is null || inlines.Count == 0)
            textBlock.Text = fallbackText ?? ExtractInlineText(inline);
        return textBlock;
    }

    private static void AppendInlines(InlineCollection inlines, MdInline? inline, InlineRenderStyle style)
    {
        for (var current = inline; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    inlines.Add(CreateRun(GetLiteralText(literal), style));
                    break;
                case CodeInline code:
                    inlines.Add(CreateRun(code.Content, style with { IsCode = true }));
                    break;
                case LineBreakInline:
                    inlines.Add(new LineBreak());
                    break;
                case LinkInline link when !link.IsImage:
                    inlines.Add(CreateRun(string.IsNullOrWhiteSpace(ExtractInlineText(link)) ? link.Url ?? string.Empty : ExtractInlineText(link), style));
                    break;
                case EmphasisInline emphasis:
                    AppendInlines(inlines, emphasis.FirstChild, style.Merge(emphasis));
                    break;
                case ContainerInline container:
                    AppendInlines(inlines, container.FirstChild, style);
                    break;
            }
        }
    }

    private static Run CreateRun(string? text, InlineRenderStyle style)
    {
        var run = new Run(text ?? string.Empty)
        {
            FontWeight = style.FontWeight,
            FontStyle = style.FontStyle
        };
        if (style.IsCode)
        {
            run.FontFamily = MonospaceFontFamily;
        }
        return run;
    }

    private static string ExtractCode(LeafBlock block)
    {
        var lines = block.Lines.Lines;
        return string.Join(Environment.NewLine, lines.Select(line => line.Slice.ToString()));
    }

    private static ContainerInline? FindInline(ContainerBlock block)
        => block.OfType<ParagraphBlock>().FirstOrDefault()?.Inline;

    private static string ExtractBlockText(Block block)
    {
        if (block is LeafBlock leaf)
            return leaf.Lines.ToString() ?? string.Empty;
        if (block is ContainerBlock container)
            return string.Join(Environment.NewLine, container.Select(ExtractBlockText));
        return string.Empty;
    }

    private static string ExtractInlineText(ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        return string.Concat(inline.Select(current => current switch
        {
            LiteralInline literal => GetLiteralText(literal),
            CodeInline code => code.Content,
            LineBreakInline => Environment.NewLine,
            LinkInline link => ExtractInlineText(link),
            EmphasisInline emphasis => ExtractInlineText(emphasis),
            ContainerInline container => ExtractInlineText(container),
            _ => string.Empty
        }));
    }

    private static string GetLiteralText(LiteralInline literal)
    {
        var content = literal.Content;
        if (string.IsNullOrEmpty(content.Text) || content.Start < 0 || content.End < content.Start)
            return string.Empty;
        var length = content.End - content.Start + 1;
        return content.Start + length > content.Text.Length ? content.ToString() : content.Text.Substring(content.Start, length);
    }

    private readonly record struct InlineRenderStyle(FontWeight FontWeight, FontStyle FontStyle, bool IsCode)
    {
        public InlineRenderStyle Merge(EmphasisInline emphasis)
            => emphasis.DelimiterCount >= 2
                ? this with { FontWeight = FontWeight.Bold }
                : this with { FontStyle = FontStyle.Italic };
    }
}







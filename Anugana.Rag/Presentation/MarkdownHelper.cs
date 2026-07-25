using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Anugana.Rag.Presentation;

public static class MarkdownHelper
{
    private static readonly Regex CodeBlockRegex = new Regex(@"```(?<lang>[a-zA-Z0-9_-]*)\n(?<code>[\s\S]*?)```", RegexOptions.Compiled);
    private static readonly Regex InlineTokenRegex = new Regex(@"(\*\*\*(.*?)\*\*\*|___(.*?)___|\*\*(.*?)\*\*|__(.*?)__|`(.*?)`|\*(.*?)\*|_(.*?)_)", RegexOptions.Compiled);

    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.RegisterAttached(
            "MarkdownText",
            typeof(string),
            typeof(MarkdownHelper),
            new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

    public static string GetMarkdownText(DependencyObject obj)
    {
        return (string)obj.GetValue(MarkdownTextProperty);
    }

    public static void SetMarkdownText(DependencyObject obj, string value)
    {
        obj.SetValue(MarkdownTextProperty, value);
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var markdown = e.NewValue as string ?? string.Empty;

        if (d is TextBlock textBlock)
        {
            RenderMarkdown(textBlock, markdown);
        }
        else if (d is RichTextBlock richTextBlock)
        {
            RenderMarkdown(richTextBlock, markdown);
        }
    }

    public static void RenderMarkdown(TextBlock textBlock, string markdown)
    {
        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(markdown))
            return;

        // Normalize newlines
        markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

        int lastIndex = 0;
        var matches = CodeBlockRegex.Matches(markdown);

        foreach (Match match in matches)
        {
            // Process text before code block
            if (match.Index > lastIndex)
            {
                var textSegment = markdown.Substring(lastIndex, match.Index - lastIndex);
                ProcessTextSegment(textBlock.Inlines, textSegment);
            }

            // Process code block
            var codeText = match.Groups["code"].Value.TrimEnd();
            if (textBlock.Inlines.Count > 0)
            {
                textBlock.Inlines.Add(new LineBreak());
            }

            var codeRun = new Run
            {
                Text = codeText,
                FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New, monospace"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248))
            };
            textBlock.Inlines.Add(codeRun);
            textBlock.Inlines.Add(new LineBreak());

            lastIndex = match.Index + match.Length;
        }

        // Process remaining text after last code block
        if (lastIndex < markdown.Length)
        {
            var remainingSegment = markdown.Substring(lastIndex);
            if (lastIndex > 0 && textBlock.Inlines.Count > 0)
            {
                textBlock.Inlines.Add(new LineBreak());
            }
            ProcessTextSegment(textBlock.Inlines, remainingSegment);
        }
    }

    private static void ProcessTextSegment(InlineCollection inlines, string textSegment)
    {
        var lines = textSegment.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (i > 0)
            {
                inlines.Add(new LineBreak());
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Headings
            if (trimmed.StartsWith("# "))
            {
                AddInlines(inlines, trimmed.Substring(2), fontSize: 18, isBold: true);
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                AddInlines(inlines, trimmed.Substring(3), fontSize: 16, isBold: true);
                continue;
            }
            if (trimmed.StartsWith("### "))
            {
                AddInlines(inlines, trimmed.Substring(4), fontSize: 14.5, isBold: true);
                continue;
            }
            if (trimmed.StartsWith("#### "))
            {
                AddInlines(inlines, trimmed.Substring(5), fontSize: 13.5, isBold: true);
                continue;
            }

            // Bullet Lists
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
            {
                var bulletRun = new Run { Text = "• ", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                inlines.Add(bulletRun);
                AddInlines(inlines, trimmed.Substring(2));
                continue;
            }

            // Numbered Lists (e.g. "1. ", "2. ")
            var numMatch = Regex.Match(trimmed, @"^(\d+)\.\s+(.*)");
            if (numMatch.Success)
            {
                var numRun = new Run { Text = $"{numMatch.Groups[1].Value}. ", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                inlines.Add(numRun);
                AddInlines(inlines, numMatch.Groups[2].Value);
                continue;
            }

            // Normal text line
            AddInlines(inlines, line);
        }
    }

    private static void AddInlines(InlineCollection inlines, string text, double? fontSize = null, bool isBold = false)
    {
        int lastPos = 0;
        foreach (Match match in InlineTokenRegex.Matches(text))
        {
            // Plain text before token
            if (match.Index > lastPos)
            {
                var plainText = text.Substring(lastPos, match.Index - lastPos);
                inlines.Add(CreateRun(plainText, fontSize, isBold));
            }

            if (match.Value.StartsWith("***") || match.Value.StartsWith("___"))
            {
                var content = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                var r = CreateRun(content, fontSize, isBold: true);
                r.FontStyle = Windows.UI.Text.FontStyle.Italic;
                inlines.Add(r);
            }
            else if (match.Value.StartsWith("**") || match.Value.StartsWith("__"))
            {
                var content = match.Groups[4].Success ? match.Groups[4].Value : match.Groups[5].Value;
                inlines.Add(CreateRun(content, fontSize, isBold: true));
            }
            else if (match.Value.StartsWith("`"))
            {
                var codeContent = match.Groups[6].Value;
                var codeRun = new Run
                {
                    Text = codeContent,
                    FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New, monospace"),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 236, 72, 153))
                };
                if (fontSize.HasValue) codeRun.FontSize = fontSize.Value;
                inlines.Add(codeRun);
            }
            else if (match.Value.StartsWith("*") || match.Value.StartsWith("_"))
            {
                var content = match.Groups[7].Success ? match.Groups[7].Value : match.Groups[8].Value;
                var italicRun = CreateRun(content, fontSize, isBold);
                italicRun.FontStyle = Windows.UI.Text.FontStyle.Italic;
                inlines.Add(italicRun);
            }

            lastPos = match.Index + match.Length;
        }

        // Remaining text
        if (lastPos < text.Length)
        {
            var remaining = text.Substring(lastPos);
            inlines.Add(CreateRun(remaining, fontSize, isBold));
        }
    }

    private static Run CreateRun(string text, double? fontSize = null, bool isBold = false)
    {
        var run = new Run { Text = text };
        if (fontSize.HasValue) run.FontSize = fontSize.Value;
        if (isBold) run.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        return run;
    }

    public static void RenderMarkdown(RichTextBlock richTextBlock, string markdown)
    {
        try
        {
            richTextBlock.Blocks.Clear();
            if (string.IsNullOrEmpty(markdown)) return;
            markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

            var p = new Paragraph { Margin = new Thickness(0) };
            AddInlines(p.Inlines, markdown);
            richTextBlock.Blocks.Add(p);
        }
        catch { }
    }
}

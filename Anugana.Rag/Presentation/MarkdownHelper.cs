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
        if (d is RichTextBlock richTextBlock)
        {
            var markdown = e.NewValue as string ?? string.Empty;
            RenderMarkdown(richTextBlock, markdown);
        }
    }

    public static void RenderMarkdown(RichTextBlock richTextBlock, string markdown)
    {
        richTextBlock.Blocks.Clear();

        if (string.IsNullOrEmpty(markdown))
            return;

        // Normalize newlines
        markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

        // Split code blocks first
        var codeBlockRegex = new Regex(@"```(?<lang>[a-zA-Z0-9_-]*)\n(?<code>[\s\S]*?)```", RegexOptions.Compiled);
        int lastIndex = 0;

        foreach (Match match in codeBlockRegex.Matches(markdown))
        {
            // Process text before code block
            if (match.Index > lastIndex)
            {
                var textSegment = markdown.Substring(lastIndex, match.Index - lastIndex);
                ProcessTextBlocks(richTextBlock, textSegment);
            }

            // Process code block
            var codeText = match.Groups["code"].Value.TrimEnd();
            var codeParagraph = new Paragraph
            {
                FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New, monospace"),
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 6)
            };

            var codeRun = new Run
            {
                Text = codeText,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 150, 255))
            };
            codeParagraph.Inlines.Add(codeRun);
            richTextBlock.Blocks.Add(codeParagraph);

            lastIndex = match.Index + match.Length;
        }

        // Process remaining text after last code block
        if (lastIndex < markdown.Length)
        {
            var remainingSegment = markdown.Substring(lastIndex);
            ProcessTextBlocks(richTextBlock, remainingSegment);
        }
    }

    private static void ProcessTextBlocks(RichTextBlock richTextBlock, string textSegment)
    {
        var lines = textSegment.Split('\n');
        Paragraph? currentParagraph = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                currentParagraph = null; // New paragraph on empty line
                continue;
            }

            // Headings
            if (trimmed.StartsWith("# "))
            {
                var h1 = new Paragraph { Margin = new Thickness(0, 8, 0, 4) };
                AddInlines(h1.Inlines, trimmed.Substring(2), fontSize: 18, isBold: true);
                richTextBlock.Blocks.Add(h1);
                currentParagraph = null;
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                var h2 = new Paragraph { Margin = new Thickness(0, 6, 0, 3) };
                AddInlines(h2.Inlines, trimmed.Substring(3), fontSize: 16, isBold: true);
                richTextBlock.Blocks.Add(h2);
                currentParagraph = null;
                continue;
            }
            if (trimmed.StartsWith("### "))
            {
                var h3 = new Paragraph { Margin = new Thickness(0, 4, 0, 2) };
                AddInlines(h3.Inlines, trimmed.Substring(4), fontSize: 14, isBold: true);
                richTextBlock.Blocks.Add(h3);
                currentParagraph = null;
                continue;
            }

            // Bullet Lists
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
            {
                var listPara = new Paragraph { Margin = new Thickness(12, 2, 0, 2) };
                listPara.Inlines.Add(new Run { Text = "• ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                AddInlines(listPara.Inlines, trimmed.Substring(2));
                richTextBlock.Blocks.Add(listPara);
                currentParagraph = null;
                continue;
            }

            // Numbered Lists (e.g., "1. ")
            var numMatch = Regex.Match(trimmed, @"^(\d+)\.\s+(.*)");
            if (numMatch.Success)
            {
                var listPara = new Paragraph { Margin = new Thickness(12, 2, 0, 2) };
                listPara.Inlines.Add(new Run { Text = $"{numMatch.Groups[1].Value}. ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                AddInlines(listPara.Inlines, numMatch.Groups[2].Value);
                richTextBlock.Blocks.Add(listPara);
                currentParagraph = null;
                continue;
            }

            // Regular paragraph line
            if (currentParagraph == null)
            {
                currentParagraph = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                richTextBlock.Blocks.Add(currentParagraph);
            }
            else
            {
                // Soft line break inside paragraph
                currentParagraph.Inlines.Add(new LineBreak());
            }

            AddInlines(currentParagraph.Inlines, line);
        }
    }

    private static void AddInlines(InlineCollection inlines, string text, double? fontSize = null, bool isBold = false)
    {
        // Tokenize inline markdown: **bold**, *italic*, `inline code`
        var tokenRegex = new Regex(@"(\*\*(.*?)\*\*|__(.*?)__|`(.*?)`|\*(.*?)\*|_(.*?)_)", RegexOptions.Compiled);
        int lastPos = 0;

        foreach (Match match in tokenRegex.Matches(text))
        {
            // Plain text before token
            if (match.Index > lastPos)
            {
                var plainText = text.Substring(lastPos, match.Index - lastPos);
                inlines.Add(CreateRun(plainText, fontSize, isBold));
            }

            if (match.Value.StartsWith("**") || match.Value.StartsWith("__"))
            {
                var content = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                inlines.Add(CreateRun(content, fontSize, isBold: true));
            }
            else if (match.Value.StartsWith("`"))
            {
                var codeContent = match.Groups[4].Value;
                var codeRun = new Run
                {
                    Text = codeContent,
                    FontFamily = new FontFamily("Consolas, Cascadia Code, monospace"),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 100, 255))
                };
                if (fontSize.HasValue) codeRun.FontSize = fontSize.Value;
                inlines.Add(codeRun);
            }
            else if (match.Value.StartsWith("*") || match.Value.StartsWith("_"))
            {
                var content = match.Groups[5].Success ? match.Groups[5].Value : match.Groups[6].Value;
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
}

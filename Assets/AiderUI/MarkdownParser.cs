using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TableElement : VisualElement
{
    public TableElement(string[][] table)
    {
        this.AddToClassList("table");

        var tableContent = new VisualElement();
        tableContent.AddToClassList("table-content");
        this.Add(tableContent);

        // Determine the number of columns based on the first row
        int numColumns = table.Length > 0 ? table[0].Length : 0;
        int numRows = table.Length;

        // Create column containers
        for (var j = 0; j < numColumns; j++)
        {
            var column = new VisualElement();
            column.AddToClassList("table-column");
            if (j == 0) column.AddToClassList("first");
            if (j == numColumns - 1) column.AddToClassList("last");
            if (j % 2 == 0) column.AddToClassList("even");
            else column.AddToClassList("odd");
            if (j > 0 && j < numColumns - 1) column.AddToClassList("middle");
            
            tableContent.Add(column);

            // Add cells to the column
            for (var i = 0; i < numRows; i++)
            {
                var cell = new Label(table[i][j]);
                cell.AddToClassList("table-cell");
                if (i == 0) cell.AddToClassList("first");
                if (i == numRows - 1) cell.AddToClassList("last");
                if (i % 2 == 0) cell.AddToClassList("even");
                else cell.AddToClassList("odd");
                if (i > 0 && i < numRows - 1) cell.AddToClassList("middle");
                column.Add(cell);
            }
        }
    }
}

public class SelectableLabel : TextField
{
    public SelectableLabel(string content)
    {
        this.isReadOnly = true;
        this.Q<TextElement>(null, "unity-text-element").enableRichText = true;
        this.value = content;
    }
}

public class CodeElement : SelectableLabel
{
    public CodeElement(CodeBlockInfo info) : base(info.code)
    {
        this.AddToClassList("code-block");
        this.AddToClassList("code-block-" + info.language);

        // add language indicator
        var languageLabel = new Label(info.language);
        languageLabel.AddToClassList("language-label");
        Add(languageLabel);
    }
}

public class BlockquoteElement : SelectableLabel
{
    public BlockquoteElement(string content) : base(content)
    {
        this.AddToClassList("blockquote");
    }
}

public class Section
{
    public int startIndex;
    public int endIndex;
}
public class CodeBlockInfo : Section
{
    public string code;
    public string language;
}


public static class MarkdownParser
{
    private static string Apply(Regex regex, string markdown, string tag, string arg = null)
    {
        foreach (Match match in regex.Matches(markdown))
        {
            var value = match.ToString();
            var replacement = $"<{tag}{((arg != null) ? ("=" + arg) : "")}>{match.Groups[1].Value}</{tag}>";
            // Debug.Log($"Replacing {value} with {replacement}");
            markdown = markdown.Replace(value, replacement);
        }

        return markdown;
    }

    public static string ParseString(string markdown)
    {
        var header6 = new Regex(@"^[ \t]*?###### (.+)", RegexOptions.Multiline);
        var header5 = new Regex(@"^[ \t]*?##### (.+)", RegexOptions.Multiline);
        var header4 = new Regex(@"^[ \t]*?#### (.+)", RegexOptions.Multiline);
        var header3 = new Regex(@"^[ \t]*?### (.+)", RegexOptions.Multiline);
        var header2 = new Regex(@"^[ \t]*?## (.+)", RegexOptions.Multiline);
        var header1 = new Regex(@"^[ \t]*?# (.+)", RegexOptions.Multiline);
        var bold = new Regex(@"__(.+?)__", RegexOptions.Multiline);
        var bold2 = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Multiline);
        var italic = new Regex(@"_(.+?)_", RegexOptions.Multiline);
        var italic2 = new Regex(@"\*(.+?)\*", RegexOptions.Multiline);
        var mark = new Regex(@"==([^=\s]+?)==", RegexOptions.Multiline);
        var link = new Regex(@"\[.+\]\((.+)\)", RegexOptions.Multiline);
        var codeNoDes = new Regex(@"(`[^`\s]+?`)", RegexOptions.Multiline);
        var code = new Regex(@"`([^`\s]+?)`", RegexOptions.Multiline);

        markdown = Apply(link, markdown, "u");
        markdown = Apply(header6, markdown, "size", "110%");
        markdown = Apply(header5, markdown, "size", "115%");
        markdown = Apply(header4, markdown, "size", "120%");
        markdown = Apply(header3, markdown, "size", "130%");
        markdown = Apply(header2, markdown, "size", "150%");
        markdown = Apply(header1, markdown, "size", "175%");
        markdown = Apply(bold, markdown, "b");
        markdown = Apply(bold2, markdown, "b");
        markdown = Apply(italic, markdown, "i");
        markdown = Apply(italic2, markdown, "i");
        markdown = Apply(mark, markdown, "mark");
        markdown = Apply(codeNoDes, markdown, "mark", EditorGUIUtility.isProSkin ? "#ffffff22" : "#00000022");
        markdown = Apply(code, markdown, "b");

        // replace bullet point lists (*) with a bullet point
        var bullet = new Regex(@"^([ \t]*?)\* (.+)", RegexOptions.Multiline);
        markdown = bullet.Replace(markdown, "$1• $2");
        bullet = new Regex(@"^([ \t]*?)- (.+)", RegexOptions.Multiline);
        markdown = bullet.Replace(markdown, "$1• $2");

        // replace common ligatures
        markdown = markdown.Replace("--", "—");
        markdown = markdown.Replace("...", "…");
        markdown = markdown.Replace("->", "→");
        markdown = markdown.Replace("<-", "←");
        markdown = markdown.Replace("<->", "↔");
        markdown = markdown.Replace("=>", "⇒");
        markdown = markdown.Replace("<=", "⇐");
        markdown = markdown.Replace("<=>", "⇔");
        markdown = markdown.Replace("==", "≡");
        markdown = markdown.Replace("!=", "≠");
        markdown = markdown.Replace("<=", "≤");
        markdown = markdown.Replace(">=", "≥");
        markdown = markdown.Replace("+-", "±");


        return markdown;
    }

    public static void Parse(VisualElement parent, string markdown, List<AiderUnityCommandBase> unityBlocks = null)
    {
        parent.Clear();
        var codeSelector = new Regex(@"```(.*?)\n([\s\S]+?)```", RegexOptions.Multiline);

        var sections = new List<Section>();

        var codeBlockMatches = codeSelector.Matches(markdown);
        foreach (Match match in codeBlockMatches)
        {
            var codeBlock = new CodeBlockInfo
            {
                startIndex = match.Index,
                endIndex = match.Index + match.Length,
                code = match.Groups[2].Value,
                language = match.Groups[1].Value
            };
            sections.Add(codeBlock);
        }

        // generate inbetween sections
        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            if (i == 0 && section.startIndex > 0)
            {
                sections.Insert(i, new Section { startIndex = 0, endIndex = section.startIndex });
                i++;
            }
            else if (i == sections.Count - 1 && section.endIndex < markdown.Length)
            {
                sections.Insert(i + 1, new Section { startIndex = section.endIndex, endIndex = markdown.Length });
            }
            else if (i < sections.Count - 1 && section.endIndex < sections[i + 1].startIndex)
            {
                sections.Insert(i + 1, new Section { startIndex = section.endIndex, endIndex = sections[i + 1].startIndex });
                i++;
            }
        }

        // if there are no sections, add the whole string as a section
        if (sections.Count == 0)
        {
            sections.Add(new Section { startIndex = 0, endIndex = markdown.Length });
        }

        if (unityBlocks == null)
            unityBlocks = new();

        int unityBlockIndex = 0;
        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            VisualElement element = null;
            switch (section)
            {
                case CodeBlockInfo codeBlock:
                    if (codeBlock.language == "unity")
                    {
                        var commandBlock = unityBlocks[unityBlockIndex];
                        element = commandBlock.BuildUI();
                        unityBlockIndex++;
                        break;
                    }
                    element = new CodeElement(codeBlock);
                    break;
                case Section _:
                    var text = markdown.Substring(section.startIndex, section.endIndex - section.startIndex);
                    var parsed = ParseString(text).Trim();
                    if (string.IsNullOrWhiteSpace(parsed)) break;
                    element = new SelectableLabel(parsed);
                    break;
            }

            if (element != null)
            {
                element.AddToClassList("markdown-section");
                parent.Add(element);
            }
        }


        // var tableSelector = new Regex(@"(\|.+\|\s+)+", RegexOptions.Multiline);
        // var blockQuoteSelector = new Regex(@"^([ \t]*?)> (.+)", RegexOptions.Multiline);

        // var codeBlocks = codeSelector.Matches(markdown).Select(match => new int2(match.Index, match.Index + match.Length)).ToList();
        // var tableBlocks = tableSelector.Matches(markdown).Select(match => new int2(match.Index, match.Index + match.Length)).ToList();
        // var blockQuotes = blockQuoteSelector.Matches(markdown).Select(match => new int2(match.Index, match.Index + match.Length)).ToList();

        // var allIndices = new List<Section>();
        // allIndices.AddRange(tableBlocks);
        // allIndices.AddRange(codeBlocks);
        // allIndices.AddRange(blockQuotes);
        // allIndices.Sort((a, b) => a.x.CompareTo(b.x));

        // now generate a list of each section including the inbetweens
        // var sections = new List<int2>();
        // for (var i = 0; i < allIndices.Count; i++)
        // {
        //     if (i == 0)
        //     {
        //         sections.Add(new int2(0, allIndices[i].x));
        //     }
        //     else
        //     {
        //         sections.Add(new int2(allIndices[i - 1].y, allIndices[i].x));
        //     }

        //     if (i == allIndices.Count - 1)
        //     {
        //         sections.Add(new int2(allIndices[i].y, markdown.Length));
        //     }
        // }

        // sections.AddRange(allIndices);
        // sections.Sort((a, b) => a.x.CompareTo(b.x));
        // sections = sections.Distinct().ToList();

        // if (sections.Count == 0)
        // {
        //     sections.Add(new int2(0, markdown.Length));
        // }

        // foreach (var section in sections)
        // {
        //     var text = markdown.Substring(section.x, section.y - section.x);

        //     if (tableBlocks.Contains(new int2(section.x, section.y)))
        //     {
        //         parent.Add(new TableElement(ParseTable(text)));
        //     }
        //     else if (codeBlocks.Contains(new int2(section.x, section.y)))
        //     {
        //         parent.Add(new CodeElement(text[3..^3]));
        //     }
        //     else if (blockQuotes.Contains(new int2(section.x, section.y)))
        //     {
        //         parent.Add(new BlockquoteElement(ParseString(text).Trim()[2..]));
        //     }
        //     else
        //     {
        //         parent.Add(new SelectableLabel(ParseString(text).Trim()));
        //     }
        // }
    }

    public static string[][] ParseTable(string tableString)
    {
        var rows = tableString.Trim().Split('\n');
        var table = new List<List<string>>();
        foreach (var row in rows)
        {
            var cells = row.Split('|')
                .Select(cell => cell.Trim())
                .Where(s => s.All(c => c != '-' && c != ':') && !string.IsNullOrWhiteSpace(s)).ToList();
            if (cells.Count == 0) continue;
            table.Add(cells);
        }

        return table.Select(row => row.ToArray()).ToArray();
    }
}
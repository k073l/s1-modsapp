using System;
using System.Text;

namespace ModsApp.Helpers;

public static class JsonSyntaxHighlighter
{
    private const string KeyColor = "#0451A5";
    private const string StringColor = "#A31515";
    private const string NumberColor = "#098658";
    private const string BoolColor = "#0000FF";
    private const string BracketColor = "#AF00DB";
    private const string PunctColor = "#333333";

    public static string Highlight(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        var sb = new StringBuilder(json.Length * 2);
        var i = 0;
        var len = json.Length;

        while (i < len)
        {
            var c = json[i];

            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '"')
            {
                var strLen = ReadString(json, i, len);
                var raw = json.Substring(i, strLen);

                // peek past whitespace to decide key vs value
                var peek = i + strLen;
                while (peek < len && char.IsWhiteSpace(json[peek])) peek++;
                var isKey = peek < len && json[peek] == ':';

                var color = isKey ? KeyColor : StringColor;
                sb.Append("<color=").Append(color).Append('>');
                AppendEscaped(sb, raw);
                sb.Append("</color>");
                i += strLen;
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < len && char.IsDigit(json[i + 1])))
            {
                var numLen = ReadNumber(json, i, len);
                sb.Append("<color=").Append(NumberColor).Append('>');
                sb.Append(json, i, numLen);
                sb.Append("</color>");
                i += numLen;
                continue;
            }

            if (TryMatchLiteral(json, i, len, "true") ||
                TryMatchLiteral(json, i, len, "false") ||
                TryMatchLiteral(json, i, len, "null"))
            {
                // find where literal ends
                var end = i;
                while (end < len && char.IsLetter(json[end])) end++;
                sb.Append("<color=").Append(BoolColor).Append('>');
                sb.Append(json, i, end - i);
                sb.Append("</color>");
                i = end;
                continue;
            }

            if (c is '{' or '}' or '[' or ']')
            {
                sb.Append("<color=").Append(BracketColor).Append('>');
                sb.Append(c);
                sb.Append("</color>");
                i++;
                continue;
            }

            if (c is ':' or ',')
            {
                sb.Append("<color=").Append(PunctColor).Append('>');
                sb.Append(c);
                sb.Append("</color>");
                i++;
                continue;
            }

            // anything else
            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }


    public static int RawIndexToHighlightedIndex(string json, int rawIndex)
    {
        if (string.IsNullOrEmpty(json) || rawIndex <= 0) return rawIndex;
        rawIndex = Math.Min(rawIndex, json.Length);

        var i = 0;
        var len = json.Length;
        var tagBytes = 0; // total tag characters emitted before current raw position

        while (i < len && i < rawIndex)
        {
            var c = json[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '"')
            {
                var strLen = ReadString(json, i, len);
                var peek = i + strLen;
                while (peek < len && char.IsWhiteSpace(json[peek])) peek++;
                var isKey = peek < len && json[peek] == ':';
                var color = isKey ? KeyColor : StringColor;

                var openTag = "<color=".Length + color.Length + ">".Length;
                var closeTag = "</color>".Length;

                var tokenEnd = Math.Min(i + strLen, rawIndex);

                var escapedExtra = 0;
                for (var k = i; k < tokenEnd; k++)
                {
                    if (json[k] == '<' || json[k] == '>') escapedExtra++;
                }

                if (tokenEnd < i + strLen)
                {
                    tagBytes += openTag;
                }
                else
                {
                    tagBytes += openTag + closeTag;
                }

                i += strLen;
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < len && char.IsDigit(json[i + 1])))
            {
                var numLen = ReadNumber(json, i, len);
                var openTag = "<color=".Length + NumberColor.Length + ">".Length;
                var closeTag = "</color>".Length;
                var tokenEnd = Math.Min(i + numLen, rawIndex);

                tagBytes += tokenEnd < i + numLen ? openTag : openTag + closeTag;
                i += numLen;
                continue;
            }

            if (TryMatchLiteral(json, i, len, "true") ||
                TryMatchLiteral(json, i, len, "false") ||
                TryMatchLiteral(json, i, len, "null"))
            {
                var end = i;
                while (end < len && char.IsLetter(json[end])) end++;
                var litLen = end - i;
                var openTag = "<color=".Length + BoolColor.Length + ">".Length;
                var closeTag = "</color>".Length;
                var tokenEnd = Math.Min(i + litLen, rawIndex);

                tagBytes += tokenEnd < i + litLen ? openTag : openTag + closeTag;
                i = end;
                continue;
            }

            if (c is '{' or '}' or '[' or ']')
            {
                var openTag = "<color=".Length + BracketColor.Length + ">".Length;
                var closeTag = "</color>".Length;
                tagBytes += rawIndex > i ? openTag + closeTag : 0;
                i++;
                continue;
            }

            if (c is ':' or ',')
            {
                var openTag = "<color=".Length + PunctColor.Length + ">".Length;
                var closeTag = "</color>".Length;
                tagBytes += rawIndex > i ? openTag + closeTag : 0;
                i++;
                continue;
            }

            i++; // unrecognized character, no tags emitted
        }

        return rawIndex + tagBytes;
    }


    private static int ReadString(string json, int start, int len)
    {
        var i = start + 1; // skip opening quote
        while (i < len)
        {
            if (json[i] == '\\')
            {
                i += 2;
                continue;
            } // skip escaped char

            if (json[i] == '"')
            {
                i++;
                break;
            }

            i++;
        }

        return i - start;
    }

    private static int ReadNumber(string json, int start, int len)
    {
        var i = start;
        if (json[i] == '-') i++;
        while (i < len)
        {
            var c = json[i];
            if (char.IsDigit(c) || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                i++;
            else
                break;
        }

        return i - start;
    }

    private static bool TryMatchLiteral(string json, int i, int len, string literal)
    {
        var litLen = literal.Length;
        if (i + litLen > len) return false;
        for (var j = 0; j < litLen; j++)
            if (json[i + j] != literal[j])
                return false;
        // must not be followed by alphanumeric
        var next = i + litLen;
        if (next < len && (char.IsLetterOrDigit(json[next]) || json[next] == '_')) return false;
        return true;
    }

    private static void AppendEscaped(StringBuilder sb, string s)
    {
        foreach (var c in s)
        {
            if (c == '<') sb.Append('\u003C');
            else if (c == '>') sb.Append('\u003E');
            else sb.Append(c);
        }
    }
}
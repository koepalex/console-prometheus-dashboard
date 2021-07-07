using System;
using System.Collections.Generic;

namespace prometheus_console_dashboard.BusinessLogic
{
    public static class ParserExtensions
    {
        public static bool IsEmpty(this ReadOnlySpan<char> line)
        {
            return line.Trim().Length == 0;
        }

        public static bool IsSpecial(this ReadOnlySpan<char> line, string kind)
        {
            var reference = new ReadOnlyMemory<char>($"# {kind}".ToCharArray());
            return line.Slice(0, 6).SequenceEqual(reference.Span);
        }

        public static string GetNameOfSpecialLine(this ReadOnlySpan<char> line, string kind)
        {
            var part = line.Slice(kind.Length + 2);

            part = part.TrimStart();
            var index = part.IndexOf(" ".AsSpan());
            var name = part.Slice(0, index).ToString();
            return name;
        }

        public static string GetPayloadOfSpecialLine(this ReadOnlySpan<char> line)
        {
            int spaceCounter = 0;
            int index = 0;
            for(int i= 0; i< line.Length; i++) 
            {
                if (line[i] == ' ')
                {
                    spaceCounter++;

                    if(spaceCounter == 3)
                    {
                        index = i + 1;
                        break;
                    }
                }
            }
            return line.Slice(index).ToString();
        }

        public static (string name, string value, IDictionary<string, string> tags) GetValueLine(this ReadOnlySpan<char> line)
        {
            int index = 0;
            int indexOfOpeningBracket = -1;
            int indexOfClosingBracket = -1;
            int tagsCharCounter = 0;
            var tags = new Dictionary<string,string>();

            for(int i= 0; i< line.Length; i++) 
            {
                var c = line[i];
                if (c == '{')
                {
                    indexOfOpeningBracket = i;
                }
                else if (c == '}')
                {
                    indexOfClosingBracket = i;
                }
                else if (c == ' ')
                {
                    index = i;
                    if (indexOfOpeningBracket == -1)
                    {
                        break;
                    }
                }

                if (indexOfOpeningBracket != -1 && indexOfClosingBracket == -1)
                {
                    tagsCharCounter++;
                }
            }

            var name = line.Slice(0, indexOfOpeningBracket != -1 ? indexOfOpeningBracket : index).ToString();
            if (indexOfOpeningBracket != -1 && indexOfClosingBracket != -1)
            {
                var tagsString = line.Slice(indexOfOpeningBracket + 1, indexOfClosingBracket - indexOfOpeningBracket - 1);

                foreach (var tagKvP in tagsString.ToString().SeparateTags())
                {
                    (var tagName, var tagValue) = tagKvP.AsSpan().ExtractTag();
                    tags.Add(tagName, tagValue);
                }
            }
            var value = line.TrimStart().Slice(index + 1).ToString();

            return (name, value, tags);
        }

        public static IEnumerable<string> SeparateTags(this string tagString)
        {
            do {
                var tagList = tagString;
                var commaIndex = tagList.IndexOf(',');

                if (commaIndex != -1)
                {
                    yield return tagList.Substring(0, commaIndex).Trim();
                    tagString = tagList.Substring(commaIndex + 1).Trim();
                }
                else
                {
                    yield return tagList;
                    break;
                }
            } while (true);
        }

        public static (string tag, string value) ExtractTag (this ReadOnlySpan<char> tagKvP)
        {
            var equalIndex = tagKvP.IndexOf('=');
            if (equalIndex != -1)
            {
                var tag = tagKvP.Slice(0, equalIndex).Trim().ToString();
                var subString = tagKvP.Slice(equalIndex + 1).Trim();
                var value = subString.Slice(1, subString.Length - 2).Trim().ToString();
                return (tag, value);
            }

            throw new InvalidOperationException("Missformmed tag format");
        }

        public static bool IsKnownMetricElement(this ReadOnlySpan<char> identifier, string baseMetric, string suffix)
        {
            bool result = true;
            result &= identifier.StartsWith(baseMetric.AsSpan());
            result &= identifier.EndsWith(suffix.AsSpan());
            return result;
        }
    }
}
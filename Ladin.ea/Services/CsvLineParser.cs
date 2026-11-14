using System.Text;

namespace Ladin.ea.Services
{
    public static class CsvLineParser
    {
        public static IReadOnlyList<string> Parse(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                result.Add(string.Empty);
                return result;
            }

            var currentPart = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentPart.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            result.Add(currentPart.ToString().Trim());
            return result;
        }
    }
}
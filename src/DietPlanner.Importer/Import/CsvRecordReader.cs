using System.Text;

namespace DietPlanner.Importer.Import;

internal static class CsvRecordReader
{
    public static ValueTask<List<string>?> ReadRecordAsync(TextReader reader, CancellationToken cancellationToken)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var next = reader.Read();
            if (next == -1)
            {
                if (inQuotes)
                {
                    throw new FormatException("Invalid CSV content. Unterminated quoted field.");
                }

                if (!fieldStarted && values.Count == 0 && current.Length == 0)
                {
                    return ValueTask.FromResult<List<string>?>(null);
                }

                values.Add(current.ToString());
                return ValueTask.FromResult<List<string>?>(values);
            }

            var character = (char)next;

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        current.Append('"');
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                current.Append(character);
                continue;
            }

            if (character == ',')
            {
                values.Add(current.ToString());
                current.Clear();
                fieldStarted = false;
                continue;
            }

            if (character == '\r')
            {
                if (reader.Peek() == '\n')
                {
                    reader.Read();
                }

                values.Add(current.ToString());
                return ValueTask.FromResult<List<string>?>(values);
            }

            if (character == '\n')
            {
                values.Add(current.ToString());
                return ValueTask.FromResult<List<string>?>(values);
            }

            if (character == '"')
            {
                if (current.Length > 0)
                {
                    throw new FormatException("Invalid CSV content. Unexpected quote inside unquoted field.");
                }

                inQuotes = true;
                fieldStarted = true;
                continue;
            }

            current.Append(character);
            fieldStarted = true;
        }
    }
}

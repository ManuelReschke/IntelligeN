namespace IntelligeN.LegacyImport.Configuration;

public sealed record CodeDefinitionCommand(
    string Name,
    string? Parameter1,
    string? Parameter1Value,
    string? Parameter2,
    string Snippet)
{
    public string Signature
    {
        get
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Parameter1))
            {
                parts.Add(Parameter1Value is { Length: > 0 }
                    ? $"{Parameter1} [{Parameter1Value}]"
                    : Parameter1);
            }

            if (!string.IsNullOrWhiteSpace(Parameter2))
            {
                parts.Add(Parameter2);
            }

            return parts.Count == 0 ? "No parameters" : string.Join(" | ", parts);
        }
    }
}

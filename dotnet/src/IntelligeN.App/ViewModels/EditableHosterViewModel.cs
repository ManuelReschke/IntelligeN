using IntelligeN.LegacyImport.Configuration;

namespace IntelligeN.App.ViewModels;

public sealed class EditableHosterViewModel : BindableObject
{
    private string _name;
    private string _shortName;
    private string _aliasesText;

    public EditableHosterViewModel(string name, string shortName, string aliasesText)
    {
        _name = name;
        _shortName = shortName;
        _aliasesText = aliasesText;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(AliasCount));
            }
        }
    }

    public string ShortName
    {
        get => _shortName;
        set
        {
            if (SetProperty(ref _shortName, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string AliasesText
    {
        get => _aliasesText;
        set
        {
            if (SetProperty(ref _aliasesText, value))
            {
                RaisePropertyChanged(nameof(AliasCount));
            }
        }
    }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(ShortName)
            ? Name
            : $"{Name} ({ShortName})";

    public int AliasCount => ParseAliases().Count;

    public HosterDefinition ToDefinition() =>
        new(
            Name: Name.Trim(),
            ShortName: ShortName.Trim(),
            Aliases: ParseAliases());

    public static EditableHosterViewModel FromDefinition(HosterDefinition definition) =>
        new(
            definition.Name,
            definition.ShortName,
            string.Join(Environment.NewLine, definition.Aliases));

    private IReadOnlyList<string> ParseAliases()
    {
        return AliasesText
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
            .Select(alias => alias.Trim())
            .Where(alias => alias.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

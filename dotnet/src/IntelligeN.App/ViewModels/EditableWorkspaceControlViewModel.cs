using System.Collections.ObjectModel;
using IntelligeN.LegacyImport.Workspace;

namespace IntelligeN.App.ViewModels;

public sealed class EditableWorkspaceControlViewModel : BindableObject
{
    private string _value;

    public EditableWorkspaceControlViewModel(
        string controlId,
        string title,
        string value,
        IReadOnlyList<LoadedXmlImageHoster> imageHosters)
    {
        ControlId = controlId;
        Title = title;
        _value = value;
        ImageHosters = new ObservableCollection<LoadedXmlImageHoster>(imageHosters);
    }

    public string ControlId { get; }

    public string Title { get; }

    public ObservableCollection<LoadedXmlImageHoster> ImageHosters { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                RaisePropertyChanged(nameof(HasValue));
                RaisePropertyChanged(nameof(ValuePreview));
            }
        }
    }

    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    public int ImageHosterCount => ImageHosters.Count;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? ControlId : Title;

    public string ValuePreview
    {
        get
        {
            var normalized = Value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= 120)
            {
                return normalized;
            }

            return normalized[..117] + "...";
        }
    }

    public static EditableWorkspaceControlViewModel FromControl(LoadedXmlControl control)
    {
        return new EditableWorkspaceControlViewModel(
            control.ControlId,
            control.Title,
            control.Value,
            control.ImageHosters);
    }
}

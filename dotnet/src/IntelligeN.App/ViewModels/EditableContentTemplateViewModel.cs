using IntelligeN.Core.Templating;

namespace IntelligeN.App.ViewModels;

public sealed class EditableContentTemplateViewModel : BindableObject
{
    private string _name;
    private string _templateType;
    private string _category;
    private string _description;
    private string _subjectTemplate;
    private string _bodyTemplate;
    private string _tagTemplate;

    public EditableContentTemplateViewModel(
        string id,
        string name,
        string templateType,
        string category,
        string description,
        string subjectTemplate,
        string bodyTemplate,
        string tagTemplate)
    {
        Id = id;
        _name = name;
        _templateType = templateType;
        _category = category;
        _description = description;
        _subjectTemplate = subjectTemplate;
        _bodyTemplate = bodyTemplate;
        _tagTemplate = tagTemplate;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string TemplateType
    {
        get => _templateType;
        set
        {
            if (SetProperty(ref _templateType, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string SubjectTemplate
    {
        get => _subjectTemplate;
        set => SetProperty(ref _subjectTemplate, value);
    }

    public string BodyTemplate
    {
        get => _bodyTemplate;
        set => SetProperty(ref _bodyTemplate, value);
    }

    public string TagTemplate
    {
        get => _tagTemplate;
        set => SetProperty(ref _tagTemplate, value);
    }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(TemplateType)
            ? Name
            : $"{Name} [{TemplateType}]";

    public bool MatchesTemplateType(string currentTemplateType)
    {
        return string.IsNullOrWhiteSpace(TemplateType) ||
               string.IsNullOrWhiteSpace(currentTemplateType) ||
               string.Equals(TemplateType, currentTemplateType, StringComparison.OrdinalIgnoreCase);
    }

    public ContentTemplateDefinition ToDefinition() =>
        new(
            Id: Id,
            Name: Name.Trim(),
            TemplateType: TemplateType.Trim(),
            Category: Category.Trim(),
            Description: Description.Trim(),
            SubjectTemplate: SubjectTemplate,
            BodyTemplate: BodyTemplate,
            TagTemplate: TagTemplate);

    public static EditableContentTemplateViewModel FromDefinition(ContentTemplateDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.TemplateType,
            definition.Category,
            definition.Description,
            definition.SubjectTemplate,
            definition.BodyTemplate,
            definition.TagTemplate);
}

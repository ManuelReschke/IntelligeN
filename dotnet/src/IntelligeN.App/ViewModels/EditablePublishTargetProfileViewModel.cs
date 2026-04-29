using IntelligeN.Core.Publishing;

namespace IntelligeN.App.ViewModels;

public sealed class EditablePublishTargetProfileViewModel : BindableObject
{
    private string _name;
    private string _pluginId;
    private string _templateType;
    private string _contentTemplateId;
    private bool _isActive;
    private string _website;
    private string _accountName;
    private string _accountPassword;
    private string _scopeValue;
    private string _threadId;
    private string _prefix;
    private string _icon;
    private bool _postReply;
    private string _notes;

    public EditablePublishTargetProfileViewModel(
        string id,
        string name,
        string pluginId,
        string templateType,
        string contentTemplateId,
        bool isActive,
        string website,
        string accountName,
        string accountPassword,
        string scopeValue,
        string threadId,
        string prefix,
        string icon,
        bool postReply,
        string notes)
    {
        Id = id;
        _name = name;
        _pluginId = pluginId;
        _templateType = templateType;
        _contentTemplateId = contentTemplateId;
        _isActive = isActive;
        _website = website;
        _accountName = accountName;
        _accountPassword = accountPassword;
        _scopeValue = scopeValue;
        _threadId = threadId;
        _prefix = prefix;
        _icon = icon;
        _postReply = postReply;
        _notes = notes;
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

    public string PluginId
    {
        get => _pluginId;
        set => SetProperty(ref _pluginId, value);
    }

    public string TemplateType
    {
        get => _templateType;
        set => SetProperty(ref _templateType, value);
    }

    public string ContentTemplateId
    {
        get => _contentTemplateId;
        set => SetProperty(ref _contentTemplateId, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string Website
    {
        get => _website;
        set
        {
            if (SetProperty(ref _website, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string AccountName
    {
        get => _accountName;
        set => SetProperty(ref _accountName, value);
    }

    public string AccountPassword
    {
        get => _accountPassword;
        set => SetProperty(ref _accountPassword, value);
    }

    public string ScopeValue
    {
        get => _scopeValue;
        set => SetProperty(ref _scopeValue, value);
    }

    public string ThreadId
    {
        get => _threadId;
        set => SetProperty(ref _threadId, value);
    }

    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public bool PostReply
    {
        get => _postReply;
        set => SetProperty(ref _postReply, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Website)
            ? Name
            : $"{Name} ({Website})";

    public bool MatchesTemplateType(string currentTemplateType)
    {
        return string.IsNullOrWhiteSpace(TemplateType) ||
               string.IsNullOrWhiteSpace(currentTemplateType) ||
               string.Equals(TemplateType, currentTemplateType, StringComparison.OrdinalIgnoreCase);
    }

    public PublishTargetProfile ToDefinition() =>
        new(
            Id: Id,
            Name: Name.Trim(),
            PluginId: PluginId.Trim(),
            TemplateType: TemplateType.Trim(),
            ContentTemplateId: ContentTemplateId.Trim(),
            IsActive: IsActive,
            Website: Website.Trim(),
            AccountName: AccountName.Trim(),
            AccountPassword: AccountPassword,
            ScopeValue: ScopeValue.Trim(),
            ThreadId: ThreadId.Trim(),
            Prefix: Prefix.Trim(),
            Icon: Icon.Trim(),
            PostReply: PostReply,
            Notes: Notes.Trim());

    public static EditablePublishTargetProfileViewModel FromDefinition(PublishTargetProfile definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.PluginId,
            definition.TemplateType,
            definition.ContentTemplateId,
            definition.IsActive,
            definition.Website,
            definition.AccountName,
            definition.AccountPassword,
            definition.ScopeValue,
            definition.ThreadId,
            definition.Prefix,
            definition.Icon,
            definition.PostReply,
            definition.Notes);
}

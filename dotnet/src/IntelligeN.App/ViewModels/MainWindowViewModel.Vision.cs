using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using IntelligeN.Core.Publishing;
using IntelligeN.Core.Templating;
using IntelligeN.Infrastructure.Storage;
using IntelligeN.SDK.Cms;

namespace IntelligeN.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private static readonly AppDataStore AppDataStore = new();
    private static readonly TemplateRenderer TemplateRenderer = new();

    private EditableContentTemplateViewModel? _selectedContentTemplate;
    private EditablePublishTargetProfileViewModel? _selectedPublishTargetProfile;
    private PublishHistoryEntry? _selectedPublishHistoryEntry;
    private string _templateStatusMessage = string.Empty;
    private string _publishTargetsStatusMessage = string.Empty;
    private string _publishHistoryStatusMessage = string.Empty;

    public ObservableCollection<EditableContentTemplateViewModel> ContentTemplates { get; } = [];

    public ObservableCollection<EditablePublishTargetProfileViewModel> PublishTargetProfiles { get; } = [];

    public ObservableCollection<PublishHistoryEntry> PublishHistory { get; } = [];

    public EditableContentTemplateViewModel? SelectedContentTemplate
    {
        get => _selectedContentTemplate;
        set
        {
            if (SetProperty(ref _selectedContentTemplate, value))
            {
                RaiseVisionDerivedPropertiesChanged();
            }
        }
    }

    public EditablePublishTargetProfileViewModel? SelectedPublishTargetProfile
    {
        get => _selectedPublishTargetProfile;
        set
        {
            if (SetProperty(ref _selectedPublishTargetProfile, value))
            {
                RaisePropertyChanged(nameof(SelectedPublishTargetProfilePlugin));
                RaiseVisionDerivedPropertiesChanged();
            }
        }
    }

    public PublishHistoryEntry? SelectedPublishHistoryEntry
    {
        get => _selectedPublishHistoryEntry;
        set
        {
            if (SetProperty(ref _selectedPublishHistoryEntry, value))
            {
                RaiseVisionDerivedPropertiesChanged();
            }
        }
    }

    public string TemplateStatusMessage
    {
        get => _templateStatusMessage;
        private set => SetProperty(ref _templateStatusMessage, value);
    }

    public string PublishTargetsStatusMessage
    {
        get => _publishTargetsStatusMessage;
        private set => SetProperty(ref _publishTargetsStatusMessage, value);
    }

    public string PublishHistoryStatusMessage
    {
        get => _publishHistoryStatusMessage;
        private set => SetProperty(ref _publishHistoryStatusMessage, value);
    }

    public int ContentTemplateCount => ContentTemplates.Count;

    public IReadOnlyList<EditableContentTemplateViewModel> MatchingContentTemplates =>
        ContentTemplates
            .Where(template => template.MatchesTemplateType(XmlWorkspaceTemplateType))
            .OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public int MatchingContentTemplateCount => MatchingContentTemplates.Count;

    public string SelectedContentTemplateName => SelectedContentTemplate?.DisplayName ?? "Keine Vorlage ausgewaehlt";

    public string SelectedContentTemplateDescription => SelectedContentTemplate?.Description ?? string.Empty;

    public string SelectedContentTemplatePreviewSubject =>
        TryRenderTemplatePreview(SelectedContentTemplate, out var preview)
            ? preview.Subject
            : string.Empty;

    public string SelectedContentTemplatePreviewBody =>
        TryRenderTemplatePreview(SelectedContentTemplate, out var preview)
            ? preview.Body
            : string.Empty;

    public string SelectedContentTemplatePreviewTags =>
        TryRenderTemplatePreview(SelectedContentTemplate, out var preview)
            ? preview.Tags
            : string.Empty;

    public string SelectedContentTemplatePreviewStatus
    {
        get
        {
            if (SelectedContentTemplate is null)
            {
                return "Bitte eine Vorlage auswaehlen oder anlegen.";
            }

            if (!HasLoadedXmlWorkspaceDocument)
            {
                return "Preview wartet auf eine geladene XML.";
            }

            return $"Preview fuer {SelectedContentTemplate.DisplayName} mit {XmlWorkspaceControlCount} XML-Feldwerten.";
        }
    }

    public IReadOnlyList<TemplateTokenPreviewEntry> TemplateTokenPreviewEntries =>
        BuildTemplateTokenPreviewEntries();

    public int PublishTargetProfileCount => PublishTargetProfiles.Count;

    public int ActivePublishTargetProfileCount => PublishTargetProfiles.Count(profile => profile.IsActive);

    public IReadOnlyList<EditablePublishTargetProfileViewModel> MatchingPublishTargetProfiles =>
        PublishTargetProfiles
            .Where(profile => profile.MatchesTemplateType(XmlWorkspaceTemplateType))
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public int MatchingPublishTargetProfileCount => MatchingPublishTargetProfiles.Count;

    public int MatchingActivePublishTargetProfileCount =>
        MatchingPublishTargetProfiles.Count(profile => profile.IsActive);

    public string SelectedPublishTargetProfileName =>
        SelectedPublishTargetProfile?.DisplayName ?? "Kein Zielprofil ausgewaehlt";

    public string SelectedPublishTargetProfileTemplateName =>
        ResolveTemplateForProfile(SelectedPublishTargetProfile)?.DisplayName ?? "Keine Vorlage zugeordnet";

    public string PublishBatchPreparationMessage
    {
        get
        {
            if (!HasLoadedXmlWorkspaceDocument)
            {
                return "Bitte zuerst eine XML laden.";
            }

            if (MatchingActivePublishTargetProfileCount == 0)
            {
                return "Es gibt noch keine aktiven Zielprofile fuer diesen XML-Typ.";
            }

            return $"{MatchingActivePublishTargetProfileCount} aktive Zielprofil(e) koennen mit der aktuellen XML beliefert werden.";
        }
    }

    public int PublishHistoryCount => PublishHistory.Count;

    public int FailedPublishHistoryCount => PublishHistory.Count(entry => !entry.IsSuccess);

    public int SuccessfulPublishHistoryCount => PublishHistory.Count(entry => entry.IsSuccess);

    public string SelectedPublishHistoryName =>
        SelectedPublishHistoryEntry?.DisplayName ?? "Kein History-Eintrag ausgewaehlt";

    public void InitializeVisionData()
    {
        TemplateStatusMessage = "Vorlagenverwaltung bereit.";
        PublishTargetsStatusMessage = "Publish-Ziele bereit.";
        PublishHistoryStatusMessage = "Publish-History bereit.";

        LoadContentTemplates();
        LoadPublishTargetProfiles();
        LoadPublishHistory();

        ContentTemplates.CollectionChanged += ContentTemplates_CollectionChanged;
        PublishTargetProfiles.CollectionChanged += PublishTargetProfiles_CollectionChanged;
        PublishHistory.CollectionChanged += PublishHistory_CollectionChanged;

        EnsureSelectedContentTemplate();
        EnsureSelectedPublishTargetProfile();
        SelectedPublishHistoryEntry = PublishHistory.FirstOrDefault();
    }

    public void AddContentTemplate()
    {
        var templateType = HasLoadedXmlWorkspaceDocument ? XmlWorkspaceTemplateType : string.Empty;
        var template = new EditableContentTemplateViewModel(
            id: Guid.NewGuid().ToString("N"),
            name: "Neue Vorlage",
            templateType: templateType,
            category: string.IsNullOrWhiteSpace(templateType) ? "General" : templateType,
            description: "Benutzerdefinierte Vorlage.",
            subjectTemplate: "{{Title}}",
            bodyTemplate: "{{Notes}}",
            tagTemplate: "{{Genre}}");

        AttachContentTemplate(template);
        ContentTemplates.Add(template);
        SelectedContentTemplate = template;
        TemplateStatusMessage = $"Neue Vorlage angelegt: {template.DisplayName}";
    }

    public void RemoveSelectedContentTemplate()
    {
        if (SelectedContentTemplate is null)
        {
            TemplateStatusMessage = "Keine Vorlage ausgewaehlt.";
            return;
        }

        var removed = SelectedContentTemplate;
        DetachContentTemplate(removed);
        ContentTemplates.Remove(removed);
        EnsureSelectedContentTemplate();
        TemplateStatusMessage = $"Vorlage entfernt: {removed.DisplayName}";
    }

    public void SaveContentTemplates()
    {
        AppDataStore.SaveTemplates(
            Directories.DataDirectory,
            ContentTemplates.Select(template => template.ToDefinition()));

        TemplateStatusMessage = $"{ContentTemplates.Count} Vorlage(n) gespeichert.";
    }

    public void AddPublishTargetProfile()
    {
        var template = SelectedContentTemplate ?? MatchingContentTemplates.FirstOrDefault();
        var profile = new EditablePublishTargetProfileViewModel(
            id: Guid.NewGuid().ToString("N"),
            name: "Neues Ziel",
            pluginId: RuntimeCmsPublisherPlugins.FirstOrDefault()?.PluginId ?? "cms.wordpress",
            templateType: HasLoadedXmlWorkspaceDocument ? XmlWorkspaceTemplateType : template?.TemplateType ?? string.Empty,
            contentTemplateId: template?.Id ?? string.Empty,
            isActive: true,
            website: string.Empty,
            accountName: string.Empty,
            accountPassword: string.Empty,
            scopeValue: string.Empty,
            threadId: string.Empty,
            prefix: string.Empty,
            icon: string.Empty,
            postReply: false,
            notes: string.Empty);

        AttachPublishTargetProfile(profile);
        PublishTargetProfiles.Add(profile);
        SelectedPublishTargetProfile = profile;
        PublishTargetsStatusMessage = $"Neues Zielprofil angelegt: {profile.DisplayName}";
    }

    public void RemoveSelectedPublishTargetProfile()
    {
        if (SelectedPublishTargetProfile is null)
        {
            PublishTargetsStatusMessage = "Kein Zielprofil ausgewaehlt.";
            return;
        }

        var removed = SelectedPublishTargetProfile;
        DetachPublishTargetProfile(removed);
        PublishTargetProfiles.Remove(removed);
        EnsureSelectedPublishTargetProfile();
        PublishTargetsStatusMessage = $"Zielprofil entfernt: {removed.DisplayName}";
    }

    public void SavePublishTargetProfiles()
    {
        AppDataStore.SavePublishTargets(
            Directories.DataDirectory,
            PublishTargetProfiles.Select(profile => profile.ToDefinition()));

        PublishTargetsStatusMessage = $"{PublishTargetProfiles.Count} Zielprofil(e) gespeichert.";
    }

    public async Task PublishToActiveTargetsAsync()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            PublishTargetsStatusMessage = "Keine XML geladen. Batch-Publish uebersprungen.";
            return;
        }

        var profiles = MatchingPublishTargetProfiles
            .Where(profile => profile.IsActive)
            .ToArray();

        if (profiles.Length == 0)
        {
            PublishTargetsStatusMessage = "Keine aktiven Zielprofile fuer die aktuelle XML gefunden.";
            return;
        }

        var publishers = LoadRuntimeCmsPublisherPlugins()
            .Where(publisher => publisher.Profile.SupportsTemplate(XmlWorkspaceTemplateType))
            .ToDictionary(publisher => publisher.Descriptor.Id, StringComparer.OrdinalIgnoreCase);

        var fieldSnapshots = BuildPublishFieldSnapshotsFromWorkspace();
        var createdHistoryEntries = new List<PublishHistoryEntry>();
        var successCount = 0;

        foreach (var profile in profiles)
        {
            var template = ResolveTemplateForProfile(profile);
            if (template is null)
            {
                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    contentTemplate: null,
                    subject: PublishSubject,
                    tags: PublishTags,
                    message: PublishMessage,
                    fieldSnapshots,
                    status: CmsPublishStatus.ValidationFailed.ToString(),
                    resultMessage: "Fuer dieses Zielprofil ist keine Vorlage zugeordnet.",
                    articleUrl: string.Empty,
                    articleId: null));
                continue;
            }

            var preview = RenderTemplatePreview(template);

            if (!publishers.TryGetValue(profile.PluginId, out var publisher))
            {
                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    template,
                    preview.Subject,
                    preview.Tags,
                    preview.Body,
                    fieldSnapshots,
                    CmsPublishStatus.Failed.ToString(),
                    $"Runtime-Publisher {profile.PluginId} ist nicht geladen.",
                    string.Empty,
                    null));
                continue;
            }

            try
            {
                using var initializationCts = new CancellationTokenSource(RuntimePublisherInitializationTimeout);
                await publisher.InitializeAsync(initializationCts.Token).AsTask()
                    .WaitAsync(RuntimePublisherInitializationTimeout, initializationCts.Token);

                var request = BuildCmsPublishRequest(profile, preview, fieldSnapshots, XmlWorkspaceTemplateType);

                using var executionCts = new CancellationTokenSource(RuntimePublisherExecutionTimeout);
                var result = await publisher.PublishAsync(request, executionCts.Token).AsTask()
                    .WaitAsync(RuntimePublisherExecutionTimeout, executionCts.Token);

                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    template,
                    preview.Subject,
                    preview.Tags,
                    preview.Body,
                    fieldSnapshots,
                    result.Status.ToString(),
                    result.Message,
                    result.ArticleUrl,
                    result.ArticleId));

                if (result.Status == CmsPublishStatus.Success)
                {
                    successCount++;
                }
            }
            catch (TimeoutException)
            {
                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    template,
                    preview.Subject,
                    preview.Tags,
                    preview.Body,
                    fieldSnapshots,
                    CmsPublishStatus.Failed.ToString(),
                    "Publish-Timeout.",
                    string.Empty,
                    null));
            }
            catch (OperationCanceledException)
            {
                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    template,
                    preview.Subject,
                    preview.Tags,
                    preview.Body,
                    fieldSnapshots,
                    CmsPublishStatus.Failed.ToString(),
                    "Publish abgebrochen.",
                    string.Empty,
                    null));
            }
            catch (Exception exception)
            {
                createdHistoryEntries.Add(CreatePublishHistoryEntry(
                    profile,
                    template,
                    preview.Subject,
                    preview.Tags,
                    preview.Body,
                    fieldSnapshots,
                    CmsPublishStatus.Failed.ToString(),
                    exception.Message,
                    string.Empty,
                    null));
            }
        }

        AppendPublishHistoryEntries(createdHistoryEntries);

        PublishTargetsStatusMessage =
            $"{createdHistoryEntries.Count} Ziel(e) verarbeitet, erfolgreich: {successCount}, fehlgeschlagen: {createdHistoryEntries.Count - successCount}.";
        PublishHistoryStatusMessage = "Publish-History aktualisiert.";
        XmlWorkspaceStatusMessage = PublishTargetsStatusMessage;
    }

    public async Task RetrySelectedPublishHistoryAsync()
    {
        if (SelectedPublishHistoryEntry is null)
        {
            PublishHistoryStatusMessage = "Kein History-Eintrag ausgewaehlt.";
            return;
        }

        var historyEntry = SelectedPublishHistoryEntry;
        var profile = PublishTargetProfiles.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, historyEntry.TargetProfileId, StringComparison.OrdinalIgnoreCase));

        if (profile is null)
        {
            PublishHistoryStatusMessage = "Das zugehoerige Zielprofil existiert nicht mehr.";
            return;
        }

        var publisher = LoadRuntimeCmsPublisherPlugins().FirstOrDefault(candidate =>
            string.Equals(candidate.Descriptor.Id, profile.PluginId, StringComparison.OrdinalIgnoreCase));

        if (publisher is null)
        {
            PublishHistoryStatusMessage = $"Runtime-Publisher {profile.PluginId} ist nicht geladen.";
            return;
        }

        try
        {
            using var initializationCts = new CancellationTokenSource(RuntimePublisherInitializationTimeout);
            await publisher.InitializeAsync(initializationCts.Token).AsTask()
                .WaitAsync(RuntimePublisherInitializationTimeout, initializationCts.Token);

            var request = BuildCmsPublishRequestFromHistory(profile, historyEntry);

            using var executionCts = new CancellationTokenSource(RuntimePublisherExecutionTimeout);
            var result = await publisher.PublishAsync(request, executionCts.Token).AsTask()
                .WaitAsync(RuntimePublisherExecutionTimeout, executionCts.Token);

            var retryHistoryEntry = historyEntry with
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedUtc = DateTimeOffset.UtcNow,
                Status = result.Status.ToString(),
                ResultMessage = result.Message,
                ArticleUrl = result.ArticleUrl,
                ArticleId = result.ArticleId
            };

            AppendPublishHistoryEntries([retryHistoryEntry]);
            PublishHistoryStatusMessage = $"History-Entry erneut gesendet: {retryHistoryEntry.ResultSummary}";
        }
        catch (TimeoutException)
        {
            AppendPublishHistoryEntries([
                historyEntry with
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CreatedUtc = DateTimeOffset.UtcNow,
                    Status = CmsPublishStatus.Failed.ToString(),
                    ResultMessage = "Retry-Timeout."
                }
            ]);
            PublishHistoryStatusMessage = "History-Entry erneut gesendet, aber in Timeout gelaufen.";
        }
        catch (OperationCanceledException)
        {
            AppendPublishHistoryEntries([
                historyEntry with
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CreatedUtc = DateTimeOffset.UtcNow,
                    Status = CmsPublishStatus.Failed.ToString(),
                    ResultMessage = "Retry abgebrochen."
                }
            ]);
            PublishHistoryStatusMessage = "History-Entry erneut gesendet, aber abgebrochen.";
        }
        catch (Exception exception)
        {
            AppendPublishHistoryEntries([
                historyEntry with
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CreatedUtc = DateTimeOffset.UtcNow,
                    Status = CmsPublishStatus.Failed.ToString(),
                    ResultMessage = exception.Message
                }
            ]);
            PublishHistoryStatusMessage = $"Retry fehlgeschlagen: {exception.Message}";
        }
    }

    private void LoadContentTemplates()
    {
        foreach (var template in AppDataStore.LoadTemplates(Directories.DataDirectory))
        {
            var viewModel = EditableContentTemplateViewModel.FromDefinition(template);
            AttachContentTemplate(viewModel);
            ContentTemplates.Add(viewModel);
        }
    }

    private void LoadPublishTargetProfiles()
    {
        foreach (var profile in AppDataStore.LoadPublishTargets(Directories.DataDirectory))
        {
            var viewModel = EditablePublishTargetProfileViewModel.FromDefinition(profile);
            AttachPublishTargetProfile(viewModel);
            PublishTargetProfiles.Add(viewModel);
        }
    }

    private void LoadPublishHistory()
    {
        foreach (var historyEntry in AppDataStore.LoadPublishHistory(Directories.DataDirectory))
        {
            PublishHistory.Add(historyEntry);
        }
    }

    private void ContentTemplates_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(ContentTemplateCount));
        RaisePropertyChanged(nameof(MatchingContentTemplates));
        RaisePropertyChanged(nameof(MatchingContentTemplateCount));
        RaiseVisionDerivedPropertiesChanged();
    }

    private void PublishTargetProfiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(PublishTargetProfileCount));
        RaisePropertyChanged(nameof(ActivePublishTargetProfileCount));
        RaisePropertyChanged(nameof(MatchingPublishTargetProfiles));
        RaisePropertyChanged(nameof(MatchingPublishTargetProfileCount));
        RaisePropertyChanged(nameof(MatchingActivePublishTargetProfileCount));
        RaiseVisionDerivedPropertiesChanged();
    }

    private void PublishHistory_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(PublishHistoryCount));
        RaisePropertyChanged(nameof(FailedPublishHistoryCount));
        RaisePropertyChanged(nameof(SuccessfulPublishHistoryCount));
        RaiseVisionDerivedPropertiesChanged();
    }

    private void AttachContentTemplate(EditableContentTemplateViewModel template)
    {
        template.PropertyChanged += ContentTemplate_PropertyChanged;
    }

    private void DetachContentTemplate(EditableContentTemplateViewModel template)
    {
        template.PropertyChanged -= ContentTemplate_PropertyChanged;
    }

    private void AttachPublishTargetProfile(EditablePublishTargetProfileViewModel profile)
    {
        profile.PropertyChanged += PublishTargetProfile_PropertyChanged;
    }

    private void DetachPublishTargetProfile(EditablePublishTargetProfileViewModel profile)
    {
        profile.PropertyChanged -= PublishTargetProfile_PropertyChanged;
    }

    private void ContentTemplate_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(MatchingContentTemplates));
        RaisePropertyChanged(nameof(MatchingContentTemplateCount));
        RaiseVisionDerivedPropertiesChanged();
    }

    private void PublishTargetProfile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(ActivePublishTargetProfileCount));
        RaisePropertyChanged(nameof(MatchingPublishTargetProfiles));
        RaisePropertyChanged(nameof(MatchingPublishTargetProfileCount));
        RaisePropertyChanged(nameof(MatchingActivePublishTargetProfileCount));
        RaisePropertyChanged(nameof(SelectedPublishTargetProfilePlugin));
        RaiseVisionDerivedPropertiesChanged();
    }

    private void RaiseVisionDerivedPropertiesChanged()
    {
        RaisePropertyChanged(nameof(SelectedContentTemplateName));
        RaisePropertyChanged(nameof(SelectedContentTemplateDescription));
        RaisePropertyChanged(nameof(SelectedContentTemplatePreviewSubject));
        RaisePropertyChanged(nameof(SelectedContentTemplatePreviewBody));
        RaisePropertyChanged(nameof(SelectedContentTemplatePreviewTags));
        RaisePropertyChanged(nameof(SelectedContentTemplatePreviewStatus));
        RaisePropertyChanged(nameof(TemplateTokenPreviewEntries));
        RaisePropertyChanged(nameof(SelectedPublishTargetProfileName));
        RaisePropertyChanged(nameof(SelectedPublishTargetProfileTemplateName));
        RaisePropertyChanged(nameof(PublishBatchPreparationMessage));
        RaisePropertyChanged(nameof(SelectedPublishHistoryName));
    }

    private void EnsureSelectedContentTemplate()
    {
        var availableTemplates = MatchingContentTemplates;
        var nextSelection = availableTemplates.FirstOrDefault(template =>
                                string.Equals(template.Id, SelectedContentTemplate?.Id, StringComparison.OrdinalIgnoreCase))
                            ?? availableTemplates.FirstOrDefault()
                            ?? ContentTemplates.FirstOrDefault();

        SelectedContentTemplate = nextSelection;
    }

    private void EnsureSelectedPublishTargetProfile()
    {
        var availableProfiles = MatchingPublishTargetProfiles;
        var nextSelection = availableProfiles.FirstOrDefault(profile =>
                                string.Equals(profile.Id, SelectedPublishTargetProfile?.Id, StringComparison.OrdinalIgnoreCase))
                            ?? availableProfiles.FirstOrDefault()
                            ?? PublishTargetProfiles.FirstOrDefault();

        SelectedPublishTargetProfile = nextSelection;
    }

    private IReadOnlyList<TemplateTokenPreviewEntry> BuildTemplateTokenPreviewEntries()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            return [];
        }

        var tokenMap = TemplateRenderer.BuildTokenMap(
            new TemplateRenderContext(
                TemplateType: XmlWorkspaceTemplateType,
                TemplateFileName: XmlWorkspaceTemplateFileName,
                XmlFileName: XmlWorkspaceFileName,
                ControlCount: XmlWorkspaceControlCount,
                MirrorCount: XmlWorkspaceMirrorCount,
                DirectlinkCount: XmlWorkspaceDirectlinkCount,
                CrypterCount: XmlWorkspaceCrypterCount,
                GeneratedAtUtc: DateTimeOffset.UtcNow),
            BuildWorkspaceFieldMap());

        return tokenMap
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new TemplateTokenPreviewEntry(entry.Key, entry.Value))
            .ToArray();
    }

    private bool TryRenderTemplatePreview(
        EditableContentTemplateViewModel? template,
        out TemplatePreview preview)
    {
        if (template is null || !HasLoadedXmlWorkspaceDocument)
        {
            preview = new TemplatePreview(string.Empty, string.Empty, string.Empty);
            return false;
        }

        preview = TemplateRenderer.Render(
            template.ToDefinition(),
            new TemplateRenderContext(
                TemplateType: XmlWorkspaceTemplateType,
                TemplateFileName: XmlWorkspaceTemplateFileName,
                XmlFileName: XmlWorkspaceFileName,
                ControlCount: XmlWorkspaceControlCount,
                MirrorCount: XmlWorkspaceMirrorCount,
                DirectlinkCount: XmlWorkspaceDirectlinkCount,
                CrypterCount: XmlWorkspaceCrypterCount,
                GeneratedAtUtc: DateTimeOffset.UtcNow),
            BuildWorkspaceFieldMap());

        return true;
    }

    private TemplatePreview RenderTemplatePreview(EditableContentTemplateViewModel template)
    {
        TryRenderTemplatePreview(template, out var preview);
        return preview;
    }

    private EditableContentTemplateViewModel? ResolveTemplateForProfile(EditablePublishTargetProfileViewModel? profile)
    {
        if (profile is null)
        {
            return SelectedContentTemplate ?? MatchingContentTemplates.FirstOrDefault();
        }

        var assignedTemplate = ContentTemplates.FirstOrDefault(template =>
            string.Equals(template.Id, profile.ContentTemplateId, StringComparison.OrdinalIgnoreCase));

        if (assignedTemplate is not null)
        {
            return assignedTemplate;
        }

        if (SelectedContentTemplate is not null &&
            SelectedContentTemplate.MatchesTemplateType(profile.TemplateType))
        {
            return SelectedContentTemplate;
        }

        return ContentTemplates.FirstOrDefault(template => template.MatchesTemplateType(profile.TemplateType));
    }

    private IReadOnlyDictionary<string, string> BuildWorkspaceFieldMap()
    {
        var fields = EditableXmlWorkspaceControls.ToDictionary(
            control => control.ControlId,
            control => control.Value,
            StringComparer.OrdinalIgnoreCase);

        AddSyntheticTemplateField(fields, "MirrorSummary", XmlWorkspaceLinksSummary);
        AddSyntheticTemplateField(fields, "DirectLinks", XmlWorkspaceDirectlinksText);
        AddSyntheticTemplateField(fields, "CrypterLinks", XmlWorkspaceCrypterLinksText);
        AddSyntheticTemplateField(fields, "DownloadLinks", XmlWorkspaceDownloadLinksText);
        AddSyntheticTemplateField(fields, "Links", XmlWorkspaceDownloadLinksText);

        return fields;
    }

    private IReadOnlyList<PublishFieldSnapshot> BuildPublishFieldSnapshotsFromWorkspace()
    {
        return EditableXmlWorkspaceControls
            .Select(control => new PublishFieldSnapshot(control.ControlId, control.Value))
            .Concat(BuildSyntheticPublishFieldSnapshots())
            .ToArray();
    }

    private IReadOnlyList<PublishFieldSnapshot> BuildSyntheticPublishFieldSnapshots()
    {
        var fields = new List<PublishFieldSnapshot>();
        AddSyntheticPublishField(fields, "MirrorSummary", XmlWorkspaceLinksSummary);
        AddSyntheticPublishField(fields, "DirectLinks", XmlWorkspaceDirectlinksText);
        AddSyntheticPublishField(fields, "CrypterLinks", XmlWorkspaceCrypterLinksText);
        AddSyntheticPublishField(fields, "DownloadLinks", XmlWorkspaceDownloadLinksText);
        return fields;
    }

    private CmsPublishRequest BuildCmsPublishRequest(
        EditablePublishTargetProfileViewModel profile,
        TemplatePreview preview,
        IReadOnlyList<PublishFieldSnapshot> fieldSnapshots,
        string templateType)
    {
        return new CmsPublishRequest(
            TemplateType: templateType,
            Credentials: new CmsPublishCredentials(profile.AccountName, profile.AccountPassword),
            Destination: new CmsPublishDestination(
                Website: profile.Website,
                CategoryIds: ParsePublishCategoryInput(profile.ScopeValue),
                ForumId: NullIfWhiteSpace(profile.ScopeValue),
                ThreadId: NullIfWhiteSpace(profile.ThreadId),
                Prefix: NullIfWhiteSpace(profile.Prefix),
                Icon: NullIfWhiteSpace(profile.Icon),
                PostReply: profile.PostReply),
            Subject: preview.Subject,
            Tags: preview.Tags,
            Message: preview.Body,
            ArticleId: null,
            ArticlePathId: null,
            Fields: fieldSnapshots
                .Select(field => new CmsPublishFieldValue(field.ControlId, field.Value))
                .ToArray(),
            CustomFields: []);
    }

    private static void AddSyntheticTemplateField(
        IDictionary<string, string> fields,
        string controlId,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[controlId] = value;
        }
    }

    private static void AddSyntheticPublishField(
        ICollection<PublishFieldSnapshot> fields,
        string controlId,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields.Add(new PublishFieldSnapshot(controlId, value));
        }
    }

    private CmsPublishRequest BuildCmsPublishRequestFromHistory(
        EditablePublishTargetProfileViewModel profile,
        PublishHistoryEntry historyEntry)
    {
        return new CmsPublishRequest(
            TemplateType: historyEntry.TemplateType,
            Credentials: new CmsPublishCredentials(profile.AccountName, profile.AccountPassword),
            Destination: new CmsPublishDestination(
                Website: profile.Website,
                CategoryIds: ParsePublishCategoryInput(profile.ScopeValue),
                ForumId: NullIfWhiteSpace(profile.ScopeValue),
                ThreadId: NullIfWhiteSpace(profile.ThreadId),
                Prefix: NullIfWhiteSpace(profile.Prefix),
                Icon: NullIfWhiteSpace(profile.Icon),
                PostReply: profile.PostReply),
            Subject: historyEntry.Subject,
            Tags: historyEntry.Tags,
            Message: historyEntry.Message,
            ArticleId: historyEntry.ArticleId,
            ArticlePathId: null,
            Fields: historyEntry.Fields
                .Select(field => new CmsPublishFieldValue(field.ControlId, field.Value))
                .ToArray(),
            CustomFields: []);
    }

    private PublishHistoryEntry CreatePublishHistoryEntry(
        EditablePublishTargetProfileViewModel profile,
        EditableContentTemplateViewModel? contentTemplate,
        string subject,
        string tags,
        string message,
        IReadOnlyList<PublishFieldSnapshot> fieldSnapshots,
        string status,
        string resultMessage,
        string articleUrl,
        int? articleId)
    {
        return new PublishHistoryEntry(
            Id: Guid.NewGuid().ToString("N"),
            CreatedUtc: DateTimeOffset.UtcNow,
            WorkspaceFileName: XmlWorkspaceFileName,
            TemplateType: XmlWorkspaceTemplateType,
            ContentTemplateId: contentTemplate?.Id ?? string.Empty,
            ContentTemplateName: contentTemplate?.DisplayName ?? "Keine Vorlage",
            TargetProfileId: profile.Id,
            TargetProfileName: profile.DisplayName,
            PluginId: profile.PluginId,
            Website: profile.Website,
            Subject: subject,
            Tags: tags,
            Message: message,
            ScopeValue: profile.ScopeValue,
            ThreadId: profile.ThreadId,
            Prefix: profile.Prefix,
            Icon: profile.Icon,
            PostReply: profile.PostReply,
            Fields: fieldSnapshots.ToArray(),
            Status: status,
            ResultMessage: resultMessage,
            ArticleUrl: articleUrl,
            ArticleId: articleId);
    }

    private void AppendPublishHistoryEntries(IEnumerable<PublishHistoryEntry> entries)
    {
        var orderedEntries = entries
            .OrderByDescending(entry => entry.CreatedUtc)
            .ToArray();

        if (orderedEntries.Length == 0)
        {
            return;
        }

        foreach (var entry in orderedEntries.Reverse())
        {
            PublishHistory.Insert(0, entry);
        }

        AppDataStore.SavePublishHistory(Directories.DataDirectory, PublishHistory);
        SelectedPublishHistoryEntry = orderedEntries[0];
    }
}

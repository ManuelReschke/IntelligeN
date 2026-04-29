using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using IntelligeN.Core.Hosting;
using IntelligeN.Core.Runtime;
using IntelligeN.Infrastructure.Projects;
using IntelligeN.SDK.Cms;
using IntelligeN.SDK.Crawlers;
using IntelligeN.LegacyImport.Configuration;
using IntelligeN.LegacyImport.Workspace;
using IntelligeN.PluginHost.Loading;

namespace IntelligeN.App.ViewModels;

public sealed partial class MainWindowViewModel : BindableObject
{
    private static readonly TimeSpan RuntimeCrawlerInitializationTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan RuntimeCrawlerExecutionTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan RuntimePublisherInitializationTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan RuntimePublisherExecutionTimeout = TimeSpan.FromSeconds(15);

    private static readonly IReadOnlyDictionary<string, string> RequiredWorkspaceControls =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IReleaseName"] = "Release name",
            ["ITitle"] = "Original title",
            ["IPicture"] = "Picture",
            ["IGenre"] = "Genre",
            ["ILanguage"] = "Language",
            ["IVideoCodec"] = "Video codec",
            ["IVideoStream"] = "Video stream",
            ["IAudioStream"] = "Audio stream"
        };

    private static readonly RepositoryLayout? Repository = RepositoryLocator.TryLocateFrom(AppContext.BaseDirectory);
    private static readonly AppDirectories Directories = AppRuntime.BuildDirectories(
        Path.Combine(AppContext.BaseDirectory, "runtime"));
    private static readonly LegacyBinInventory LegacyInventory = new();
    private static readonly IntelligenXmlWorkspaceWriter XmlWorkspaceWriter = new();
    private static readonly PluginProjectInventory PluginInventory = new();
    private static readonly PluginCatalog PluginCatalog = new();
    private static readonly LegacyConfigurationSnapshot LegacySnapshot =
        Repository is null
            ? new LegacyConfigurationSnapshot([], [], [], [])
            : LegacyInventory.LoadSnapshot(Repository.LegacyConfigurationDirectory);

    private EditableHosterViewModel? _selectedHoster;
    private ControlDefinitionSummary? _selectedControlDefinition;
    private ControlTemplateSummary? _selectedControlTemplate;
    private LoadedXmlWorkspaceDocument? _loadedXmlWorkspaceDocument;
    private EditableWorkspaceControlViewModel? _selectedWorkspaceControl;
    private string _hosterEditorStatusMessage;
    private string _pluginInspectorStatusMessage;
    private string _xmlWorkspaceStatusMessage;
    private bool _xmlWorkspaceHasUnsavedChanges;
    private string _publishWebsite;
    private string _publishAccountName;
    private string _publishAccountPassword;
    private string _publishSubject;
    private string _publishTags;
    private string _publishMessage;
    private string _publishForumId;
    private string _publishThreadId;
    private string _publishPrefix;
    private string _publishIcon;
    private bool _publishPostReply;
    private string? _selectedPublishTargetPluginId;

    public string StatusMessage { get; } = AppRuntime.BuildStatusMessage(new SystemClock());

    public string PluginFolderMessage { get; } =
        $"Plugin directory scaffolded at: {Directories.PluginDirectory}";

    public string RepositoryRootMessage { get; } =
        Repository is null
            ? "Repository root could not be located from the current app base directory."
            : $"Repository root: {Repository.RootDirectory}";

    public string LegacyConfigurationMessage { get; } =
        Repository is null
            ? "Legacy configuration directory not available."
            : $"Legacy configuration source: {Repository.LegacyConfigurationDirectory}";

    public string PluginSourceMessage { get; } =
        Repository is null
            ? "Plugin-Quellordner nicht verfuegbar."
            : $"Plugin-Quellordner: {Repository.PluginSourceDirectory}";

    public IReadOnlyList<LegacyConfigurationSummary> LegacyConfigurations { get; } = LegacySnapshot.Files;

    public ObservableCollection<EditableHosterViewModel> HosterDefinitions { get; } =
        new(LegacySnapshot.Hosters.Select(EditableHosterViewModel.FromDefinition));

    public IReadOnlyList<CodeDefinitionGroup> CodeDefinitionGroups { get; } = LegacySnapshot.CodeDefinitions;

    public IReadOnlyList<ControlDefinitionSummary> ControlDefinitions { get; } = LegacySnapshot.Controls;

    public ObservableCollection<PluginProjectSummary> PluginProjects { get; } =
        new(Repository is null ? [] : PluginInventory.Enumerate(Repository.PluginSourceDirectory));

    public ObservableCollection<RuntimePluginAssemblySummary> RuntimePluginAssemblies { get; } = [];

    public ObservableCollection<DiscoveredPluginSummary> DiscoveredPlugins { get; } = [];

    public ObservableCollection<EditableWorkspaceControlViewModel> EditableXmlWorkspaceControls { get; } = [];

    public ObservableCollection<CrawlerSuggestionSummary> LastCrawlerSuggestions { get; } = [];

    public ObservableCollection<RuntimeCrawlerMatchSummary> MatchingRuntimeCrawlerPlugins { get; } = [];

    public ObservableCollection<RuntimeCmsPublisherMatchSummary> MatchingRuntimeCmsPublisherPlugins { get; } = [];

    public LoadedXmlWorkspaceDocument? LoadedXmlWorkspaceDocument
    {
        get => _loadedXmlWorkspaceDocument;
        private set
        {
            if (SetProperty(ref _loadedXmlWorkspaceDocument, value))
            {
                RaisePropertyChanged(nameof(HasLoadedXmlWorkspaceDocument));
                RaisePropertyChanged(nameof(XmlWorkspaceFileName));
                RaisePropertyChanged(nameof(XmlWorkspaceFilePath));
                RaisePropertyChanged(nameof(XmlWorkspaceTemplateType));
                RaisePropertyChanged(nameof(XmlWorkspaceTemplateFileName));
                RaisePropertyChanged(nameof(XmlWorkspaceDocumentState));
                RaisePropertyChanged(nameof(XmlWorkspaceControlCount));
                RaisePropertyChanged(nameof(XmlWorkspaceMirrorCount));
                RaisePropertyChanged(nameof(XmlWorkspaceDirectlinkCount));
                RaisePropertyChanged(nameof(XmlWorkspaceDirectlinkUrlCount));
                RaisePropertyChanged(nameof(XmlWorkspaceCrypterCount));
                RaisePropertyChanged(nameof(XmlWorkspaceMirrors));
                RaisePropertyChanged(nameof(XmlWorkspaceLinksSummary));
                RaisePropertyChanged(nameof(XmlWorkspaceDirectlinksText));
                RaisePropertyChanged(nameof(XmlWorkspaceCrypterLinksText));
                RaisePropertyChanged(nameof(XmlWorkspaceDownloadLinksText));
                RaisePropertyChanged(nameof(XmlWorkspaceControls));
                RaisePropertyChanged(nameof(MissingRequiredXmlWorkspaceControls));
                RaisePropertyChanged(nameof(XmlWorkspaceMissingRequiredFieldCount));
                RaisePropertyChanged(nameof(XmlWorkspaceReadinessMessage));
            }
        }
    }

    public EditableWorkspaceControlViewModel? SelectedWorkspaceControl
    {
        get => _selectedWorkspaceControl;
        set
        {
            if (SetProperty(ref _selectedWorkspaceControl, value))
            {
                RaisePropertyChanged(nameof(IsWorkspaceControlSelected));
                RaisePropertyChanged(nameof(SelectedWorkspaceControlId));
                RaisePropertyChanged(nameof(SelectedWorkspaceControlTitle));
                RaisePropertyChanged(nameof(SelectedWorkspaceControlValue));
                RaisePropertyChanged(nameof(SelectedWorkspaceControlImageHosters));
                RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTargetControlId));
                RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTarget));
                RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
            }
        }
    }

    public EditableHosterViewModel? SelectedHoster
    {
        get => _selectedHoster;
        set
        {
            if (SetProperty(ref _selectedHoster, value))
            {
                RaisePropertyChanged(nameof(IsHosterSelected));
            }
        }
    }

    public bool IsHosterSelected => SelectedHoster is not null;

    public ControlDefinitionSummary? SelectedControlDefinition
    {
        get => _selectedControlDefinition;
        set
        {
            if (SetProperty(ref _selectedControlDefinition, value))
            {
                SelectedControlTemplate = value?.Templates.FirstOrDefault();
                RaisePropertyChanged(nameof(IsControlDefinitionSelected));
                RaisePropertyChanged(nameof(SelectedControlTemplates));
                RaisePropertyChanged(nameof(SelectedControlName));
                RaisePropertyChanged(nameof(SelectedControlDefaultTitle));
                RaisePropertyChanged(nameof(SelectedControlDefaultHint));
                RaisePropertyChanged(nameof(SelectedControlTemplateCount));
                RaisePropertyChanged(nameof(SelectedControlListItemCount));
            }
        }
    }

    public ControlTemplateSummary? SelectedControlTemplate
    {
        get => _selectedControlTemplate;
        set
        {
            if (SetProperty(ref _selectedControlTemplate, value))
            {
                RaisePropertyChanged(nameof(IsControlTemplateSelected));
                RaisePropertyChanged(nameof(SelectedTemplateDisplayName));
                RaisePropertyChanged(nameof(SelectedTemplateTitle));
                RaisePropertyChanged(nameof(SelectedTemplateHint));
                RaisePropertyChanged(nameof(SelectedTemplateDefaultValue));
                RaisePropertyChanged(nameof(SelectedTemplateListItemCount));
            }
        }
    }

    public bool IsControlDefinitionSelected => SelectedControlDefinition is not null;

    public bool IsControlTemplateSelected => SelectedControlTemplate is not null;

    public IReadOnlyList<ControlTemplateSummary> SelectedControlTemplates =>
        SelectedControlDefinition?.Templates ?? [];

    public string SelectedControlName => SelectedControlDefinition?.ControlName ?? "Kein Control ausgewaehlt";

    public string SelectedControlDefaultTitle => SelectedControlDefinition?.DefaultTitle ?? string.Empty;

    public string SelectedControlDefaultHint => SelectedControlDefinition?.DefaultHint ?? string.Empty;

    public int SelectedControlTemplateCount => SelectedControlDefinition?.TemplateCount ?? 0;

    public int SelectedControlListItemCount => SelectedControlDefinition?.TotalListItemCount ?? 0;

    public string SelectedTemplateDisplayName => SelectedControlTemplate?.DisplayName ?? "Kein Template ausgewaehlt";

    public string SelectedTemplateTitle => SelectedControlTemplate?.Title ?? string.Empty;

    public string SelectedTemplateHint => SelectedControlTemplate?.Hint ?? string.Empty;

    public string SelectedTemplateDefaultValue => SelectedControlTemplate?.DefaultValue ?? string.Empty;

    public int SelectedTemplateListItemCount => SelectedControlTemplate?.ListItemCount ?? 0;

    public string HosterEditorStatusMessage
    {
        get => _hosterEditorStatusMessage;
        private set => SetProperty(ref _hosterEditorStatusMessage, value);
    }

    public string PluginInspectorStatusMessage
    {
        get => _pluginInspectorStatusMessage;
        private set => SetProperty(ref _pluginInspectorStatusMessage, value);
    }

    public string XmlWorkspaceStatusMessage
    {
        get => _xmlWorkspaceStatusMessage;
        private set => SetProperty(ref _xmlWorkspaceStatusMessage, value);
    }

    public bool XmlWorkspaceHasUnsavedChanges
    {
        get => _xmlWorkspaceHasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _xmlWorkspaceHasUnsavedChanges, value))
            {
                RaisePropertyChanged(nameof(XmlWorkspaceDocumentState));
            }
        }
    }

    public int LegacyConfigurationCount => LegacyConfigurations.Count;

    public int HosterDefinitionCount => HosterDefinitions.Count;

    public int CodeDefinitionGroupCount => CodeDefinitionGroups.Count;

    public int CodeDefinitionCommandCount => CodeDefinitionGroups.Sum(group => group.Commands.Count);

    public int ControlDefinitionCount => ControlDefinitions.Count;

    public int PluginProjectCount => PluginProjects.Count;

    public int BuiltPluginProjectCount => PluginProjects.Count(project => project.HasBuildOutput);

    public int RuntimePluginAssemblyCount => RuntimePluginAssemblies.Count;

    public int DiscoveredPluginCount => DiscoveredPlugins.Count;

    public bool HasLoadedXmlWorkspaceDocument => LoadedXmlWorkspaceDocument is not null;

    public string XmlWorkspaceFileName => LoadedXmlWorkspaceDocument?.FileName ?? "Keine XML geladen";

    public string XmlWorkspaceFilePath => LoadedXmlWorkspaceDocument?.FilePath ?? string.Empty;

    public string XmlWorkspaceTemplateType => LoadedXmlWorkspaceDocument?.TemplateType ?? string.Empty;

    public string XmlWorkspaceTemplateFileName => LoadedXmlWorkspaceDocument?.TemplateFileName ?? string.Empty;

    public string XmlWorkspaceDocumentState =>
        !HasLoadedXmlWorkspaceDocument
            ? "Keine Datei geladen"
            : XmlWorkspaceHasUnsavedChanges
                ? "Ungespeicherte Aenderungen"
                : "Gespeichert";

    public int XmlWorkspaceControlCount => EditableXmlWorkspaceControls.Count;

    public int XmlWorkspaceMirrorCount => LoadedXmlWorkspaceDocument?.MirrorCount ?? 0;

    public int XmlWorkspaceDirectlinkCount => LoadedXmlWorkspaceDocument?.DirectlinkCount ?? 0;

    public int XmlWorkspaceDirectlinkUrlCount => LoadedXmlWorkspaceDocument?.DirectlinkUrlCount ?? 0;

    public int XmlWorkspaceCrypterCount => LoadedXmlWorkspaceDocument?.CrypterCount ?? 0;

    public IReadOnlyList<LoadedXmlMirror> XmlWorkspaceMirrors =>
        LoadedXmlWorkspaceDocument?.Mirrors ?? [];

    public string XmlWorkspaceLinksSummary =>
        !HasLoadedXmlWorkspaceDocument
            ? "Keine XML geladen."
            : $"{XmlWorkspaceMirrorCount} Mirror(s), {XmlWorkspaceDirectlinkUrlCount} Directlink-URL(s), {XmlWorkspaceCrypterCount} Crypter-Link(s).";

    public string XmlWorkspaceDirectlinksText => BuildDirectlinksText();

    public string XmlWorkspaceCrypterLinksText => BuildCrypterLinksText();

    public string XmlWorkspaceDownloadLinksText => BuildDownloadLinksText();

    public string XmlWorkspaceReleaseName => GetWorkspaceControlValue("IReleaseName");

    public string XmlWorkspaceOriginalTitle => GetWorkspaceControlValue("ITitle");

    public string XmlWorkspacePictureLink => GetWorkspaceControlValue("IPicture");

    public string XmlWorkspaceReleaseDate => GetWorkspaceControlValue("IReleaseDate");

    public string XmlWorkspaceGenre => GetWorkspaceControlValue("IGenre");

    public string XmlWorkspaceLanguage => GetWorkspaceControlValue("ILanguage");

    public string XmlWorkspaceRuntime => GetWorkspaceControlValue("IRuntime");

    public string XmlWorkspaceVideoCodec => GetWorkspaceControlValue("IVideoCodec");

    public string XmlWorkspaceVideoStream => GetWorkspaceControlValue("IVideoStream");

    public string XmlWorkspaceAudioStream => GetWorkspaceControlValue("IAudioStream");

    public string XmlWorkspaceSample => GetWorkspaceControlValue("ISample");

    public string XmlWorkspaceNotes => GetWorkspaceControlValue("INotes");

    public string XmlWorkspaceNfo => GetWorkspaceControlValue("INFO");

    public IReadOnlyList<EditableWorkspaceControlViewModel> XmlWorkspaceControls => EditableXmlWorkspaceControls;

    public IReadOnlyList<EditableWorkspaceControlViewModel> MissingRequiredXmlWorkspaceControls =>
        EditableXmlWorkspaceControls
            .Where(IsRequiredWorkspaceControlMissing)
            .OrderBy(control => control.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public int XmlWorkspaceMissingRequiredFieldCount => MissingRequiredXmlWorkspaceControls.Count;

    public IReadOnlyList<DiscoveredPluginSummary> RuntimeCrawlerPlugins =>
        DiscoveredPlugins
            .Where(plugin => string.Equals(plugin.Kind, "Crawler", StringComparison.OrdinalIgnoreCase))
            .OrderBy(plugin => plugin.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public IReadOnlyList<PluginProjectSummary> SourceCrawlerPluginProjects =>
        PluginProjects
            .Where(project => string.Equals(project.Category, "crawler", StringComparison.OrdinalIgnoreCase))
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public int RuntimeCrawlerPluginCount => RuntimeCrawlerPlugins.Count;

    public int SourceCrawlerPluginCount => SourceCrawlerPluginProjects.Count;

    public int LastCrawlerSuggestionCount => LastCrawlerSuggestions.Count;

    public int MatchingRuntimeCrawlerPluginCount => MatchingRuntimeCrawlerPlugins.Count;

    public string MatchingRuntimeCrawlerPluginNames =>
        MatchingRuntimeCrawlerPlugins.Count == 0
            ? "Keine"
            : string.Join(", ", MatchingRuntimeCrawlerPlugins.Select(plugin => plugin.DisplayName));

    public IReadOnlyList<DiscoveredPluginSummary> RuntimeCmsPublisherPlugins =>
        DiscoveredPlugins
            .Where(plugin => string.Equals(plugin.Kind, "Cms", StringComparison.OrdinalIgnoreCase))
            .OrderBy(plugin => plugin.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public IReadOnlyList<DiscoveredPluginSummary> AvailablePublishTargetProfilePlugins =>
        RuntimeCmsPublisherPlugins;

    public DiscoveredPluginSummary? SelectedPublishTargetProfilePlugin
    {
        get
        {
            if (SelectedPublishTargetProfile is null)
            {
                return null;
            }

            return AvailablePublishTargetProfilePlugins.FirstOrDefault(plugin =>
                string.Equals(plugin.PluginId, SelectedPublishTargetProfile.PluginId, StringComparison.OrdinalIgnoreCase));
        }
        set
        {
            if (SelectedPublishTargetProfile is null)
            {
                return;
            }

            var nextPluginId = value?.PluginId ?? string.Empty;
            if (string.Equals(SelectedPublishTargetProfile.PluginId, nextPluginId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedPublishTargetProfile.PluginId = nextPluginId;
            RaisePropertyChanged();
            RaiseVisionDerivedPropertiesChanged();
        }
    }

    public int RuntimeCmsPublisherPluginCount => RuntimeCmsPublisherPlugins.Count;

    public int MatchingRuntimeCmsPublisherPluginCount => MatchingRuntimeCmsPublisherPlugins.Count;

    public string MatchingRuntimeCmsPublisherPluginNames =>
        MatchingRuntimeCmsPublisherPlugins.Count == 0
            ? "Keine"
            : string.Join(", ", MatchingRuntimeCmsPublisherPlugins.Select(plugin => plugin.DisplayName));

    public string XmlWorkspaceCrawlerTargetControlId =>
        GetCrawlerTargetControl()?.ControlId ?? string.Empty;

    public string XmlWorkspaceCrawlerTarget =>
        GetCrawlerTargetControl()?.DisplayTitle ??
        "Kein Feld ausgewaehlt";

    public string XmlWorkspaceReadinessMessage
    {
        get
        {
            if (!HasLoadedXmlWorkspaceDocument)
            {
                return "Noch keine XML geladen.";
            }

            if (XmlWorkspaceMissingRequiredFieldCount == 0)
            {
                return "Die Pflichtfelder sind gefuellt. Jetzt fehlen nur noch Crawler und Publish-Ziel.";
            }

            return $"{XmlWorkspaceMissingRequiredFieldCount} Pflichtfeld(er) fehlen noch.";
        }
    }

    public string XmlWorkspaceCrawlerPreparationMessage
    {
        get
        {
            if (!HasLoadedXmlWorkspaceDocument)
            {
                return "Bitte zuerst eine XML laden.";
            }

            if (XmlWorkspaceMissingRequiredFieldCount == 0)
            {
                return "Im Moment muessen keine Pflichtfelder mehr ueber Crawler gefuellt werden.";
            }

            if (RuntimeCrawlerPluginCount > 0)
            {
                return MatchingRuntimeCrawlerPluginCount > 0
                    ? $"Naechstes Zielfeld: {XmlWorkspaceCrawlerTarget}. Passende Crawler: {MatchingRuntimeCrawlerPluginNames}."
                    : $"Naechstes Zielfeld: {XmlWorkspaceCrawlerTarget}. Aktuell passt kein Runtime-Crawler zum Template {XmlWorkspaceTemplateType}.";
            }

            if (SourceCrawlerPluginCount > 0)
            {
                return $"{XmlWorkspaceCrawlerTarget} ist das naechste Zielfeld, aber es ist noch kein Runtime-Crawler geladen. Im Quellbaum gibt es {SourceCrawlerPluginCount} Crawler-Projekt(e).";
            }

            return $"{XmlWorkspaceCrawlerTarget} ist das naechste Zielfeld, aber es gibt noch keine Crawler-Plugins.";
        }
    }

    public string XmlWorkspacePublishPreparationMessage
    {
        get
        {
            if (!HasLoadedXmlWorkspaceDocument)
            {
                return "Bitte zuerst eine XML laden.";
            }

            if (MatchingRuntimeCmsPublisherPluginCount > 0)
            {
                return $"Publish-Ziel vorbereitet. Passende Ziele: {MatchingRuntimeCmsPublisherPluginNames}.";
            }

            if (RuntimeCmsPublisherPluginCount > 0)
            {
                return $"Es sind Publish-Plugins geladen, aber keines passt aktuell zum Template {XmlWorkspaceTemplateType}.";
            }

            return "Es ist noch kein Runtime-Publish-Plugin geladen.";
        }
    }

    public bool IsWorkspaceControlSelected => SelectedWorkspaceControl is not null;

    public string SelectedWorkspaceControlId => SelectedWorkspaceControl?.ControlId ?? string.Empty;

    public string SelectedWorkspaceControlTitle => SelectedWorkspaceControl?.DisplayTitle ?? "Kein Feld ausgewaehlt";

    public string SelectedWorkspaceControlValue
    {
        get => SelectedWorkspaceControl?.Value ?? string.Empty;
        set
        {
            if (SelectedWorkspaceControl is null || string.Equals(SelectedWorkspaceControl.Value, value, StringComparison.Ordinal))
            {
                return;
            }

            SelectedWorkspaceControl.Value = value;
            RaisePropertyChanged();
        }
    }

    public IReadOnlyList<LoadedXmlImageHoster> SelectedWorkspaceControlImageHosters =>
        SelectedWorkspaceControl?.ImageHosters ?? [];

    public string PublishWebsite
    {
        get => _publishWebsite;
        set => SetProperty(ref _publishWebsite, value);
    }

    public string PublishAccountName
    {
        get => _publishAccountName;
        set => SetProperty(ref _publishAccountName, value);
    }

    public string PublishAccountPassword
    {
        get => _publishAccountPassword;
        set => SetProperty(ref _publishAccountPassword, value);
    }

    public string PublishSubject
    {
        get => _publishSubject;
        set => SetProperty(ref _publishSubject, value);
    }

    public string PublishTags
    {
        get => _publishTags;
        set => SetProperty(ref _publishTags, value);
    }

    public string PublishMessage
    {
        get => _publishMessage;
        set => SetProperty(ref _publishMessage, value);
    }

    public string PublishForumId
    {
        get => _publishForumId;
        set => SetProperty(ref _publishForumId, value);
    }

    public string PublishThreadId
    {
        get => _publishThreadId;
        set => SetProperty(ref _publishThreadId, value);
    }

    public string PublishPrefix
    {
        get => _publishPrefix;
        set => SetProperty(ref _publishPrefix, value);
    }

    public string PublishIcon
    {
        get => _publishIcon;
        set => SetProperty(ref _publishIcon, value);
    }

    public bool PublishPostReply
    {
        get => _publishPostReply;
        set => SetProperty(ref _publishPostReply, value);
    }

    public RuntimeCmsPublisherMatchSummary? SelectedPublishTarget
    {
        get => MatchingRuntimeCmsPublisherPlugins.FirstOrDefault(plugin =>
            string.Equals(plugin.PluginId, _selectedPublishTargetPluginId, StringComparison.OrdinalIgnoreCase));
        set
        {
            var nextPluginId = value?.PluginId;
            if (string.Equals(_selectedPublishTargetPluginId, nextPluginId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedPublishTargetPluginId = nextPluginId;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasSelectedPublishTarget));
            RaisePropertyChanged(nameof(SelectedPublishTargetName));
            RaisePropertyChanged(nameof(PublishScopeLabel));
            RaisePropertyChanged(nameof(PublishScopePlaceholder));
            RaisePropertyChanged(nameof(PublishTargetNotes));
            RaisePropertyChanged(nameof(ShowBoardPublishFields));
            RaisePropertyChanged(nameof(ShowPublishTagFields));
        }
    }

    public bool HasSelectedPublishTarget => SelectedPublishTarget is not null;

    public string SelectedPublishTargetName => SelectedPublishTarget?.DisplayName ?? "Kein Ziel ausgewaehlt";

    public string PublishScopeLabel =>
        ShowBoardPublishFields ? "Forum-ID" : "Kategorien";

    public string PublishScopePlaceholder =>
        ShowBoardPublishFields
            ? "z. B. 12"
            : "z. B. Action, Thriller";

    public string PublishTargetNotes => SelectedPublishTarget?.Notes ?? "Bitte zuerst ein Publish-Ziel auswaehlen.";

    public bool ShowBoardPublishFields =>
        string.Equals(SelectedPublishTarget?.TargetKind, "Board", StringComparison.OrdinalIgnoreCase);

    public bool ShowPublishTagFields => !ShowBoardPublishFields;

    public MainWindowViewModel()
    {
        _publishWebsite = string.Empty;
        _publishAccountName = string.Empty;
        _publishAccountPassword = string.Empty;
        _publishSubject = string.Empty;
        _publishTags = string.Empty;
        _publishMessage = string.Empty;
        _publishForumId = string.Empty;
        _publishThreadId = string.Empty;
        _publishPrefix = string.Empty;
        _publishIcon = string.Empty;
        _publishPostReply = false;
        _xmlWorkspaceHasUnsavedChanges = false;
        _hosterEditorStatusMessage = Repository is null
            ? "Repository root nicht verfuegbar. Hoster-Editor ist nur lesbar."
            : "Hoster-Editor bereit.";
        _pluginInspectorStatusMessage = Repository is null
            ? "Repository root nicht verfuegbar. Plugin-Bereich ist nur lesbar."
            : "Plugin-Bereich bereit.";
        _xmlWorkspaceStatusMessage = "Workflow bereit. XML laden, Daten ergaenzen und danach veroeffentlichen.";

        HosterDefinitions.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(HosterDefinitionCount));
        };

        PluginProjects.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(PluginProjectCount));
            RaisePropertyChanged(nameof(BuiltPluginProjectCount));
            RaisePropertyChanged(nameof(SourceCrawlerPluginProjects));
            RaisePropertyChanged(nameof(SourceCrawlerPluginCount));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
            RaisePropertyChanged(nameof(XmlWorkspacePublishPreparationMessage));
        };

        RuntimePluginAssemblies.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(RuntimePluginAssemblyCount));
        };

        EditableXmlWorkspaceControls.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(XmlWorkspaceControls));
            RaisePropertyChanged(nameof(XmlWorkspaceControlCount));
            RaisePropertyChanged(nameof(MissingRequiredXmlWorkspaceControls));
            RaisePropertyChanged(nameof(XmlWorkspaceMissingRequiredFieldCount));
            RaisePropertyChanged(nameof(XmlWorkspaceReadinessMessage));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTargetControlId));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTarget));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
            RaisePropertyChanged(nameof(XmlWorkspacePublishPreparationMessage));
        };

        DiscoveredPlugins.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(DiscoveredPluginCount));
            RaisePropertyChanged(nameof(RuntimeCrawlerPlugins));
            RaisePropertyChanged(nameof(RuntimeCrawlerPluginCount));
            RaisePropertyChanged(nameof(RuntimeCmsPublisherPlugins));
            RaisePropertyChanged(nameof(AvailablePublishTargetProfilePlugins));
            RaisePropertyChanged(nameof(SelectedPublishTargetProfilePlugin));
            RaisePropertyChanged(nameof(RuntimeCmsPublisherPluginCount));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
            RaisePropertyChanged(nameof(XmlWorkspacePublishPreparationMessage));
        };

        LastCrawlerSuggestions.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(LastCrawlerSuggestionCount));
        };

        MatchingRuntimeCrawlerPlugins.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(MatchingRuntimeCrawlerPluginCount));
            RaisePropertyChanged(nameof(MatchingRuntimeCrawlerPluginNames));
            RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
        };

        MatchingRuntimeCmsPublisherPlugins.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(MatchingRuntimeCmsPublisherPluginCount));
            RaisePropertyChanged(nameof(MatchingRuntimeCmsPublisherPluginNames));
            RaisePropertyChanged(nameof(XmlWorkspacePublishPreparationMessage));
            EnsureSelectedPublishTarget();
            RaiseVisionDerivedPropertiesChanged();
        };

        SelectedHoster = HosterDefinitions.FirstOrDefault();
        SelectedControlDefinition = ControlDefinitions.FirstOrDefault();
        InitializeVisionData();
        RefreshPluginWorkspace();
    }

    public void AddHoster()
    {
        var baseName = "NewHoster";
        var suffix = 1;
        var name = baseName;

        while (HosterDefinitions.Any(hoster => string.Equals(hoster.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            suffix++;
            name = $"{baseName}{suffix}";
        }

        var hoster = new EditableHosterViewModel(name, string.Empty, string.Empty);
        HosterDefinitions.Add(hoster);
        SelectedHoster = hoster;
        HosterEditorStatusMessage = $"Neuer Hoster-Entwurf angelegt: {name}";
    }

    public void RemoveSelectedHoster()
    {
        if (SelectedHoster is null)
        {
            HosterEditorStatusMessage = "Kein Hoster ausgewaehlt.";
            return;
        }

        var removedName = SelectedHoster.Name;
        var removedIndex = HosterDefinitions.IndexOf(SelectedHoster);

        HosterDefinitions.Remove(SelectedHoster);
        SelectedHoster = HosterDefinitions.ElementAtOrDefault(Math.Max(0, removedIndex - 1)) ?? HosterDefinitions.FirstOrDefault();
        HosterEditorStatusMessage = $"Hoster entfernt: {removedName}";
    }

    public void ReloadHosters()
    {
        if (Repository is null)
        {
            HosterEditorStatusMessage = "Repository root nicht verfuegbar. Neuladen uebersprungen.";
            return;
        }

        var hosters = LegacyInventory.LoadHostersFile(Path.Combine(Repository.LegacyConfigurationDirectory, "hoster.xml"));

        HosterDefinitions.Clear();
        foreach (var hoster in hosters.Select(EditableHosterViewModel.FromDefinition))
        {
            HosterDefinitions.Add(hoster);
        }

        SelectedHoster = HosterDefinitions.FirstOrDefault();
        HosterEditorStatusMessage = $"{HosterDefinitions.Count} Hoster aus hoster.xml neu geladen.";
    }

    public void SaveHosters()
    {
        if (Repository is null)
        {
            HosterEditorStatusMessage = "Repository root nicht verfuegbar. Speichern uebersprungen.";
            return;
        }

        var sanitizedHosters = HosterDefinitions
            .Select(hoster => hoster.ToDefinition())
            .Where(hoster => hoster.Name.Length > 0)
            .ToArray();

        LegacyInventory.SaveHostersFile(
            Path.Combine(Repository.LegacyConfigurationDirectory, "hoster.xml"),
            sanitizedHosters);

        HosterEditorStatusMessage = $"{sanitizedHosters.Length} Hoster nach hoster.xml gespeichert.";
    }

    public void LoadBundledSampleXml()
    {
        if (Repository is null)
        {
            XmlWorkspaceStatusMessage = "Repository root nicht verfuegbar. Beispiel konnte nicht geladen werden.";
            return;
        }

        var realMovieSample = Path.Combine(
            Repository.DotnetDirectory,
            "samples",
            "The_Tuxedo_Gefahr_im_Anzug_2002_GERMAN_DL_1080p_WEB_x264_TSCC.xml");
        var fallbackSample = Path.Combine(Repository.DotnetDirectory, "samples", "sample-movie.intelligen.xml");

        LoadXmlWorkspace(File.Exists(realMovieSample) ? realMovieSample : fallbackSample);
    }

    public void LoadXmlWorkspace(string filePath)
    {
        try
        {
            var loader = new IntelligenXmlWorkspaceLoader();
            var loadedDocument = loader.Load(filePath);
            LoadedXmlWorkspaceDocument = loadedDocument;
            ReplaceWorkspaceControls(loadedDocument.Controls);
            XmlWorkspaceHasUnsavedChanges = false;
            RefreshMatchingRuntimeCrawlerPlugins();
            RefreshMatchingRuntimeCmsPublisherPlugins();
            EnsureSelectedContentTemplate();
            EnsureSelectedPublishTargetProfile();
            XmlWorkspaceStatusMessage =
                $"{LoadedXmlWorkspaceDocument.FileName} geladen: {LoadedXmlWorkspaceDocument.ControlCount} Felder, {LoadedXmlWorkspaceDocument.MirrorCount} Mirror(s).";
        }
        catch (Exception exception)
        {
            LoadedXmlWorkspaceDocument = null;
            XmlWorkspaceHasUnsavedChanges = false;
            ReplaceWorkspaceControls([]);
            RefreshMatchingRuntimeCrawlerPlugins();
            RefreshMatchingRuntimeCmsPublisherPlugins();
            EnsureSelectedContentTemplate();
            EnsureSelectedPublishTargetProfile();
            XmlWorkspaceStatusMessage = $"XML konnte nicht geladen werden: {exception.Message}";
        }
    }

    public void ReloadXmlWorkspace()
    {
        if (LoadedXmlWorkspaceDocument is null)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Neuladen uebersprungen.";
            return;
        }

        LoadXmlWorkspace(LoadedXmlWorkspaceDocument.FilePath);
    }

    public void SaveXmlWorkspace()
    {
        if (LoadedXmlWorkspaceDocument is null)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Speichern uebersprungen.";
            return;
        }

        try
        {
            var values = EditableXmlWorkspaceControls.ToDictionary(
                control => control.ControlId,
                control => control.Value,
                StringComparer.OrdinalIgnoreCase);

            XmlWorkspaceWriter.Save(LoadedXmlWorkspaceDocument.FilePath, values);
            LoadedXmlWorkspaceDocument = LoadedXmlWorkspaceDocument with
            {
                Controls = BuildLoadedWorkspaceControlsSnapshot()
            };
            XmlWorkspaceHasUnsavedChanges = false;
            XmlWorkspaceStatusMessage =
                $"{EditableXmlWorkspaceControls.Count} Feldwert(e) in {LoadedXmlWorkspaceDocument.FileName} gespeichert.";
        }
        catch (Exception exception)
        {
            XmlWorkspaceStatusMessage = $"XML konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    public void ApplyWorkspaceHeuristics()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Automatische Ergaenzung uebersprungen.";
            return;
        }

        var releaseName = GetWorkspaceControlValue("IReleaseName");
        if (string.IsNullOrWhiteSpace(releaseName))
        {
            XmlWorkspaceStatusMessage = "Der Releasename ist leer. Fuer die automatische Ergaenzung wird er benoetigt.";
            return;
        }

        var applied = 0;
        applied += TryAutofillWorkspaceValue("ITitle", BuildTitleFromReleaseName(releaseName));
        applied += TryAutofillWorkspaceValue("ILanguage", DetectLanguage(releaseName));
        applied += TryAutofillWorkspaceValue("IVideoCodec", DetectVideoCodec(releaseName));
        applied += TryAutofillWorkspaceValue("IVideoStream", DetectVideoStream(releaseName));
        applied += TryAutofillWorkspaceValue("IAudioStream", DetectAudioStream(releaseName));

        XmlWorkspaceStatusMessage = applied == 0
            ? "Keine leeren Standardfelder fuer die Release-Namen-Heuristik gefunden."
            : $"{applied} Feld(er) automatisch aus dem Releasenamen ergaenzt.";
    }

    public void PrepareCrawlerRequest()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Crawler-Vorbereitung uebersprungen.";
            return;
        }

        var target = GetCrawlerTargetControl();
        if (target is null)
        {
            XmlWorkspaceStatusMessage = "Kein Crawler-Zielfeld gefunden.";
            return;
        }

        RefreshMatchingRuntimeCrawlerPlugins();

        if (MatchingRuntimeCrawlerPluginCount > 0)
        {
            var candidates = string.Join(", ", MatchingRuntimeCrawlerPlugins.Select(plugin => plugin.DisplayName).Take(3));
            XmlWorkspaceStatusMessage = $"Crawler bereit fuer {target.DisplayTitle}. Passende Plugins: {candidates}";
            return;
        }

        if (SourceCrawlerPluginCount > 0)
        {
            XmlWorkspaceStatusMessage =
                $"Crawler fuer {target.DisplayTitle} sind im Quellbaum vorhanden, aber noch nicht als Runtime-Plugins verfuegbar.";
            return;
        }

        XmlWorkspaceStatusMessage = $"Keine passenden Crawler fuer {target.DisplayTitle} gefunden.";
    }

    public async Task RunRuntimeCrawlersAsync()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Crawler-Lauf uebersprungen.";
            return;
        }

        LastCrawlerSuggestions.Clear();
        RefreshMatchingRuntimeCrawlerPlugins();

        var crawlerPlugins = LoadRuntimeCrawlerPlugins()
            .Where(crawler => crawler.Profile.Supports(XmlWorkspaceTemplateType, XmlWorkspaceCrawlerTargetControlId))
            .ToArray();
        if (crawlerPlugins.Length == 0)
        {
            XmlWorkspaceStatusMessage = RuntimeCrawlerPluginCount == 0
                ? "Keine Runtime-Crawler verfuegbar. Erst bauen, kopieren und laden."
                : $"Es sind Runtime-Crawler geladen, aber keiner passt zu Template {XmlWorkspaceTemplateType} und Feld {XmlWorkspaceCrawlerTargetControlId}.";
            return;
        }

        var request = BuildCrawlerRequest();
        var applied = 0;
        var timedOut = new List<string>();
        var failed = new List<string>();

        foreach (var crawler in crawlerPlugins)
        {
            try
            {
                using var initializationCts = new CancellationTokenSource(RuntimeCrawlerInitializationTimeout);
                await crawler.InitializeAsync(initializationCts.Token).AsTask()
                    .WaitAsync(RuntimeCrawlerInitializationTimeout, initializationCts.Token);

                using var executionCts = new CancellationTokenSource(RuntimeCrawlerExecutionTimeout);
                var result = await crawler.CrawlAsync(request, executionCts.Token).AsTask()
                    .WaitAsync(RuntimeCrawlerExecutionTimeout, executionCts.Token);

                foreach (var suggestion in result.Suggestions)
                {
                    var wasApplied = ApplyCrawlerSuggestion(suggestion);
                    if (wasApplied)
                    {
                        applied++;
                    }

                    LastCrawlerSuggestions.Add(new CrawlerSuggestionSummary(
                        CrawlerId: result.CrawlerId,
                        ControlId: suggestion.ControlId,
                        Value: suggestion.Value,
                        Reason: suggestion.Reason,
                        Confidence: suggestion.Confidence,
                        Applied: wasApplied));
                }
            }
            catch (TimeoutException)
            {
                timedOut.Add(crawler.Descriptor.DisplayName);
            }
            catch (OperationCanceledException)
            {
                timedOut.Add(crawler.Descriptor.DisplayName);
            }
            catch
            {
                failed.Add(crawler.Descriptor.DisplayName);
            }
        }

        var statusParts = new List<string>
        {
            LastCrawlerSuggestions.Count == 0
                ? "Crawler haben keine Vorschlaege geliefert."
                : $"Crawler lieferten {LastCrawlerSuggestions.Count} Vorschlag/Vorschlaege; uebernommen: {applied}."
        };

        if (timedOut.Count > 0)
        {
            statusParts.Add($"Timeout: {string.Join(", ", timedOut)}.");
        }

        if (failed.Count > 0)
        {
            statusParts.Add($"Fehler: {string.Join(", ", failed)}.");
        }

        XmlWorkspaceStatusMessage = string.Join(" ", statusParts);
    }

    public void RefreshPluginWorkspace()
    {
        if (Repository is null)
        {
            PluginInspectorStatusMessage = "Repository root nicht verfuegbar. Aktualisieren uebersprungen.";
            return;
        }

        PluginProjects.Clear();
        foreach (var project in PluginInventory.Enumerate(Repository.PluginSourceDirectory))
        {
            PluginProjects.Add(project);
        }

        ReloadRuntimePluginAssemblies();
        DiscoverRuntimePlugins();
        RefreshMatchingRuntimeCrawlerPlugins();
        RefreshMatchingRuntimeCmsPublisherPlugins();

        PluginInspectorStatusMessage =
            $"Plugin-Bereich aktualisiert: {PluginProjectCount} Projekte, {BuiltPluginProjectCount} Build-Ausgaenge, {RuntimePluginAssemblyCount} Runtime-Assemblies.";
    }

    public void CopyBuiltPluginsToRuntime()
    {
        if (Repository is null)
        {
            PluginInspectorStatusMessage = "Repository root nicht verfuegbar. Kopieren uebersprungen.";
            return;
        }

        Directory.CreateDirectory(Directories.PluginDirectory);

        var copied = 0;
        foreach (var project in PluginProjects.Where(project => project.HasBuildOutput))
        {
            var targetPath = Path.Combine(Directories.PluginDirectory, Path.GetFileName(project.OutputAssemblyPath));
            File.Copy(project.OutputAssemblyPath, targetPath, overwrite: true);
            copied++;
        }

        ReloadRuntimePluginAssemblies();
        DiscoverRuntimePlugins();
        RefreshMatchingRuntimeCrawlerPlugins();
        RefreshMatchingRuntimeCmsPublisherPlugins();

        PluginInspectorStatusMessage = copied == 0
            ? "Keine gebauten Plugin-Assemblies unter dotnet/plugins gefunden."
            : $"{copied} Plugin-Assemblies in den Runtime-Ordner kopiert.";
    }

    public void DiscoverRuntimePlugins()
    {
        DiscoveredPlugins.Clear();

        foreach (var result in PluginCatalog.DiscoverFromDirectory(Directories.PluginDirectory))
        {
            if (result.Error is not null)
            {
                DiscoveredPlugins.Add(new DiscoveredPluginSummary(
                    AssemblyFile: Path.GetFileName(result.AssemblyPath),
                    DisplayName: $"Laden fehlgeschlagen: {result.Error.GetType().Name}",
                    PluginId: result.Error.Message,
                    Kind: "Error",
                    Version: "-"));
                continue;
            }

            foreach (var plugin in result.Plugins)
            {
                DiscoveredPlugins.Add(new DiscoveredPluginSummary(
                    AssemblyFile: Path.GetFileName(result.AssemblyPath),
                    DisplayName: plugin.Descriptor.DisplayName,
                    PluginId: plugin.Descriptor.Id,
                    Kind: plugin.Descriptor.Kind.ToString(),
                    Version: plugin.Descriptor.Version));
            }
        }

        PluginInspectorStatusMessage = DiscoveredPlugins.Count == 0
            ? "Keine Runtime-Plugins gefunden. Erst bauen und dann in den Runtime-Ordner kopieren."
            : $"{DiscoveredPlugins.Count} Runtime-Plugin-Eintraege gefunden.";
    }

    private void ReloadRuntimePluginAssemblies()
    {
        RuntimePluginAssemblies.Clear();

        if (!Directory.Exists(Directories.PluginDirectory))
        {
            return;
        }

        foreach (var assemblyPath in Directory.EnumerateFiles(Directories.PluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(assemblyPath);
            RuntimePluginAssemblies.Add(new RuntimePluginAssemblySummary(
                FileName: info.Name,
                FullPath: info.FullName,
                SizeInBytes: info.Length));
        }
    }

    private string GetWorkspaceControlValue(string controlId)
    {
        return EditableXmlWorkspaceControls.FirstOrDefault(control =>
            string.Equals(control.ControlId, controlId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }

    private string BuildDirectlinksText()
    {
        if (LoadedXmlWorkspaceDocument is null)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var mirror in LoadedXmlWorkspaceDocument.Mirrors)
        {
            foreach (var directlink in mirror.Directlinks)
            {
                if (directlink.Urls.Count == 0)
                {
                    continue;
                }

                AddIfPresent(lines, $"{mirror.DisplayName} - {directlink.DisplayName}");
                foreach (var url in directlink.Urls)
                {
                    AddIfPresent(lines, url);
                }

                AddIfPresent(lines, string.Empty);
            }
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private string BuildCrypterLinksText()
    {
        if (LoadedXmlWorkspaceDocument is null)
        {
            return string.Empty;
        }

        var lines = new List<string>();
        foreach (var mirror in LoadedXmlWorkspaceDocument.Mirrors)
        {
            foreach (var crypter in mirror.Crypters)
            {
                AddIfPresent(lines, $"{mirror.DisplayName} - {crypter.DisplayName} - {crypter.Hoster}");
                AddIfPresent(lines, crypter.Url);
            }
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private string BuildDownloadLinksText()
    {
        var crypterLinks = XmlWorkspaceCrypterLinksText;
        var directlinks = XmlWorkspaceDirectlinksText;

        if (string.IsNullOrWhiteSpace(crypterLinks))
        {
            return directlinks;
        }

        if (string.IsNullOrWhiteSpace(directlinks))
        {
            return crypterLinks;
        }

        return string.Join(
            Environment.NewLine,
            [
                "Crypter:",
                crypterLinks,
                string.Empty,
                "Directlinks:",
                directlinks
            ]).Trim();
    }

    private EditableWorkspaceControlViewModel? GetCrawlerTargetControl()
    {
        return IsRequiredWorkspaceControlMissing(SelectedWorkspaceControl)
            ? SelectedWorkspaceControl
            : MissingRequiredXmlWorkspaceControls.FirstOrDefault() ?? SelectedWorkspaceControl;
    }

    private CrawlerRequest BuildCrawlerRequest()
    {
        return new CrawlerRequest(
            TemplateType: XmlWorkspaceTemplateType,
            ReleaseName: XmlWorkspaceReleaseName,
            TargetControlId: XmlWorkspaceCrawlerTargetControlId,
            Fields: EditableXmlWorkspaceControls
                .Select(control => new CrawlerFieldValue(control.ControlId, control.Value))
                .ToArray());
    }

    private CmsPublishRequest BuildCmsPublishRequest()
    {
        return new CmsPublishRequest(
            TemplateType: XmlWorkspaceTemplateType,
            Credentials: new CmsPublishCredentials(PublishAccountName, PublishAccountPassword),
            Destination: new CmsPublishDestination(
                Website: PublishWebsite,
                CategoryIds: ParsePublishCategoryInput(PublishForumId),
                ForumId: NullIfWhiteSpace(PublishForumId),
                ThreadId: NullIfWhiteSpace(PublishThreadId),
                Prefix: NullIfWhiteSpace(PublishPrefix),
                Icon: NullIfWhiteSpace(PublishIcon),
                PostReply: PublishPostReply),
            Subject: PublishSubject,
            Tags: PublishTags,
            Message: PublishMessage,
            ArticleId: null,
            ArticlePathId: null,
            Fields: EditableXmlWorkspaceControls
                .Select(control => new CmsPublishFieldValue(control.ControlId, control.Value))
                .Concat(BuildSyntheticPublishFieldSnapshots()
                    .Select(field => new CmsPublishFieldValue(field.ControlId, field.Value)))
                .ToArray(),
            CustomFields: []);
    }

    private IReadOnlyList<ICrawlerPlugin> LoadRuntimeCrawlerPlugins()
    {
        var crawlers = new List<ICrawlerPlugin>();

        foreach (var result in PluginCatalog.DiscoverFromDirectory(Directories.PluginDirectory))
        {
            if (result.Error is not null)
            {
                continue;
            }

            foreach (var crawler in result.Plugins.OfType<ICrawlerPlugin>())
            {
                crawlers.Add(crawler);
            }
        }

        return crawlers;
    }

    private IReadOnlyList<ICmsPublisherPlugin> LoadRuntimeCmsPublisherPlugins()
    {
        var publishers = new List<ICmsPublisherPlugin>();

        foreach (var result in PluginCatalog.DiscoverFromDirectory(Directories.PluginDirectory))
        {
            if (result.Error is not null)
            {
                continue;
            }

            foreach (var publisher in result.Plugins.OfType<ICmsPublisherPlugin>())
            {
                publishers.Add(publisher);
            }
        }

        return publishers;
    }

    private void RefreshMatchingRuntimeCrawlerPlugins()
    {
        MatchingRuntimeCrawlerPlugins.Clear();

        if (!HasLoadedXmlWorkspaceDocument)
        {
            return;
        }

        var targetControlId = XmlWorkspaceCrawlerTargetControlId;
        foreach (var crawler in LoadRuntimeCrawlerPlugins()
                     .Where(crawler => crawler.Profile.Supports(XmlWorkspaceTemplateType, targetControlId))
                     .OrderBy(crawler => crawler.Descriptor.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            MatchingRuntimeCrawlerPlugins.Add(new RuntimeCrawlerMatchSummary(
                DisplayName: crawler.Descriptor.DisplayName,
                PluginId: crawler.Descriptor.Id,
                Version: crawler.Descriptor.Version,
                SupportedTemplates: crawler.Profile.SupportedTemplateTypes.Count == 0
                    ? "Any"
                    : string.Join(", ", crawler.Profile.SupportedTemplateTypes),
                SupportedControls: crawler.Profile.SupportedControlIds.Count == 0
                    ? "Any"
                    : string.Join(", ", crawler.Profile.SupportedControlIds),
                Notes: crawler.Profile.Notes));
        }
    }

    private void RefreshMatchingRuntimeCmsPublisherPlugins()
    {
        MatchingRuntimeCmsPublisherPlugins.Clear();

        if (!HasLoadedXmlWorkspaceDocument)
        {
            return;
        }

        foreach (var publisher in LoadRuntimeCmsPublisherPlugins()
                     .Where(publisher => publisher.Profile.SupportsTemplate(XmlWorkspaceTemplateType))
                     .OrderBy(publisher => publisher.Descriptor.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            MatchingRuntimeCmsPublisherPlugins.Add(new RuntimeCmsPublisherMatchSummary(
                DisplayName: publisher.Descriptor.DisplayName,
                PluginId: publisher.Descriptor.Id,
                Version: publisher.Descriptor.Version,
                TargetKind: publisher.Profile.TargetKind.ToString(),
                SupportedTemplates: publisher.Profile.SupportedTemplateTypes.Count == 0
                    ? "Any"
                    : string.Join(", ", publisher.Profile.SupportedTemplateTypes),
                SupportedCapabilities: publisher.Profile.SupportedCapabilities.Count == 0
                    ? "-"
                    : string.Join(", ", publisher.Profile.SupportedCapabilities),
                Notes: publisher.Profile.Notes));
        }

        EnsureSelectedPublishTarget();
    }

    private bool ApplyCrawlerSuggestion(CrawlerSuggestion suggestion)
    {
        var control = EditableXmlWorkspaceControls.FirstOrDefault(candidate =>
            string.Equals(candidate.ControlId, suggestion.ControlId, StringComparison.OrdinalIgnoreCase));

        if (control is null || control.HasValue || string.IsNullOrWhiteSpace(suggestion.Value))
        {
            return false;
        }

        control.Value = suggestion.Value;
        return true;
    }

    private bool IsRequiredWorkspaceControlMissing(EditableWorkspaceControlViewModel? control)
    {
        return control is not null &&
               RequiredWorkspaceControls.ContainsKey(control.ControlId) &&
               !control.HasValue;
    }

    private void ReplaceWorkspaceControls(IEnumerable<LoadedXmlControl> controls)
    {
        foreach (var control in EditableXmlWorkspaceControls)
        {
            control.PropertyChanged -= WorkspaceControl_PropertyChanged;
        }

        EditableXmlWorkspaceControls.Clear();

        foreach (var control in controls.Select(EditableWorkspaceControlViewModel.FromControl))
        {
            control.PropertyChanged += WorkspaceControl_PropertyChanged;
            EditableXmlWorkspaceControls.Add(control);
        }

        SelectedWorkspaceControl = EditableXmlWorkspaceControls.FirstOrDefault();
        XmlWorkspaceHasUnsavedChanges = false;
        PreparePublishDraftCore();
        RaiseXmlWorkspaceDerivedPropertiesChanged();
        RefreshMatchingRuntimeCrawlerPlugins();
        RefreshMatchingRuntimeCmsPublisherPlugins();
        EnsureSelectedContentTemplate();
        EnsureSelectedPublishTargetProfile();
    }

    private void WorkspaceControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(EditableWorkspaceControlViewModel.Value), StringComparison.Ordinal) &&
            !string.Equals(e.PropertyName, nameof(EditableWorkspaceControlViewModel.HasValue), StringComparison.Ordinal))
        {
            return;
        }

        if (HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceHasUnsavedChanges = true;
        }

        RaiseXmlWorkspaceDerivedPropertiesChanged();
    }

    private void RaiseXmlWorkspaceDerivedPropertiesChanged()
    {
        RaisePropertyChanged(nameof(XmlWorkspaceReleaseName));
        RaisePropertyChanged(nameof(XmlWorkspaceOriginalTitle));
        RaisePropertyChanged(nameof(XmlWorkspacePictureLink));
        RaisePropertyChanged(nameof(XmlWorkspaceReleaseDate));
        RaisePropertyChanged(nameof(XmlWorkspaceGenre));
        RaisePropertyChanged(nameof(XmlWorkspaceLanguage));
        RaisePropertyChanged(nameof(XmlWorkspaceRuntime));
        RaisePropertyChanged(nameof(XmlWorkspaceVideoCodec));
        RaisePropertyChanged(nameof(XmlWorkspaceVideoStream));
        RaisePropertyChanged(nameof(XmlWorkspaceAudioStream));
        RaisePropertyChanged(nameof(XmlWorkspaceSample));
        RaisePropertyChanged(nameof(XmlWorkspaceNotes));
        RaisePropertyChanged(nameof(XmlWorkspaceNfo));
        RaisePropertyChanged(nameof(MissingRequiredXmlWorkspaceControls));
        RaisePropertyChanged(nameof(XmlWorkspaceMissingRequiredFieldCount));
        RaisePropertyChanged(nameof(XmlWorkspaceReadinessMessage));
        RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTargetControlId));
        RaisePropertyChanged(nameof(XmlWorkspaceCrawlerTarget));
        RaisePropertyChanged(nameof(XmlWorkspaceCrawlerPreparationMessage));
        RaisePropertyChanged(nameof(XmlWorkspacePublishPreparationMessage));
        RaisePropertyChanged(nameof(SelectedWorkspaceControlValue));
        EnsureSelectedContentTemplate();
        EnsureSelectedPublishTargetProfile();
        RaiseVisionDerivedPropertiesChanged();
        RefreshMatchingRuntimeCrawlerPlugins();
        RefreshMatchingRuntimeCmsPublisherPlugins();
    }

    private IReadOnlyList<LoadedXmlControl> BuildLoadedWorkspaceControlsSnapshot()
    {
        return EditableXmlWorkspaceControls
            .Select(control => new LoadedXmlControl(
                ControlId: control.ControlId,
                Title: control.Title,
                Value: control.Value,
                ImageHosters: control.ImageHosters.ToArray()))
            .ToArray();
    }

    public void PreparePublishDraft()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Publish-Vorschlag uebersprungen.";
            return;
        }

        PreparePublishDraftCore();
        RefreshMatchingRuntimeCmsPublisherPlugins();

        XmlWorkspaceStatusMessage = MatchingRuntimeCmsPublisherPluginCount == 0
            ? "Publish-Vorschlag erzeugt, aber noch kein passendes Runtime-Publish-Plugin gefunden."
            : $"Publish-Vorschlag fuer {SelectedPublishTargetName} erzeugt.";
    }

    public void PreparePublishRequest()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Publish-Anfrage uebersprungen.";
            return;
        }

        RefreshMatchingRuntimeCmsPublisherPlugins();

        if (string.IsNullOrWhiteSpace(PublishSubject))
        {
            XmlWorkspaceStatusMessage = "Der Betreff ist leer. Erst aus der XML uebernehmen oder manuell fuellen.";
            return;
        }

        XmlWorkspaceStatusMessage = MatchingRuntimeCmsPublisherPluginCount == 0
            ? "Die Publish-Anfrage ist fertig, aber es gibt noch kein passendes Runtime-Publish-Plugin."
            : $"Die Publish-Anfrage ist fertig fuer {SelectedPublishTargetName}: {PublishSubject}.";
    }

    public async Task RunRuntimePublishersAsync()
    {
        if (!HasLoadedXmlWorkspaceDocument)
        {
            XmlWorkspaceStatusMessage = "Keine XML geladen. Publish-Lauf uebersprungen.";
            return;
        }

        RefreshMatchingRuntimeCmsPublisherPlugins();

        var selectedPluginId = SelectedPublishTarget?.PluginId;
        if (string.IsNullOrWhiteSpace(selectedPluginId))
        {
            XmlWorkspaceStatusMessage = "Kein Publish-Ziel ausgewaehlt.";
            return;
        }

        var publishers = LoadRuntimeCmsPublisherPlugins()
            .Where(publisher =>
                publisher.Profile.SupportsTemplate(XmlWorkspaceTemplateType) &&
                string.Equals(publisher.Descriptor.Id, selectedPluginId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (publishers.Length == 0)
        {
            XmlWorkspaceStatusMessage = RuntimeCmsPublisherPluginCount == 0
                ? "Kein Runtime-Publish-Plugin verfuegbar. Erst bauen, kopieren und laden."
                : $"Das ausgewaehlte Publish-Ziel {SelectedPublishTargetName} ist aktuell nicht verfuegbar.";
            return;
        }

        var request = BuildCmsPublishRequest();
        var results = new List<string>();
        var timedOut = new List<string>();
        var failed = new List<string>();

        foreach (var publisher in publishers)
        {
            try
            {
                using var initializationCts = new CancellationTokenSource(RuntimePublisherInitializationTimeout);
                await publisher.InitializeAsync(initializationCts.Token).AsTask()
                    .WaitAsync(RuntimePublisherInitializationTimeout, initializationCts.Token);

                using var executionCts = new CancellationTokenSource(RuntimePublisherExecutionTimeout);
                var result = await publisher.PublishAsync(request, executionCts.Token).AsTask()
                    .WaitAsync(RuntimePublisherExecutionTimeout, executionCts.Token);
                results.Add($"{publisher.Descriptor.DisplayName}: {result.Status} - {result.Message}");
            }
            catch (TimeoutException)
            {
                timedOut.Add(publisher.Descriptor.DisplayName);
            }
            catch (OperationCanceledException)
            {
                timedOut.Add(publisher.Descriptor.DisplayName);
            }
            catch (Exception exception)
            {
                failed.Add(publisher.Descriptor.DisplayName);
                results.Add($"{publisher.Descriptor.DisplayName}: Fehler - {exception.Message}");
            }
        }

        if (timedOut.Count > 0)
        {
            results.Add($"Timeout: {string.Join(", ", timedOut)}.");
        }

        if (failed.Count > 0)
        {
            results.Add($"Fehler: {string.Join(", ", failed)}.");
        }

        XmlWorkspaceStatusMessage = string.Join(" | ", results);
    }

    private void PreparePublishDraftCore()
    {
        if (TryRenderTemplatePreview(SelectedContentTemplate, out var preview))
        {
            PublishSubject = preview.Subject;
            PublishTags = preview.Tags;
            PublishMessage = preview.Body;
        }
        else
        {
            PublishSubject = FirstNonEmpty(XmlWorkspaceOriginalTitle, XmlWorkspaceReleaseName, XmlWorkspaceFileName);
            PublishTags = string.Join(", ",
                new[] { XmlWorkspaceGenre, XmlWorkspaceLanguage }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .SelectMany(value => value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            PublishMessage = BuildPublishMessage();
        }

        if (!ShowBoardPublishFields && string.IsNullOrWhiteSpace(PublishForumId))
        {
            PublishForumId = BuildSuggestedPublishCategories();
        }
    }

    private string BuildPublishMessage()
    {
        var lines = new List<string>();
        AddIfPresent(lines, FirstNonEmpty(XmlWorkspaceOriginalTitle, XmlWorkspaceReleaseName));
        AddIfPresent(lines, BuildMetadataLine("Genre", XmlWorkspaceGenre));
        AddIfPresent(lines, BuildMetadataLine("Sprache", XmlWorkspaceLanguage));
        AddIfPresent(lines, BuildMetadataLine("Laufzeit", XmlWorkspaceRuntime));
        AddIfPresent(lines, BuildMetadataLine("Video", $"{XmlWorkspaceVideoStream} / {XmlWorkspaceVideoCodec}".Trim(' ', '/')));
        AddIfPresent(lines, BuildMetadataLine("Audio", XmlWorkspaceAudioStream));
        AddIfPresent(lines, BuildMetadataLine("Bild", XmlWorkspacePictureLink));
        AddIfPresent(lines, string.Empty);
        AddIfPresent(lines, XmlWorkspaceNotes);
        var downloadLinks = XmlWorkspaceDownloadLinksText;
        if (!string.IsNullOrWhiteSpace(downloadLinks))
        {
            AddIfPresent(lines, string.Empty);
            AddIfPresent(lines, "Downloadlinks:");
            AddIfPresent(lines, downloadLinks);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddIfPresent(ICollection<string> lines, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add(value);
        }
    }

    private static string BuildMetadataLine(string label, string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{label}: {value}";
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string> ParsePublishCategoryInput(string input)
    {
        return input
            .Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void EnsureSelectedPublishTarget()
    {
        var availableTargets = MatchingRuntimeCmsPublisherPlugins.ToArray();
        var preferredPluginId =
            availableTargets.Any(plugin => string.Equals(plugin.PluginId, _selectedPublishTargetPluginId, StringComparison.OrdinalIgnoreCase))
                ? _selectedPublishTargetPluginId
                : availableTargets.Select(plugin => plugin.PluginId).FirstOrDefault(pluginId =>
                    string.Equals(pluginId, "cms.wordpress", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pluginId, "cms.mybb", StringComparison.OrdinalIgnoreCase)) ??
                  availableTargets.FirstOrDefault()?.PluginId;

        if (string.Equals(_selectedPublishTargetPluginId, preferredPluginId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _selectedPublishTargetPluginId = preferredPluginId;
        RaisePropertyChanged(nameof(SelectedPublishTarget));
        RaisePropertyChanged(nameof(HasSelectedPublishTarget));
        RaisePropertyChanged(nameof(SelectedPublishTargetName));
        RaisePropertyChanged(nameof(PublishScopeLabel));
        RaisePropertyChanged(nameof(PublishScopePlaceholder));
        RaisePropertyChanged(nameof(PublishTargetNotes));
        RaisePropertyChanged(nameof(ShowBoardPublishFields));
        RaisePropertyChanged(nameof(ShowPublishTagFields));
    }

    private string BuildSuggestedPublishCategories()
    {
        return string.Join(", ",
            new[]
            {
                XmlWorkspaceGenre,
                XmlWorkspaceLanguage
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value.Split(['/', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private int TryAutofillWorkspaceValue(string controlId, string suggestedValue)
    {
        if (string.IsNullOrWhiteSpace(suggestedValue))
        {
            return 0;
        }

        var control = EditableXmlWorkspaceControls.FirstOrDefault(candidate =>
            string.Equals(candidate.ControlId, controlId, StringComparison.OrdinalIgnoreCase));

        if (control is null || control.HasValue)
        {
            return 0;
        }

        control.Value = suggestedValue;
        return 1;
    }

    private static string BuildTitleFromReleaseName(string releaseName)
    {
        var parts = releaseName
            .Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        var titleParts = new List<string>();
        foreach (var part in parts)
        {
            var normalized = part.Trim();
            if (normalized.Length == 0)
            {
                continue;
            }

            if (Regex.IsMatch(normalized, "^(19|20)\\d{2}$") || IsReleaseMetadataToken(normalized))
            {
                break;
            }

            titleParts.Add(normalized);
        }

        return titleParts.Count == 0 ? string.Empty : string.Join(" ", titleParts);
    }

    private static string DetectLanguage(string releaseName)
    {
        var upper = releaseName.ToUpperInvariant();
        var hasGerman = upper.Contains("GERMAN", StringComparison.Ordinal);
        var hasEnglish = upper.Contains("ENGLISH", StringComparison.Ordinal) || upper.Contains("ENG", StringComparison.Ordinal);
        var hasDual = upper.Contains(".DL.", StringComparison.Ordinal) ||
                      upper.Contains("_DL_", StringComparison.Ordinal) ||
                      upper.StartsWith("DL.", StringComparison.Ordinal) ||
                      upper.EndsWith(".DL", StringComparison.Ordinal) ||
                      upper.Contains("DUAL", StringComparison.Ordinal);

        if (hasGerman && (hasEnglish || hasDual))
        {
            return "German / English";
        }

        if (hasGerman)
        {
            return "German";
        }

        if (hasEnglish)
        {
            return "English";
        }

        return string.Empty;
    }

    private static string DetectVideoCodec(string releaseName)
    {
        var upper = releaseName.ToUpperInvariant();

        if (upper.Contains("X265", StringComparison.Ordinal) || upper.Contains("H265", StringComparison.Ordinal))
        {
            return "x265";
        }

        if (upper.Contains("X264", StringComparison.Ordinal) || upper.Contains("H264", StringComparison.Ordinal))
        {
            return "x264";
        }

        return string.Empty;
    }

    private static string DetectVideoStream(string releaseName)
    {
        var resolutionMatch = Regex.Match(releaseName, "(2160p|1080p|720p|480p)", RegexOptions.IgnoreCase);
        var source = DetectFirstReleaseToken(releaseName, ["BluRay", "WEB-DL", "WEBDL", "WEBRip", "HDTV", "DVDRip", "REMUX"]);

        if (!resolutionMatch.Success && source.Length == 0)
        {
            return string.Empty;
        }

        if (!resolutionMatch.Success)
        {
            return NormalizeSourceToken(source);
        }

        if (source.Length == 0)
        {
            return resolutionMatch.Value;
        }

        return $"{resolutionMatch.Value} {NormalizeSourceToken(source)}";
    }

    private static string DetectAudioStream(string releaseName)
    {
        var token = DetectFirstReleaseToken(releaseName, ["DTS-HD.MA", "TRUEHD.ATMOS", "TRUEHD", "ATMOS", "DDP5.1", "DD5.1", "AC3", "DTS", "AAC"]);
        return NormalizeAudioToken(token);
    }

    private static string DetectFirstReleaseToken(string releaseName, IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (releaseName.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static bool IsReleaseMetadataToken(string token)
    {
        return token.Equals("GERMAN", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("ENGLISH", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("DL", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("DUAL", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("DTS", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("AAC", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("AC3", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("BLURAY", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("WEBRIP", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("WEBDL", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("WEB-DL", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("HDTV", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("REMUX", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("X264", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("X265", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("H264", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("H265", StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(token, "^\\d{3,4}P$", RegexOptions.IgnoreCase);
    }

    private static string NormalizeSourceToken(string token)
    {
        return token.ToUpperInvariant() switch
        {
            "WEBDL" => "WEB-DL",
            "BLURAY" => "BluRay",
            _ => token
        };
    }

    private static string NormalizeAudioToken(string token)
    {
        return token.ToUpperInvariant() switch
        {
            "TRUEHD.ATMOS" => "TrueHD Atmos",
            "DTS-HD.MA" => "DTS-HD MA",
            "DDP5.1" => "DDP 5.1",
            "DD5.1" => "DD 5.1",
            _ => token
        };
    }
}

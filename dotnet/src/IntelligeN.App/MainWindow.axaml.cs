using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using IntelligeN.App.ViewModels;

namespace IntelligeN.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void AddHoster_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.AddHoster();
    }

    private void RemoveHoster_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.RemoveSelectedHoster();
    }

    private void ReloadHosters_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReloadHosters();
    }

    private void SaveHosters_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SaveHosters();
    }

    private void RefreshPlugins_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.RefreshPluginWorkspace();
    }

    private void CopyPlugins_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.CopyBuiltPluginsToRuntime();
    }

    private void DiscoverPlugins_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.DiscoverRuntimePlugins();
    }

    private async void OpenXmlWorkspace_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "IntelligeN-XML oeffnen",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("XML-Dateien")
                {
                    Patterns = ["*.xml", "*.xml.2"]
                }
            ]
        });

        var localPath = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            ViewModel.LoadXmlWorkspace(localPath);
        }
    }

    private void XmlWorkspace_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = TryGetDroppedXmlPath(e.DataTransfer) is null
            ? DragDropEffects.None
            : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void XmlWorkspace_Drop(object? sender, DragEventArgs e)
    {
        var localPath = TryGetDroppedXmlPath(e.DataTransfer);
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            ViewModel.LoadXmlWorkspace(localPath);
        }

        e.Handled = true;
    }

    private void ReloadXmlWorkspace_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReloadXmlWorkspace();
    }

    private void SaveXmlWorkspace_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SaveXmlWorkspace();
    }

    private void LoadSampleXmlWorkspace_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.LoadBundledSampleXml();
    }

    private void ApplyXmlWorkspaceHeuristics_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ApplyWorkspaceHeuristics();
    }

    private void PrepareCrawlerRequest_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.PrepareCrawlerRequest();
    }

    private async void RunRuntimeCrawlers_Click(object? sender, RoutedEventArgs e)
    {
        await ViewModel.RunRuntimeCrawlersAsync();
    }

    private void PreparePublishDraft_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.PreparePublishDraft();
    }

    private void AddContentTemplate_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.AddContentTemplate();
    }

    private void RemoveContentTemplate_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.RemoveSelectedContentTemplate();
    }

    private void SaveContentTemplates_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SaveContentTemplates();
    }

    private void AddPublishTargetProfile_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.AddPublishTargetProfile();
    }

    private void RemovePublishTargetProfile_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.RemoveSelectedPublishTargetProfile();
    }

    private void SavePublishTargetProfiles_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SavePublishTargetProfiles();
    }

    private async void PublishActiveTargets_Click(object? sender, RoutedEventArgs e)
    {
        await ViewModel.PublishToActiveTargetsAsync();
    }

    private void PreparePublishRequest_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.PreparePublishRequest();
    }

    private async void RunRuntimePublishers_Click(object? sender, RoutedEventArgs e)
    {
        await ViewModel.RunRuntimePublishersAsync();
    }

    private async void RetrySelectedHistoryEntry_Click(object? sender, RoutedEventArgs e)
    {
        await ViewModel.RetrySelectedPublishHistoryAsync();
    }

    private static string? TryGetDroppedXmlPath(IDataTransfer data)
    {
        var files = data.TryGetFiles();
        var localPath = files?.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return null;
        }

        return localPath.EndsWith(".xml.2", StringComparison.OrdinalIgnoreCase) ||
               localPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            ? localPath
            : null;
    }
}

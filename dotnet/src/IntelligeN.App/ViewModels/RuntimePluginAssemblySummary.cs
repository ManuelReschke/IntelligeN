namespace IntelligeN.App.ViewModels;

public sealed record RuntimePluginAssemblySummary(
    string FileName,
    string FullPath,
    long SizeInBytes);

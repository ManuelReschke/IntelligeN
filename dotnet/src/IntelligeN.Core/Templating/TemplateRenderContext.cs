namespace IntelligeN.Core.Templating;

public sealed record TemplateRenderContext(
    string TemplateType,
    string TemplateFileName,
    string XmlFileName,
    int ControlCount,
    int MirrorCount,
    int DirectlinkCount,
    int CrypterCount,
    DateTimeOffset GeneratedAtUtc);

using System.Xml.Linq;

namespace IntelligeN.LegacyImport.Workspace;

public sealed class IntelligenXmlWorkspaceWriter
{
    public void Save(string filePath, IReadOnlyDictionary<string, string> controlValues)
    {
        var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidDataException("XML root element is missing.");
        var controlsElement = root.Element("controls")
            ?? throw new InvalidDataException("controls node is missing.");

        foreach (var controlValue in controlValues)
        {
            var controlElement = controlsElement.Element(controlValue.Key);
            if (controlElement is null)
            {
                continue;
            }

            var valueElement = controlElement.Element("value");
            if (valueElement is null)
            {
                valueElement = new XElement("value", controlValue.Value ?? string.Empty);
                valueElement.SetAttributeValue("list", "#");

                var titleElement = controlElement.Element("title");
                if (titleElement is null)
                {
                    controlElement.AddFirst(valueElement);
                }
                else
                {
                    titleElement.AddAfterSelf(valueElement);
                }

                continue;
            }

            valueElement.Value = controlValue.Value ?? string.Empty;
        }

        document.Save(filePath, SaveOptions.DisableFormatting);
    }
}

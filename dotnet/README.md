# IntelligeN .NET Scaffold

Dieses Verzeichnis enthält das neue Grundgerüst für die Migration nach `C# + .NET 10 + Avalonia`.

## Struktur

- `src/IntelligeN.Core.Abstractions`: kleine, stabile Basisverträge
- `src/IntelligeN.Core`: gemeinsame Kernlogik und Laufzeitpfade
- `src/IntelligeN.Infrastructure`: Dateisystem- und Infrastruktur-Helfer
- `src/IntelligeN.SDK`: Plugin-Verträge und Basisklassen
- `src/IntelligeN.PluginHost`: einfaches Discovery-/Loading-Grundgerüst
- `src/IntelligeN.App.Contracts`: App-nahe Serviceverträge
- `src/IntelligeN.LegacyImport`: Einstiegspunkt für spätere Legacy-Importe
- `src/IntelligeN.App`: Avalonia-Desktop-App
- `plugins/*`: Referenz-Plugins für die neue Plugin-Architektur
- `tests/*`: Platzhalter für spätere Testprojekte

## Bewusste Entscheidung

Der bestehende Delphi-Baum bleibt unverändert an seinem Ort. Für das Grundgerüst gibt es keinen technischen Grund, `src/`, `bin/` oder `res/` zu verschieben.

## Nächste Schritte

1. NuGet-Restore mit Internetzugang ausführen.
2. Avalonia-App bauen und starten.
3. Plugin-Host mit dem finalen Vertragsmodell schärfen.
4. Erste Legacy-Konfigurations- und Binärformate inventarisieren.

## Typische Kommandos

```bash
cd dotnet
DOTNET_CLI_HOME=/tmp dotnet restore
DOTNET_CLI_HOME=/tmp dotnet build
DOTNET_CLI_HOME=/tmp dotnet run --project src/IntelligeN.App/IntelligeN.App.csproj
```

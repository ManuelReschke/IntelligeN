# Migration Plan: Delphi 2010 -> C# / .NET 10 / Avalonia

## Ziel

Dieses Dokument beschreibt die Migration von IntelligeN von `Delphi 2010 + VCL` nach `C# + .NET 10 + Avalonia`.

Ziel ist kein blinder Rewrite, sondern eine kontrollierte Migration mit diesen Prioritäten:

1. Fachlogik und Plugin-Verträge sauber übernehmen.
2. das bestehende Desktop-Verhalten erhalten.
3. UI schrittweise modernisieren, ohne die Produktfunktion zu gefährden.
4. Risiken durch harte Schnitte im Plugin- und UI-Bereich vermeiden.

## Produktziel

Die Zielanwendung soll nicht nur ein XML-Viewer oder Plugin-Demo sein, sondern den eigentlichen alten Kernworkflow wiederherstellen:

1. Benutzer oeffnet oder zieht eine bestehende XML-Datei in die Anwendung.
2. Die Anwendung liest die vorhandenen Daten und zeigt fehlende oder unvollstaendige Felder.
3. Crawler-Plugins helfen beim Nachladen oder Ergaenzen fehlender Informationen.
4. Der Benutzer prueft und bearbeitet die Daten.
5. CMS-/Board-/Website-Plugins publizieren den fertigen Inhalt auf das ausgewaehlte Ziel.

Kurz gesagt:

- `XML rein`
- `Daten ergaenzen`
- `ueber Plugins publizieren`

## Legacy-Referenz

Die alten Screens unter [screenshots/intelligeN-2009-start-screen.png](/home/dev/Workspace/Github/IntelligeN/screenshots/intelligeN-2009-start-screen.png) und [screenshots/intelligeN-2009-start-filled-xml.png](/home/dev/Workspace/Github/IntelligeN/screenshots/intelligeN-2009-start-filled-xml.png) zeigen das Zielbild gut:

- links Login und Control-/Crawler-Bereich
- in der Mitte die XML-/Datenansicht
- rechts Publish-Ziele bzw. Website-/Board-Auswahl

Diese Referenz ist wichtig, weil die neue Avalonia-App nicht bei isolierten Editoren stehen bleiben soll. Am Ende geht es wieder um einen zusammenhaengenden Editor-/Crawler-/Publish-Workflow.

## Ausgangslage

Der aktuelle Codebestand ist in drei Hauptbereiche gegliedert:

- `src/core`
- `src/sdk`
- `src/view`

Dazu kommen:

- viele VCL-Forms und Frames in `src/view`
- ein DLL-basiertes Plugin-System in `src/sdk/plugins`
- Windows-spezifische Units und Bibliotheken
- integrierte Drittanbieter-Komponenten aus dem Delphi-Umfeld

Das Projekt ist damit kein einfacher UI-Port, sondern eine Migration von:

- Sprache: `Object Pascal` -> `C#`
- UI-Framework: `VCL` -> `Avalonia`
- Build-System: `Delphi/MSBuild` -> `.NET SDK`
- Plugin-Modell: `native DLL plugins` -> `managed plugin contracts`

## Zielstack

### Sprache

- `C#`

### Runtime

- `.NET 10`

### Desktop UI

- `Avalonia`

### Projekttypen

- `Class Library` für Core- und SDK-Bausteine
- `Avalonia App` für die Desktop-Anwendung
- `Class Library` für Plugins
- optional `Console`- oder `Worker`-Tools für Tooling und Migrationstools

## Migrationsprinzipien

### 1. Core zuerst, UI zuletzt

Die stabile Reihenfolge ist:

1. `core`
2. `sdk`
3. `plugins`
4. `view`

Damit wird zuerst die wiederverwendbare Logik übertragen und die UI erst dann migriert, wenn die fachlichen Verträge sauber stehen.

### 2. Verträge vor Implementierungen

Interfaces, Models, DTOs, Konfiguration und Fehlerverhalten werden vor konkreten Implementierungen festgezogen. Das ist besonders wichtig für das Plugin-System.

### 3. Keine 1:1-Abbildung auf Klassenebene erzwingen

Nicht jede Delphi-Unit muss als gleichnamige C#-Datei wieder auftauchen. Die fachliche Struktur darf erhalten bleiben, die technische Struktur darf modernisiert werden.

### 4. Plattformziel klar begrenzen

Das Legacy-System ist klar Windows-zentriert. Die neue Anwendung soll aber so geschnitten werden, dass Entwicklung und Ausführung unter `Windows` und `Linux` möglich bleiben, soweit keine Altlasten das verhindern.

### 5. Erst lauffähig, dann schön

Am Anfang zählen:

- Startbarkeit
- Plugin-Laden
- zentrale Workflows
- Daten- und Netzwerklogik

UI-Polish und größere UX-Verbesserungen kommen danach.

## Empfohlene Zielstruktur

```text
/dotnet
  /IntelligeN.sln
  /src
    /IntelligeN.Core
    /IntelligeN.Core.Abstractions
    /IntelligeN.Infrastructure
    /IntelligeN.SDK
    /IntelligeN.PluginHost
    /IntelligeN.App
    /IntelligeN.App.Contracts
    /IntelligeN.LegacyImport
  /plugins
    /IntelligeN.Plugin.Cms.WordPress
    /IntelligeN.Plugin.Crawler.Example
    /IntelligeN.Plugin.FileHoster.Example
  /tests
    /IntelligeN.Core.Tests
    /IntelligeN.SDK.Tests
    /IntelligeN.PluginHost.Tests
```

## Mapping Alt -> Neu

### `src/core`

Wird zu:

- `IntelligeN.Core`
- `IntelligeN.Core.Abstractions`
- ggf. Teilen von `IntelligeN.Infrastructure`

Enthält künftig:

- Basis-Modelle
- Enums und Konstanten
- Utilities
- Parsing-Helfer
- Serialisierung
- gemeinsame Fehler- und Result-Typen

### `src/sdk`

Wird zu:

- `IntelligeN.SDK`
- `IntelligeN.PluginHost`

Enthält künftig:

- Plugin-Interfaces
- Plugin-Metadaten
- Lade- und Aktivierungslogik
- Tooling für Plugin-Erstellung

### `src/view`

Wird zu:

- `IntelligeN.App`

Enthält künftig:

- Avalonia Windows und UserControls
- ViewModels
- Commands
- Navigation
- App-Konfiguration

## UI-Strategie

### VCL -> Avalonia

Zuordnung grob:

- `Form` -> `Window`
- `Frame` -> `UserControl`
- `Action` / Event-Handler -> `Command` oder Event
- `TStringList`, `TCollection` -> `ObservableCollection<T>`, `List<T>`, eigene Modelle
- visuelle Statuslogik -> Bindings, Converter, Styles, DataTemplates

### MVVM pragmatisch einsetzen

Nicht dogmatisch. Empfohlen:

- Views in XAML
- Logik in ViewModels
- pragmatisches Code-Behind für klar UI-nahe Dinge

Es ist nicht sinnvoll, die komplette VCL-Eventstruktur direkt in riesige Code-Behind-Dateien zu kopieren.

### Drittanbieter-Komponenten ersetzen

Für jede VCL-Komponente wird entschieden:

1. durch Avalonia-Bordmittel ersetzen
2. durch .NET-/NuGet-Bibliothek ersetzen
3. neu implementieren, falls fachlich notwendig

Besonders zu prüfen:

- DevExpress VCL Controls
- FastScript / Script-bezogene Features
- TMS AdvMemo
- HtmlViewer
- FastReports-nahe Funktionalität

### Avalonia-spezifische Leitplanken

- keine Windows-spezifische UI-Logik in `IntelligeN.App`
- plattformabhängige Funktionen sauber hinter Interfaces kapseln
- Dateidialoge, Clipboard, Drag-and-Drop und Shell-Integration explizit abstrahieren
- nur dort auf Windows-APIs zugreifen, wo es fachlich notwendig ist

## Plugin-Strategie

Das Plugin-System ist der kritischste Migrationsbereich.

### Aktuell

- native DLL-basierte Plugins
- verschiedene Plugin-Typen wie CMS, Crawler, Filehoster, Imagehoster
- enge Kopplung an Delphi-Typen und SDK-Basisunits

### Ziel

Managed Plugins auf Basis klarer .NET-Contracts:

- gemeinsames Plugin-Interface
- Plugin-Metadaten
- definierter Lebenszyklus
- saubere Fehlergrenzen

### Empfehlung

Plugin-Loading über:

- Assembly-Scanning
- separates Plugin-Verzeichnis
- optional isoliertes Laden über `AssemblyLoadContext`

### Übergangsstrategie

Kein gleichzeitiger Komplettport aller Plugins.

Stattdessen:

1. Plugin-Vertrag definieren
2. PluginHost bauen
3. 1-2 Referenzplugins portieren
4. Rest in Wellen migrieren

## Netzwerk, Parsing, HTML, Skripting

Hier liegt vermutlich ein großer Teil der Fachlogik.

Empfehlungen:

- HTTP: `HttpClient`
- JSON/XML: `System.Text.Json`, `XmlSerializer`, `XDocument` oder gezielte Bibliotheken
- Regex: `System.Text.RegularExpressions`
- HTML Parsing: z. B. `AngleSharp` oder `HtmlAgilityPack`
- Kompression/Archive: .NET-Bordmittel oder gezielte NuGet-Pakete
- Kryptografie: moderne .NET APIs statt Delphi-DEC-Äquivalenten

Skripting ist ein Sonderfall. Falls FastScript oder scriptartige Benutzerlogik relevant ist, muss früh entschieden werden:

- entfernen
- begrenzen
- durch C#-basierte oder DSL-basierte Lösung ersetzen

## Konfiguration und Datenformate

Vor der Portierung muss geklärt werden:

- welche Konfigurationsdateien zur Laufzeit relevant sind
- welche Formate kompatibel bleiben müssen
- welche Legacy-Importpfade benötigt werden

Empfehlung:

- altes Format zunächst lesbar halten
- intern auf klar definierte .NET-Modelle mappen
- später optional neues Format einführen

## Logging, Fehlerbehandlung, Diagnose

Im Zielsystem standardisieren:

- `Microsoft.Extensions.Logging`
- strukturierte Fehlerobjekte
- klare Trennung zwischen Benutzerfehlern, Netzwerkfehlern und Pluginfehlern
- Crash-Logging und Trace-Dateien

## Teststrategie

Das Altsystem hat praktisch keine belastbare automatisierte Testbasis. Deshalb:

### Vor der Migration

- kritische Benutzerflows identifizieren
- manuelle Soll-Verhalten dokumentieren
- Referenzdaten und Beispielseiten sammeln

### Während der Migration

- Unit-Tests für Core-Logik
- Tests für Parsing und HTTP-nahe Transformationen
- Tests für Plugin-Metadaten und Plugin-Laden
- Smoke-Tests für Hauptworkflows

### Nach der Migration

- Regressionstest der wichtigsten End-to-End-Szenarien

## Vorgehen in Phasen

### Phase 0 - Discovery

Ziel:

- Architektur und Abhängigkeiten vollständig erfassen

Ergebnis:

- Inventar von Units, Forms, Plugins, Tools, Drittbibliotheken und Dateiformaten

### Phase 1 - Zielarchitektur festziehen

Ziel:

- neue Solution-Struktur und Contracts definieren

Ergebnis:

- leere .NET-Solution mit Projekten
- Namespace-Konzept
- Plugin-Verträge
- Logging- und Konfigurationskonzept

### Phase 2 - Core portieren

Ziel:

- gemeinsam genutzte Logik lauffähig nach .NET bringen

Ergebnis:

- portierte Modelle, Utilities, Parser, Hilfstypen
- erste Unit-Tests

### Phase 3 - SDK und PluginHost portieren

Ziel:

- Plugins im neuen System definieren und laden

Ergebnis:

- SDK-Basis
- Plugin-Metadaten
- Plugin-Loader
- Referenzplugin

### Phase 4 - erste UI vertikal portieren

Ziel:

- einen kleinen, aber vollständigen User-Workflow in Avalonia lauffähig machen

Ergebnis:

- startbare App
- Shell/MainWindow
- ein funktionierender End-to-End-Flow

### Phase 4a - Legacy-Inspektoren

Ziel:

- alte XML-Konfigurationen erst lesbar und gezielt editierbar machen

Ergebnis:

- `hoster.xml` Editor
- `controls.xml` Inspector
- `codedef.xml` Viewer
- Plugin-Workspace mit Runtime-Discovery

### Phase 4b - Produktkern wieder aufbauen

Ziel:

- vom Infrastruktur-Prototypen zum eigentlichen Produktworkflow kommen

Ergebnis:

- XML-Datei oeffnen oder hineinziehen
- Datenmodell fuer einen Post/Edit-Workflow
- fehlende Felder markieren
- Crawler-Anbindung fuer Feldanreicherung
- Publish-Zielauswahl und Plugin-gestuetztes Publizieren

### Phase 5 - Plugin-Wellen

Ziel:

- Plugin-Typen nacheinander migrieren

Empfohlene Reihenfolge:

1. einfache Plugins
2. häufig genutzte Plugins
3. komplexe oder historisch fragile Plugins

### Phase 6 - UI-Vervollständigung

Ziel:

- restliche Forms, Dialoge, Listen, Editoren und Assistenten portieren

### Phase 7 - Legacy-Abbau

Ziel:

- alte Workarounds, harte Kopplungen und Delphi-spezifische Annahmen entfernen

## Risiken

### 1. Plugin-Kompatibilität

Höchstes Risiko, weil das aktuelle Modell DLL- und Delphi-zentriert ist.

### 2. Drittanbieter-VCL-Komponenten

Einige Controls oder Editoren haben in Avalonia keine direkte Entsprechung.

### 3. Verdeckte Geschäftslogik in UI-Code

Typisch bei älteren VCL-Anwendungen. Muss beim Portieren aktiv getrennt werden.

### 4. Historische Sonderfälle in Plugins

Gerade bei CMS- und Filehoster-Plugins sind spezielle Edge-Cases wahrscheinlich.

### 5. Fehlende Tests im Altsystem

Erhöht das Risiko stiller Funktionsverluste.

## Definition of Done pro Migrationspaket

Ein Paket gilt erst als fertig, wenn:

- der Code baut
- relevante Tests vorhanden sind
- Logging vorhanden ist
- Fehlerfälle sichtbar behandelt werden
- ein manueller Smoke-Test dokumentiert ist

## Todo

### Aktueller Status

- [x] neues `.NET 10 + Avalonia` Grundgeruest unter `dotnet/` angelegt
- [x] startbare Avalonia-App aufgebaut
- [x] Legacy-XML-Inventar fuer `hoster.xml`, `controls.xml`, `codedef.xml`
- [x] erster echter Editor fuer `hoster.xml`
- [x] Inspector fuer `controls.xml`
- [x] Viewer fuer `codedef.xml`
- [x] Plugin-Workspace mit Projektuebersicht, Runtime-Ordner und Discovery
- [x] XML-Datei als eigentlichen Arbeitsdatensatz oeffnen
- [x] fehlende Daten im Post-Datensatz markieren
- [x] lokale Heuristik fuer erste Feldergaenzung aus dem Release-Namen
- [~] Crawler-Workflow in die neue App integrieren
  erster Runtime-Pfad vorhanden: Ziel-Feld, Matching nach Template/Control und ein ausfuehrbarer Beispiel-Crawler fuer Autofill leerer Felder
- [x] erster Legacy-Crawler als .NET-Plugin portiert
  `Releasename` liefert jetzt Titel-, Sprach-, Notiz- und Stream-Metadaten direkt aus dem Release-Namen
- [x] erster Web-Crawler als .NET-Plugin portiert
  `IMDb` liefert jetzt best-effort Metadaten fuer Bild, Regie, Genre, Laufzeit, Beschreibung und Release-Date aus Search-/Title-Metadaten; Firmen-/Distributor-Daten werden ueber die Company-Credits-Seite versucht
- [x] erster CMS-Publish-Contract im neuen SDK angelegt
  der neue CMS-Slice bildet `blog`, `board` und `formbased` ab und deckt damit auch `warezddl`-artige Board-Ziele ueber Website-, Forum-, Thread-, Prefix-, Icon- und Reply-Felder sauber ab
- [x] erstes CMS-Referenzplugin auf den neuen Contract gehoben
  `WordPress` implementiert jetzt `ICmsPublisherPlugin` mit Publish-Profil und einem ersten echten XML-RPC-Transport ueber `wp.newPost` und `wp.editPost`
- [x] zweites reales Publish-Ziel aus dem Legacy-Bestand portiert
  `MyBB` ist jetzt als neues Runtime-CMS-Plugin im .NET-Stack vorhanden und bildet den bestehenden Delphi-Pfad als best-effort HTTP-Transport nach: Login, Pre-Post-Seite laden, Hidden-Inputs uebernehmen und neuen Thread oder Reply absenden
- [x] erstes board-orientiertes Publish-Ziel fuer warezddl-artige Foren angelegt
  `warezddl Board` ist als neues Runtime-CMS-Plugin im .NET-Stack vorhanden, validiert Website-, Login-, Forum-/Thread-, Prefix-/Icon- und Reply-Daten und spiegelt damit die alte `TCMSBoardPlugIn`-/`TCMSBoardIPPlugIn`-Richtung; der echte HTTP-Transport steht noch aus
- [x] erster Publish-Vorbereitungsfluss in der App
  der XML-Workspace baut jetzt einen Publish-Draft aus dem geladenen Datensatz, erzeugt einen CMS-Publish-Request und kann Runtime-Publisher gegen diesen Request laufen lassen
- [x] Publish-Workflow im UI weiter vereinfacht
  der Hauptpfad waehlt jetzt genau ein Publish-Ziel aus, oeffnet standardmaessig direkt den `Workflow`-Tab und blendet Board-spezifische Felder wie Thread, Prefix, Icon und Reply-Modus nur noch bei echten Board-Zielen wie `MyBB` ein
- [x] Runtime-Publisher gegen Haenger abgesichert
  auch CMS-/Publish-Plugins laufen jetzt mit festen Initialisierungs- und Ausfuehrungs-Timeouts, damit die App nicht an haengenden Publish-Zielen blockiert
- [x] Runtime-Crawler gegen Haenger abgesichert
  Crawler-Initialisierung und -Ausfuehrung laufen jetzt mit Timeouts, damit einzelne Plugins die UI nicht mehr unbegrenzt blockieren
- [x] UI fuer normale Bildschirmbreiten verdichtet
  kompakter Header statt dauerhafter Sidebar, kleinere Dichtewerte und mehrere ehemals nebeneinanderliegende Editorbereiche jetzt vertikal gestapelt
- [x] XML-Workspace speicherbar gemacht
  bearbeitete Control-Werte koennen jetzt wieder in die geladene XML-Datei zurueckgeschrieben werden; der Workspace zeigt dazu einen Saved/Unsaved-Zustand an
- [x] XML-Workspace auf Drag and Drop gehoben
  `.xml`- und `.xml.2`-Dateien koennen jetzt direkt auf den Workspace gezogen werden, statt nur ueber den Dateidialog geladen zu werden
- [x] UI fuer kleine Screens weiter verdichtet
  breite Zwei- und Drei-Spalten-Bloecke im XML-, Legacy- und Plugin-Bereich sind weiter auf Wrap-/Stack-Layouts reduziert, damit die App auch ohne grossen Monitor bedienbar bleibt
- [x] Workflow-UI auf Kernschritte reduziert
  die Hauptansicht ist jetzt staerker deutsch beschriftet und auf die drei Schritte `Pflichtfelder`, `Daten ergaenzen` und `Veroeffentlichen` verdichtet; technische Detaillisten sind hinter Expandern versteckt und ueberfluessige Vorbereitungs-Buttons aus dem Hauptblick entfernt
- [~] Live-Verifikation fuer Web-Crawler
  der Code baut und ist in den Runtime-Plugin-Ordner kopiert, aber der echte HTTP-Smoke-Test gegen IMDb steht noch aus, weil die Zielseite in der aktuellen Umgebung nicht sinnvoll aufloesbar war
- [~] Publish-Workflow mit CMS-/Board-Plugins wiederherstellen
  `WordPress` und `MyBB` sind als erste reale Publish-Ziele vorhanden, aber Live-Smoke-Tests gegen echte Zielsysteme, weitere Board-Transporte und der vollstaendige Publish-Komfort aus dem Legacy-Client fehlen noch

### Sofort

- [ ] vollständiges Inventar aller Projekte, Units, Forms, Frames und Plugins erstellen
- [ ] alle externen Delphi-Abhängigkeiten und VCL-Komponenten erfassen
- [ ] Plugin-Typen und ihre gemeinsamen Verträge dokumentieren
- [ ] Laufzeitdateien in `bin/` klassifizieren: Pflicht, optional, Legacy
- [ ] kritische Benutzerflows identifizieren und als Referenz dokumentieren

### Architektur

- [ ] neue `.NET 10` Solution-Struktur definieren
- [ ] Ziel-Namensräume und Projektgrenzen festlegen
- [ ] gemeinsames Domain-/Core-Modell definieren
- [ ] Konfigurationsmodell für die neue App festlegen
- [ ] Logging- und Fehlerbehandlungskonzept festlegen

### Plugin-System

- [ ] gemeinsames `IPlugin`-Basiskonzept definieren
- [ ] Plugin-Metadatenmodell definieren
- [ ] Plugin-Kategorien für CMS, Crawler, Filehoster, Imagehoster modellieren
- [ ] Plugin-Ladeprozess in .NET entwerfen
- [ ] Referenzplugin als Proof of Concept umsetzen

### Core-Port

- [ ] zentrale Konstanten, Enums und Interfaces portieren
- [ ] String-, HTML-, Regex-, Größen- und Dateihilfen portieren
- [ ] Serialisierung und Konfigurationsparser portieren
- [ ] HTTP-bezogene Basisschichten modernisieren
- [ ] Core-Unit-Tests aufbauen

### UI-Port

- [ ] App-Shell mit `MainWindow` und Grundnavigation aufsetzen
- [ ] erste VCL-Form als Avalonia-Fenster portieren
- [ ] erste VCL-Frame als Avalonia-UserControl portieren
- [ ] Listen-, Dialog- und Editor-Muster standardisieren
- [ ] Bindings, Commands und ViewModels für Kernflows einführen
- [ ] plattformabhängige UI-Dienste als Interfaces definieren

### Migration Delivery

- [ ] ersten vertikalen End-to-End-Workflow auswählen
- [ ] Workflow komplett in `.NET 10 + Avalonia` umsetzen
- [ ] Smoke-Test dokumentieren
- [ ] weitere Portierungswellen priorisieren
- [ ] Legacy-Teile nach erfolgreicher Ablösung stilllegen

### Produktworkflow

- [ ] XML-Datei per Datei-Dialog oeffnen
- [ ] XML-Datei per Drag-and-Drop importieren
- [ ] neues .NET-Datenmodell fuer den Bearbeitungsdatensatz festlegen
- [ ] fehlende Felder im Editor sichtbar kennzeichnen
- [ ] Crawler-Ergebnisse in den Datensatz zurueckschreiben
- [~] ersten echten externen Web-Crawler-End-to-End pruefen
  `IMDb` ist portiert, aber der Live-Lauf gegen die echte Zielseite muss auf einem normalen Dev-Setup noch manuell bestaetigt werden
- [ ] Publish-Zielauswahl fuer Boards/Websites aufbauen
- [ ] Plugin-gestuetztes Publizieren aus der neuen App ausloesen

### Plattform

- [ ] Linux- und Windows-Entwicklungssetup dokumentieren
- [ ] alle Windows-spezifischen Annahmen im neuen Code markieren
- [ ] UI- und Plugin-Basis unter Ubuntu pruefen
- [ ] unvermeidbare Windows-Abhaengigkeiten separat kapseln

## Erste konkrete Empfehlung

Der erste echte Umsetzungsmeilenstein sollte nicht "UI komplett portieren" sein, sondern:

`App startet -> PluginHost lädt Referenzplugin -> ein einfacher Workflow funktioniert`

Das reduziert Risiko und schafft früh ein belastbares technisches Fundament.

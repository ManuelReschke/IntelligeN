# Migration Inventory: Delphi 2010 -> C# / .NET 10 / Avalonia

## Zweck

Dieses Dokument hält den aktuellen Migrationsbestand des Legacy-Repos fest. Es dient als Arbeitsinventar fuer Planung, Priorisierung und Zuschnitt der ersten .NET-Umsetzung.

Stand: erste statische Erfassung auf Basis der vorhandenen Repository-Struktur.

## Aktueller .NET-Status

Der neue Stand liegt unter `dotnet/` und ist nicht mehr nur theoretisch:

- Avalonia-App scaffolded und baubar
- Legacy-Konfigurationsansichten fuer `hoster.xml`, `controls.xml` und `codedef.xml`
- editierbarer `hoster.xml`-Bereich
- Plugin-Workspace mit Projektuebersicht, Runtime-Ordner und Discovery
- XML-Workspace zum Laden echter `intelligen.xml.2`-Dateien
- XML-Workspace mit editierbaren Feldern und Rueckspeichern in die geladene XML-Datei
- XML-Workspace mit Drag-and-Drop fuer `.xml`- und `.xml.2`-Dateien
- Pflichtfeld-Check fuer den XML-Post-Datensatz
- erster lokaler Autofill ueber Release-Name-Heuristiken
- erster `Crawler Prep`-Bereich mit Ziel-Feld und Plugin-Kandidaten
- erster Runtime-Crawler-Vertrag im SDK plus Beispiel-Crawler fuer Autofill leerer Felder
- Runtime-Crawler-Matching nach Template-Typ und Ziel-Control im XML-Workspace
- erster echter Legacy-Crawler-Port: `Releasename`
- erster echter Web-Crawler-Port: `IMDb`
- erster CMS-Publish-Contract im SDK fuer `blog`, `board` und `formbased`
- erstes CMS-Referenzplugin auf dem neuen Contract: `WordPress`
- `WordPress` mit erstem echtem XML-RPC-Transport fuer `wp.newPost` / `wp.editPost`
- `MyBB` als zweites reales Board-Publish-Ziel mit best-effort Login-/Thread-/Reply-Transport entlang des vorhandenen Delphi-Plugins
- erstes board-orientiertes Publish-Ziel fuer `warezddl`-artige Foren
- erster Publish-Draft/Request/Runtime-Publisher-Pfad im XML-Workspace
- Runtime-Crawler mit Timeouts gegen haengende Plugins abgesichert
- Runtime-Publisher mit Timeouts gegen haengende Plugins abgesichert
- kompaktere UI fuer kleinere Screens mit Header statt linker Dauersidebar, weiter verdichteten Karten und mehr vertikal gestapelten Arbeitsbereichen
- vereinfachter Haupt-Workflow mit staerker deutscher Beschriftung und weniger Technik-Buttons im direkten Blick
- vereinfachter Publish-Bereich mit klarer Zielauswahl statt parallelem Start aller Publisher und mit board-spezifischen Feldern nur bei Board-Zielen
- sieben gebaute Referenz-Plugins im neuen .NET-Format

Wichtig:

Der aktuelle Prototyp ist nicht mehr nur ein Infrastruktur-Frontend, aber der eigentliche Produktkern ist noch unvollstaendig. XML laden, per Drag and Drop reinziehen, Pflichtfelder sehen, lokal ergaenzen, wieder abspeichern und ueber Runtime-Crawler erste Suggestions anwenden geht jetzt; mit `Releasename` existiert jetzt auch der erste echte Legacy-Port und mit `IMDb` der erste echte Web-Crawler-Port. Im Publish-Bereich gibt es jetzt den ersten echten CMS-Contract fuer Blog-/Board-/Formbased-Ziele, einen echten WordPress-Transport ueber XML-RPC, ein erstes reales Board-Ziel ueber `MyBB`, ein optionales `warezddl`-nahes Stub-Ziel und einen vereinfachten Publish-Draft/Runtime-Publisher-Pfad direkt im XML-Workspace. Die echte Live-Verifikation gegen IMDb, WordPress und MyBB steht noch aus.

## Legacy-Produktkern

Aus Codebasis, Screenshots und bisheriger Einordnung ergibt sich der eigentliche Kern des Produkts:

1. XML-Datei oeffnen oder in die Anwendung ziehen
2. vorhandene Daten lesen
3. fehlende Informationen ueber Crawler und Editoren ergaenzen
4. Ergebnis mit Plugins auf Board, Forum oder Website publizieren

Die alten Screens [intelligeN-2009-start-screen.png](/home/dev/Workspace/Github/IntelligeN/screenshots/intelligeN-2009-start-screen.png) und [intelligeN-2009-start-filled-xml.png](/home/dev/Workspace/Github/IntelligeN/screenshots/intelligeN-2009-start-filled-xml.png) bestaetigen diesen Ablauf visuell:

- linke Spalte fuer Login und Control-/Crawler-Helfer
- zentrale Bearbeitungsflaeche fuer den XML-/Post-Datensatz
- rechte Publish-/Website-Zielspalte

Das ist das Zielbild fuer spaetere Portierungswellen. Die aktuelle Avalonia-App ist der Unterbau dafuer, noch nicht das Endprodukt.

## Grobe Groessenordnung

- `src/core`: 232 Dateien
- `src/sdk`: 831 Dateien
- `src/view`: 266 Dateien

Die reine Dateimenge zeigt bereits, dass der groesste technische Migrationsdruck nicht nur in der UI liegt, sondern vor allem im `sdk` mit Plugin-System, Tooling und Verträgen.

## Hauptbereiche

### `src/core`

Charakter:

- gemeinsame Konstanten, Interfaces und Utilities
- Windows-nahe Delphi-Units
- Drittbibliotheken und Basishilfen

Migrationswert:

- sehr hoch

Migrationsreihenfolge:

- zuerst

Bemerkung:

Dieser Bereich ist die beste Eintrittsstelle fuer die Migration, weil hier fachliche und technische Basisbausteine liegen, die spaeter von App und Plugins wiederverwendet werden.

### `src/sdk`

Charakter:

- Plugin-Basis
- Plugin-Implementierungen
- SDK-Hilfen
- Tooling
- Update- und OLE-nahe Helfer

Migrationswert:

- sehr hoch

Migrationsreihenfolge:

- direkt nach `core`

Bemerkung:

Dieser Bereich bestimmt, ob die neue Architektur spaeter wirklich erweiterbar bleibt. Hier sitzt das groesste strukturelle Risiko.

### `src/view`

Charakter:

- VCL-Desktop-Anwendung
- Forms, Frames, API-nahe UI-Controller
- modifizierte UI-Komponenten

Migrationswert:

- hoch

Migrationsreihenfolge:

- nach `core` und `sdk`

Bemerkung:

Die UI darf nicht zuerst portiert werden. Sonst wird nur alter Zustand neu verpackt, ohne tragfaehige .NET-Verträge darunter.

## UI-Bestand

- `src/view/forms`: 22 Dateien
- `src/view/frames`: 30 Dateien

Interpretation:

- die Anwendung hat mehrere eigenstaendige Dialog- und Screen-Bausteine
- ein Teil der VCL-Logik wird vermutlich in Formular-Events und Frames versteckt sein
- fuer Avalonia ist mit einer Kombination aus `Window`, `UserControl`, `ViewModel` und pragmatischem Code-Behind zu rechnen

## Plugin-Bestand

Top-Level-Kategorien unter `src/sdk/plugins`:

- `app`: 6 Verzeichnisse
- `captcha`: 2 Verzeichnisse
- `cms`: 26 Verzeichnisse
- `crawler`: 30 Verzeichnisse
- `crypter`: 10 Verzeichnisse
- `fileformats`: 5 Verzeichnisse
- `filehoster`: 52 Verzeichnisse
- `imagehoster`: 10 Verzeichnisse

Hinweis:

Die Zaehler enthalten auch Template-Verzeichnisse. Fuer die Portierungspriorisierung sind trotzdem vor allem die Relationen wichtig: `filehoster`, `crawler` und `cms` dominieren deutlich.

## Plugin-Kategorien im Detail

### `app`

Beispiele:

- `Cloudflare`
- `CustomScript`
- `DirWatch`
- `linkgrabber`
- `mirrorsort`

Einschaetzung:

- vermutlich Hilfs- oder Workflow-Plugins
- moeglicherweise gut geeignet fuer erste Referenzports, falls die Abhaengigkeiten klein sind

### `captcha`

Beispiele:

- `vBulletin RandomQuestion`

Einschaetzung:

- klein, aber potenziell eng an Speziallogik gekoppelt

### `cms`

Beispiele:

- `WordPress`
- `Joomla`
- `XenForo`
- `phpbb2`
- `phpbb3`
- `vBulletin`
- `MyBB`

Einschaetzung:

- fachlich wahrscheinlich zentral
- gute Kandidaten fuer spaetere, aber wichtige Referenzplugins
- hohes Risiko durch historisch gewachsene Edge-Cases

### `crawler`

Beispiele:

- `amazon.com`
- `amazon.de`
- `imdb.com`
- `youtube.com`
- `xrel.to`

Einschaetzung:

- stark HTML- und Parsing-lastig
- gute Kandidaten fuer die Validierung einer neuen Parser-/HTTP-Schicht

### `crypter`

Beispiele:

- `filecrypt.cc`
- `linkcrypt.ws`
- `ncrypt.in`

Einschaetzung:

- potenziell stark regex-, redirect- und decryptionslastig

### `fileformats`

Beispiele:

- `intelligen.xml.1`
- `intelligen.xml.2`
- `releasename.reader`

Einschaetzung:

- sehr wichtig fuer Legacy-Kompatibilitaet
- frueh analysieren, auch wenn die Menge klein ist

### `filehoster`

Beispiele:

- `rapidgator.net`
- `rapidshare.com`
- `uploaded.net`
- `zippyshare.com`
- `mediafire.com`
- `depositfiles.com`

Einschaetzung:

- groesste Plugin-Familie
- sehr wahrscheinlich historisch fragil
- nicht als erstes voll portieren

### `imagehoster`

Beispiele:

- `imgur.com`
- `directupload.net`
- `tinypic.com`
- `picload.org`

Einschaetzung:

- oft einfacher als CMS-Plugins
- gute Kandidaten fuer Referenz-Plugins, falls HTTP- und Upload-Contract klar ist

## Weitere auffaellige Bereiche

### Tooling unter `src/sdk/tools`

Sichtbar sind unter anderem:

- `PluginWizard`
- `UpdateManager`
- `IScriptConverter`
- `SimpleRegExTester`
- `OLE_Demo_C#`
- `OLE_SimpleDemo`
- `UpdateServer`

Einschaetzung:

- nicht alles davon muss mit in die erste .NET-Portierung
- `PluginWizard` und `UpdateManager` sind wahrscheinlich relevanter als Demos
- das vorhandene `OLE_Demo_C#` ist interessant, weil es zeigt, dass bereits .NET-Interop im Umfeld existierte

### Drittanbieter- und Legacy-Abhaengigkeiten

Aus README und Codebasis erkennbar:

- Indy
- Spring4D
- OmniThreadLibrary
- TRegExpr
- DEC
- DevExpress VCL
- FastScript
- TMS AdvMemo
- HtmlViewer
- Abbrevia
- EurekaLog

Einschaetzung:

- jede dieser Abhaengigkeiten braucht eine Ersatzstrategie
- der UI-Bereich ist besonders betroffen durch VCL-spezifische Komponenten
- `core` und `sdk` sind besonders betroffen durch HTTP, Regex, Threading, Kryptografie und Serialisierung

## Vorlaeufige Priorisierung

### Welle 1

- `src/core`
- `src/sdk/plugins` Vertrags- und Basisschicht
- `src/sdk/tools` nur soweit noetig fuer PluginHost und App-Betrieb

Ziel:

- technische Grundlage fuer eine neue .NET-Anwendung

### Welle 2

- ein kleiner Referenz-Workflow in der App
- 1 Referenzplugin mit moeglichst ueberschaubarer HTTP- und Datenlogik
- 1 Dateiformat- oder Importpfad

Ziel:

- vertikaler End-to-End-Beweis

Praezisierung:

- XML-Datei laden
- Daten im Editor sichtbar machen
- erste fehlende Felder nachziehen
- Publish-Ziel vorbereiten

### Welle 3

- ausgewaehlte `crawler`- und `imagehoster`-Plugins

Ziel:

- neue Parser-/Netzwerkschicht real pruefen

### Welle 4

- ausgewaehlte `cms`-Plugins

Ziel:

- produktnahe Publishing-Szenarien abdecken

### Welle 5

- breite `filehoster`-Migration
- restliche Spezialplugins

Ziel:

- Langstrecke und Restabdeckung

## Erste harte Annahmen

- Die Hauptkomplexitaet liegt nicht im UI-Rendering, sondern in Plugin-Vertraegen und historischer Web-/Parsing-Logik.
- Ein vollstaendiger Big-Bang-Port ist unnoetig riskant.
- Ohne fruehen PluginHost in .NET wird die Avalonia-App nur eine Huelle ohne belastbare Architektur.

## Plattformannahme

Neue Zielannahme:

- Entwicklung unter `Ubuntu 24` soll moeglich sein
- die neue App soll mindestens unter `Linux` und `Windows` baubar und startbar sein
- Windows-spezifische Legacy-Funktionalitaet muss im neuen Code explizit isoliert werden

Folge fuer die Migration:

- UI nicht gegen Windows-APIs entwickeln
- Shell-, Dialog-, Clipboard- und Dateisystemintegration abstrahieren
- Plugin- und Infrastrukturcode sauber von UI-spezifischem Code trennen
- Legacy-Dateiformate muessen frueh analysiert werden, auch wenn sie im Repo mengenmaessig klein wirken.

## Naechste Inventar-Schritte

- [ ] `src/core` in konkrete Submodule und Verantwortlichkeiten zerlegen
- [ ] `src/sdk` in Basis-SDK, Tooling und Plugin-Host-Anteile aufteilen
- [ ] alle Plugin-Kategorien mit je einem technischen Steckbrief versehen
- [ ] `src/view/forms` und `src/view/frames` mit Portierungsaufwand bewerten
- [ ] alle relevanten Laufzeitdateien unter `bin/` inventarisieren
- [ ] externe Abhaengigkeiten durch .NET-Alternativen ersetzen oder klassifizieren
- [ ] ersten Referenz-Workflow fuer einen vertikalen Port auswaehlen

## Naechster Produktschnitt

Der naechste wirklich produktnahe Schritt sollte nicht nur ein weiterer Inspector sein, sondern:

- XML-Datei als Arbeitsdatensatz laden
- Datensatz in einer neuen Editoransicht darstellen
- fehlende Felder markieren
- erste Crawler-Anbindung vorbereiten
- Publish-Zielspalte in neuer Form wieder aufbauen

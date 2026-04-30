# Plugin Development Notes

Diese Notizen sammeln die Punkte, die beim Pixelfox-Plugin entscheidend waren. Sie sollen helfen, neue Plugins fuer IntelligeN 2.129 ohne die gleichen Build- und Ladeprobleme zu erstellen.

## Zielumgebung

- IntelligeN ist urspruenglich ein Delphi-2010/VCL-Projekt.
- Fuer die originale 129er Anwendung ist Delphi 2010 Professional die sauberste Zielumgebung.
- RAD Studio 12 Community Edition kann fuer lokale Plugin-Builds ueber die IDE funktionieren, ist aber nicht die originale Laufzeitumgebung.
- Die Community Edition unterstuetzt kein echtes Command-Line-Compiling mit `dcc32`. MSBuild kann trotzdem "erfolgreich" melden, obwohl `dcc32` nur `This version of the product does not support command line compiling.` ausgibt und keine neue DLL erzeugt.

## Plugin-Kompatibilitaet: FileVersion ist Pflicht

IntelligeN prueft beim Laden eines Plugins nicht nur das Interface, sondern die Minor-Version der Datei.

Der Loader liest in `src\view\api\uApiPluginsBase.pas`:

```pascal
LPluginMinorVersion := TFileVersionInfo.GetVersionInfo(APluginFile).FileVersionNumber.Minor;
```

Diese Minor-Version muss zur laufenden IntelligeN-Version passen. Fuer IntelligeN 2.129 muss ein Plugin deshalb eine FileVersion mit Minor `129` haben, z.B.:

```text
2.129.2.0
```

Wenn die DLL z.B. `1.0.0.0` oder `0.1.0.0` hat, erscheint beim Laden:

```text
Plugin version (0) by definition incompatible for this version (129)
```

## Wichtige `.dproj`-Einstellungen

In neuen Plugins muessen die VersionInfos konsistent gesetzt sein. RAD Studio kann mehrere Stellen in der `.dproj` pflegen; wichtig ist, dass keine plattformspezifische Property die richtige Version wieder mit `1.0.0.0` ueberschreibt.

Fuer IntelligeN 2.129:

```xml
<VerInfo_IncludeVerInfo>true</VerInfo_IncludeVerInfo>
<VerInfo_MajorVer>2</VerInfo_MajorVer>
<VerInfo_MinorVer>129</VerInfo_MinorVer>
<VerInfo_Release>2</VerInfo_Release>
<VerInfo_Build>0</VerInfo_Build>
<VerInfo_Keys>CompanyName=;FileDescription=My plugin;FileVersion=2.129.2.0;InternalName=myplugin;LegalCopyright=;LegalTrademarks=;OriginalFilename=myplugin.dll;ProductName=IntelligeN;ProductVersion=2.5;Comments=</VerInfo_Keys>
```

Auch der `BorlandProject`-Block sollte dazu passen:

```xml
<VersionInfo Name="MajorVer">2</VersionInfo>
<VersionInfo Name="MinorVer">129</VersionInfo>
<VersionInfo Name="Release">2</VersionInfo>
<VersionInfo Name="Build">0</VersionInfo>
<VersionInfoKeys Name="FileVersion">2.129.2.0</VersionInfoKeys>
```

Wenn RAD Studio einen Win32-Block wie diesen erzeugt, auch dort die Werte setzen:

```xml
<PropertyGroup Condition="'$(Base_Win32)'!=''">
  <VerInfo_MajorVer>2</VerInfo_MajorVer>
  <VerInfo_MinorVer>129</VerInfo_MinorVer>
  <VerInfo_Release>2</VerInfo_Release>
  <VerInfo_Build>0</VerInfo_Build>
</PropertyGroup>
```

## Runtime-Packages vermeiden, wenn nur ein Plugin gebaut wird

Fuer Plugins, die in eine bestehende IntelligeN-129-Installation geladen werden sollen, ist diese Einstellung wichtig:

```xml
<UsePackages>false</UsePackages>
```

Der Pixelfox-Test zeigte: Eine RAD12-gebaute Plugin-DLL mit Runtime-Packages kann gegen falsche oder nicht passende `framework.bpl` / `frameworkX.bpl` laufen. Dann ist die Version zwar korrekt, aber Windows kann die DLL nicht laden:

```text
DLL file of <plugin>.dll plugin defect [Die angegebene Prozedur wurde nicht gefunden.]
```

Das ist meist ein Package-/BPL-Mix: Plugin, BPLs und Anwendung stammen nicht aus exakt derselben Delphi-/Package-Umgebung. Eine standalone gebaute Plugin-DLL vermeidet diese Abhaengigkeit.

Runtime-Packages sind nur dann sinnvoll, wenn Anwendung, Plugin und alle BPLs gemeinsam mit derselben Delphi-Version und denselben Package-Sourcen gebaut und zusammen ausgeliefert werden.

## Plugin-Aufbau

Ein Plugin ist eine Delphi Library, die eine Funktion mit Exportname `LoadPlugIn` bereitstellt.

Beispiel fuer ein Image-Hoster-Plugin:

```pascal
library myimagehoster;

{$R *.dres}

uses
  uPlugInInterface,
  uPlugInImageHosterClass,
  uMyImageHoster in 'uMyImageHoster.pas';

{$R *.res}

function LoadPlugin(var APlugIn: IImageHosterPlugIn): WordBool; safecall; export;
begin
  try
    APlugIn := TMyImageHoster.Create;
    Result := True;
  except
    Result := False;
  end;
end;

exports
  LoadPlugin name 'LoadPlugIn';

begin
end.
```

Die Plugin-Klasse erbt fuer Image-Hoster von `TImageHosterPlugIn`:

```pascal
type
  TMyImageHoster = class(TImageHosterPlugIn)
  public
    function GetName: WideString; override; safecall;
    function LocalUpload(const ALocalPath: WideString; out AUrl: WideString): WordBool; override; safecall;
    function RemoteUpload(const ARemoteUrl: WideString; out AUrl: WideString): WordBool; override; safecall;
  end;
```

Wichtig:

- Der Exportname muss exakt `LoadPlugIn` sein.
- Interfaces verwenden `safecall`, `WordBool` und `WideString`; diese Konvention beibehalten.
- Bei Fehlern `ErrorMsg` setzen und `False` zurueckgeben.
- Der Plugin-Typ kommt bei Image-Hostern aus `TImageHosterPlugIn.GetType`.

## Account-Daten und API-Keys

Image-Hoster-Plugins bekommen Account-Daten ueber:

```pascal
AccountName
AccountPassword
UseAccount
```

Beim Pixelfox-Plugin wird der API-Key im Feld `AccountName` gespeichert. Das Passwort kann leer bleiben. Der Request setzt daraus den Header:

```text
X-API-Key: <AccountName>
```

Fuer neue API-basierte Plugins ist dieses Muster praktisch:

- API-Key in `AccountName`.
- Optionales Secret/Passwort in `AccountPassword`, falls der Dienst es braucht.
- Wenn der Key fehlt, `ErrorMsg` mit einer klaren UI-Meldung setzen.

## Build-Workflow

In RAD Studio:

1. IntelligeN schliessen, damit die DLL nicht gelockt ist.
2. Plugin-Projekt neu laden, wenn die `.dproj` manuell geaendert wurde.
3. `Release | Win32` auswaehlen.
4. `Clean` ausfuehren.
5. `Build` ausfuehren.
6. Ergebnis in `bin\plugins\<plugin>.dll` pruefen.

Bei RAD Studio CE nicht auf den MSBuild-Exitcode verlassen. Immer Zeitstempel und FileVersion der DLL pruefen.

PowerShell-Check:

```powershell
Get-Item bin\plugins\pixelfoxcc.dll |
  Select-Object FullName,Length,LastWriteTime,
    @{Name='FileVersion';Expression={$_.VersionInfo.FileVersion}},
    @{Name='ProductVersion';Expression={$_.VersionInfo.ProductVersion}}
```

Erwartung fuer IntelligeN 2.129:

```text
FileVersion    2.129.2.0
ProductVersion 2.5
```

## Gruppenprojekt aktualisieren

Soll das Plugin im Hauptbuild mitlaufen, muss es im Gruppenprojekt eingetragen sein:

```text
src\IntelligeN-2k9.groupproj
```

Dort muss das Plugin in die Sammelziele fuer `Build`, `Clean` und `Make` aufgenommen werden. Beim Pixelfox-Plugin waren die Einzelziele vorhanden, aber die Sammelziele hatten es noch nicht in der Liste.

## Typische Fehler und Bedeutung

### `Plugin version (0) by definition incompatible for this version (129)`

Die DLL hat eine falsche oder fehlende FileVersion. Meist steht sie noch auf `1.0.0.0` oder `0.1.0.0`.

Fix:

- `.dproj` VersionInfo auf `2.129.2.0` setzen.
- Auch `Base_Win32` und `BorlandProject/VersionInfoKeys` pruefen.
- DLL neu bauen.
- Danach FileVersion der echten `bin\plugins\<plugin>.dll` pruefen.

### `DLL file ... plugin defect [Die angegebene Prozedur wurde nicht gefunden.]`

Windows konnte die DLL oder eine ihrer Abhaengigkeiten nicht korrekt laden.

Haeufige Ursachen:

- Export `LoadPlugIn` fehlt oder ist falsch geschrieben.
- Die DLL haengt an nicht passenden Runtime-Packages.
- `framework.bpl` / `frameworkX.bpl` passen nicht zur Delphi-Version des Plugins.

Fix:

- Export in der `.dpr` pruefen.
- Fuer standalone Plugin-Builds `UsePackages=false` setzen.
- DLL sauber neu bauen.

### MSBuild meldet Erfolg, aber DLL wurde nicht aktualisiert

Bei RAD Studio Community Edition kann `dcc32` ueber die Commandline blocken:

```text
This version of the product does not support command line compiling.
```

Fix:

- Ueber die RAD-Studio-IDE bauen.
- Danach Timestamp, Groesse und FileVersion der DLL pruefen.

## RAD12-Kompatibilitaetsnotizen aus Pixelfox

Fuer den Pixelfox-Test waren zusaetzliche Anpassungen noetig, damit die alten Third-Party-Sourcen unter RAD12 ueberhaupt kompilieren:

- `BESEN.inc`: fuer neue Compiler-Versionen die Delphi-XE-und-hoeher Defines setzen.
- `OtlContainers.pas`: `GetThreadId` eindeutig als `OtlSync.GetThreadId` referenzieren.
- `OtlTaskControl.pas`: Typkonvertierungen fuer `AcquireExceptionObject` und anonyme Callback-Variablen anpassen.
- `frameworkX.dpk`: fuer diesen RAD12-Plugin-Build nicht benoetigte/problematische alte Units wie `OtlParallel` und alte Spring4D-Units aus dem Package-Build nehmen.
- `uPixelfoxCc.pas`: `TOleStream` unter neuen Compilern aus `Vcl.AxCtrls` einbinden; `IStream.Seek` mit passendem `LargeUInt`-Parameter verwenden.

Diese Punkte sind kein kompletter Port von IntelligeN auf RAD12. Sie waren nur der pragmatische Weg, das neue Plugin lokal zu bauen und in der 129er Anwendung zu testen.

## Kurze Checkliste fuer neue Plugins

- Plugin-Ordner unter passendem Typ anlegen, z.B. `src\sdk\plugins\imagehoster\<domain>`.
- `.dpr` mit Export `LoadPlugIn` erstellen.
- Plugin-Klasse von passender Basisklasse ableiten.
- Nur `safecall`-kompatible Interface-Typen verwenden.
- Fehler ueber `ErrorMsg` melden und `False` zurueckgeben.
- `.dproj` auf `Release | Win32`, `FileVersion 2.129.2.0` und `UsePackages=false` pruefen.
- Plugin in `src\IntelligeN-2k9.groupproj` aufnehmen, wenn es im Gruppenbuild laufen soll.
- IntelligeN schliessen, Plugin sauber neu bauen.
- `bin\plugins\<plugin>.dll` auf Timestamp, Groesse und FileVersion pruefen.
- Plugin in IntelligeN ueber Settings -> Plugins -> passenden Typ hinzufuegen und laden testen.

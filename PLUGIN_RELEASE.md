# Plugin Build And Share

Diese Datei beschreibt, wie du aus dem neuen Pixelfox-Plugin eine weitergebbare Datei erzeugst.

## 1. Voraussetzungen
- Windows-VM oder echter Windows-Rechner
- Delphi 2010 und die Projektabhaengigkeiten
- siehe [WINDOWS7_VM_SETUP.md](/home/dev/Workspace/Github/IntelligeN/WINDOWS7_VM_SETUP.md)

## 2. Nur das Pixelfox-Plugin bauen
Am direktesten baust du nur das Plugin-Projekt:

```bat
msbuild src\sdk\plugins\imagehoster\pixelfox.cc\pixelfoxcc.dproj /t:Build /p:Config=Release
```

Alternativ ueber das Gruppenprojekt:

```bat
msbuild src\IntelligeN-2k9.groupproj /t:pixelfoxcc /p:Config=Release
```

## 3. Ergebnisdatei
Nach erfolgreichem Build liegt das Plugin hier:

```text
bin\plugins\pixelfoxcc.dll
```

Das ist die eigentliche Datei, die du weitergeben kannst.

## 4. Was du weitergeben musst
Fuer **dieses** Plugin reicht normalerweise:

```text
pixelfoxcc.dll
```

Es gibt aktuell keine zusaetzliche `.ini`-Datei oder weitere Begleitdatei speziell fuer Pixelfox.

## 5. Beim Empfaenger
Die DLL muss in den Plugin-Ordner der IntelligeN-Installation:

```text
bin\plugins\
```

Danach IntelligeN starten und das Plugin als Imagehoster konfigurieren. Der Pixelfox API-Key gehoert in das Account-Name-Feld des Plugins.

## 6. Wichtiger Hinweis
Wenn du spaeter andere Plugins weitergibst, reicht nicht immer nur die DLL. Manche Plugins brauchen zusaetzlich `.ini`-Dateien oder andere Begleitdateien im gleichen Ordner. Beim Pixelfox-Plugin ist das nach aktuellem Stand nicht noetig.

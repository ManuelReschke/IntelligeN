# Windows 7 VM Setup fuer IntelligeN

Diese Datei beschreibt die pragmatische Reihenfolge fuer eine frische Windows-7-VM, um das Projekt spaeter bauen zu koennen.

## 1. Basis im Gast-System
- Windows 7 SP1 x64 installieren
- alle verfuegbaren Updates einspielen
- Visual C++ Laufzeiten nur bei Bedarf nachziehen
- Git installieren
- optional: `make`/MSYS2 oder Git Bash, falls du die `Makefile`-Targets nutzen willst

## 2. Delphi selbst
- Embarcadero Delphi 2010 Professional installieren
- danach das letzte verfuegbare Update/Hotfix fuer Delphi 2010 einspielen
- pruefen, ob `msbuild` und `dcc32` funktionieren

## 3. Projektinterne Packages bauen
Diese Packages liegen im Repo und muessen zur Toolchain passen:
- `src/core/framework/framework.dproj`
- `src/sdk/frameworkX/frameworkX.dproj`
- `src/view/frameworkUI/frameworkUI.dproj`

Erst danach das Hauptprojekt:
- `src/view/IntelligeN.dproj`
- oder komplett: `src/IntelligeN-2k9.groupproj`

## 4. Externe Pflicht-Komponenten
Laut `README.md` und den `.dproj`-Dateien braucht das VIEW-Projekt mindestens:
- DevExpress VCL fuer Delphi 2010
- FastScript fuer Delphi 2010
- TMS AdvMemo fuer Delphi 2010
- EurekaLog 7 fuer Delphi 2010

Wahrscheinlich ebenfalls relevant:
- Indy 10 fuer Delphi 2010 (normalerweise mit Delphi gebuendelt)

Ohne diese Pakete wird das Hauptprojekt sehr wahrscheinlich nicht bauen.

## 5. Empfohlene Installationsreihenfolge
1. Windows 7 SP1
2. Delphi 2010
3. Delphi-Updates/Hotfixes
4. DevExpress VCL
5. FastScript
6. TMS AdvMemo
7. EurekaLog
8. Repository auschecken
9. zuerst `framework`, `frameworkX`, `frameworkUI` bauen
10. danach `make build` oder `msbuild src\IntelligeN-2k9.groupproj /t:Build /p:Config=Release`

## 6. Wichtige Unsicherheit
Ich kann aus dem Repo nicht garantieren, welche exakten alten Versionsstaende der kommerziellen Komponenten noetig sind. Die Paketnamen in den Projektdateien deuten auf Delphi 2010 (`RS14`) hin, aber bei einem ersten Build koennen noch fehlende BPL/DCU-Pfade auftauchen. Dann muss die VM gezielt nachjustiert werden.

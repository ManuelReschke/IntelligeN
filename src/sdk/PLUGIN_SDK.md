# Plugin SDK

This repository still contains the legacy IntelligeN desktop application, but you do not need the full UI stack to build regular runtime plugins.

## Supported standalone plugin types

- `cms`
- `crawler`
- `crypter`
- `filehoster`
- `imagehoster`
- `captcha`

`app` and `fileformats` plugins are still tied more closely to the application runtime and are intentionally not part of the slim SDK workflow.

## 1. Build the SDK runtime

Build the SDK-only group project first:

```bat
msbuild src\IntelligeN-plugin-sdk.groupproj /t:Build /p:Config=Release
```

This produces the package/runtime files required by plugin DLLs:

- `bin\framework.bpl`
- `bin\frameworkX.bpl`
- `bin\IntelligeN.dll`
- `bin\IXML.dll`

## 2. Create a new plugin scaffold

Use one of the wrapper scripts inside `src\sdk\plugins\<type>\createProject.bat`, for example:

```bat
src\sdk\plugins\cms\createProject.bat
src\sdk\plugins\filehoster\createProject.bat -FullName MyHoster -FolderName myhoster.com
```

The wrappers call the new script-based scaffolder:

```bat
powershell -ExecutionPolicy Bypass -File src\sdk\tools\CreatePlugin.ps1 -PluginType cms -FullName MyCMS
```

Common parameters:

- `-PluginType cms|crawler|crypter|filehoster|imagehoster|captcha`
- `-FullName MyPlugin`
- `-BasicName myplugin`
- `-FolderName myplugin.com`
- `-CompanyName "Your Name"`
- `-Website "https://example.com"`
- `-MajorVer 1 -MinorVer 0 -Release 0 -Build 0`

`FullName` is used for Delphi identifiers and should stay PascalCase without spaces.

## 3. Build the plugin

Build the generated project file:

```bat
msbuild src\sdk\plugins\cms\MyCMS\mycms.dproj /t:Build /p:Config=Release
```

The resulting DLL is written to:

```text
bin\plugins\<basicname>.dll
```

## 4. Publish the plugin

For publication you normally need:

- the generated plugin DLL from `bin\plugins`
- sidecar files stored next to the plugin sources, for example `.xml`, `.json`, `.ini` or other runtime data files

Do not ship Delphi source or project files unless you explicitly want to publish source code.

## Optional: host application for debugging

The new templates no longer hardcode `bin_portable\IntelligeN.exe` as the debug host application.

If you still want to debug against a local host executable, set:

```bat
set INTELLIGEN_HOST_APP=C:\Path\To\IntelligeN.exe
```

before opening or building the project.

## Current constraints

- The SDK is still Win32-only.
- Plugin DLLs still use Delphi runtime packages `framework` and `frameworkX`.
- The plugin ABI remains the existing IntelligeN 2.5 interface set.
- The main VCL application and commercial UI dependencies are not required for the standalone plugin workflow.

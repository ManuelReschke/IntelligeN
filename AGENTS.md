# Repository Guidelines

## Project Structure & Module Organization
`src/` is the main codebase and is split into `core/`, `sdk/`, and `view/` as described in [README.md](/home/dev/Workspace/Github/IntelligeN/README.md). `src/core/` holds shared constants, interfaces, utilities, and bundled third-party Delphi units. `src/sdk/` contains plugin bases, DLL/tool support, and plugin implementations under folders such as `plugins/cms`, `plugins/crawler`, and `plugins/filehoster`. `src/view/` contains the VCL application, with forms in `forms/`, reusable UI in `frames/`, and higher-level controller code in `api/`. Runtime templates and configuration ship from `bin/`; UI images and icons live in `res/`.

## Build, Test, and Development Commands
Use a Windows machine with Embarcadero Delphi 2010 Professional or a compatible MSBuild environment.

- `msbuild src\IntelligeN-2k9.groupproj /t:Build /p:Config=Release`
  Builds the desktop app plus the plugin projects included in the main group.
- `msbuild src\IntelligeN-sdk.groupproj /t:Build`
  Builds SDK-related projects and tools only.
- `make_portable.bat`
  Refreshes the `bin_portable` bundle from the checked-in `bin\` runtime files.
- `src\sdk\plugins\<type>\createProject.bat`
  Starts the Plugin Wizard for a new plugin based on the matching template.

## Coding Style & Naming Conventions
Follow the existing Delphi style: two-space indentation, one unit per file, and grouped `uses` clauses with short section comments when helpful. Unit names use a prefixed PascalCase pattern such as `uMain.pas`, `ufPublish.pas`, and `uWordPress.pas`; forms and frames keep matching `.pas` and `.dfm` filenames. Plugin folders are named after the target site or service, usually by domain (`WordPress`, `rapidgator.net`). There is no repository-wide formatter, so keep edits small and consistent with surrounding code.

## Testing Guidelines
There is no automated DUnit/DUnitX suite in this repository. For changes, build the affected `.dproj` or group project, then smoke-test the relevant UI flow or plugin behavior in the application. Include manual verification notes in your PR, especially for login flows, publishing, crawling, and plugin loading from `bin/plugins`.

## Commit & Pull Request Guidelines
Keep commit messages short and imperative. Recent history favors direct summaries such as `Fixed possible errors for mirrors without directlinks.` and optional markers like `[+]` or `[*]` for plugin additions or fixes. PRs should state the user-visible change, list affected modules or plugins, describe manual test coverage, and include screenshots when a `src/view/forms` or `src/view/frames` change affects the UI.

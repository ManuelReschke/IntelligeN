# Building IntelligeN

## Short answer
Not realistically with normal Docker on Linux.

This repository is an old Delphi 2010 / VCL Windows application. The main GUI project also references commercial component suites such as DevExpress, TMS, FastScript, and EurekaLog. That means the build depends on a Windows-only compiler plus locally installed design/runtime packages.

## Recommended setup
Use one of these:

1. A dedicated Windows VM with Delphi 2010 and the required component packages installed.
2. A Windows workstation with the same toolchain.

After that, use the repository root as the entrypoint:

```bash
make doctor
make build
```

Additional targets:

```bash
make view
make sdk
make portable
```

## Why not Docker?
- Linux containers cannot natively run the Delphi 2010 compiler toolchain.
- Windows containers do not solve Delphi licensing or package installation cleanly.
- The project is not self-contained: required third-party libraries are referenced from the Delphi environment, not vendored here as a reproducible toolchain.

## Realistic next step
If you want a reproducible build, the practical route is:

1. Create a Windows VM image with Delphi 2010 and all required packages.
2. Verify `msbuild src\IntelligeN-2k9.groupproj` works there.
3. Keep `make build` as the stable wrapper command for developers and CI on Windows runners.

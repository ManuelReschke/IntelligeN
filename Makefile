SHELL := bash

MSBUILD ?= msbuild
CONFIG ?= Release

APP_GROUP := src/IntelligeN-2k9.groupproj
SDK_GROUP := src/IntelligeN-sdk.groupproj
VIEW_PROJECT := src/view/IntelligeN.dproj

.PHONY: help doctor build sdk view portable docker-build

help:
	@printf "%s\n" \
		"Targets:" \
		"  make doctor       Check whether the local machine can build this repo" \
		"  make build        Build the main app group ($(APP_GROUP))" \
		"  make sdk          Build SDK/tools group ($(SDK_GROUP))" \
		"  make view         Build only the VCL app ($(VIEW_PROJECT))" \
		"  make portable     Build the Portable-D config of the VCL app" \
		"  make docker-build Explain why Docker is not supported here"

doctor:
ifeq ($(OS),Windows_NT)
	@command -v "$(MSBUILD)" >/dev/null 2>&1 || { \
		echo "msbuild not found in PATH."; \
		echo "Install Delphi/MSBuild on Windows, then run 'make build' again."; \
		exit 1; \
	}
	@echo "Windows build environment detected."
	@echo "msbuild: $(MSBUILD)"
	@echo "Next step: make build"
else
	@echo "Unsupported host for native builds: $$(uname -s)"
	@echo "This repository targets Delphi 2010/VCL on Windows and uses commercial components."
	@echo "Use a Windows machine or Windows VM with Delphi installed, then run 'make build'."
	@exit 1
endif

build:
ifeq ($(OS),Windows_NT)
	@"$(MSBUILD)" "$(APP_GROUP)" /t:Build /p:Config="$(CONFIG)"
else
	@$(MAKE) --no-print-directory docker-build
endif

sdk:
ifeq ($(OS),Windows_NT)
	@"$(MSBUILD)" "$(SDK_GROUP)" /t:Build /p:Config="$(CONFIG)"
else
	@$(MAKE) --no-print-directory docker-build
endif

view:
ifeq ($(OS),Windows_NT)
	@"$(MSBUILD)" "$(VIEW_PROJECT)" /t:Build /p:Config="$(CONFIG)"
else
	@$(MAKE) --no-print-directory docker-build
endif

portable:
ifeq ($(OS),Windows_NT)
	@"$(MSBUILD)" "$(VIEW_PROJECT)" /t:Build /p:Config="Portable-D"
else
	@$(MAKE) --no-print-directory docker-build
endif

docker-build:
	@echo "No supported Docker build is provided for this repository."
	@echo "Reasons:"
	@echo "  1. Delphi 2010 is a Windows-only proprietary toolchain."
	@echo "  2. The VCL app depends on commercial UI/runtime packages (DevExpress, TMS, FastScript, EurekaLog)."
	@echo "  3. Shipping those dependencies inside a container is fragile and license-bound."
	@echo "Pragmatic option: build on Windows and keep 'make build' as the stable entrypoint."
	@exit 1

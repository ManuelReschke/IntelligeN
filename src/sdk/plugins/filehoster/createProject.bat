@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\tools\CreatePlugin.ps1" -PluginType filehoster %*

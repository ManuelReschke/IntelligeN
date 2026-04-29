[CmdletBinding()]
param(
  [string]$PluginType,
  [string]$FullName,
  [string]$BasicName,
  [string]$FolderName,
  [string]$Website = '',
  [string]$CompanyName,
  [string]$Copyright,
  [int]$MajorVer = 1,
  [int]$MinorVer = 0,
  [int]$Release = 0,
  [int]$Build = 0,
  [switch]$Force,
  [switch]$ListTypes
)

$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot '..\..\..')).Path
$PluginsRoot = Join-Path $RepoRoot 'src\sdk\plugins'
$SupportedTypes = @('app', 'captcha', 'cms', 'crawler', 'crypter', 'fileformats', 'filehoster', 'imagehoster')
$TextExtensions = @('.pas', '.dpr', '.dproj', '.txt', '.md', '.xml', '.json', '.ini', '.cfg', '.bat')
$TemplateExtensions = @('.pas', '.dpr', '.dproj')

function Read-RequiredValue([string]$Prompt) {
  do {
    $Value = Read-Host $Prompt
  } until (-not [string]::IsNullOrWhiteSpace($Value))

  return $Value.Trim()
}

function Replace-Tokens([string]$Value, [System.Collections.Specialized.OrderedDictionary]$Tokens) {
  $Result = $Value

  foreach ($Key in $Tokens.Keys) {
    $Result = $Result.Replace($Key, [string]$Tokens[$Key])
  }

  return $Result
}

function Get-TemplateInfo([string]$Type) {
  $TemplatePath = Join-Path $PluginsRoot "$Type\!template"
  $Exists = Test-Path $TemplatePath
  $AllFiles = @()
  $TemplateFiles = @()

  if ($Exists) {
    $AllFiles = @(Get-ChildItem -Path $TemplatePath -File)
    $TemplateFiles = @($AllFiles | Where-Object { $TemplateExtensions -contains $_.Extension.ToLowerInvariant() })
  }

  return [PSCustomObject]@{
    Type = $Type
    TemplatePath = $TemplatePath
    Exists = $Exists
    FileCount = $AllFiles.Count
    HasScaffold = $TemplateFiles.Count -gt 0
  }
}

if ($ListTypes) {
  Write-Host 'Available plugin types:'

  foreach ($Type in $SupportedTypes) {
    $TemplateInfo = Get-TemplateInfo $Type
    $Status = if ($TemplateInfo.HasScaffold) { 'ready' } else { 'missing scaffold template' }
    Write-Host ("  {0,-12} {1}" -f $Type, $Status)
  }

  exit 0
}

if ([string]::IsNullOrWhiteSpace($PluginType)) {
  $PluginType = Read-RequiredValue 'Plugin type'
}

$PluginType = $PluginType.Trim().ToLowerInvariant()

if ($SupportedTypes -notcontains $PluginType) {
  throw "Unsupported plugin type '$PluginType'. Use -ListTypes to see the available values."
}

$TemplateInfo = Get-TemplateInfo $PluginType

if (-not $TemplateInfo.HasScaffold) {
  throw "Plugin type '$PluginType' does not have a complete scaffold template yet."
}

if ([string]::IsNullOrWhiteSpace($FullName)) {
  $FullName = Read-RequiredValue 'Plugin name (PascalCase, no spaces)'
}

$FullName = $FullName.Trim()

if ($FullName -match '\s') {
  throw 'FullName must not contain spaces because it is used for Delphi identifiers.'
}

if ([string]::IsNullOrWhiteSpace($BasicName)) {
  $BasicName = ($FullName -replace '[^A-Za-z0-9]', '').ToLowerInvariant()
}

if ([string]::IsNullOrWhiteSpace($BasicName)) {
  throw 'BasicName resolved to an empty value.'
}

if ([string]::IsNullOrWhiteSpace($FolderName)) {
  $FolderName = $FullName
}

if ([string]::IsNullOrWhiteSpace($CompanyName)) {
  $CompanyName = if (-not [string]::IsNullOrWhiteSpace($env:USERNAME)) { $env:USERNAME } else { 'Unknown Author' }
}

if ([string]::IsNullOrWhiteSpace($Copyright)) {
  $Copyright = "Copyright (c) $(Get-Date -Format yyyy) $CompanyName"
}

$TargetPath = Join-Path (Join-Path $PluginsRoot $PluginType) $FolderName

if ((Test-Path $TargetPath) -and -not $Force) {
  throw "Target path already exists: $TargetPath"
}

New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null

$Tokens = New-Object System.Collections.Specialized.OrderedDictionary
$Tokens.Add('%ProjectGuid%', ([Guid]::NewGuid().ToString('B').ToUpperInvariant()))
$Tokens.Add('%BasicName%', $BasicName)
$Tokens.Add('%FullName%', $FullName)
$Tokens.Add('%Website%', $Website)
$Tokens.Add('%CompanyName%', $CompanyName)
$Tokens.Add('%Copyright%', $Copyright)
$Tokens.Add('%MajorVer%', $MajorVer)
$Tokens.Add('%MinorVer%', $MinorVer)
$Tokens.Add('%Release%', $Release)
$Tokens.Add('%Build%', $Build)

foreach ($File in @(Get-ChildItem -Path $TemplateInfo.TemplatePath -File | Sort-Object Name)) {
  $TargetName = Replace-Tokens $File.Name $Tokens
  $TargetFile = Join-Path $TargetPath $TargetName

  if ($TextExtensions -contains $File.Extension.ToLowerInvariant()) {
    $Content = [System.IO.File]::ReadAllText($File.FullName)
    $Content = Replace-Tokens $Content $Tokens
    $Encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($TargetFile, $Content, $Encoding)
  }
  else {
    Copy-Item -Path $File.FullName -Destination $TargetFile -Force
  }
}

Write-Host ''
Write-Host 'Plugin scaffold created:'
Write-Host "  Type      : $PluginType"
Write-Host "  FullName  : $FullName"
Write-Host "  BasicName : $BasicName"
Write-Host "  Folder    : $TargetPath"
Write-Host ''
Write-Host 'Next step: build the generated .dproj with msbuild.'

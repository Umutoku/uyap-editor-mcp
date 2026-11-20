[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$EditorDirectory,
    [string]$JavaBin,
    [ValidateSet('Codex','ClaudeCode','ClaudeDesktop','Gemini','Antigravity','OpenCode')]
    [string]$Client = 'Codex',
    [ValidateSet('1','2')]
    [string]$OpenCodeVersion = '2',
    [switch]$Register,
    [switch]$Uninstall,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'UyapEditorMcp'),
    [string]$DataRoot = (Join-Path $env:LOCALAPPDATA 'UyapEditorMcpData')
)

$ErrorActionPreference = 'Stop'
$product = 'UyapEditorMcp'
$markerName = 'uyap-mcp-install.json'
$packages = Join-Path ([IO.Path]::GetFullPath($InstallRoot)) 'packages'
$versionPattern = '^\d+\.\d+\.\d+(?:-[A-Za-z0-9][A-Za-z0-9.-]*)?$'

function Get-ManagedInstall($Directory) {
    if ($Directory.Name -notmatch $versionPattern -or
        ($Directory.Attributes -band [IO.FileAttributes]::ReparsePoint)) { return }

    $marker = Join-Path $Directory.FullName $markerName
    if (!(Test-Path -LiteralPath $marker -PathType Leaf)) { return }

    $note = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
    if ($note.product -eq $product -and $note.version -eq $Directory.Name) { $note }
}

function Test-InstalledApp([string]$Source, [string]$Installed) {
    if (!(Test-Path -LiteralPath $Installed -PathType Container)) { return $false }
    $targetRoot = Get-Item -LiteralPath $Installed -Force
    if ($targetRoot.Attributes -band [IO.FileAttributes]::ReparsePoint) { return $false }
    $installedFiles = Get-ChildItem -LiteralPath $Installed -Recurse -Force
    if ($installedFiles | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { return $false }

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse -Force) {
        $relative = $file.FullName.Substring($Source.Length + 1)
        $target = Join-Path $Installed $relative
        if (!(Test-Path -LiteralPath $target -PathType Leaf) -or
            (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
            return $false
        }
    }
    return $true
}

if ($Uninstall) {
    if (!(Test-Path -LiteralPath $packages -PathType Container)) { return }

    $packageFolder = Get-Item -LiteralPath $packages
    if ($packageFolder.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'The installation directory must be a local folder.'
    }

    foreach ($directory in Get-ChildItem -LiteralPath $packageFolder.FullName -Directory) {
        if (!(Get-ManagedInstall $directory)) { continue }
        $target = (Resolve-Path -LiteralPath $directory.FullName).Path
        if (![string]::Equals([IO.Path]::GetDirectoryName($target), $packageFolder.FullName,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The installed version is outside the managed directory.'
        }

        $redirected = Get-ChildItem -LiteralPath $target -Recurse -Force |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
            Select-Object -First 1
        if ($redirected) { throw 'The installed version contains a redirected path.' }

        if ($PSCmdlet.ShouldProcess($target, 'Remove the installed MCP version')) {
            Remove-Item -LiteralPath $target -Recurse -Force
        }
    }
    return
}

if ($Register -and $Client -ne 'Codex') {
    throw 'Automatic registration is available for Codex. Setup prints JSON for the other clients.'
}

function Select-Folder([string]$Title) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object Windows.Forms.FolderBrowserDialog
    try {
        $dialog.Description = $Title
        $dialog.ShowNewFolderButton = $false
        if ($dialog.ShowDialog() -ne [Windows.Forms.DialogResult]::OK) {
            throw 'Folder selection cancelled.'
        }
        $dialog.SelectedPath
    } finally {
        $dialog.Dispose()
    }
}

if (!$EditorDirectory) { $EditorDirectory = Select-Folder 'Select the UYAP 5.4.20 folder containing editor_lib.jar' }
if (!$JavaBin) { $JavaBin = Select-Folder 'Select the Java 8 x64 bin folder containing java.exe and WindowsAccessBridge-64.dll' }
$EditorDirectory = (Resolve-Path -LiteralPath $EditorDirectory).Path
$JavaBin = (Resolve-Path -LiteralPath $JavaBin).Path
if (!(Test-Path -LiteralPath (Join-Path $EditorDirectory 'editor_lib.jar') -PathType Leaf)) {
    throw 'Choose the UYAP editor folder containing editor_lib.jar.'
}
foreach ($name in 'java.exe','WindowsAccessBridge-64.dll') {
    if (!(Test-Path -LiteralPath (Join-Path $JavaBin $name) -PathType Leaf)) {
        throw "The selected Java folder does not contain $name."
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$version = $manifest.version
if ($version -notmatch $versionPattern) { throw 'The package version is invalid.' }
$sourceApp = Join-Path $PSScriptRoot 'app'
$sourceExe = Join-Path $sourceApp 'UyapEditorMcp.exe'
$sourceDll = Join-Path $sourceApp 'UyapEditorMcp.dll'
foreach ($file in $sourceExe,$sourceDll,(Join-Path $sourceApp 'coreclr.dll')) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw 'Extract the complete Windows package first.' }
}

$appHash = (Get-FileHash -LiteralPath $sourceDll).Hash
$versionFolder = Join-Path $packages $version
if (Test-Path -LiteralPath $versionFolder) {
    $existing = Get-ManagedInstall (Get-Item -LiteralPath $versionFolder)
    $existingApp = Join-Path $versionFolder 'app'
    if (!$existing -or $existing.sha256 -ne $appHash -or
        !(Test-InstalledApp $sourceApp $existingApp)) {
        throw 'The installed version is different or incomplete. Uninstall it before reinstalling.'
    }
} elseif ($PSCmdlet.ShouldProcess($versionFolder, 'Install the local MCP server')) {
    $null = New-Item -Path $packages -ItemType Directory -Force
    $staging = Join-Path $packages ('.setup-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -Path $staging -ItemType Directory
    Copy-Item -LiteralPath $sourceApp -Destination $staging -Recurse
    @{ product = $product; version = $version; sha256 = $appHash } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $staging $markerName) -Encoding utf8
    Move-Item -LiteralPath $staging -Destination $versionFolder
}
if ($WhatIfPreference -or !(Test-Path -LiteralPath (Join-Path $versionFolder 'app/UyapEditorMcp.exe'))) { return }

$workspace = [IO.Path]::GetFullPath($DataRoot)
$inputDirectory = Join-Path $workspace 'in'
$outputDirectory = Join-Path $workspace 'out'
$stateDirectory = Join-Path $workspace 'state'
$null = New-Item -Path $inputDirectory,$outputDirectory,$stateDirectory -ItemType Directory -Force
$fixture = 'sentetik-3-sayfa.udf'
if (!(Test-Path -LiteralPath (Join-Path $inputDirectory $fixture))) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "fixtures/$fixture") -Destination $inputDirectory
}

$executable = Join-Path $versionFolder 'app/UyapEditorMcp.exe'
$serverArgs = @(
    '--editor-dir', $EditorDirectory, '--java-bin', $JavaBin,
    '--input-root', $inputDirectory, '--output-root', $outputDirectory, '--state-dir', $stateDirectory
)

if ($Register) {
    $codex = Get-Command codex -ErrorAction Stop
    $null = & $codex mcp get uyap_editor --json 2>$null
    if ($LASTEXITCODE -eq 0) {
        throw 'Codex already has a uyap_editor entry. Update or remove that entry before registering.'
    }
    & $codex mcp add uyap_editor -- $executable @serverArgs
    if ($LASTEXITCODE) { throw 'Codex did not register the server.' }
    return
}

if ($Client -eq 'OpenCode') {
    $connection = @{ type = 'local'; command = @($executable) + $serverArgs }
    $servers = @{ uyap_editor = $connection }
    $mcp = if ($OpenCodeVersion -eq '1') { $servers } else { @{ servers = $servers } }
    @{ '$schema' = 'https://opencode.ai/config.json'; mcp = $mcp } | ConvertTo-Json -Depth 8
    return
}

@{ mcpServers = @{ uyap_editor = @{ command = $executable; args = $serverArgs } } } | ConvertTo-Json -Depth 8

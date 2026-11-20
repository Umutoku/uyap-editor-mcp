[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$sourceApp = Join-Path $PSScriptRoot 'app'
if (!(Test-Path (Join-Path $sourceApp 'UyapEditorMcp.exe')) -or
    !(Test-Path (Join-Path $sourceApp 'java/classes/EditorLauncher.class'))) {
    throw 'Build the Windows app with Build.ps1 before packaging.'
}

$dist = New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'dist') -Force
$bundle = New-Item -ItemType Directory -Path (Join-Path $dist.FullName ('bundle-' + [guid]::NewGuid().ToString('N')))
$bundleApp = New-Item -ItemType Directory -Path (Join-Path $bundle.FullName 'app')
Get-ChildItem -LiteralPath $sourceApp -File | Where-Object Extension -in '.exe', '.dll', '.json' |
    Copy-Item -Destination $bundleApp.FullName
foreach ($folder in @('java', 'runtimes')) {
    $dependency = Join-Path $sourceApp $folder
    if (Test-Path -LiteralPath $dependency -PathType Container) {
        Copy-Item -LiteralPath $dependency -Destination $bundleApp.FullName -Recurse
    }
}
foreach ($item in @('manifest.json', 'README.md', 'PRIVACY.md', 'docs', 'fixtures', 'third-party')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $item) -Destination $bundle.FullName -Recurse
}
if (Get-ChildItem -LiteralPath $bundle.FullName -Recurse -File |
    Where-Object Extension -in '.jar', '.pfx', '.p12', '.key', '.pdb', '.log') {
    throw 'Unexpected file in the release tree.'
}

$stem = "uyap-editor-mcp-$($manifest.version)-windows-x64"
$zip = Join-Path $dist.FullName "$stem.zip"
$mcpb = Join-Path $dist.FullName "$stem.mcpb"
Compress-Archive -Path (Join-Path $bundle.FullName '*') -DestinationPath $zip -Force
Copy-Item -LiteralPath $zip -Destination $mcpb -Force
Get-FileHash -Algorithm SHA256 -LiteralPath $zip,$mcpb | Select-Object Hash,Path

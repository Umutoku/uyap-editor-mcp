[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$sourceApp = Join-Path $PSScriptRoot 'app'
if (!(Test-Path (Join-Path $sourceApp 'UyapEditorMcp.exe')) -or
    !(Test-Path (Join-Path $sourceApp 'java/classes/EditorLauncher.class')) -or
    !(Test-Path (Join-Path $sourceApp 'coreclr.dll')) -or
    !(Test-Path (Join-Path $sourceApp 'hostfxr.dll'))) {
    throw 'Build the self-contained Windows app with Build.ps1 before packaging.'
}

$dist = New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'dist') -Force
$bundle = New-Item -ItemType Directory -Path (Join-Path $dist.FullName ('bundle-' + [guid]::NewGuid().ToString('N')))
$bundleApp = New-Item -ItemType Directory -Path (Join-Path $bundle.FullName 'app')
Get-ChildItem -LiteralPath $sourceApp -File | Where-Object Extension -in '.exe', '.dll', '.json', '.txt' |
    Copy-Item -Destination $bundleApp.FullName
foreach ($folder in @('java', 'runtimes')) {
    $dependency = Join-Path $sourceApp $folder
    if (Test-Path -LiteralPath $dependency -PathType Container) {
        Copy-Item -LiteralPath $dependency -Destination $bundleApp.FullName -Recurse
    }
}
foreach ($item in @('manifest.json', 'README.md', 'PRIVACY.md', 'Setup-Windows.ps1', 'docs', 'fixtures', 'third-party')) {
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
$hashes = Get-FileHash -Algorithm SHA256 -LiteralPath $zip,$mcpb
$hashes | ForEach-Object {
    "$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_.Path))"
} | Set-Content (Join-Path $dist.FullName 'SHA256SUMS.txt') -Encoding ascii

$bundleFile = [IO.Path]::GetFileName($mcpb)
$repositoryUrl = $manifest.repository.url
$metadata = [ordered]@{
    '$schema' = 'https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json'
    name = 'io.github.umutoku/uyap-editor-mcp'
    title = $manifest.display_name
    description = $manifest.description
    repository = @{ url = $repositoryUrl; source = 'github' }
    websiteUrl = $repositoryUrl
    version = $manifest.version
    packages = @(
        @{
            registryType = 'mcpb'
            identifier = "$repositoryUrl/releases/download/v$($manifest.version)/$bundleFile"
            fileSha256 = ($hashes | Where-Object Path -eq $mcpb).Hash.ToLowerInvariant()
            transport = @{ type = 'stdio' }
        }
    )
}
$metadata | ConvertTo-Json -Depth 8 |
    Set-Content (Join-Path $dist.FullName 'server.json') -Encoding utf8
$hashes | Select-Object Hash,Path

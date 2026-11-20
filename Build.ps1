[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EditorDirectory,
    [Parameter(Mandatory)][string]$JavaCompiler,
    [string]$ArtifactsPath = (Join-Path $PSScriptRoot '.build')
)

$ErrorActionPreference = 'Stop'
$editor = (Resolve-Path -LiteralPath $EditorDirectory).Path
$javac = (Resolve-Path -LiteralPath $JavaCompiler).Path
$classes = New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot 'java/classes') -Force
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'java') -Filter '*.java' -File |
    Select-Object -ExpandProperty FullName)
& $javac --release 8 -Xlint:-options -encoding UTF-8 -classpath (Join-Path $editor '*') -d $classes.FullName @sources
if ($LASTEXITCODE -ne 0) { throw "javac exited with code $LASTEXITCODE" }

$project = Join-Path $PSScriptRoot 'UyapEditorMcp.csproj'
$destination = Join-Path $PSScriptRoot 'app'
dotnet publish $project -c Release --artifacts-path $ArtifactsPath --disable-build-servers -o $destination
if ($LASTEXITCODE -ne 0) { throw "dotnet publish exited with code $LASTEXITCODE" }

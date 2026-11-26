param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$EditorDirectory,
    [Parameter(Mandatory)][string]$JavaBin,
    [Parameter(Mandatory)][string]$InputDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$StateDirectory
)

$ErrorActionPreference = 'Stop'
$binary = Get-Item -LiteralPath $Executable
if ($binary.PSIsContainer -or $binary.Name -ine 'UyapEditorMcp.exe') {
    throw 'Choose UyapEditorMcp.exe from the Windows x64 release.'
}
& $binary.FullName --editor-dir $EditorDirectory --java-bin $JavaBin --input-root $InputDirectory --output-root $OutputDirectory --state-dir $StateDirectory
exit $LASTEXITCODE

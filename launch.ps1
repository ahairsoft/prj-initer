param(
    [ValidateSet("menu", "initializer", "tagmanager")]
    [string]$App = "menu",
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$initializerProject = Join-Path $root "src\ProjectInitializer\ProjectInitializer.csproj"
$tagManagerProject = Join-Path $root "src\TagManager\TagManager.csproj"

function Start-App([string]$projectPath) {
    if ($Build) {
        dotnet build $projectPath -nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed: $projectPath"
        }
    }

    dotnet run --project $projectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Run failed: $projectPath"
    }
}

if ($App -eq "menu") {
    Write-Host "Select app to launch:"
    Write-Host "1) ProjectInitializer"
    Write-Host "2) TagManager"

    $choice = Read-Host "Enter 1 or 2"
    if ($choice -eq "1") {
        $App = "initializer"
    }
    elseif ($choice -eq "2") {
        $App = "tagmanager"
    }
    else {
        throw "Invalid choice: $choice"
    }
}

switch ($App) {
    "initializer" { Start-App $initializerProject }
    "tagmanager" { Start-App $tagManagerProject }
    default { throw "Unsupported app: $App" }
}

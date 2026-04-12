# prj-initer

A lightweight project initializer and tag manager for multi-repo client projects.

## Projects
- src/ProjectInitializer: WinForms tool to initialize a new project repository with selected submodules.
- src/TagManager: WinForms tool to create, restore, and update tags across root repo and submodules.

## Prerequisites
- .NET 8 SDK
- git CLI
- gh CLI (only required if creating GitHub repository from UI)

## Build
```powershell
dotnet build .\prj-initer.sln
```

## Run
```powershell
dotnet run --project .\src\ProjectInitializer\ProjectInitializer.csproj
dotnet run --project .\src\TagManager\TagManager.csproj
```

## Notes
- Tag operations work for both GitHub and Gitee remotes because all operations use git CLI.
- To create GitHub remote repositories, run `gh auth login` first.

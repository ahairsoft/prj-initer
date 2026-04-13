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

## One-Click Launchers
- `launch.cmd`: interactive menu to choose ProjectInitializer or TagManager.
- `start-initializer.cmd`: directly launch ProjectInitializer.
- `start-tagmanager.cmd`: directly launch TagManager.
- `launch.ps1`: script entry (supports `menu|initializer|tagmanager`, optional `-Build`).

Examples:
```powershell
.\launch.ps1 initializer -Build
.\launch.ps1 tagmanager
```

## ProjectInitializer Branch UX
- Base branch column is a dropdown per submodule.
- Remote branches are loaded on-demand when clicking the dropdown cell.
- Branch list is cached by remote URL.
- `Refresh Selected Branches` button force-refreshes selected rows (or all included rows when no row selected).
- `Branch Filter` supports keyword filtering (contains match, case-insensitive) for dropdown options.
- Press Enter in filter box or click `Apply Filter`; click `Clear` to reset.

## TagManager Safe Restore
- `Safe Restore` is enabled by default.
- Restore operation checks each repository is clean first.
- In safe mode, restore runs: `checkout -B <prefix>/<tag> <tag>` to avoid detached HEAD.
- Branch prefix is configurable (default: `restore`).

## TagManager Dry Run
- `Dry Run` executes no git write operations and prints the exact commands to the log.
- Supported for create tags, restore to tag, and update tags workflows.
- Useful to verify impact before touching GitHub/Gitee repositories.

## Notes
- Tag operations work for both GitHub and Gitee remotes because all operations use git CLI.
- To create GitHub remote repositories, run `gh auth login` first.

# Bannerlord Mod Doctor

Bannerlord Mod Doctor is a portable Windows troubleshooting tool for Mount & Blade II: Bannerlord mod setups.

It is designed to help players diagnose common modding issues without digging through launcher files, crash logs, and Discord threads first.

## Features

- Checks missing dependencies
- Finds duplicate module IDs
- Detects blocked DLLs
- Can fix blocked DLL markers
- Suggests a dependency-based load order
- Can write the suggested load order to `LauncherData.xml`
- Creates backups before load-order changes
- Can undo load-order sorting
- Finds duplicate framework DLL copies
- Can clean duplicate DLLs with backup
- Can undo DLL cleanup
- Checks mod-vs-mod XML record conflicts
- Filters out core game, DLC, and language XML noise
- Creates a support report for Nexus or Discord
- Includes recent crash-log clues when available

## Download

Use `BannerlordModDoctor.exe` from the release package.

Close Bannerlord and the Bannerlord launcher before using actions that modify files.

## What The Tool Can Modify

Only after user confirmation/action:

- `Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml`
- Windows blocked-file markers on DLLs
- Duplicate framework DLL copies, moved to backup
- Tool-generated report and backup files

Safety behavior:

- Load-order sorting creates a timestamped `LauncherData.xml.backup_*` first.
- Duplicate DLL cleanup moves files to `HelperDllBackups`.
- Undo buttons exist for load-order sorting and DLL cleanup.
- Reports are saved beside the tool in `Reports`.

## Build

On Windows with PowerShell:

```powershell
.\build.ps1
```

The build script compiles `src\BannerlordModDoctor.cs` into `dist\BannerlordModDoctor.exe`.

## Code Signing

This project is being prepared for open-source code signing review.

See:

- `docs/SIGNPATH_APPLICATION.md`
- `docs/SECURITY_AND_SCANNER_NOTES.md`

## License

MIT License. See `LICENSE`.

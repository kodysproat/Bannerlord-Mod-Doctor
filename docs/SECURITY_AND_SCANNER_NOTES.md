# Security And Scanner Notes

Bannerlord Mod Doctor is a portable Windows executable. Some scanners may flag unsigned tools, especially tools that can modify local files.

## What It Does

The tool reads Bannerlord module folders and Bannerlord launcher data to identify common setup problems.

It can modify files only after user action:

- Sort load order in `LauncherData.xml`
- Restore a previous `LauncherData.xml` backup
- Remove Windows blocked-file markers from DLLs
- Move duplicate framework DLLs into a backup folder
- Restore duplicate DLL backups
- Save reports

## Safety Measures

- Load-order changes create timestamped backups.
- DLL cleanup moves files to backup instead of permanent deletion.
- Undo actions are available.
- No background downloads are performed.
- The PayPal button only opens a browser link.

## Antivirus / Nexus Review Context

Likely reasons for scanner suspicion:

- Unsigned Windows executable
- File modification behavior
- DLL-related maintenance actions

This repository includes source code and GitHub Actions build instructions so reviewers can inspect and rebuild the tool.

Bannerlord Mod Doctor
=====================

Run:

  BannerlordModDoctor.exe

Bannerlord Mod Doctor is a portable outside-the-game troubleshooting tool for Mount & Blade II: Bannerlord mod users.

Use it when the game will not start, a save begins crashing, or a large mod list needs a quick sanity check.

Quick Start
-----------

1. Run BannerlordModDoctor.exe.
2. Confirm your Modules folder.
3. Leave Include Steam Workshop checked if you use Workshop mods.
4. Click Check My Mods.
5. Read the Mod Doctor Summary first.

Useful Buttons
--------------

- Check My Mods: full scan and report.
- Quick Check: faster scan without blocked DLL checks.
- Fix Blocked DLLs: removes Windows downloaded-file blocking from DLLs.
- Sort Load Order: prints a suggested dependency-based load order.
- Clean Duplicate DLLs: moves safe duplicate framework DLLs to a backup folder.
- Undo DLL Cleanup: restores DLLs moved by this tool.
- Copy Results: copies the visible report.
- Save: saves the visible report.
- Reports: opens the report folder.

What It Checks
--------------

- Missing dependencies
- Duplicate module IDs
- Suggested load order
- Duplicate framework DLLs
- XML record conflicts between mods
- Duplicate DLL file names
- Windows-blocked DLLs

Notes
-----

The tool is designed to catch common setup problems quickly. It cannot guarantee that every runtime Harmony patch conflict will be detected.

Core game, DLC, and language XML files are filtered out of XML conflict checks to reduce noise.

Duplicate DLL cleanup is reversible. Files moved by the tool are stored in:

  HelperDllBackups

Reports are saved in:

  Reports

Support
-------

If Bannerlord Mod Doctor saves you time or stress, donations help support continued updates:

  https://paypal.me/KodySp

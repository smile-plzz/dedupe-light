# Development and release

## Project

Native WinForms application targeting .NET 9 Windows. Product name,
executable and data directory remain DedupeLight. Version is set in
DedupeLight.csproj. Icon assets include a vector master and a Windows ICO with
16, 32, 48 and 256 pixel frames.

## Build

Use .NET 9 SDK on Windows:

```powershell
dotnet run --project tests/ScannerChecks.csproj -c Release
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

The executable is publish/DedupeLight.exe. Linux cross-builds require
-p:EnableWindowsTargeting=true; GUI execution remains Windows-only.

## Structure

- MainForm.cs / ModernControls.cs: layout, selection, actions and native styling.
- DuplicateScanner.cs: background discovery, sampling, full hashing and cache.
- Models.cs / AppSettings.cs: result records and local preferences.
- ExclusionRules.cs: path matching and recycle selection policy.
- ExclusionDialog.cs: rule manager/import/export.
- FileReviewDialog.cs: image preview, optional Windows Media Player and fallback.
- RecycleOverrideDialog.cs: typed, operation-only override.
- tests/: scanner/policy/settings regression checks.

## CI and releases

Pull requests run checks, validate icon frames, build and package on Windows.
Only a successful main-branch push publishes a new versioned release.
Version is read from the project; an existing release for that version is left
unchanged. Bump the version for any later bug-fix release.

The package contains the executable, license, README, user guide, release
status and changelog. A separate SHA-256 file covers the ZIP. The release job
has repository contents-write permission solely for tagging/uploading releases;
the build job uses read permission.

Development for 1.0 is closed. Maintenance should use a new PR, meaningful
regression checks, a version bump, and changelog notes.

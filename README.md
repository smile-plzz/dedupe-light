<p align="center"><img src="assets/dedupelight.svg" width="96" alt="DedupeLight icon"></p>

# DedupeLight

**Less clutter. More space.**

A lightweight, native Windows app for finding, reviewing, and handling exact
duplicate files. DedupeLight verifies matches with SHA-256, reuses cached hashes,
and lets you protect intentional copies. Nothing is deleted automatically.

**Version 1.0.0 · Windows x64 · MIT license**

## Download

[Download the latest release](https://github.com/smile-plzz/dedupe-light/releases/latest)

Choose **DedupeLight-1.0.0-win-x64.zip**, extract it to a folder, and run
`DedupeLight.exe`. No installer or separate .NET installation is needed.
Release assets include a SHA-256 checksum. Windows may show a SmartScreen
notice because this release is unsigned.

## Use

1. Browse to a folder/drive, or drag it into the window.
2. Choose minimum size (default: 1 MB) and hidden/system scanning preferences.
3. Select **Scan now**. You can cancel while scanning.
4. Review duplicate sets. **Select extras** keeps the oldest, newest, or
   shortest-path copy according to your selected rule.
5. Use **Review** for previews or double-click a file to open its default app.
6. Recycle, move, copy, or export results when you are ready.

Use **Exclude file** or **Exclude folder** for intentional copies.
**Excluded sources** manages saved rules and imports/exports them as JSON.
Remove a rule and rescan to restore that source to the results.

## Features

- Exact duplicate detection: file size → sample fingerprint → full SHA-256.
- Cached fingerprints/hashes for unchanged paths, sizes, and modification times.
- Background scans and disk operations, cancellation, skipped inaccessible paths.
- Search, sorting, duplicate-set counts, and estimated recoverable space.
- Keep oldest/newest/shortest-path selection rules.
- Per-file or recursive folder exclusions, saved between sessions.
- In-app image review and optional video/audio playback.
- Double-click opening, Explorer reveal, move/copy, Recycle Bin and CSV export.
- Explicit per-operation override for recycling every listed copy of a set.
- Modern native UI, rounded controls, hover transitions and keyboard focus.

## What to know

DedupeLight finds **exact files**, not visually similar images or edited videos.
Exclusions leave files on disk. An excluded copy does not appear in a duplicate
set or its recoverable-space estimate.

Recycling every listed copy requires typing **RECYCLE**, then accepting the
normal confirmation. That override is never saved as a preference. The app
does not verify copies outside the scan.

Video/audio preview depends on Windows Media Player and installed codecs.
**Open in default app** remains available. Playback does not start automatically.
Image previews support common raster formats; large images use the default app.

Scans still enumerate the chosen location. The cache avoids repeat hashing;
it does not provide an instant catalog of disconnected drives. An individual
filesystem call on a slow/disconnected drive may delay cancellation.

## Requirements and data

- Windows 10/11, 64-bit.
- Portable, self-contained .NET 9 build.
- Preferences, exclusions and hashes: `%LOCALAPPDATA%\\DedupeLight`.
- Scanning and hashing are local; the application has no network requests.
- Import/export exclusions and exported CSV files contain local paths.

## Documentation

- [User guide and troubleshooting](docs/USER_GUIDE.md)
- [Developer and release guide](docs/DEVELOPMENT.md)
- [Release status and validation](docs/RELEASE_STATUS.md)
- [Changelog](CHANGELOG.md)
- [MIT license](LICENSE)

Version 1.0 feature development is complete. Future changes are maintenance,
bug fixes, or a separately scoped version.

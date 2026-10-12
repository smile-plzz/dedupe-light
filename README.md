# DedupeLight

A lightweight, native Windows app for finding and safely handling exact duplicate files.

DedupeLight scans any folder or drive, narrows candidates by file size, verifies matches with SHA-256, and caches unchanged fingerprints to make later scans faster. Nothing is removed automatically.

## Download and test

Download [DedupeLight for Windows x64](dist/DedupeLight-win-x64.zip), extract the ZIP, and run `DedupeLight.exe`.

The package is self-contained and does not require a separate .NET installation. Windows may show a SmartScreen notice because the executable is not code-signed. Select **More info**, verify the publisher status, and choose **Run anyway** only if you downloaded it from this repository.

SHA-256 for `DedupeLight-win-x64.zip`:

```text
7763D763CAC4FC22DB66EA2F145861D5B49BF296FEF701D410CEBA9AE67EE494
```

The complete project source is stored directly in the repository root.

## Features

- Scan any folder or drive
- Drag a folder into the window to select it
- Minimum file-size filter and optional hidden/system file scanning
- Search duplicate results by file name or location
- Sort results by set, name, location, size, or modified time
- See duplicate-set count and recoverable space
- Select extra copies while keeping the oldest, newest, or shortest-path copy
- Safety check prevents recycling every copy in a set
- Open files or reveal them in Windows Explorer
- Recycle, move, or copy selected files
- Export results to CSV
- Cancel scans and continue past inaccessible locations
- Avoids directory junction loops and reuses cached hashes
- Remembers the last location and scan preferences

## Requirements

- Windows 10 or Windows 11, 64-bit
- The published standalone build needs no separate .NET installation

## Using the app

1. Choose a folder or drive.
2. Set the minimum file size and whether hidden/system files should be included.
3. Select **Scan now**.
4. Review or filter the exact duplicate sets.
5. Tick individual files or use **Select extras** to keep the oldest copy in every set.
6. Recycle, move, copy, open, reveal, or export the selected results.

## Safety

- Nothing is removed automatically.
- **Select extras** keeps the oldest file in each set unselected.
- The app blocks a recycle operation that would remove every copy in a set.
- Deletion uses the Windows Recycle Bin.
- Locked and inaccessible folders are skipped instead of stopping the scan.

## Build

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The executable is written to `bin/Release/net9.0-windows/win-x64/publish/`.

## How scanning works

1. Files below the chosen minimum size are ignored.
2. Remaining files are grouped by byte size, so unique-size files never need hashing.
3. A lightweight fingerprint reads only the first and last 64 KB of larger candidates.
4. Only files that still match are fully verified with SHA-256.
5. A local cache reuses fingerprints and full hashes when a file's path, size, and modification time are unchanged.

The partial fingerprint is only a performance filter. DedupeLight never reports large files as duplicates until their complete SHA-256 hashes match.

The cache is stored under `%LOCALAPPDATA%\DedupeLight` and is not added to the repository.

## License

[MIT](LICENSE)

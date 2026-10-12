# DedupeLight 1.0 user guide

## Install and update

Download the portable ZIP from GitHub Releases. Extract all files, then run
DedupeLight.exe. Updating means closing the old app and extracting the new
package. Preferences and exclusions remain in Local AppData. No administrator
privileges are required by the application.

## Scan and select

Choose a folder or drive. The default minimum size is 1 MB; set 0 to include
smaller files. Hidden/system files are omitted unless enabled. Directory
junctions are not followed. Inaccessible locations are skipped and counted.

Results contain exact matches grouped into sets. The checkbox marks a file for
an operation; row highlighting identifies the file to review/open. If files are
checked, those files take priority for recycle/move/copy; otherwise highlighted
rows are used. Search affects visible rows, not existing checkmarks. Check the
selected count before acting: checked files can remain selected outside the
current search.

Select extras applies to every result set, including filtered-out results.
Keep oldest/newest uses modification time; shortest path uses path length.
Clear removes all checkmarks.

## Review

Select a single row and choose Review. The review window displays its path,
size, modification time, set and SHA-256. Common images get a bounded thumbnail.
Supported media can play in optional Windows Media Player controls. Press Play
to start. Missing components/codecs may require Open in default app. Closing
the review stops embedded playback.

Double-click a non-checkbox cell to open that exact file in the default app.
Show in Explorer reveals its location. Review includes exclusion shortcuts.

## Intentional copies

Exclude file skips only those selected paths. Exclude folder skips their entire
containing folders, including descendants. Confirm the scope before applying.
The files are not moved, copied or deleted.

Excluded sources lets you add files/folders, remove selected rules, or import
and export JSON lists. Apply saves changes; Cancel leaves them unchanged.
Imports merge with the list and reject relative paths. Removing exclusions
requires a rescan to restore results. Exclusion JSON contains paths, not media.

Exclusions use absolute paths. If an external drive receives another drive
letter, update the rule to its new path. Results that no longer have two
included copies disappear from the duplicate list.

## Recycle and transfer

Recycle sends selected files to Windows Recycle Bin. To recycle every listed
copy of a set, review the affected paths, type RECYCLE in the override dialog,
then confirm the normal recycle action. Keep files/Escape cancels. This
permission applies only once; it is not a global setting. Copies outside the
scan are not verified. Recycle Bin behavior and availability depend on the
drive and Windows.

Move/copy chooses a destination and gives colliding file names a numbered
suffix. Transferring to the same containing folder is skipped. Failed operations
are counted. Background operations disable conflicting controls until finished.

CSV export exports the complete current results, including filtered-out rows.

## Cache and preferences

Files are first grouped by size, then sampled, then fully hashed if needed.
Cached hashes are reused when path, size and modification time match.
The partial fingerprint alone never confirms a large duplicate file.

Local data lives in %LOCALAPPDATA%\\DedupeLight:
- settings.json: scan preferences, last location and exclusion rules.
- hash-cache.json: sample and full hashes.

To rebuild the cache, close the app, remove hash-cache.json and rescan.
Removing settings.json resets preferences and exclusions; export rules first.

## Troubleshooting

- Slow first scan: large candidate files require full reads. Repeat scans can
  reuse hashes, but still enumerate folders.
- Cancel delayed: Windows filesystem calls must return before cancellation can
  be observed.
- No results: check minimum size, exclusions and search.
- Video preview missing: use the default player or install the appropriate
  Windows media component/codec.
- File cannot open/move/recycle: it may be locked, disconnected, inaccessible or
  changed since scanning. Reconnect/unlock it and rescan.
- SmartScreen notice: version 1.0 is unsigned. Verify the release checksum and
  source before deciding whether to run it.

Report bugs with app version, Windows version, steps to reproduce and expected
versus actual behavior. Avoid posting private file paths or exported lists.

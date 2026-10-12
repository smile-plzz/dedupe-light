# DedupeLight 1.0.0 release status

Version 1.0 feature development is closed. This release includes exact-file
deduplication, caching, background operations, exclusions, media review,
typed recycle override and a finalized native identity.

## Evidence

- The responsiveness and UI build was tested by the owner.
- The feature build passed Windows scanner/feature regression checks,
  compilation and standalone publication in GitHub Actions run 38305954208.
- Final branded packaging repeats those checks and validates all icon frames.
  Its successful workflow run is the release-build evidence.
- Final branding/media codec behavior and high-DPI layouts have not been
  independently exercised by an automated desktop test.

## Known limits

- Unsigned Windows executable.
- Exact matches only; no perceptual similarity detection.
- Cache keys use absolute path, size and modification time; drive-letter changes
  invalidate reuse, and unusual metadata-preserving edits can evade invalidation.
- Exclusion paths are absolute and need updating after path/drive-letter changes.
- Preview video/audio depends on optional Windows components and codecs.
- Cancellation cannot interrupt every synchronous Windows filesystem call.
- File state may change after a scan. Rescan before acting on stale results.
- Recycle protection concerns listed scan results, not every copy on all drives.

These are documented behavior and testing limits, not claims of universal
compatibility. Future issues are handled as maintenance.

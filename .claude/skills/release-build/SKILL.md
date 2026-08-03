---
name: release-build
description: Produce a packaged installer build for a release.
---

Use when cutting a release build.

1. Package via MSIX (or the justified alternative recorded in the packaging ADR) with correct versioning and code signing.
2. Update the changelog before packaging, not after.
3. Never publish, upload, or push a release externally without explicit user confirmation — this skill covers producing the build artifact locally/in CI, not distributing it.
4. Verify the build against `installer-validation` before considering the release candidate ready.

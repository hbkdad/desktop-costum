---
name: installer-validation
description: Validate install, update, repair, and uninstall flows for a packaged build.
---

Use against any release candidate before it's approved.

1. Verify clean install on a fresh environment, upgrade-in-place from the previous version, repair, and full uninstall.
2. Uninstall must leave no orphaned processes, scheduled tasks, registry state, or user-data surprises — confirm explicitly rather than assuming.
3. Verify logs and a diagnostics export are produced and readable after each flow, including a forced-failure case.
4. Record any failure as a blocking issue in `backlog/task-backlog.md`, not a note to fix later — a broken install/uninstall flow blocks release.

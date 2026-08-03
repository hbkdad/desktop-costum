---
name: security-review
description: Run a threat-model / permission-manifest review on a new module, package type, or capability surface.
---

Use before shipping any module that handles third-party content (widgets, wallpapers, plugins, marketplace packages) or a new capability.

1. Walk the threat categories from `AGENTS.md`'s non-negotiables and the master threat model (malicious packages, remote web content, script execution, credential theft, clipboard/file/network access, process launching, privilege escalation, GPU/DoS abuse, package spoofing, update-channel compromise, marketplace fraud).
2. Confirm default-deny: list every capability the module could use, and verify each requires an explicit manifest entry (see the JSON permission format in the master spec, e.g. `system.metrics.read`, `network.domain:...`, `files.user-selected.read`, `clipboard.read-on-user-action`, `process.launch:declared-application`).
3. Confirm no path to arbitrary PowerShell/CMD/JS-outside-sandbox/native-DLL/unsigned-binary execution from untrusted content.
4. Write at least one abuse-case test that attempts the worst-case action for this surface and confirms it's blocked.
5. Record findings and any accepted residual risk in `backlog/risk-register.md`; anything requiring an architecture change goes through `architecture-decision`.

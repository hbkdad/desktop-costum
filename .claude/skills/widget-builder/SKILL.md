---
name: widget-builder
description: Author or modify a desktop widget definition/component under the creator-studio conventions.
---

Use when adding a widget type or changing the widget authoring format.

1. Every widget needs an explicit permission manifest (default-deny — see the capability list format in `AGENTS.md`/security docs); a widget must not gain a capability it didn't declare.
2. Data bindings and any expression system must not allow arbitrary code/script execution outside the declared, isolated runtime — treat all widget content as untrusted, including first-party sample widgets.
3. Define responsive sizes/states explicitly; do not assume a fixed canvas size.
4. Every widget must report an estimated performance cost (ties into the Performance Agent's budgets) before it's accepted into the default set.
5. Run `security-review` on any widget capability that touches files, network, clipboard, or process launch.

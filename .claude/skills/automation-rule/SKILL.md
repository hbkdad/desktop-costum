---
name: automation-rule
description: Define or modify an automation trigger/action rule (app start/exit, monitor connect, power source, schedule, workspace activation).
---

Use when adding a trigger type, action type, or changing the automation engine's rule schema.

1. Triggers and actions are both explicit, declared capabilities — no rule may execute an arbitrary shell command, script, or unsigned binary. Actions are limited to declared-application launch and other explicitly modeled capabilities (see the permission-manifest format in security docs).
2. Every new trigger/action type needs a permission entry (default-deny) and a security review (`security-review` skill) before it ships.
3. Rules must be inspectable and editable by the user in plain form — no opaque/hidden automation.
4. Add a test that proves the rule fires on the trigger and performs only the declared action, nothing else.

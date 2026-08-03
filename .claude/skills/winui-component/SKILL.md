---
name: winui-component
description: Build or modify a WinUI 3 UI component following this project's UI conventions.
---

Use when adding or changing UI in the shell, widget host, or creator studio.

1. Keep UI (XAML + code-behind/view-model) separate from domain logic — the component should not talk to Explorer-specific or native-interop code directly; go through the domain layer.
2. Accessibility is not optional: set automation names/roles, verify keyboard navigation and screen-reader behaviour, and support text scaling/high-contrast themes before calling a component done.
3. Use resource dictionaries / theme resources for styling so light/dark and custom themes work without code changes.
4. Respect multi-monitor and per-monitor-DPI correctness — test the component on a mixed-DPI multi-monitor layout, a known failure area for competitor products (see `docs/research/competitor-matrix.md`).
5. Add or update a test for any behaviour change, not just new components.

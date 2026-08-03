# Prototype 13 Report: Process Isolation Mechanisms

Format per the `desktop-host-prototype` skill.

- **Purpose:** [ADR-0004](../../docs/architecture/adr/0004-process-and-isolation-model.md) commits to sandboxing package code in a per-package process with an AppContainer, a restricted token, and a job object whose caps derive from the manifest's declared budget — none of which was validated when the ADR was written. That gap made this prototype a Phase 5 blocker. This probe tests the assumptions that carry the most weight.
- **Implementation:** `prototypes/process-isolation-poc/Program.cs` — standalone console probe. Creates a job object with `JOB_OBJECT_LIMIT_PROCESS_MEMORY`/`JOB_OBJECT_LIMIT_JOB_MEMORY`, assigns the current process, reads `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION`, then attempts an allocation deliberately above the cap. Separately creates and deletes an AppContainer profile. Leaves no permanent system state.
- **Test method:** Built and run against the live machine, **unelevated** (confirmed: `Elevated: False`) — the same privilege level the product will have.
- **Measurements / findings:**

  | Assumption | Result |
  |---|---|
  | Job memory cap is **enforced**, not merely settable | **PASS** — with a 146 MB cap, a 512 MB allocation was refused with `OutOfMemoryException` |
  | Job accounting is readable from the trusted side | **PASS** — 1 active process, 62.5 ms user / 15.6 ms kernel time, 5,065 page faults |
  | Accounting query is cheap enough to poll | **0.168 ms** — the same order as the ~0.7 ms fullscreen check in Prototype 10 |
  | AppContainer profile creatable without elevation | **PASS** — SID returned, profile deleted cleanly |

  Windows build tested: `10.0.26200.0`.

  A first version of this probe queried accounting on an *empty* job and reported all zeros, which would have proved only that the call compiles. It was restructured to read accounting from a job that actually contains a process, which is where the non-zero figures above come from.

- **Limitations — the important part:** three of ADR-0004's claims remain **unverified**.
  1. **Launching a process *into* an AppContainer was not tested.** Creating a profile is the easy half; `CreateProcess` with `STARTUPINFOEX` and `PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES` is the half that can fail, and whether a WinUI 3/WebView2 renderer will run inside one is exactly the open question.
  2. **Restricted tokens were not tested at all.**
  3. **CPU capping was not tested** — only memory. `JOB_OBJECT_LIMIT_JOB_TIME`/CPU rate control is a different mechanism with different semantics (it can terminate rather than throttle, which would be the wrong behaviour for a widget).

  Also untested: whether nesting works when the product itself runs inside a job (a CI container or debugger host), which the probe warns about but did not encounter here.

- **Recommendation:** The enforcement half of ADR-0004 is sound — a job object genuinely makes a declared memory budget a hard ceiling, and per-job accounting is a viable, cheap sample source for `ResourceLedger`, read from the trusted side rather than self-reported by the sandboxed process. Proceed on that basis. **Do not** treat the AppContainer half as settled: a follow-up spike must launch a real renderer into a container before Phase 5 depends on it.
- **Keep/discard decision:** **Keep.** ADR-0004 moves from wholly unvalidated to partially validated, and Prototype 13 is downgraded from a hard Phase 5 blocker to a **narrowed** one: the remaining blocker is specifically "launch a renderer inside an AppContainer with a restricted token", not the whole isolation model.

## Impact on other project artifacts

- ADR-0004 status updated from Proposed to Accepted-in-part, with the unverified claims named explicitly.
- `docs/architecture/resource-accounting.md`: the sample source is no longer hypothetical — per-job accounting at ~0.17 ms per query is measured.
- `backlog/prototype-backlog.md`: item 13 narrowed rather than closed.

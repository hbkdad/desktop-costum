# Prototype 13b Report: Launching a Process Into an AppContainer

Format per the `desktop-host-prototype` skill. Follow-up to `prototypes/process-isolation-poc/REPORT.md`, which validated ADR-0004's enforcement half but explicitly left this untested — the remaining Phase 5 blocker.

- **Purpose:** Prototype 13 showed an AppContainer *profile* can be created. Creating a profile is the easy half. The open question was whether a process can actually be launched *inside* a container, and whether the sandbox then denies what it is supposed to. Everything the permission model promises rests on this.
- **Implementation:** `prototypes/appcontainer-launch-poc/Program.cs` — `CreateProcess` with `STARTUPINFOEX` and `PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES`, passing a `SECURITY_CAPABILITIES` naming the container SID with **zero capabilities granted** (the default-deny baseline a package with an empty permission set would run under).

  A deliberate design choice: rather than sandbox a custom .NET child — which would require ACL'ing the shared framework directory for the container SID, an invasive change that would also muddy the result — the probe launches `cmd.exe`. System32 already grants `ALL APPLICATION PACKAGES` read+execute, and every AppContainer belongs to that group, so **no ACL anywhere needed modifying**.

- **Test method:** Three runs, ordered so the result is meaningful rather than merely suggestive:

  | | Test | Expectation |
  |---|---|---|
  | A | Control — `exit 42` **inside** the container | Proves process creation into an AppContainer works at all |
  | B | Baseline — read the probe file **outside** the container | Proves the file exists and is readable normally |
  | C | Isolation — read the same file **inside** the container | Must fail |

  B is what makes C meaningful. Without it, C failing could simply mean a bad path.

- **Measurements / findings:** All three passed, unelevated, on build `10.0.26200.0`.

  - **A:** process launched inside the container and returned exit code 42.
  - **B:** `type <probe>` outside the container → exit 0.
  - **C:** the identical command inside the container → exit 1.

  Container SID (freshly derived each run, deleted afterwards): `S-1-15-2-112438720-…`. The probe file and the AppContainer profile were both removed at the end of the run.

- **Limitations:**
  - `cmd.exe` returns a generic exit code 1 on failure, so C proves the read **failed**, not the specific error code. The differential against B — same command, same file, exit 0 outside the container — is what makes access denial the compelling explanation, but it is inference from a differential rather than a captured `ERROR_ACCESS_DENIED`.
  - **Restricted tokens remain untested.** ADR-0004 specifies AppContainer *and* a restricted token; only the former is now validated.
  - A real widget host would be **our** binary in our install directory, which does require granting the container SID read+execute on that directory. That is standard, well-documented practice rather than a research risk, but it is a real deployment step and was not exercised here.
  - No capability was granted, so this does not test that *granted* capabilities work — only that ungranted access is denied. Both directions matter eventually.

- **Recommendation:** Treat AppContainer isolation as viable and proceed with ADR-0004 as designed. The combination now validated across 13 and 13b — job memory caps genuinely enforced, per-job accounting readable cheaply from the trusted side, processes launchable into a container, and default-deny actually denying — is the whole load-bearing basis of the sandbox model. Remaining work (restricted tokens, install-directory ACLs, positive capability tests) is engineering with known shapes, not open research.

- **Keep/discard decision:** **Keep.** This closes the narrowed Phase 5 blocker. ADR-0004 moves from *Accepted in part* to *Accepted*, with restricted tokens noted as an implementation detail still to be exercised.

## Impact on other project artifacts

- ADR-0004 status updated; the Phase 5 blocker is cleared.
- `backlog/prototype-backlog.md` item 13 closed.
- Phase 5 Slice 1 can now begin on validated ground rather than on assumption.

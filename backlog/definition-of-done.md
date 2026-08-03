# Definition of Done

A task is done only when:

- Requirements are met.
- Code builds.
- Relevant tests pass.
- Security implications are reviewed (`security-review` skill for anything touching untrusted content or a new capability).
- Performance implications are reviewed (`performance-test` skill for anything rendering or running continuously).
- Documentation is updated.
- State files are updated (`.agents/state/current-phase.md`, `decisions.md`, `handoff.md` as applicable).
- No unexplained warnings remain.
- Failure behaviour is documented.
- The result has been manually or automatically verified — never claim success without build or runtime evidence.

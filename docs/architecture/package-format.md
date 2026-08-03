# Package Format and Signing

Source of truth for how distributable packages are structured and validated. Implementation: `src/DesktopRuntime.Core/Packaging/`. Governed by the `security-review` and `installer-validation` skills.

This is the **trust boundary** for everything a marketplace distributes. A package archive is attacker-authored data, and it is validated in full *before* anything is extracted.

## Structure

```
manifest.json          (required, at the root)
assets/…               content files
README.md              optional
```

The manifest is validated separately — see `widget-manifest.md`. This document covers the archive that carries it.

## Entry path rules

Every rule exists because of a specific known attack, and each is named in `tests/DesktopRuntime.Core.Tests/Packaging/`:

| Rejected | Why |
|---|---|
| `../evil.json`, `assets/../../evil.json` | **Zip slip** — writes outside the extraction directory |
| `/etc/passwd`, `C:\windows\evil.json` | Rooted or drive-qualified paths |
| `CON`, `nul.txt`, `assets/COM1.json` | Windows treats these as **device names** in any directory, with or without an extension |
| `evil.json.`, `trailing .json ` | Windows **silently strips** trailing dots/spaces, so these collide with another entry — a way past a naive duplicate check |
| `notes.txt:hidden` | **Alternate data stream** (also catches drive letters, since both use `:`) |
| `assets//bg.png`, `assets/` | Empty segments normalize to a different path than declared |
| `<`, `>`, `"`, `\|`, `?`, `*`, control chars | Invalid or dangerous on Windows |
| Paths over 200 chars, segments over 64, depth over 8 | Bounded by construction |

Backslashes are normalized to forward slashes **before** segment inspection, so `..\evil` is caught by the same check as `../evil`.

Duplicate detection is **case-insensitive**, because two entries differing only in case collide on Windows and the second silently overwrites the first.

## Content types: allowlist

Packages may contain only: `.json`, `.png`, `.jpg`, `.jpeg`, `.webp`, `.gif`, `.mp4`, `.webm`, `.woff2`, `.txt`, `.md`.

An allowlist rather than a blocklist, for the same reason widget ids use one: a blocklist is a promise to have thought of every dangerous extension, and it only needs to be wrong once. Executables, libraries and scripts are therefore excluded automatically rather than individually.

Two deliberate exclusions worth stating: **`.svg`** (can embed script) and **`.html`/`.css`/`.js`** (web content is deferred past MVP per PRD §2, and when it arrives it belongs in the isolated web runtime, not as loose files in a package directory).

## Decompression bombs

Checked from **declared sizes, before any bytes are written**:

- Per-entry uncompressed:compressed ratio ≤ 100× — a bomb is characterised by an extreme ratio.
- Total expanded size ≤ 256 MB — individually plausible entries can still sum to an unacceptable total.
- Entry count ≤ 2,000.
- An entry claiming to expand from zero compressed bytes is rejected; a genuinely empty file (0/0) is fine.

## Signing

| Signature state | Default | Sideload mode enabled |
|---|---|---|
| Valid | Accepted, publisher recorded | Accepted |
| Absent | **Rejected** | Accepted, no publisher |
| Present but invalid | **Rejected** | **Still rejected** |

A broken signature is not the same as no signature — it suggests tampering with something that *was* signed. Permitting sideloads must therefore not permit it. Sideload mode is intended for developers and must be an explicit, visible choice.

**Cryptography is deliberately not implemented here.** `IPackageSignatureVerifier` is an abstraction with no implementation in this project: signature verification requires platform cryptography and certificate-chain validation, and hand-rolling it would be a mistake. This project defines the *policy* — what a valid signature entitles a package to — and leaves the verification to the platform. `PackageSignature` carries only a verdict and a publisher identity, never key material or raw signature bytes.

## Error reporting

Rejected paths are sanitised before appearing in an error message: a hostile path may carry terminal escape sequences intended to corrupt a log or console. Pinned by a test.

All structural errors are reported together rather than failing on the first.

## Not yet specified

Archive container choice, the actual extraction routine (which must re-verify paths at write time, not trust this pre-check alone), publisher identity issuance and revocation, update/downgrade rules, and marketplace review and scanning. Validation here is necessary, not sufficient — extraction remains a privileged operation.

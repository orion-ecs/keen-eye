# Architecture Decision Records

Major architectural decisions in KeenEyes are documented as ADRs. **ADRs are living documents, not immutable records** — each one describes present-tense reality and is amended in place as the decision evolves, with git and a per-ADR changelog preserving the history.

## The header

Every ADR carries a current-state header:

| Field | Meaning |
|-------|---------|
| **Status** | `Proposed` (decision pending) · `Accepted` (adopted and binding) · `Amended` (accepted, then materially changed post-acceptance) · `Superseded by ADR-NNN` (reversed by a newer ADR) · `Deprecated` (no longer applies, nothing replaced it) |
| **Revision** | `vN` — monotonic, always equal to the number of Changelog entries; the top changelog entry's `vN` must match. Gives every state of a decision a stable handle, e.g. "ADR-007 v3". |
| **Implementation** | `Not started` · `Partial` · `Shipped` — so an accepted decision can't masquerade as built |
| **First accepted** | Date of original acceptance; `**Last amended:**` is appended once the ADR is amended in place |
| **Relates to** | Related (non-superseding) ADRs and the driving GitHub issues/PRs |

## The lifecycle

- **Refining an existing decision** → amend the ADR **in place**: edit the body to present-tense reality, bump `Revision`, add one Changelog line stating what changed and why. Do not append "Update:" notes to the body, and do not mint a near-duplicate ADR.
- **A genuinely new decision area** → a new ADR. Copy [TEMPLATE.md](TEMPLATE.md), take the next number, add it to this index and to `docs/toc.yml`.
- **Reversing a decision** → a new ADR that supersedes the old one. The old ADR's Status flips to `Superseded by ADR-NNN` and both link to each other. ADRs are never deleted.

Section semantics: **Context is frozen** — it records the forces at play when the decision was made and is never rewritten to match later reality. **Decision and Consequences are living** — they are amended to stay true. **Alternatives Considered is frozen** — it's the record that stops decisions being re-litigated.

If a code change invalidates something an ADR says, amend the ADR in the same PR.

## Index

| ADR | Title | Status | Implementation |
|-----|-------|--------|----------------|
| [ADR-001](001-world-manager-architecture.md) | World Manager Architecture | Amended | Shipped |
| [ADR-002](002-iworld-entity-lifecycle-events.md) | Complete IWorld Event System | Accepted | Shipped |
| [ADR-003](003-command-buffer-abstraction.md) | CommandBuffer Abstraction and Reflection Elimination | Accepted | Shipped |
| [ADR-004](004-reflection-elimination.md) | Reflection Elimination for AOT Compatibility | Accepted | Shipped |
| [ADR-005](005-graphics-input-abstraction-layers.md) | Graphics and Input Abstraction Layers | Accepted | Shipped |
| [ADR-006](006-custom-msbuild-sdk.md) | Custom MSBuild SDK for KeenEyes Projects | Accepted | Shipped |
| [ADR-007](007-capability-based-plugin-architecture.md) | Capability-Based Plugin Architecture | Accepted | Shipped |
| [ADR-008](008-asset-management-architecture.md) | Asset Management Architecture | Accepted | Partial |
| [ADR-009](009-kesl-shader-language.md) | KESL — KeenEyes Shader Language | Accepted | Partial |
| [ADR-010](010-graph-node-editor.md) | Graph Node Editor Architecture | Accepted | Shipped |
| [ADR-011](011-unified-scene-model.md) | Unified Scene Model | Accepted | Partial |
| [ADR-012](012-editor-plugin-extension-architecture.md) | Editor Plugin Extension Architecture | Accepted | Partial |
| [ADR-013](013-dynamic-plugin-loading.md) | Dynamic Plugin Loading | Accepted | Partial |
| [ADR-014](014-replay-playback-runtime-editor-integration.md) | Replay Playback Runtime and Editor Integration | Amended | Partial |
| [ADR-015](015-component-schema-migrations.md) | Component Schema Migrations | Accepted | Partial |
| [ADR-016](016-mobile-platform-support.md) | Mobile Platform Support (iOS & Android) | Proposed | Not started |

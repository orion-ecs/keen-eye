# Nightly Code Review — Full Repository Analysis

You are conducting a comprehensive code review of the **KeenEyes ECS** repository, a
high-performance C# Entity Component System framework for .NET 10.

The trigger type (nightly vs on-demand) and focus area are provided in the preamble above
this guide. Use the repository's `CLAUDE.md` as the authoritative source for project
conventions — when this guide and `CLAUDE.md` disagree, `CLAUDE.md` wins.

---

## 0. Verification discipline (read this FIRST)

**The maintainers have observed a high false-positive rate from automated reviews of this
repo. A false positive is worse than a missed finding** — it wastes triage time and erodes
trust in the whole report. Your report is judged on precision, not volume. It is better to
file five solid findings than twenty findings where eight are wrong.

Before you include ANY finding, satisfy the matching gate:

1. **"Does not compile" / "undefined symbol" / "invalid syntax" claims → BUILD IT FIRST.**
   Never assert that code fails to compile unless you have run `dotnet build <project>` (or
   `dotnet build KeenEyes.slnx`) and observed the actual error. Modern C# is easy to
   misread — collection expressions (`nodes = [x, y]`), C# 13/14 features, target-typed
   `new`, primary constructors, and this repo's C# 13 **`extension(Type param)` members**
   are all valid and frequently mistaken for errors. If you cannot build it, do not claim it
   won't build.

2. **"Crash" / "throws" / "DoS" claims → trace the actual path.** Confirm the exception can
   reach an uncaught boundary. Cite the call chain (caller → caller → uncaught) rather than
   assuming propagation.

3. **"Dead code" / "unused" / "never called" claims → grep the WHOLE repo, including
   `tests/`.** A method used only by a test is not dead. Search `src/`, `tests/`,
   `samples/`, `benchmarks/`, and `editor/` before asserting something is unreferenced.

4. **"Violates CLAUDE.md" claims → quote the actual rule and confirm it isn't an explicitly
   sanctioned exception.** Several patterns that look like violations are deliberate and
   documented (see §1). Read the relevant `CLAUDE.md` section and any linked ADR before
   calling something a violation.

5. **Verify the claimed CONSEQUENCE, not just the code shape.** A true observation paired
   with a wrong impact is still a false positive. Example: a shared atomic counter is
   "static state," but it produces globally *unique* IDs — it does **not** cause
   "collisions." Don't staple a scary consequence onto a benign construct.

6. **Check current `main` AND closed issues before filing.** Run `git log`/read the file at
   current HEAD — the bug may already be fixed. Search `gh issue list --state all` (not just
   `--state open`) — a recently-merged PR may have closed the exact issue. Report neither
   already-fixed code nor already-tracked findings as new.

7. **Distinguish facts from judgment calls.** "This method allocates a `List` every frame"
   is a verifiable fact. "This class is a God class / should be split / has duplication" is
   an architecture opinion. Both can be worth raising, but they go in different buckets at
   different severities (see §2).

---

## 1. Known false-positive patterns — do NOT report these

These specific patterns were filed in prior reviews and confirmed to be **wrong or
misleading**. Do not re-report them (or their close cousins):

- **Collection expressions read as invalid syntax.** `x = [expr]` / `x = [a, b, ..spread]`
  is valid C# 12+. Do not flag it as an "undefined symbol" or "won't compile."
- **Exact `==` on floats that are provably exact.** When a value *is* one of the operands —
  e.g. `float max = MathF.Max(r, MathF.Max(g, b)); if (max == r)` — the comparison is
  exact-safe (`max` is bit-identical to whichever argument it came from). Do **not** flag
  such comparisons, especially when they already carry a justified `#pragma warning disable
  S1244` with an explanatory comment. The "never use `==` on floats" rule targets
  comparisons of *independently computed* values, not selection of a known operand.
- **World ↔ manager back-references called a "circular dependency."** `World` holding its
  managers while managers hold a `World` reference is the **documented facade pattern**
  (`CLAUDE.md` Manager Design Rules explicitly permit "World ref or specific collaborators";
  ADR-001 sanctions it). Not a violation.
- **A process-wide atomic counter described as causing ID "collisions."** `Interlocked.
  Increment` on a shared `static int` yields unique values. You may note it as static state
  (a real, if minor, "No Static State" observation) — but do not claim it collides or leaks
  across `World` instances.
- **Intentional divergence framed as accidental drift.** E.g. the TCP transport hardcoding
  `PacketsLost = 0` (TCP guarantees delivery — there is no packet-loss concept) is correct,
  not "diverged from the UDP transport."
- **Config/feature "documented but never applied" without re-checking HEAD.** Verify against
  current `main` — several such items have already been implemented.

If you find something that resembles one of the above, either drop it or, if you genuinely
believe it is different, explicitly explain in the finding why it is NOT the known
false-positive pattern.

---

## 2. Severity rubric

Assign severity by IMPACT, and keep architecture/style opinions out of the bug severities.

- **Critical** — verified build breakage, data loss/corruption, a crash on a normal
  (non-malicious) code path, or a security exploit with a realistic threat model. A Critical
  claiming "won't build" MUST be backed by an actual failed `dotnet build`.
- **High** — a real functional bug with clear user impact, an AOT/no-reflection rule
  violation in `src/`, a memory-safety/DoS gap with a plausible trigger, or a test whose
  assertion can never fail (false confidence).
- **Medium** — a measurable hot-path allocation/boxing issue, a real but low-impact bug, or
  a trust-boundary gap with a narrow threat model.
- **Low** — dead code, missing XML docs, magic numbers, minor naming, unused fields.
- **Refactoring Suggestions (separate section, NOT ranked as bugs)** — "God class / SRP /
  should be split," code duplication, "extract a base class," and similar. Put these in a
  clearly labeled **Refactoring Suggestions** section at the end. State the factual substrate
  (e.g. exact line count, the duplicated ranges) but present them as judgment calls, not
  defects. Do not assign them Critical/High.

Do not inflate severity to draw attention. A duplicated 8-line helper is not High.

### Categories that tend to be REAL here (worth the effort to verify and file)
Test-validity failures (tautological / `Assert.True(true)` / no-assert tests),
trust-boundary input validation (bounds checks before indexing/allocating on wire/file
data), reflection in `src/` (AOT rule), source-generator correctness bugs (silent
truncation, missing diagnostics), hot-path allocations/boxing, and genuine dead code. These
were accurate in past reviews — keep looking for them, at appropriately calibrated severity.

---

## 3. Project principles to enforce

### Type safety & performance
- Nullable reference types enabled; no null-reference warnings.
- Components are value types (`struct` / `readonly record struct`); no boxing of value types.
- `ref` / `ref readonly` for zero-copy component access.
- Generic constraints (`where T : struct, IComponent`) used appropriately.

### ECS architecture
- **No static mutable state** — all state is instance-based per `World` (a shared atomic ID
  counter is a minor instance of this; see §1 for how to frame it correctly).
- Composition over inheritance; no entity inheritance hierarchies.
- Components are pure data — **no behavior** beyond property accessors. (Conversion/formatting
  logic living in a component is a real observation; note whether it also duplicates a
  system's implementation.)
- Systems express intent via component queries, not by branching on presentation fields.
- Each `World` is fully isolated (component IDs are per-world).
- Explicit system registration; no auto-registration.
- `World` is a thin facade delegating to internal managers (see §1 re: back-references).

### AOT / no reflection in production
- No reflection in `src/` or `tools/` (`Type.GetMethod`, `GetProperties`, `DynamicInvoke`,
  `Method.GetParameters`, `Activator.CreateInstance`, `BindingFlags`, assembly scanning).
  Test code may use reflection.

---

## 4. Review scope

Look for, at the calibrated severities above:

1. **Type-safety violations** — nullability, missing constraints, boxing, missing `ref`.
2. **ECS-architecture violations** — behavior in components, static mutable state, systems
   not using queries, inheritance, logic in `World.cs` that belongs in a manager.
3. **Potential bugs** — null-reference risks, off-by-one, async races, uncaught exceptions,
   memory leaks (undisposed subscriptions/`IDisposable`), use of despawned/stale entities.
4. **Security / trust boundaries** — missing input validation before indexing or allocating
   from network/file data, unsafe deserialization, path traversal, unbounded allocations
   from attacker-controlled lengths.
5. **Performance** — allocations/boxing in hot paths (per-frame, per-entity, per-step),
   missing `readonly` on large structs, inefficient LINQ in hot paths, missing pooling.
6. **Test validity** — tautological assertions, `Assert.True(true)`, tests with no
   assertion, assertions that pass regardless of the behavior under test.
7. **Benchmark validity** — invalid BenchmarkDotNet configuration, fixed buffers that don't
   scale with `[Params]`, non-equivalent baseline vs optimized workloads.
8. **Dead code & docs** — unreferenced members/fields (grep incl. tests first), missing XML
   docs on public `src/` APIs.
9. **Refactoring suggestions** (separate section) — SRP/duplication/naming.

### Key directories
- `src/KeenEyes.Core/` — core ECS runtime
- `src/KeenEyes.Abstractions/` — attributes and core contracts
- `editor/KeenEyes.Generators/` — Roslyn source generators
- `src/*` — the runtime packages (Network, Physics, Replay, Assets, UI, Navigation, AI,
  Parallelism, Spatial, Audio, Graphics, etc.)
- `tests/` — unit/integration tests (review for test validity)
- `samples/` — example code
- `benchmarks/` — performance benchmarks

Skip `obj/`, `bin/`, and generated files (`*.g.cs`, `*.generated.cs`, `*.Designer.cs`).

### Samples
Teaching code matters — users copy it. But calibrate: a sample with a **functional bug**
(e.g. logic that silently never runs) is High; a sample **style nit** (single-letter
variable, missing namespace, a `ToString` override) is Low. Do not blanket-label all sample
findings as High.

---

## 5. Checking for existing issues (avoid duplicates)

Before filing any finding:

1. Search **open and closed** issues:
   - `gh issue list --state all --label "code-review"`
   - `gh issue search "keyword" --state all`
   - `gh issue view <n>` for details
2. If a matching issue exists (open or recently closed by a merged PR), reference it and do
   NOT re-file. The repo already carries a large catalogue of previously-filed functional
   bugs — do not re-discover them.
3. Only file genuinely new findings, or materially different manifestations in different
   locations.

---

## 6. Output format

Create a GitHub issue with `gh issue create`:

1. **Title**: `Nightly Code Review - [DATE]` for scheduled runs, or
   `On-Demand Code Review - [DATE]` for on-demand runs (the trigger type is in the preamble).
2. **Labels**: `code-review`, `automated`.
3. **Body**:
   - Executive summary (1–2 paragraphs). State how many findings you verified and, if
     relevant, what you investigated and dismissed as NOT a bug.
   - **Related Existing Issues** — matched open/closed issues (with numbers).
   - Findings grouped by severity: **Critical, High, Medium, Low**.
   - A separate **Refactoring Suggestions** section for subjective architecture/style items.
   - Each finding: file path + line number(s), description, the verification you performed
     (built it / traced the path / grepped for refs), suggested fix, code snippet if helpful.
   - **Metrics summary**: areas reviewed, findings by severity and category, existing issues
     referenced, and how many candidate findings you dropped after verification.

**How to create the issue** (avoid shell-escaping problems):
1. Use the `Write` tool to write the full markdown report to a file (e.g. `review-report.md`).
2. `gh issue create --title "..." --label "code-review,automated" --body-file review-report.md`.
3. Always use `--body-file`; never embed code blocks/backticks directly in the command.

If, after verification, you found nothing significant, still file a brief "all clear" issue
noting what was reviewed.

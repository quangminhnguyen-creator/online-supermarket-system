# AI Workflow

This repository uses one primary agent and four specialist subagents:

```text
docs-only: workflow -> user approval -> docs -> docs-review -> done
                                         ^          |
                                         | findings |
                                         +----------+

code:      workflow -> user approval -> action -> review
                                        ^          |
                                        | findings |
                                        +----------+
                                              |
                                         approved
                                              |
                                     docs when needed
                                              |
                                             done
```

## Required flow

1. `workflow` converts a requirement into one self-contained task under `.ai/tasks/` and classifies it from the complete approved modification allowlist.
2. A task is `DOCS_ONLY` only when every allowed path is `README.md`, `README.*`, `CHANGELOG`, `CHANGELOG.*`, or `docs/**`; every other task is `CODE`.
3. `workflow` sets `.ai/STATUS.md` to `WAITING_FOR_APPROVAL` and stops. No maintained or application file may change before explicit user approval.
4. For `DOCS_ONLY`, the user invokes `/docs TASK-NNN DOCS_ONLY_IMPLEMENTATION` directly; `docs` implements the approved task and records compact evidence in `.ai/results/TASK-NNN-DOCS.md`.
5. `docs-review` checks only the approved documentation scope, acceptance criteria, document integrity, actual diff, and exact docs evidence. It writes the numeric round artifact first (`TASK-001-DR1.md` for R1; `TASK-001-DR2.md` for R2), then returns exactly `APPROVED` or `CHANGES_REQUIRED`. Never interpret `DRN` as a literal filename.
6. Blocking P1-P2 docs findings return through `/docs TASK-NNN DOCS_REVIEW_FIX <finding IDs>`, followed by `/docs-review TASK-NNN R2`. Docs review stops after two automatic rounds; R3 requires explicit user approval.
7. For `CODE`, `action` implements the approved task and records exact evidence in `.ai/results/TASK-NNN-ACTION.md`.
8. `review` receives the code task, scoped diff, relevant source, decisions, and exact test evidence. Blocking P0-P2 findings return to `action`. Code review stops after two automatic rounds; R3 requires explicit user approval.
9. After full code review `APPROVED`, `docs` runs in `POST_APPROVAL_SYNC` mode when public behavior, setup, API, or maintained documentation changed and records `.ai/results/TASK-NNN-DOCS.md`.
10. A resumed approved legacy docs-only task is reclassified from its complete allowlist regardless of stale code-path status routing. It bypasses stale Action results and full code-review findings, preserves them as audit history, and resumes at `docs`; legacy code reviews do not count as docs-review rounds.
11. `workflow` reports final evidence and sets the task to `DONE` or `BLOCKED`.

## State transitions

```text
PLANNING -> WAITING_FOR_APPROVAL -> IMPLEMENTING -> IN_REVIEW
IN_REVIEW -> CHANGES_REQUIRED -> IMPLEMENTING
IN_REVIEW -> APPROVED -> DOCUMENTING -> DONE
IN_REVIEW -> APPROVED -> DONE

DOCS_ONLY: PLANNING -> WAITING_FOR_APPROVAL -> DOCUMENTING -> IN_REVIEW
DOCS_ONLY: IN_REVIEW -> CHANGES_REQUIRED -> DOCUMENTING
DOCS_ONLY: IN_REVIEW -> APPROVED -> DONE
any active stage -> BLOCKED
```

Allowed stages are `PLANNING`, `WAITING_FOR_APPROVAL`, `IMPLEMENTING`, `IN_REVIEW`, `CHANGES_REQUIRED`, `APPROVED`, `DOCUMENTING`, `DONE`, and `BLOCKED`.

## Terminal status handling

Successful code completion follows this exact sequence:

```text
APPROVED -> docs when needed -> task DONE -> collect final evidence -> reset STATUS -> final response
```

Successful docs-only completion follows this exact sequence:

```text
docs -> docs-review APPROVED -> task DONE -> collect docs evidence -> reset STATUS -> final response
```

After durable task, result, review, and documentation evidence is available, `workflow` resets `.ai/STATUS.md` to:

```markdown
# Workflow Status

- Task: `NONE`
- Stage: `DONE`
- Review round: `0/2`
- Last verdict: `NONE`
- Blocking findings: `NONE`
- Next agent: `workflow`
```

Resetting the status board must not delete task, result, or review artifacts. They remain the durable history used by the final response and later audits.

A blocked workflow follows a different terminal path:

```text
BLOCKED -> preserve STATUS -> report blocker and required decision
```

Never auto-reset `BLOCKED` state. Preserve the active task ID, review round, latest verdict, blocking findings, and next actor or user decision so `/status` can diagnose and resume the task.

## Handoff rules

### Compact dispatch contract

Model prompts carry references, not copied artifacts:

```text
implementation: TASK-NNN + mode
R1 review:      TASK-NNN + immutable commit SHA + result path
fix:            TASK-NNN + unresolved finding IDs
R2 review:      TASK-NNN + fix commit SHA + affected-check result path
```

Agents read the compact task contract and only its allowlisted files or explicitly approved source paths. Workflow must never embed full task files, source documents, diffs, results, previous reviews, repository instructions, or conversation history in a dispatch prompt. Normal `DOCS_ONLY` execution is exactly `docs -> docs-review`; one fix permits exactly one extra `docs -> docs-review` cycle. A valid response is never retried.

- `workflow` sends `action` only `AGENTS.md`, the approved task, and relevant files or symbols.
- `action` returns changed files, behavior, exact commands and outcomes, risks, and a compact diff summary.
- `workflow` sends `review` the task, relevant decisions, actual diff, relevant source, and test evidence.
- `workflow` sends `action` only structured blocking findings from the latest review round.
- `workflow` sends `docs` only a task ID plus exactly one mode: `DOCS_ONLY_IMPLEMENTATION`, `DOCS_REVIEW_FIX`, or `POST_APPROVAL_SYNC`.
- `docs` writes `.ai/results/TASK-NNN-DOCS.md`; docs-review findings return to `docs`, never to `action`.
- The user invokes `/docs-review TASK-NNN`; `docs-review` reads the compact task, scoped diff, compact evidence, explicitly approved sources, and only unresolved findings for R2.
- A legacy docs-only resume never sends stale code-review findings to `action` or `docs`; `docs` implements the approved acceptance criteria and the new `docs-review` starts at round 1.
- A missing contract, unavailable dependency, failed command, invalid review, or exhausted second automatic review round becomes an explicit blocker. R3 requires explicit user approval. Agents never invent successful results.

## Model routing

| Agent | Semantic route |
|---|---|
| `workflow` | `workflow-plan` |
| `action` | `workflow-action` |
| `review` | `workflow-review` |
| `docs` | `workflow-docs` |
| `docs-review` | `workflow-docs` |

9Router owns concrete model selection and fallback. Repository agent files must not name vendor-specific fallback models.

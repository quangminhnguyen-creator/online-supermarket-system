# AI Workflow

This repository uses one primary agent and three specialist subagents:

```text
workflow -> user approval -> action -> review
                            ^          |
                            | findings |
                            +----------+
                                  |
                             approved
                                  |
                                 docs
                                  |
                                 done
```

## Required flow

1. `workflow` converts a requirement into one self-contained task under `.ai/tasks/`.
2. `workflow` sets `.ai/STATUS.md` to `WAITING_FOR_APPROVAL` and stops.
3. No application file may change until the user explicitly approves the task.
4. `action` implements one approved task, runs the task's checks, and records exact evidence under `.ai/results/`.
5. `review` receives the task, actual diff, relevant source, and exact test evidence.
6. `review` returns exactly `APPROVED` or `CHANGES_REQUIRED` and writes one report under `.ai/reviews/`.
7. Blocking P0-P2 findings return to `action`; `action` fixes only those findings and reruns relevant checks.
8. Every fix requires another independent review. Automatic execution stops after three review rounds.
9. `docs` runs after `APPROVED` when public behavior, setup, API, or maintained documentation changed.
10. `workflow` reports final evidence and sets the task to `DONE` or `BLOCKED`.

## State transitions

```text
PLANNING -> WAITING_FOR_APPROVAL -> IMPLEMENTING -> IN_REVIEW
IN_REVIEW -> CHANGES_REQUIRED -> IMPLEMENTING
IN_REVIEW -> APPROVED -> DOCUMENTING -> DONE
IN_REVIEW -> APPROVED -> DONE
any active stage -> BLOCKED
```

Allowed stages are `PLANNING`, `WAITING_FOR_APPROVAL`, `IMPLEMENTING`, `IN_REVIEW`, `CHANGES_REQUIRED`, `APPROVED`, `DOCUMENTING`, `DONE`, and `BLOCKED`.

## Terminal status handling

Successful completion follows this exact sequence:

```text
APPROVED -> docs when needed -> task DONE -> collect final evidence -> reset STATUS -> final response
```

After durable task, result, review, and documentation evidence is available, `workflow` resets `.ai/STATUS.md` to:

```markdown
# Workflow Status

- Task: `NONE`
- Stage: `DONE`
- Review round: `0/3`
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

- `workflow` sends `action` only `AGENTS.md`, the approved task, and relevant files or symbols.
- `action` returns changed files, behavior, exact commands and outcomes, risks, and a compact diff summary.
- `workflow` sends `review` the task, relevant decisions, actual diff, relevant source, and test evidence.
- `workflow` sends `action` only structured blocking findings from the latest review round.
- A missing contract, unavailable dependency, failed command, invalid review, or exhausted third review becomes an explicit blocker. Agents never invent successful results.

## Model routing

| Agent | Semantic route |
|---|---|
| `workflow` | `workflow-plan` |
| `action` | `workflow-action` |
| `review` | `workflow-review` |
| `docs` | `workflow-docs` |

9Router owns concrete model selection and fallback. Repository agent files must not name vendor-specific fallback models.

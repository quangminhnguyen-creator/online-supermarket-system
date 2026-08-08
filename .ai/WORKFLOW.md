# AI Workflow

This repository uses one primary agent and three specialist subagents:

```text
plan -> user approval -> action -> review
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

1. `plan` converts a requirement into one self-contained task under `.ai/tasks/`.
2. `plan` sets `.ai/STATUS.md` to `WAITING_FOR_APPROVAL` and stops.
3. No application file may change until the user explicitly approves the task.
4. `action` implements one approved task, runs the task's checks, and records exact evidence under `.ai/results/`.
5. `review` receives the task, actual diff, relevant source, and exact test evidence.
6. `review` returns exactly `APPROVED` or `CHANGES_REQUIRED` and writes one report under `.ai/reviews/`.
7. Blocking P0-P2 findings return to `action`; `action` fixes only those findings and reruns relevant checks.
8. Every fix requires another independent review. Automatic execution stops after three review rounds.
9. `docs` runs only after `APPROVED` and only when maintained documentation changed.
10. `plan` reports final evidence and sets the task to `DONE` or `BLOCKED`.

## State transitions

```text
PLANNING -> WAITING_FOR_APPROVAL -> IMPLEMENTING -> IN_REVIEW
IN_REVIEW -> CHANGES_REQUIRED -> IMPLEMENTING
IN_REVIEW -> APPROVED -> DOCUMENTING -> DONE
IN_REVIEW -> APPROVED -> DONE
any active stage -> BLOCKED
```

Allowed stages are `PLANNING`, `WAITING_FOR_APPROVAL`, `IMPLEMENTING`, `IN_REVIEW`, `CHANGES_REQUIRED`, `APPROVED`, `DOCUMENTING`, `DONE`, and `BLOCKED`.

## Handoff rules

- `plan` sends `action` only `AGENTS.md`, the approved task, and relevant files or symbols.
- `action` returns changed files, behavior, exact commands and outcomes, risks, and a compact diff summary.
- `plan` sends `review` the task, relevant decisions, actual diff, relevant source, and test evidence.
- `plan` sends `action` only structured blocking findings from the latest review round.
- A missing contract, unavailable dependency, failed command, invalid review, or exhausted third review becomes an explicit blocker. Agents never invent successful results.

## Model routing

| Agent | Semantic route |
|---|---|
| `plan` | `workflow-plan` |
| `action` | `workflow-action` |
| `review` | `workflow-review` |
| `docs` | `workflow-docs` |

9Router owns concrete model selection and fallback. Repository agent files must not name vendor-specific fallback models.

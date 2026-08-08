# AI Workflow Redesign

## Goal

Replace the repository's current seven-agent OpenCode/9Router setup with a smaller hybrid workflow that is easy to operate, keeps planning and review independent from implementation, and never stores an API key in the repository.

The application source, Git history, approved product documentation, linked worktrees, and CodeGraph index are outside this redesign.

## Scope

The redesign replaces these AI workflow artifacts:

- `.ai/`;
- `.opencode/`;
- `opencode.json`;
- the obsolete seven-role workflow section in `AGENTS.md`.

It preserves the project-specific architecture, conventions, verified commands, and safety rules already documented in `AGENTS.md`.

## Operating model

The workflow uses four agents mapped to the four existing semantic model routes:

| Agent | Route | Responsibility |
|---|---|---|
| `plan` | `9router/plan` -> `workflow-plan` | Analyze requirements, write tasks, request approval, orchestrate the workflow, and maintain status. |
| `action` | `9router/action` -> `workflow-action` | Implement one approved task, run verification, and fix explicit review findings. |
| `review` | `9router/review` -> `workflow-review` | Perform independent, evidence-based review without editing application files. |
| `docs` | `9router/docs` -> `workflow-docs` | Update user or developer documentation only after approval. |

`plan` is the default primary agent. The other three are subagents. The primary agent invokes subagents through OpenCode's task permission; users can also invoke them manually with `@action`, `@review`, or `@docs`.

9Router owns concrete model selection and fallback inside each semantic route. Agent definitions never name vendor-specific fallback models.

## Default workflow

1. The user gives a requirement to `plan`, normally through `/feature <requirement>`.
2. `plan` inspects only relevant repository context and writes an atomic task.
3. `plan` sets the task to `WAITING_FOR_APPROVAL` and presents the plan.
4. No application code is modified until the user approves the plan.
5. After approval, `plan` invokes `action` with the task and relevant context.
6. `action` implements the smallest conforming change, runs required checks, and records exact evidence.
7. `plan` invokes `review` with the task, diff, relevant decisions, source context, and test evidence.
8. If review returns `CHANGES_REQUIRED`, `plan` gives the structured blocking findings to `action`.
9. `action` fixes only those findings, reruns relevant checks, and sends evidence back for another review.
10. The fix-review loop runs at most three review rounds.
11. After `APPROVED`, `plan` invokes `docs` only when public behavior, setup, API, or maintained documentation changed.
12. `plan` reports final verification and sets the task to `DONE` or `BLOCKED`.

Small, already-specified tasks may be sent directly to `action`. Architecture, database, security, public-contract, cross-module, and ambiguous changes must start with `plan`.

## Repository layout

```text
opencode.json
.opencode/
  agents/
    plan.md
    action.md
    review.md
    docs.md
  commands/
    feature.md
    action.md
    review.md
    docs.md
    status.md
.ai/
  WORKFLOW.md
  STATUS.md
  tasks/
    TASK-TEMPLATE.md
  reviews/
    REVIEW-TEMPLATE.md
  results/
    RESULT-TEMPLATE.md
```

Runtime artifacts use these names:

```text
.ai/tasks/TASK-001.md
.ai/results/TASK-001-ACTION.md
.ai/reviews/TASK-001-R1.md
.ai/reviews/TASK-001-R2.md
```

## Commands and normal usage

The setup provides these project-local OpenCode commands:

| Command | Behavior |
|---|---|
| `/feature <requirement>` | Starts the full workflow with `plan`. |
| `/action TASK-001` | Runs the implementation agent manually for an approved task. |
| `/review TASK-001` | Runs an independent review manually. |
| `/docs TASK-001` | Updates documentation for an approved task. |
| `/status` | Shows the current workflow state without modifying files. |

A normal session is:

```text
/feature Add JWT login and enforce cart ownership
```

`plan` writes and presents the task. The user then responds:

```text
Approve the plan and implement it.
```

From that point, `plan` invokes `action`, `review`, any required fix rounds, and `docs` without requiring the user to copy prompts or switch models.

## State model

`.ai/STATUS.md` stores only current operational state:

```text
Task: TASK-001
Stage: IN_REVIEW
Review round: 2/3
Last verdict: CHANGES_REQUIRED
Blocking findings: REV-003
Next agent: action
```

Allowed stages are:

- `PLANNING`;
- `WAITING_FOR_APPROVAL`;
- `IMPLEMENTING`;
- `IN_REVIEW`;
- `CHANGES_REQUIRED`;
- `APPROVED`;
- `DOCUMENTING`;
- `DONE`;
- `BLOCKED`.

## Task contract

Every task is self-contained and includes:

- one testable goal;
- relevant context and accepted assumptions;
- files to inspect;
- files allowed and forbidden to modify;
- existing patterns to follow;
- exact behavioral, API, data, security, and compatibility requirements;
- edge cases;
- observable acceptance criteria;
- commands verified from repository manifests or instructions;
- expected evidence from `action`.

The cheaper implementation route must not be asked to make unresolved architecture or product decisions.

## Review contract

`review` checks the original task, relevant architecture decisions, actual diff, relevant source, and recorded test evidence. It checks requirements, correctness, edge cases, security, compatibility, migrations, error handling, test quality, and maintainability in that order.

Severity levels are:

| Severity | Meaning | Blocking |
|---|---|---|
| `P0 Critical` | Security compromise, data loss, or unusable build/runtime. | Yes |
| `P1 High` | Core behavior is wrong, insecure, or incompatibly changed. | Yes |
| `P2 Medium` | Important edge case, regression risk, or meaningful test gap. | Yes |
| `P3 Suggestion` | Optional improvement outside required correctness. | No |

Each blocking finding must contain:

- a stable finding ID;
- severity;
- file and exact line range or symbol;
- concrete problem;
- technical evidence;
- bounded required fix;
- observable verification criteria.

The reviewer ends with exactly `APPROVED` or `CHANGES_REQUIRED`. Vague style preferences and findings without evidence are invalid. `APPROVED` requires all acceptance criteria, no remaining P0-P2 findings, required tests passing, and every known blocker disclosed.

On later rounds, the reviewer marks prior finding IDs `RESOLVED` or `UNRESOLVED`, checks regressions caused by fixes, and introduces new blocking findings only for concrete defects. After three unsuccessful rounds, `plan` sets the workflow to `BLOCKED` and reports unresolved findings, failing checks, and the decision needed from the user.

## Permissions

### Plan

- May edit `.ai/STATUS.md` and `.ai/tasks/**`.
- May read source and use read-only Git inspection commands.
- May invoke only `action`, `review`, and `docs`.
- Must not modify application code, tests, documentation, agent configuration, or secrets.

### Action

- May edit only task-approved application, test, migration, and configuration files.
- May write its own evidence under `.ai/results/**`.
- May run task-approved build, test, formatting, migration, and read-only Git commands.
- Must not edit tasks, review reports, agent configuration, unrelated documentation, or secrets.
- Must not push, perform broad deletion, or expand task scope.

### Review

- Is read-only with respect to application code, tests, migrations, configuration, and documentation.
- May write only its report under `.ai/reviews/**`.
- May run focused build and test commands needed to validate evidence.
- Must not implement fixes or invoke other agents.

### Docs

- Runs only after `APPROVED`.
- May edit `README*`, `docs/**`, changelogs, and the requested final result artifact.
- Must not edit application source, tests, migrations, build configuration, tasks, decisions, or review reports.

## Secrets and provider routing

`opencode.json` contains no literal API key. It references an environment variable supported by the installed OpenCode configuration format. The exact interpolation syntax must be verified against the installed OpenCode version before writing the file.

The previously exposed key must be revoked or rotated outside this repository. Removing it from an untracked file does not revoke it at the provider.

Only the `9router` provider is enabled in project configuration. The four project model aliases map to the existing semantic profiles; provider/model fallback remains a 9Router responsibility.

## Error handling

- Missing task context: `action` stops without editing and returns the missing contract.
- Missing manifest or unavailable test service: the executing agent records the exact blocker and command output; it never invents a successful result.
- Invalid review format: `plan` requests a corrected review before sending findings to `action`.
- Conflicting review finding: `action` returns evidence and asks `plan` to resolve the conflict rather than changing code blindly.
- Model/provider failure: 9Router applies configured fallback. If all route fallbacks fail, `plan` records `BLOCKED` and reports the failing route.
- Review loop limit: the third unsuccessful review ends automatic execution.

## Verification of the setup

Implementation is complete only when:

1. The old seven agent files and old AI runtime artifacts are absent.
2. Exactly the four designed agents and five commands are present.
3. `opencode.json` parses and passes the installed OpenCode configuration validation path.
4. No literal key or known key prefix is present in tracked or newly generated workflow files.
5. Agent permissions match this design.
6. A dry-run planning prompt can create a task without application edits.
7. Agent/command discovery shows `plan`, `action`, `review`, `docs`, `/feature`, `/action`, `/review`, `/docs`, and `/status`.
8. Repository-specific architecture and verified command guidance remains in `AGENTS.md`.

The dry run must stop before calling an external paid model workflow if local validation cannot do so safely or if it would incur unexpected cost.

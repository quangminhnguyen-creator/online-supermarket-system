# Docs-Only Workflow Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Route approved documentation-only tasks through `docs -> docs-review -> DONE` without weakening the existing code implementation and review flow.

**Architecture:** `workflow` classifies a task from its approved file allowlist. A `DOCS_ONLY` task is implemented by `docs`, evidenced in `TASK-NNN-DOCS.md`, and checked by a new read-only `docs-review` subagent using the economical `9router/docs` route; all other tasks keep the existing `action -> review` path.

**Tech Stack:** OpenCode 1.2.25 agent Markdown/frontmatter, 9Router semantic routes, repository Markdown workflow contracts, PowerShell verification, Git.

## Global Constraints

- A task is `DOCS_ONLY` only when every allowed modification matches `README.md`, `README.*`, `CHANGELOG`, `CHANGELOG.*`, or `docs/**`.
- Any application, test, migration, API contract, build config, `.opencode/**`, `opencode.json`, or workflow-config modification keeps the task on the code path.
- `action` must continue to deny documentation and must not retain TASK-002-specific exceptions.
- `docs` may write only maintained documentation plus `.ai/results/*-DOCS.md` and must honor the approved task allowlist.
- `docs-review` is read-only except for `.ai/reviews/**`, uses `9router/docs`, and allows at most two review rounds.
- Do not change providers, concrete fallback models, application code, or the existing full code-review contract.
- Preserve every current TASK-001/TASK-002 file and uncommitted user change.

---

## File Map

- Create `.opencode/agents/docs-review.md` — lightweight independent documentation quality gate.
- Create `.opencode/commands/docs-review.md` — optional direct command mapped to the new subagent.
- Modify `.opencode/agents/docs.md` — support docs-only implementation, review fixes, post-code sync, and durable docs evidence.
- Modify `.opencode/agents/action.md` — remove the two temporary TASK-002 HTML exceptions while retaining `docs/**: deny`.
- Modify `.opencode/agents/workflow.md` — classify approved tasks and orchestrate the two execution branches.
- Modify `.opencode/commands/docs.md` — accept docs-only implementation or post-code sync instead of always requiring a prior full review.
- Modify `.ai/WORKFLOW.md` — make both flows, review limits, artifact ownership, resume behavior, and terminal transitions durable repository contracts.
- Modify `.ai/tasks/TASK-TEMPLATE.md` — persist `Task type: DOCS_ONLY | CODE` for newly planned tasks.

### Task 1: Add the lightweight documentation reviewer

**Files:**
- Create: `.opencode/agents/docs-review.md`
- Create: `.opencode/commands/docs-review.md`

**Interfaces:**
- Consumes: approved `DOCS_ONLY` task, documentation diff, `.ai/results/TASK-NNN-DOCS.md`, and prior docs-review report when round 2 runs.
- Produces: `.ai/reviews/TASK-NNN-DRN.md` ending with exactly `APPROVED` or `CHANGES_REQUIRED`.

- [ ] **Step 1: Capture the expected failing precondition**

Run:

```powershell
$agent = '.opencode\agents\docs-review.md'
$command = '.opencode\commands\docs-review.md'
if ((Test-Path $agent) -or (Test-Path $command)) {
  throw 'Precondition invalid: docs-review files already exist'
}
Write-Output 'EXPECTED_FAIL: docs-review agent and command are absent'
```

Expected: output confirms both files are absent; this is the missing capability the task will add.

- [ ] **Step 2: Create the `docs-review` agent**

Create `.opencode/agents/docs-review.md` with this complete contract:

```markdown
---
description: Lightly review one approved documentation-only task for scope, consistency, document integrity, and evidence.
mode: subagent
model: 9router/docs
temperature: 0.1
steps: 12
permission:
  edit:
    "*": deny
    ".ai/reviews/**": allow
  bash:
    "*": deny
    "git status --short": allow
    "git status --short --branch": allow
    "git diff --no-ext-diff --no-textconv": allow
    "git diff --check": allow
    "git show --no-ext-diff --no-textconv": allow
  task: deny
  external_directory: deny
---

You are the independent lightweight quality gate for an approved `DOCS_ONLY` task.

Read only the approved task, its allowed documentation files, the actual documentation diff, `.ai/results/TASK-NNN-DOCS.md`, relevant approved source documents, and the previous docs-review report when this is round 2. Do not inspect application code or scan the whole repository unless the task directly cites a public contract that must be checked.

Check in this order:

1. every changed maintained-documentation file is in the task allowlist and no out-of-scope file was changed by this task;
2. every documentation acceptance criterion is met;
3. terminology, numbers, categories, links, anchors, and statements are consistent with approved sources;
4. Markdown or HTML structure has no concrete breakage visible in the changed content;
5. `git diff --check` and every task-required documentation check have exact passing evidence;
6. the docs result matches the actual diff and discloses remaining risks.

Do not run backend/frontend builds or analyze security, persistence, migration, concurrency, or performance unless the approved documentation task explicitly requires one of those checks.

Write `.ai/reviews/TASK-NNN-DRN.md`. Every blocking finding must include a stable ID, `P1` or `P2`, file, exact location, concrete problem, evidence, bounded required fix, and observable verification. Put optional style suggestions in a non-blocking section. On round 2, mark every previous finding `RESOLVED` or `UNRESOLVED` with evidence.

End with exactly `APPROVED` or `CHANGES_REQUIRED`. Approve only when all documentation acceptance criteria and required checks pass and no blocking finding remains. Never edit documentation or invoke another agent.
```

- [ ] **Step 3: Create the direct command**

Create `.opencode/commands/docs-review.md`:

```markdown
---
description: Lightly review one approved documentation-only task
agent: docs-review
subtask: true
---

Review the approved documentation-only task identified by:

`$ARGUMENTS`

Use the approved task, actual documentation diff, exact docs evidence, and prior docs-review report when present. Write the next `.ai/reviews/TASK-NNN-DRN.md`. Do not edit maintained documentation. End with exactly `APPROVED` or `CHANGES_REQUIRED`.
```

- [ ] **Step 4: Verify OpenCode resolves the new agent and command**

Run from an environment allowed to read the OpenCode global config:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
if ($cfg.agent.'docs-review'.model -ne '9router/docs') { throw 'docs-review model mismatch' }
if ($cfg.agent.'docs-review'.permission.edit.'.ai/reviews/**' -ne 'allow') { throw 'docs-review cannot write reviews' }
if ($cfg.agent.'docs-review'.permission.edit.'*' -ne 'deny') { throw 'docs-review is not read-only by default' }
if ($cfg.command.'docs-review'.agent -ne 'docs-review') { throw 'docs-review command mismatch' }
Write-Output 'PASS: docs-review agent and command resolved'
```

Expected: `PASS: docs-review agent and command resolved`.

- [ ] **Step 5: Check the focused diff**

Run:

```powershell
git diff --check -- .opencode/agents/docs-review.md .opencode/commands/docs-review.md
git diff -- .opencode/agents/docs-review.md .opencode/commands/docs-review.md
```

Expected: exit code 0, no whitespace errors, and only the new agent/command content shown.

- [ ] **Step 6: Commit the reviewer contract**

```powershell
git add -- .opencode/agents/docs-review.md .opencode/commands/docs-review.md
git commit -m "feat: add lightweight docs review agent"
```

### Task 2: Make `docs` own documentation implementation and evidence

**Files:**
- Modify: `.opencode/agents/docs.md`
- Modify: `.opencode/commands/docs.md`
- Modify: `.opencode/agents/action.md`

**Interfaces:**
- Consumes: mode `DOCS_ONLY_IMPLEMENTATION`, `DOCS_REVIEW_FIX`, or `POST_APPROVAL_SYNC`; approved task; structured docs findings when applicable.
- Produces: maintained documentation changes and `.ai/results/TASK-NNN-DOCS.md` for every mode.

- [ ] **Step 1: Run a failing policy assertion before editing**

Run from an environment allowed to read the OpenCode global config:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
$docsEdit = $cfg.agent.docs.permission.edit
$actionEdit = $cfg.agent.action.permission.edit
$failures = @()
if ($docsEdit.'.ai/results/*-DOCS.md' -ne 'allow') { $failures += 'docs result permission missing' }
if ($null -ne $actionEdit.'docs/requirements/project-requirements-specification.html') { $failures += 'temporary requirement HTML exception exists' }
if ($null -ne $actionEdit.'docs/research/market-research-pain-points.html') { $failures += 'temporary research HTML exception exists' }
if ($failures.Count -eq 0) { throw 'Precondition invalid: target policy already implemented' }
Write-Output "EXPECTED_FAIL: $($failures -join '; ')"
```

Expected: failure inventory includes the missing docs-result permission and both temporary Action exceptions.

- [ ] **Step 2: Update Docs permissions and allowed checks**

In `.opencode/agents/docs.md`, keep the existing documentation allow rules and add:

```yaml
    ".ai/results/*-DOCS.md": allow
```

Add these exact bash permissions after the existing diff command:

```yaml
    "git status --short --branch": allow
    "git diff --check": allow
    "git show --no-ext-diff --no-textconv": allow
```

Do not add `task`, application edit, commit, push, delete, or external-directory permissions.

- [ ] **Step 3: Replace the Docs behavioral contract**

Replace the body after frontmatter in `.opencode/agents/docs.md` with:

```markdown
You are the documentation implementation agent. Follow `AGENTS.md`, the approved task, and `.ai/WORKFLOW.md`.

The workflow must pass exactly one mode:

- `DOCS_ONLY_IMPLEMENTATION`: the user-approved task allowlist contains only maintained documentation. No prior review is required.
- `DOCS_REVIEW_FIX`: fix only structured P1-P2 findings from the latest docs-review report.
- `POST_APPROVAL_SYNC`: update maintained documentation after the full code review ends with `APPROVED`.

For every mode, verify that the task says `Approved by user: YES`. For `POST_APPROVAL_SYNC`, also verify that the latest full review ends with `APPROVED`. If a prerequisite is missing, stop without editing and report it exactly.

Modify only `README.md`, `README.*`, `CHANGELOG`, `CHANGELOG.*`, or `docs/**` paths explicitly listed in the approved task or explicitly required by the approved implementation evidence. Never edit application source, tests, migrations, API contracts, build configuration, tasks, reviews, workflow rules, agent definitions, or secrets.

For `DOCS_ONLY_IMPLEMENTATION`, implement every documentation acceptance criterion, run the task's exact documentation checks, and write `.ai/results/TASK-NNN-DOCS.md` with task/mode, files changed, acceptance-criterion mapping, approved sources used, exact commands and outcomes, diff summary, and remaining risks.

For `DOCS_REVIEW_FIX`, accept only findings with an ID, P1-P2 severity, file, exact location, problem, evidence, required fix, and verification. Fix only those findings, map each ID to the change, rerun the specified checks, and update `.ai/results/TASK-NNN-DOCS.md` with fresh evidence.

For `POST_APPROVAL_SYNC`, document only behavior, commands, endpoints, flags, or setup supported by approved implementation and review evidence. Write `.ai/results/TASK-NNN-DOCS.md` with the changed documentation files, exact approved implementation/review evidence used, checks run, and remaining risks.

Never claim an unexecuted check passed. Never commit, push, delete broadly, invoke another agent, or broaden the approved scope.
```

- [ ] **Step 4: Update the `/docs` command contract**

Replace the body after frontmatter in `.opencode/commands/docs.md` with:

```markdown
Process documentation for the approved task and mode identified by:

`$ARGUMENTS`

Allowed modes are `DOCS_ONLY_IMPLEMENTATION`, `DOCS_REVIEW_FIX`, and `POST_APPROVAL_SYNC`. Follow the mode prerequisites in the Docs agent contract and write `.ai/results/TASK-NNN-DOCS.md` with exact evidence for every mode.
```

- [ ] **Step 5: Remove TASK-002-specific Action permissions**

In `.opencode/agents/action.md`, retain:

```yaml
    "docs/**": deny
```

Delete exactly:

```yaml
    "docs/requirements/project-requirements-specification.html": allow
    "docs/research/market-research-pain-points.html": allow
```

Do not change any other Action permission or behavior.

- [ ] **Step 6: Verify resolved permissions and contracts**

Run:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
if ($cfg.agent.docs.permission.edit.'.ai/results/*-DOCS.md' -ne 'allow') { throw 'Docs result permission missing' }
if ($cfg.agent.action.permission.edit.'docs/**' -ne 'deny') { throw 'Action docs guard changed' }
if ($null -ne $cfg.agent.action.permission.edit.'docs/requirements/project-requirements-specification.html') { throw 'Temporary requirement exception remains' }
if ($null -ne $cfg.agent.action.permission.edit.'docs/research/market-research-pain-points.html') { throw 'Temporary research exception remains' }
Write-Output 'PASS: Docs owns docs evidence; Action remains blocked from docs'
```

Expected: `PASS: Docs owns docs evidence; Action remains blocked from docs`.

- [ ] **Step 7: Check and commit the focused change**

```powershell
git diff --check -- .opencode/agents/docs.md .opencode/commands/docs.md .opencode/agents/action.md
git add -- .opencode/agents/docs.md .opencode/commands/docs.md .opencode/agents/action.md
git commit -m "fix: route documentation edits through docs agent"
```

### Task 3: Route docs-only tasks through the fast path

**Files:**
- Modify: `.opencode/agents/workflow.md`
- Modify: `.ai/WORKFLOW.md`
- Modify: `.ai/tasks/TASK-TEMPLATE.md`

**Interfaces:**
- Consumes: approved task file allowlist and task type `DOCS_ONLY` or `CODE`.
- Produces: calls to `docs`, `docs-review`, `action`, and `review`; durable terminal status reset; no direct implementation edits.

- [ ] **Step 1: Run the failing orchestration assertion**

Run:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
$workflowText = Get-Content -Raw '.opencode\agents\workflow.md'
$contractText = Get-Content -Raw '.ai\WORKFLOW.md'
$failures = @()
if ($cfg.agent.workflow.permission.task.'docs-review' -ne 'allow') { $failures += 'workflow cannot invoke docs-review' }
if (-not $workflowText.Contains('DOCS_ONLY')) { $failures += 'workflow agent has no DOCS_ONLY branch' }
if (-not $contractText.Contains('DOCS_ONLY')) { $failures += 'durable workflow has no DOCS_ONLY branch' }
if ($failures.Count -eq 0) { throw 'Precondition invalid: orchestration already implemented' }
Write-Output "EXPECTED_FAIL: $($failures -join '; ')"
```

Expected: all three missing orchestration capabilities are reported.

- [ ] **Step 2: Permit Workflow to call Docs Review**

In `.opencode/agents/workflow.md`, add after `docs: allow`:

```yaml
    docs-review: allow
```

Do not grant Workflow any additional edit or bash permission.

- [ ] **Step 3: Add task classification to the task template**

In `.ai/tasks/TASK-TEMPLATE.md`, add immediately after the task approval field:

```markdown
- Task type: `DOCS_ONLY | CODE`
```

New tasks must replace the alternatives with exactly one value. Legacy approved tasks without this field, including TASK-002, are classified from their complete allowlist without rewriting the approved artifact.

- [ ] **Step 4: Replace Workflow orchestration steps 3-12**

Keep context-inspection steps 1-2 and replace the remaining numbered flow with this contract:

```markdown
3. Classify the task from its complete `Files allowed to modify` allowlist. Use `DOCS_ONLY` only when every allowed path is `README.md`, `README.*`, `CHANGELOG`, `CHANGELOG.*`, or `docs/**`; otherwise use `CODE`.
4. Write one self-contained atomic task under `.ai/tasks/` using `.ai/tasks/TASK-TEMPLATE.md`, record `Task type: DOCS_ONLY | CODE`, set `.ai/STATUS.md` to `WAITING_FOR_APPROVAL`, present it, and stop.
5. After explicit approval of `DOCS_ONLY`, set `DOCUMENTING`, invoke `docs` in `DOCS_ONLY_IMPLEMENTATION` mode, require `.ai/results/TASK-NNN-DOCS.md`, then set `IN_REVIEW` and invoke `docs-review` round 1 with the approved task, actual docs diff, relevant approved sources, and exact docs evidence.
6. If docs-review returns `CHANGES_REQUIRED`, validate every P1-P2 finding has ID, file, location, problem, evidence, required fix, and verification. Set `CHANGES_REQUIRED`, invoke `docs` in `DOCS_REVIEW_FIX` mode with only those findings, and run docs-review round 2. If round 2 is not `APPROVED`, set `BLOCKED` with unresolved IDs and required user decision.
7. After docs-review `APPROVED`, mark the durable task `DONE`, collect docs and review evidence, reset `.ai/STATUS.md` to the neutral contract, and report completion.
8. After explicit approval of `CODE`, set `IMPLEMENTING` and invoke `action` with `AGENTS.md`, the approved task, and only relevant context.
9. Receive Action's actual files, diff summary, exact command outcomes, and action result; set `IN_REVIEW` and invoke `review` with the task, actual diff, relevant source and decisions, and exact test evidence.
10. On code `CHANGES_REQUIRED`, validate the full review finding schema, pass only P0-P2 findings to `action`, and review every fix. Stop after three code-review rounds and set `BLOCKED` with exact unresolved evidence.
11. After full code review `APPROVED`, invoke `docs` in `POST_APPROVAL_SYNC` mode only when public behavior, setup, API, or maintained documentation changed; then mark the durable task `DONE`, collect final evidence, reset status, and report completion.
12. On any `BLOCKED` outcome, preserve the active status and exact blocker. Never auto-reset blocked state.
```

After the numbered list, explicitly state that a resumed approved legacy docs-only task such as TASK-002 is reclassified from its allowlist regardless of stale status routing; stale Action results and full code-review reports/findings are preserved as history, do not count as docs-review rounds, and do not return to `action`. Resume at step 5 from the approved acceptance criteria.

- [ ] **Step 5: Update the durable `.ai/WORKFLOW.md` contract**

Revise the overview diagram and required flow so it contains both branches:

```text
docs-only: workflow -> user approval -> docs -> docs-review -> done
code:      workflow -> user approval -> action -> review -> docs when needed -> done
```

Add these exact durable rules:

- `DOCS_ONLY` is determined only from the approved modification allowlist.
- Docs-only implementation evidence lives at `.ai/results/TASK-NNN-DOCS.md`.
- Docs findings return to `docs`, not `action`.
- Docs review stops after two rounds; code review stops after three rounds.
- A resumed approved legacy docs-only task bypasses stale Action results and full code-review routing, preserves them as audit history, and starts docs-review at round 1.
- Both successful branches reset `.ai/STATUS.md` using the existing neutral contract.

Keep existing code finding schema, model-routing ownership, and blocked-state preservation unchanged. Add `docs-review | workflow-docs` to the model-routing table.

- [ ] **Step 6: Verify orchestration resolution and contract text**

Run:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
if ($cfg.agent.workflow.permission.task.'docs-review' -ne 'allow') { throw 'Workflow cannot call docs-review' }
if ($cfg.agent.workflow.permission.edit.'*' -ne 'deny') { throw 'Workflow edit guard changed' }
$workflowText = Get-Content -Raw '.opencode\agents\workflow.md'
$contractText = Get-Content -Raw '.ai\WORKFLOW.md'
$templateText = Get-Content -Raw '.ai\tasks\TASK-TEMPLATE.md'
foreach ($required in @('DOCS_ONLY','DOCS_ONLY_IMPLEMENTATION','DOCS_REVIEW_FIX','POST_APPROVAL_SYNC','two','TASK-NNN-DOCS.md')) {
  if (-not ($workflowText.Contains($required) -or $contractText.Contains($required))) { throw "Missing contract term: $required" }
}
if (-not $templateText.Contains('Task type: `DOCS_ONLY | CODE`')) { throw 'Task type field missing from template' }
Write-Output 'PASS: docs-only fast path resolved'
```

Expected: `PASS: docs-only fast path resolved`.

- [ ] **Step 7: Check and commit orchestration**

```powershell
git diff --check -- .opencode/agents/workflow.md .ai/WORKFLOW.md .ai/tasks/TASK-TEMPLATE.md
git add -- .opencode/agents/workflow.md .ai/WORKFLOW.md .ai/tasks/TASK-TEMPLATE.md
git commit -m "feat: add docs-only workflow fast path"
```

### Task 4: Verify safety, compatibility, and TASK-002 resume readiness

**Files:**
- Verify only; do not modify TASK-001/TASK-002 artifacts or maintained documentation.

**Interfaces:**
- Consumes: completed configuration from Tasks 1-3 and the current dirty-worktree inventory.
- Produces: exact verification evidence and a resume instruction for TASK-002.

- [ ] **Step 1: Capture current user/task changes without modifying them**

Run:

```powershell
git status --short
Get-Content -Raw '.ai\STATUS.md'
```

Expected: current TASK-001/TASK-002 artifacts and documentation changes remain; TASK-002 may still be `BLOCKED` until the user resumes it.

- [ ] **Step 2: Run the full resolved-config policy gate**

Run from an environment allowed to read the OpenCode global config:

```powershell
$oc = (Get-Command opencode.cmd -ErrorAction Stop).Source
$cfg = ((& $oc debug config | Out-String) | ConvertFrom-Json)
$errors = @()
if ($cfg.agent.docs.model -ne '9router/docs') { $errors += 'docs route' }
if ($cfg.agent.'docs-review'.model -ne '9router/docs') { $errors += 'docs-review route' }
if ($cfg.agent.docs.permission.edit.'.ai/results/*-DOCS.md' -ne 'allow') { $errors += 'docs evidence permission' }
if ($cfg.agent.'docs-review'.permission.edit.'.ai/reviews/**' -ne 'allow') { $errors += 'docs-review report permission' }
if ($cfg.agent.workflow.permission.task.docs -ne 'allow') { $errors += 'workflow -> docs' }
if ($cfg.agent.workflow.permission.task.'docs-review' -ne 'allow') { $errors += 'workflow -> docs-review' }
if ($cfg.agent.action.permission.edit.'docs/**' -ne 'deny') { $errors += 'Action docs guard' }
if ($null -ne $cfg.agent.action.permission.edit.'docs/requirements/project-requirements-specification.html') { $errors += 'temporary requirement exception' }
if ($null -ne $cfg.agent.action.permission.edit.'docs/research/market-research-pain-points.html') { $errors += 'temporary research exception' }
if ($errors.Count -gt 0) { throw "Policy gate failed: $($errors -join ', ')" }
Write-Output 'PASS: resolved docs-only policy gate'
```

Expected: `PASS: resolved docs-only policy gate`.

- [ ] **Step 3: Run repository-level static checks**

```powershell
git diff --check
rg -n --hidden --glob '!**/.git/**' --glob '!**/node_modules/**' `
  'DOCS_ONLY|DOCS_ONLY_IMPLEMENTATION|DOCS_REVIEW_FIX|TASK-NNN-DOCS|docs-review' `
  .ai/WORKFLOW.md .opencode/agents .opencode/commands
```

Expected: diff check exits 0; search output shows the classification, three docs modes, docs artifact, and docs-review references in the intended files.

- [ ] **Step 4: Confirm no task content was overwritten by workflow implementation**

Run:

```powershell
git status --short -- `
  .ai/STATUS.md `
  .ai/tasks/TASK-001.md `
  .ai/tasks/TASK-002.md `
  .ai/results/TASK-001-ACTION.md `
  .ai/results/TASK-001-DOCS.md `
  .ai/results/TASK-002-ACTION.md `
  .ai/reviews/TASK-001-R1.md `
  .ai/reviews/TASK-001-R2.md `
  .ai/reviews/TASK-001-R3.md `
  README.md `
  docs/architecture/erd.md `
  docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md `
  docs/requirements/project-requirements-specification.html `
  docs/research/market-research-pain-points.html
```

Compare this inventory with the pre-implementation inventory. Expected: no existing task or maintained-document change was removed, reverted, or silently staged by the workflow commits.

- [ ] **Step 5: Hand off TASK-002 resume without changing its state**

Report:

```text
Restart OpenCode so it reloads the new agents and tool policies, then send:
Tiếp tục TASK-002 đã duyệt theo luồng DOCS_ONLY.
```

Expected on resume: Workflow reclassifies TASK-002 from its allowlist, preserves `TASK-002-ACTION.md` and stale full-review reports/findings as audit history, ignores their code-path routing, invokes `docs` in `DOCS_ONLY_IMPLEMENTATION`, requires `TASK-002-DOCS.md`, then invokes `docs-review` round 1.

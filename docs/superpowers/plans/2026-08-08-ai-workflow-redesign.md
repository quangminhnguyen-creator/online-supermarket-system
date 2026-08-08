# AI Workflow Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the repository's old seven-agent OpenCode setup with a secure four-agent hybrid workflow that plans, implements, reviews, fixes, and documents tasks through four existing 9Router semantic profiles.

**Architecture:** A primary `workflow` agent owns task approval and orchestration. Read/write boundaries isolate the `action`, `review`, and `docs` subagents; durable task, result, review, and status artifacts provide explicit handoffs. Five project-local commands expose the full workflow and manual escape hatches.

**Tech Stack:** OpenCode 1.2.25 Markdown agents and commands, `opencode.json`, 9Router's OpenAI-compatible endpoint, Markdown workflow artifacts, PowerShell validation.

## Global Constraints

- Preserve application source, Git history, `docs/superpowers/`, `.worktrees/`, and `.codegraph/`.
- Keep the project-specific architecture, conventions, verified commands, and safety rules in `AGENTS.md`.
- Configure exactly four agents: `workflow`, `action`, `review`, and `docs`.
- Map agents only to `workflow-plan`, `workflow-action`, `workflow-review`, and `workflow-docs`; concrete vendor fallback remains inside 9Router.
- Never store a literal API key; use `{env:NINE_ROUTER_API_KEY}`.
- Require user approval before `action` changes application files.
- Limit the automatic fix-review loop to three review rounds.
- A blocking review finding must include ID, severity, file, location, problem, evidence, required fix, and verification.
- Only P0-P2 findings block approval; P3 suggestions are non-blocking.
- Use `opencode.cmd` on Windows because the PowerShell execution policy blocks the `opencode.ps1` shim.

---

## File map

**Replace:**

- `opencode.json` — provider aliases, environment-based secret, default agent, and project-wide permission defaults.
- `AGENTS.md` — preserve technical repository guidance and replace only obsolete seven-role workflow rules.

**Delete old workflow artifacts:**

- `.ai/PLAN.md`
- `.ai/decisions/ADR-001-cart-coupon-contract.md`
- `.ai/decisions/README.md`
- `.ai/results/README.md`
- `.ai/reviews/README.md`
- `.ai/tasks/TASK-001.md`
- `.ai/tasks/TASK-002.md`
- `.ai/tasks/TASK-TEMPLATE.md`
- `.opencode/agents/architect.md`
- `.opencode/agents/builder.md`
- `.opencode/agents/docs.md`
- `.opencode/agents/fixer.md`
- `.opencode/agents/planner.md`
- `.opencode/agents/reviewer.md`
- `.opencode/agents/scout.md`

**Create workflow contracts:**

- `.ai/WORKFLOW.md` — stage transitions, approval gate, agent handoffs, and loop limit.
- `.ai/STATUS.md` — single current-state record.
- `.ai/tasks/TASK-TEMPLATE.md` — self-contained task contract.
- `.ai/reviews/REVIEW-TEMPLATE.md` — mandatory review schema and verdict rules.
- `.ai/results/RESULT-TEMPLATE.md` — action evidence and finding-to-change mapping.

**Create agents:**

- `.opencode/agents/workflow.md` — primary orchestrator.
- `.opencode/agents/action.md` — implementation and review-fix worker.
- `.opencode/agents/review.md` — independent quality gate.
- `.opencode/agents/docs.md` — post-approval documentation worker.

**Create commands:**

- `.opencode/commands/feature.md` — full workflow entry point.
- `.opencode/commands/action.md` — manual implementation entry point.
- `.opencode/commands/review.md` — manual review entry point.
- `.opencode/commands/docs.md` — manual documentation entry point.
- `.opencode/commands/status.md` — read-only workflow state view.

---

### Task 1: Replace durable workflow artifacts

**Files:**

- Delete: the eight old `.ai/` files listed in the file map.
- Create: `.ai/WORKFLOW.md`
- Create: `.ai/STATUS.md`
- Create: `.ai/tasks/TASK-TEMPLATE.md`
- Create: `.ai/reviews/REVIEW-TEMPLATE.md`
- Create: `.ai/results/RESULT-TEMPLATE.md`

**Interfaces:**

- Consumes: the approved workflow stages and review schema from `docs/superpowers/specs/2026-08-08-ai-workflow-redesign.md`.
- Produces: task files named `TASK-NNN.md`, action results named `TASK-NNN-ACTION.md`, and reviews named `TASK-NNN-RN.md`.

- [ ] **Step 1: Record the old-artifact baseline**

Run:

```powershell
$oldAiFiles = @(
  '.ai/PLAN.md',
  '.ai/decisions/ADR-001-cart-coupon-contract.md',
  '.ai/decisions/README.md',
  '.ai/results/README.md',
  '.ai/reviews/README.md',
  '.ai/tasks/TASK-001.md',
  '.ai/tasks/TASK-002.md'
)
$oldAiFiles | Where-Object { Test-Path $_ }
```

Expected: all seven listed runtime files are printed, proving that the old state still exists before replacement.

- [ ] **Step 2: Delete old AI files and write the new workflow contracts**

Use `apply_patch` to delete the old files and replace the template. Create `.ai/WORKFLOW.md` with these exact invariants:

```markdown
# AI Workflow

`workflow -> user approval -> action -> review -> action fixes -> review -> docs -> done`

- `workflow` must stop at `WAITING_FOR_APPROVAL` before application edits.
- `action` implements one approved task and records exact command outcomes.
- `review` never edits implementation files and returns `APPROVED` or `CHANGES_REQUIRED`.
- Blocking findings use the required structured schema and return to `action`.
- Review runs at most three rounds; an unsuccessful third round becomes `BLOCKED`.
- `docs` runs only after `APPROVED` and only when maintained documentation changed.
```

Create `.ai/STATUS.md` with the neutral initial state:

```markdown
# Workflow Status

- Task: `NONE`
- Stage: `DONE`
- Review round: `0/3`
- Last verdict: `NONE`
- Blocking findings: `NONE`
- Next agent: `workflow`
```

The three templates must reproduce every required field from the spec. In particular, `REVIEW-TEMPLATE.md` must include this blocking-finding block verbatim:

```markdown
## REV-NNN

- Severity: `P0 Critical | P1 High | P2 Medium`
- File: `path/to/file`
- Location: `line range or symbol`
- Problem: concrete incorrect behavior
- Evidence: technical proof from the diff, source, or test
- Required fix: bounded correction
- Verification: observable checks and exact commands
```

- [ ] **Step 3: Verify the replacement contracts**

Run:

```powershell
$required = @(
  '.ai/WORKFLOW.md',
  '.ai/STATUS.md',
  '.ai/tasks/TASK-TEMPLATE.md',
  '.ai/reviews/REVIEW-TEMPLATE.md',
  '.ai/results/RESULT-TEMPLATE.md'
)
$missing = $required | Where-Object { -not (Test-Path $_) }
if ($missing) { throw "Missing workflow files: $($missing -join ', ')" }
if (Test-Path '.ai/PLAN.md') { throw 'Old PLAN.md remains' }
if (Get-ChildItem '.ai/decisions' -File -ErrorAction SilentlyContinue) { throw 'Old decisions remain' }
$review = Get-Content -Raw '.ai/reviews/REVIEW-TEMPLATE.md'
@('Severity', 'File', 'Location', 'Problem', 'Evidence', 'Required fix', 'Verification', 'APPROVED', 'CHANGES_REQUIRED') |
  ForEach-Object { if ($review -notmatch [regex]::Escape($_)) { throw "Review field missing: $_" } }
```

Expected: exit code 0 with no output.

- [ ] **Step 4: Commit the workflow contracts**

```powershell
git add -- '.ai'
git commit -m "chore: replace AI workflow contracts"
```

Expected: one commit containing only the `.ai/` replacement.

---

### Task 2: Replace provider routing and agent definitions

**Files:**

- Modify: `opencode.json`
- Delete: the seven old `.opencode/agents/*.md` files listed in the file map.
- Create: `.opencode/agents/workflow.md`
- Create: `.opencode/agents/action.md`
- Create: `.opencode/agents/review.md`
- Create: `.opencode/agents/docs.md`

**Interfaces:**

- Consumes: `.ai/WORKFLOW.md`, task/result/review templates, `AGENTS.md`, and 9Router at `http://localhost:20128/v1`.
- Produces: agent IDs `workflow`, `action`, `review`, and `docs`; model aliases `9router/plan`, `9router/action`, `9router/review`, and `9router/docs`.

- [ ] **Step 1: Write a pre-change assertion that identifies the insecure old configuration**

Run:

```powershell
$config = Get-Content -Raw 'opencode.json'
if ($config -notmatch '"default_agent"\s*:\s*"planner"') { throw 'Expected old planner config not found' }
if ($config -notmatch '"apiKey"\s*:\s*"sk-') { throw 'Expected literal-key baseline not found' }
$oldAgents = Get-ChildItem '.opencode/agents' -File
if ($oldAgents.Count -ne 7) { throw "Expected 7 old agents, found $($oldAgents.Count)" }
```

Expected: exit code 0. Do not print the key or the full config.

- [ ] **Step 2: Replace `opencode.json`**

Use this provider/model shape and preserve the existing watcher ignores:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "default_agent": "workflow",
  "share": "disabled",
  "instructions": ["AGENTS.md", ".ai/WORKFLOW.md"],
  "enabled_providers": ["9router"],
  "provider": {
    "9router": {
      "npm": "@ai-sdk/openai-compatible",
      "name": "9Router (local semantic routes)",
      "options": {
        "baseURL": "http://localhost:20128/v1",
        "apiKey": "{env:NINE_ROUTER_API_KEY}"
      },
      "models": {
        "plan": { "id": "workflow-plan", "name": "Planning route" },
        "action": { "id": "workflow-action", "name": "Implementation route" },
        "review": { "id": "workflow-review", "name": "Review route" },
        "docs": { "id": "workflow-docs", "name": "Documentation route" }
      }
    }
  }
}
```

Also retain the restrictive global `read`, `edit`, `bash`, `external_directory`, and `watcher.ignore` values from the old configuration.

- [ ] **Step 3: Replace the seven agents with four bounded agents**

Use `apply_patch` to remove all old definitions and create:

- `workflow.md`: `mode: primary`, `model: 9router/plan`, may edit only `.ai/STATUS.md` and `.ai/tasks/**`, and may invoke only `action`, `review`, and `docs`.
- `action.md`: `mode: subagent`, `model: 9router/action`, asks before task-approved implementation edits and may write only `.ai/results/*-ACTION.md`; it auto-allows only the repository's exact build/test/config commands, while other Bash commands require approval, and it must deny push and broad deletion.
- `review.md`: `mode: subagent`, `model: 9router/review`, application edits denied; may write only `.ai/reviews/**`; may run focused build/test commands; cannot invoke agents.
- `docs.md`: `mode: subagent`, `model: 9router/docs`, may edit only README, changelog, and `docs/**`; it cannot edit result artifacts or invoke agents.

The `workflow` prompt must contain this explicit orchestration rule:

```text
After user approval, invoke action. Then invoke review with the task, actual diff, relevant source, and exact test evidence. For CHANGES_REQUIRED, pass only structured P0-P2 findings back to action and invoke review again. Stop after three review rounds. Invoke docs only after APPROVED.
```

The `action` prompt must explicitly handle both first implementation and review fixes. The `review` prompt must require the approved schema and the exact final verdict. The `docs` prompt must reject work without an `APPROVED` review.

- [ ] **Step 4: Validate JSON and resolved agent configuration without exposing a real secret**

Run:

```powershell
$null = Get-Content -Raw 'opencode.json' | ConvertFrom-Json
$validationRoot = Join-Path $env:TEMP 'project3-opencode-validation'
$configRoot = Join-Path $validationRoot 'config'
$dataRoot = Join-Path $validationRoot 'data'
$cacheRoot = Join-Path $validationRoot 'cache'
New-Item -ItemType Directory -Force -Path $configRoot,$dataRoot,$cacheRoot | Out-Null
$env:XDG_CONFIG_HOME = $configRoot
$env:XDG_DATA_HOME = $dataRoot
$env:XDG_CACHE_HOME = $cacheRoot
$env:NINE_ROUTER_API_KEY = 'validation-placeholder'
$resolved = opencode.cmd debug config | Out-String
if ($LASTEXITCODE -ne 0) { throw 'OpenCode rejected the configuration' }
$parsed = $resolved | ConvertFrom-Json
if ($parsed.default_agent -ne 'workflow') { throw 'Default agent is not workflow' }
$agentNames = @($parsed.agent.PSObject.Properties.Name | Sort-Object)
$expected = @('action','docs','review','workflow')
if (Compare-Object $agentNames $expected) { throw "Unexpected agents: $($agentNames -join ', ')" }
```

Expected: exit code 0. The resolved configuration is held in memory and is not printed.

- [ ] **Step 5: Commit routing and agent definitions**

```powershell
git add -- 'opencode.json' '.opencode/agents'
git commit -m "feat: add four-agent OpenCode workflow"
```

Expected: one commit containing provider routing and exactly four agent definitions.

---

### Task 3: Add workflow commands and align repository instructions

**Files:**

- Create: `.opencode/commands/feature.md`
- Create: `.opencode/commands/action.md`
- Create: `.opencode/commands/review.md`
- Create: `.opencode/commands/docs.md`
- Create: `.opencode/commands/status.md`
- Modify: `AGENTS.md`

**Interfaces:**

- Consumes: command argument `$ARGUMENTS`, agent IDs from Task 2, and artifacts from Task 1.
- Produces: `/feature`, `/action`, `/review`, `/docs`, and `/status`.

- [ ] **Step 1: Confirm commands are absent before creation**

Run:

```powershell
if (Test-Path '.opencode/commands') {
  $existing = Get-ChildItem '.opencode/commands' -File
  if ($existing) { throw "Unexpected existing commands: $($existing.Name -join ', ')" }
}
```

Expected: exit code 0.

- [ ] **Step 2: Create five command definitions**

Create project-local Markdown commands with these frontmatter bindings:

```yaml
# feature.md
description: Plan an approved feature and orchestrate implementation, review, fixes, and docs
agent: workflow
subtask: false
```

```yaml
# action.md
description: Implement or fix one approved task
agent: action
subtask: true
```

```yaml
# review.md
description: Independently review one task and its current diff
agent: review
subtask: true
```

```yaml
# docs.md
description: Update documentation for an approved task
agent: docs
subtask: true
```

```yaml
# status.md
description: Show the current AI workflow status
agent: workflow
subtask: false
```

The command bodies must use `$ARGUMENTS`. `/feature` instructs `workflow` to write a task and stop for approval. `/action`, `/review`, and `/docs` require a task ID or path. `/status` reads `.ai/STATUS.md` and makes no edits.

- [ ] **Step 3: Replace obsolete role instructions in `AGENTS.md`**

Preserve every technical section through `Migration rules`. Replace only `## Role rules` and its obsolete final workflow paragraph with:

```markdown
## AI workflow rules

- Workflow: primary orchestrator; writes task/status artifacts, waits for approval, and invokes only Action, Review, and Docs.
- Action: implements one approved task or fixes explicit blocking review findings, runs required checks, and records exact evidence.
- Review: independently reviews task, diff, relevant source, and test evidence; application code is read-only.
- Docs: runs after `APPROVED` when public behavior, setup, API, or maintained documentation changed; it edits maintained documentation only.
- Follow `.ai/WORKFLOW.md`; use `.ai/tasks/TASK-TEMPLATE.md`, `.ai/reviews/REVIEW-TEMPLATE.md`, and `.ai/results/RESULT-TEMPLATE.md`.
- Never bypass the approval gate or the mandatory second review after fixes.
```

- [ ] **Step 4: Validate command discovery and preserved repository guidance**

Run:

```powershell
$expectedCommands = @('action.md','docs.md','feature.md','review.md','status.md')
$actualCommands = @(Get-ChildItem '.opencode/commands' -File | Select-Object -ExpandProperty Name | Sort-Object)
if (Compare-Object $actualCommands $expectedCommands) { throw "Unexpected commands: $($actualCommands -join ', ')" }
$agentsGuide = Get-Content -Raw 'AGENTS.md'
@('Technology stack','Verified commands','Architecture and coding conventions','Migration rules','AI workflow rules') |
  ForEach-Object { if ($agentsGuide -notmatch [regex]::Escape($_)) { throw "AGENTS.md lost section: $_" } }
@('Scout:', 'Architect:', 'Builder:', 'Fixer:', 'Reviewer:') |
  ForEach-Object { if ($agentsGuide -match [regex]::Escape($_)) { throw "Obsolete role remains: $_" } }
```

Expected: exit code 0 with no output.

- [ ] **Step 5: Commit commands and repository instructions**

```powershell
git add -- '.opencode/commands' 'AGENTS.md'
git commit -m "docs: add AI workflow commands and guidance"
```

Expected: one commit containing five commands and the targeted `AGENTS.md` role-section replacement.

---

### Task 4: Run final security and workflow verification

**Files:**

- Inspect: `opencode.json`
- Inspect: `.opencode/agents/*.md`
- Inspect: `.opencode/commands/*.md`
- Inspect: `.ai/**/*.md`
- Inspect: `AGENTS.md`

**Interfaces:**

- Consumes: all artifacts from Tasks 1-3.
- Produces: verified four-agent setup with no repository-stored secret.

- [ ] **Step 1: Verify exact file inventory**

Run:

```powershell
$agents = @(Get-ChildItem '.opencode/agents' -File | Select-Object -ExpandProperty BaseName | Sort-Object)
$commands = @(Get-ChildItem '.opencode/commands' -File | Select-Object -ExpandProperty BaseName | Sort-Object)
if (Compare-Object $agents @('action','docs','review','workflow')) { throw "Agent inventory failed: $($agents -join ', ')" }
if (Compare-Object $commands @('action','docs','feature','review','status')) { throw "Command inventory failed: $($commands -join ', ')" }
```

Expected: exit code 0.

- [ ] **Step 2: Scan workflow files for literal secrets and obsolete roles**

Run:

```powershell
$workflowFiles = @('opencode.json','AGENTS.md') + @(rg --files '.ai' '.opencode')
$secretHits = Select-String -Path $workflowFiles -Pattern 'sk-[A-Za-z0-9_-]{12,}'
if ($secretHits) { throw 'Literal API-key-like value found in workflow files' }
$obsolete = Select-String -Path $workflowFiles -Pattern '9router/(architect|coder|fast|free|reviewer)|agent[s]?[ /`](architect|builder|fixer|planner|reviewer|scout)'
if ($obsolete) { throw 'Obsolete route or role reference remains' }
```

Expected: exit code 0. Never print matched secret content.

- [ ] **Step 3: Resolve the complete OpenCode configuration in an isolated validation home**

Run the same isolated `opencode.cmd debug config` procedure from Task 2, then assert:

```powershell
if ($parsed.provider.'9router'.options.apiKey -ne 'validation-placeholder') { throw 'Environment substitution failed' }
$models = @($parsed.provider.'9router'.models.PSObject.Properties.Name | Sort-Object)
if (Compare-Object $models @('action','docs','plan','review')) { throw "Model aliases failed: $($models -join ', ')" }
if (@($parsed.command.PSObject.Properties.Name | Sort-Object).Count -ne 5) { throw 'OpenCode did not discover five commands' }
```

Expected: exit code 0. A models.dev network warning is non-blocking if config resolution exits 0.

- [ ] **Step 4: Inspect the final diff and working tree**

Run:

```powershell
git diff --check
git status --short --branch
git log -4 --oneline
```

Expected: no whitespace errors; the three implementation commits and the preceding spec/plan commits are visible. Unrelated pre-existing worktree entries are reported but not modified.

- [ ] **Step 5: Document the external secret action in the handoff**

The final response must state:

```text
Set NINE_ROUTER_API_KEY in the environment used to launch OpenCode, and revoke/rotate the previously exposed key at 9Router/OpenRouter. Repository cleanup does not revoke a provider credential.
```

Do not set or rotate the external credential automatically; that requires the user's provider account authority.

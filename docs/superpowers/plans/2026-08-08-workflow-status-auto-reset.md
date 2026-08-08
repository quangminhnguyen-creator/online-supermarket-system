# Workflow Status Auto-Reset Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Workflow agent reset `.ai/STATUS.md` to the neutral state after successful completion while preserving `BLOCKED` state for diagnosis and resumption.

**Architecture:** The behavior remains instruction-driven. The durable workflow contract, primary agent prompt, and repository rules will define the same terminal-state sequence; task/result/review artifacts retain history while the status board becomes reusable.

**Tech Stack:** OpenCode 1.2.25 Markdown agents, Markdown workflow contracts, PowerShell assertions.

## Global Constraints

- Reset only after successful completion; never reset `BLOCKED` state.
- Persist final task state and collect final evidence before resetting the status board.
- Keep task, result, and review artifacts as durable history.
- Do not change application source, model routing, provider configuration, permissions, commands, or templates.
- The neutral state must be `Task: NONE`, `Stage: DONE`, review `0/3`, verdict `NONE`, blockers `NONE`, and next agent `workflow`.
- Do not introduce literal API keys or smoke-test artifacts.

---

### Task 1: Define and verify successful status auto-reset

**Files:**

- Modify: `.ai/WORKFLOW.md`
- Modify: `.opencode/agents/workflow.md`
- Modify: `AGENTS.md`
- Inspect: `.ai/STATUS.md`
- Inspect: `opencode.json`

**Interfaces:**

- Consumes: Review verdict `APPROVED` or terminal stage `BLOCKED`.
- Produces: neutral `.ai/STATUS.md` after success, or preserved blocker details after failure.

- [ ] **Step 1: Run a failing contract assertion**

Run:

```powershell
$files = @('.ai/WORKFLOW.md', '.opencode/agents/workflow.md', 'AGENTS.md')
foreach ($file in $files) {
  $text = Get-Content -Raw $file
  if ($text -notmatch 'reset.*STATUS' -or $text -notmatch 'BLOCKED') {
    throw "RED: $file does not define successful reset and BLOCKED preservation"
  }
}
```

Expected: failure naming at least one file that lacks the new contract.

- [ ] **Step 2: Update the durable workflow contract**

Append a `## Terminal status handling` section to `.ai/WORKFLOW.md` containing this exact sequence:

```text
APPROVED -> docs when needed -> task DONE -> collect final evidence -> reset STATUS -> final response
BLOCKED -> preserve STATUS -> report blocker and required decision
```

Define the exact neutral fields and state that task/result/review artifacts must remain available.

- [ ] **Step 3: Update the primary Workflow agent**

Replace its final orchestration step with explicit instructions to:

```text
On successful completion, mark the durable task DONE, collect final evidence, reset .ai/STATUS.md to the neutral contract, then return the final report. On BLOCKED, preserve the active status and blocker details; never reset them.
```

Do not change frontmatter, permissions, models, or subagent routing.

- [ ] **Step 4: Update repository rules**

Add one AI workflow rule to `AGENTS.md`:

```text
- After successful completion, Workflow resets `.ai/STATUS.md` to the documented neutral state only after durable evidence is recorded; `BLOCKED` state is never auto-reset.
```

- [ ] **Step 5: Run contract and configuration verification**

Run:

```powershell
$files = @('.ai/WORKFLOW.md', '.opencode/agents/workflow.md', 'AGENTS.md')
foreach ($file in $files) {
  $text = Get-Content -Raw $file
  if ($text -notmatch 'reset.*STATUS' -or $text -notmatch 'BLOCKED') {
    throw "$file does not define successful reset and BLOCKED preservation"
  }
}

$status = Get-Content -Raw '.ai/STATUS.md'
@('Task: `NONE`','Stage: `DONE`','Review round: `0/3`','Last verdict: `NONE`','Blocking findings: `NONE`','Next agent: `workflow`') |
  ForEach-Object { if ($status -notmatch [regex]::Escape($_)) { throw "Neutral status missing: $_" } }

$workflowFiles = @('opencode.json','AGENTS.md') + @(rg --files '.ai' '.opencode')
if (Select-String -Path $workflowFiles -Pattern 'sk-[A-Za-z0-9_-]{12,}' -Quiet) { throw 'Literal API key found' }

git diff --check
```

Then run `opencode.cmd debug config` in isolated XDG config/data/cache directories with `NINE_ROUTER_API_KEY=validation-placeholder` and assert:

- default agent is `workflow`;
- Workflow remains a primary agent;
- Workflow may edit `.ai/STATUS.md`;
- Workflow may call `action`, `review`, and `docs`;
- the resolved API key equals the placeholder without printing it.

Expected: all assertions exit 0.

- [ ] **Step 6: Commit**

```powershell
git add -- '.ai/WORKFLOW.md' '.opencode/agents/workflow.md' 'AGENTS.md'
git commit -m "feat: reset workflow status after completion"
```

Expected: one commit containing exactly the three instruction files.

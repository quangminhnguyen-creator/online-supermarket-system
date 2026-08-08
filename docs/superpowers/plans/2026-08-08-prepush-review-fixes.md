# Pre-Push Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve all six findings from the `origin/main..HEAD` review so the local 12-commit series is secure, internally consistent, verifiable, and ready to push.

**Architecture:** Harden OpenCode agent permissions at the tool boundary, align durable workflow documentation with the `workflow` primary agent, restore the complete Docs trigger, and make checkout inventory reservation concurrency-safe by contract. Repository hygiene checks must leave no secret, whitespace, or CodeGraph noise.

**Tech Stack:** OpenCode 1.2.25 Markdown agents, Markdown specifications/plans, MySQL 8.4/InnoDB concurrency contract, PowerShell assertions, Git.

## Global Constraints

- Do not change provider routes, concrete model fallback, application source, or generated artifacts.
- Action must not edit secrets, AI control files, or maintained documentation without a permission decision.
- Docs must not edit Action evidence under `.ai/results/**`.
- The primary agent ID is `workflow`; the semantic model alias remains `plan` and route remains `workflow-plan`.
- Docs runs after `APPROVED` when public behavior, setup, API, or maintained documentation changed.
- Concurrent checkout must never oversell the last available unit.
- No literal API key, smoke-test artifact, trailing-whitespace failure, or untracked CodeGraph metadata may remain.

---

### Task 1: Harden agent permissions and restore Docs trigger

**Files:**

- Modify: `.opencode/agents/action.md`
- Modify: `.opencode/agents/docs.md`
- Modify: `.opencode/agents/workflow.md`
- Modify: `.ai/WORKFLOW.md`
- Modify: `AGENTS.md`
- Modify: `docs/superpowers/specs/2026-08-08-workflow-status-auto-reset-design.md`
- Modify: `docs/superpowers/plans/2026-08-08-workflow-status-auto-reset.md`

**Interfaces:**

- Consumes: approved task file ownership and latest Review verdict.
- Produces: protected edit boundaries and the complete Docs trigger.

- [ ] **Step 1: Run failing permission/trigger assertions**

Assert that Action currently has `edit."*" == allow`, Docs currently exposes `.ai/results/**`, and at least one workflow contract lacks all four Docs triggers: `public behavior`, `setup`, `API`, and `maintained documentation`.

- [ ] **Step 2: Harden Action edit permissions**

Set the default edit rule to `ask`. Add explicit `deny` rules after it for:

```text
.env
.env.*
README.md
README.*
CHANGELOG
CHANGELOG.*
docs/**
.ai/**
.opencode/**
opencode.json
AGENTS.md
```

Then add one final allow rule for `.ai/results/*-ACTION.md`. Keep task-approved source edits approval-gated and keep all existing bash/task/external-directory restrictions.

- [ ] **Step 3: Protect Action evidence from Docs**

Remove `.ai/results/**: allow` from Docs permissions and remove prompt language that allows a final result artifact. Docs may edit only README files, changelogs, and `docs/**` after approval.

- [ ] **Step 4: Restore the full Docs trigger**

Use this exact condition in the durable workflow, Workflow prompt, repository rules, auto-reset spec, and auto-reset plan:

```text
after APPROVED when public behavior, setup, API, or maintained documentation changed
```

- [ ] **Step 5: Resolve config and assert boundaries**

Run `opencode.cmd debug config` in isolated XDG directories with a placeholder key. Assert:

- Action default edit is `ask`;
- Action denies `.env`, README, changelog, `docs/**`, `.ai/**`, `.opencode/**`, `opencode.json`, and `AGENTS.md`;
- Action finally allows `.ai/results/*-ACTION.md`;
- Docs has no `.ai/results/**` edit rule;
- Workflow retains Action/Review/Docs task permissions;
- all five workflow contract files contain the four Docs triggers.

- [ ] **Step 6: Commit**

```powershell
git add -- '.opencode/agents/action.md' '.opencode/agents/docs.md' '.opencode/agents/workflow.md' '.ai/WORKFLOW.md' 'AGENTS.md' 'docs/superpowers/specs/2026-08-08-workflow-status-auto-reset-design.md' 'docs/superpowers/plans/2026-08-08-workflow-status-auto-reset.md'
git commit -m "fix: harden workflow agent boundaries"
```

---

### Task 2: Align canonical workflow docs and repository hygiene

**Files:**

- Modify: `docs/superpowers/specs/2026-08-08-ai-workflow-redesign.md`
- Modify: `docs/superpowers/plans/2026-08-08-ai-workflow-redesign.md`
- Modify: `.gitignore`
- Modify: `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md`

**Interfaces:**

- Consumes: final agent inventory `action`, `docs`, `review`, `workflow` and model aliases `action`, `docs`, `plan`, `review`.
- Produces: canonical docs and assertions that match the final implementation.

- [ ] **Step 1: Run failing documentation/hygiene assertions**

Verify that the canonical redesign spec/plan still contains `plan` as an agent ID, `git diff --check origin/main..HEAD` fails, and `.codegraph/.gitignore` would remain untracked in the main checkout.

- [ ] **Step 2: Replace stale agent-ID references**

Update the redesign spec and plan so:

- primary agent ID and path are `workflow` and `.opencode/agents/workflow.md`;
- default agent is `workflow`;
- command bindings for feature/status use `workflow`;
- resolved agent inventory is `action`, `docs`, `review`, `workflow`;
- model alias inventory remains `action`, `docs`, `plan`, `review`;
- `workflow` still uses model `9router/plan` and semantic route `workflow-plan`.

- [ ] **Step 3: Fix repository hygiene**

Replace the narrow CodeGraph database ignore with `.codegraph/`. Remove the two trailing spaces from design lines 3-4 without changing their text.

- [ ] **Step 4: Verify canonical consistency**

Assert no canonical workflow document refers to `plan` as an agent ID or `agents/plan.md`; assert the intended semantic model/route references remain. Run `git diff --check origin/main..HEAD` and verify no `.codegraph/` path appears in status when the main checkout is checked after merge.

- [ ] **Step 5: Commit**

```powershell
git add -- 'docs/superpowers/specs/2026-08-08-ai-workflow-redesign.md' 'docs/superpowers/plans/2026-08-08-ai-workflow-redesign.md' '.gitignore' 'docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md'
git commit -m "docs: align workflow design and repository hygiene"
```

---

### Task 3: Define atomic checkout reservation

**Files:**

- Modify: `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md`

**Interfaces:**

- Consumes: MySQL 8.4/InnoDB transaction and branch inventory rows.
- Produces: deterministic pessimistic locking contract and concurrent integration acceptance test.

- [ ] **Step 1: Run a failing concurrency-contract assertion**

Assert the checkout section lacks `SELECT ... FOR UPDATE`, deterministic lock ordering, bounded deadlock retry, an explicit loser response, or an exact concurrent-test outcome.

- [ ] **Step 2: Specify atomic reservation**

Extend the checkout section with this contract:

1. Begin one InnoDB transaction.
2. Lock every required `BranchInventories` row using `SELECT ... FOR UPDATE` in ascending `(BranchId, ProductId)` order.
3. Recalculate availability as `QuantityOnHand - ReservedQuantity` after acquiring locks.
4. If any item is insufficient, roll back and return HTTP `409` with code `INSUFFICIENT_STOCK`.
5. Otherwise increment `ReservedQuantity` and insert the pending order in the same transaction, then commit.
6. Retry MySQL deadlock/lock-timeout failures at most three times with bounded jitter; after exhaustion return a retryable conflict without partially reserving stock.

- [ ] **Step 3: Strengthen the concurrent acceptance test**

Specify that two simultaneous checkout requests competing for one remaining unit produce exactly one success and one `409 INSUFFICIENT_STOCK`; final available stock is zero, never negative, and the losing transaction creates no order or reservation.

- [ ] **Step 4: Verify and commit**

Assert every locking/retry/response/test phrase exists, run `git diff --check`, then commit:

```powershell
git add -- 'docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md'
git commit -m "docs: define atomic checkout reservation"
```

---

### Task 4: Final review gate and push readiness

**Files:**

- Inspect: all files in `origin/main..HEAD`

**Interfaces:**

- Consumes: Tasks 1-3.
- Produces: an independent review verdict and a push/no-push decision.

- [ ] **Step 1: Run full local verification**

Run OpenCode resolved-config assertions, secret scan over every commit in `origin/main..HEAD`, canonical-doc consistency checks, permission boundary checks, checkout concurrency contract checks, `git diff --check origin/main..HEAD`, and `git status --short`.

- [ ] **Step 2: Request independent review**

Dispatch a read-only reviewer with base `origin/main` and current `HEAD`. Require Critical/Important/Minor findings and `Ready to merge: Yes|No|With fixes`.

- [ ] **Step 3: Push only on approval**

If the independent review returns no Critical or Important finding and all local checks pass, merge the worktree branch into `main`, re-run the full checks on merged `main`, then push `main` to `origin`. Otherwise do not push and return the structured remaining findings.

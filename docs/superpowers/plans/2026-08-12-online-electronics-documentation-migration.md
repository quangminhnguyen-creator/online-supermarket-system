# Online Electronics Documentation Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update maintained specifications to describe a simplified online electronics supermarket and produce a standalone Vietnamese HTML project specification with balanced four-person ownership.

**Architecture:** Perform three bounded documentation-only tasks so each direct `/docs` invocation receives only a compact task ID and reads named source paths itself. Preserve application code and the generated OpenAPI contract; distinguish current Sprint 1 implementation from target-system requirements throughout.

**Tech Stack:** Markdown, semantic HTML5, embedded responsive/print CSS, PowerShell verification, direct OpenCode `docs` and `docs-review` agents.

## Global Constraints

- Current product groups are mobile phones, laptops, televisions, refrigeration and climate appliances, household electrical appliances, networking equipment, and accessories.
- Current fulfilment methods are `BRANCH_PICKUP` and `HOME_DELIVERY`.
- Warranty claims, serial/IMEI tracking, installment financing, installation services, carrier integrations, and route optimization are future work.
- Never store card number, CVV, or expiry data.
- Preserve the ASP.NET Core/React/MySQL modular-monolith architecture and branch-specific price/inventory model.
- Do not edit application source, migrations, `docs/api/openapi.json`, workflow history, or AI configuration.
- Each normal task uses one direct `/docs` call followed by one direct `/docs-review` call.

---

### Task 1: Migrate the maintained system specification and README

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md`
- Create: `.ai/results/TASK-NNN-DOCS.md`

**Interfaces:**
- Consumes: `docs/superpowers/specs/2026-08-12-online-electronics-supermarket-scope-design.md` as the approved scope authority.
- Produces: project identity, current-versus-target status, product groups, fulfilment modes, deferred features, and secure payment rules used by Tasks 2 and 3.

- [ ] **Step 1: Establish the scoped baseline**

Run:

```powershell
git status --short -- README.md docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md
```

Expected: no pre-existing changes in either target file. If either is dirty, stop without overwriting it.

- [ ] **Step 2: Update the main specification**

Replace general-supermarket assumptions with the seven approved electronics groups. Add `BRANCH_PICKUP` and `HOME_DELIVERY` to checkout, validation, order persistence, and acceptance criteria. Mark warranty, serial/IMEI, installments, installation, and advanced logistics as future work. Preserve explicit prohibition on storing card data.

- [ ] **Step 3: Update README identity and status**

Describe the repository as an online electronics supermarket. State that Sprint 1 implements only foundation entities and that fulfilment, checkout, payment, ratings, and reports remain target capabilities until implemented.

- [ ] **Step 4: Verify terminology and whitespace**

Run:

```powershell
rg -n -i "pharmacy|nhà thuốc|grocery|thực phẩm" README.md docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md
git diff --check -- README.md docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md
```

Expected: search returns no contradictory active scope; diff check exits 0.

- [ ] **Step 5: Write compact evidence and request direct review**

Record changed files, acceptance criteria, exact commands, exit codes, and remaining risks in `.ai/results/TASK-NNN-DOCS.md`. Then call `/docs-review` with only task ID, scoped diff reference, and result path.

### Task 2: Align the target ERD narrative

**Files:**
- Modify: `docs/architecture/erd.md`
- Create: `.ai/results/TASK-NNN-DOCS.md`

**Interfaces:**
- Consumes: the approved electronics scope and the updated main specification from Task 1.
- Produces: target relationships for technical product attributes and the two fulfilment modes without claiming a migration exists.

- [ ] **Step 1: Confirm a clean ERD target**

```powershell
git status --short -- docs/architecture/erd.md
```

Expected: no pre-existing ERD change.

- [ ] **Step 2: Align target data semantics**

Retain `BranchInventory` for branch-specific price and stock. Describe category-specific technical attributes without serial/IMEI inventory. Distinguish pickup branch from delivery-address snapshot and label these as target design where not implemented.

- [ ] **Step 3: Verify ERD consistency**

```powershell
rg -n "BranchInventory|BRANCH_PICKUP|HOME_DELIVERY|IMEI|serial" docs/architecture/erd.md
git diff --check -- docs/architecture/erd.md
```

Expected: branch inventory and both fulfilment methods are present; serial/IMEI appears only as deferred/out-of-scope; diff check exits 0.

- [ ] **Step 4: Write compact evidence and request direct review**

Record only concise command signals and changed sections, then call `/docs-review` for this task.

### Task 3: Create the standalone HTML project specification pilot

**Files:**
- Create: `docs/project-spec.html`
- Create: `.ai/results/TASK-NNN-DOCS.md`

**Interfaces:**
- Consumes: `AGENTS.md`, `README.md`, the approved electronics scope, the HTML design, the main system design, ERD, and current OpenAPI only as explicitly named paths.
- Produces: one offline Vietnamese HTML5 artifact used to assess direct-agent request count and token usage.

- [ ] **Step 1: Confirm the output is unowned**

```powershell
git status --short -- docs/project-spec.html
Test-Path docs/project-spec.html
```

Expected: no dirty path and `False`. If the file exists, stop for a user decision.

- [ ] **Step 2: Create semantic offline HTML**

Create exactly one `<!doctype html>` document with `lang="vi"`, semantic landmarks, embedded CSS, responsive tables/cards, and print styles. Do not use JavaScript, external fonts, CDNs, remote images, or external stylesheets.

Include the ten approved sections, all seven product groups, Guest/Customer/Admin roles, branch-specific inventory, secure payment, pickup and home delivery, current-versus-future status, risks, definition of done, and the four-person vertical ownership matrix.

- [ ] **Step 3: Verify required HTML signals**

Run:

```powershell
$html = Get-Content -Raw docs/project-spec.html
if (($html | Select-String -AllMatches '<!doctype html>' -CaseSensitive:$false).Matches.Count -ne 1) { throw 'Expected one doctype' }
if ($html -notmatch '<html[^>]+lang="vi"') { throw 'Missing Vietnamese lang' }
if ($html -match 'https?://|<script|<link[^>]+stylesheet') { throw 'External or scripted dependency found' }
1..4 | ForEach-Object { if ($html -notmatch "Thành viên $_") { throw "Missing member $_" } }
git diff --check -- docs/project-spec.html
```

Expected: PowerShell exits 0 and `git diff --check` reports no errors.

- [ ] **Step 4: Verify scope wording**

```powershell
rg -n "điện thoại|laptop|TV|điện lạnh|điện gia dụng|thiết bị mạng|phụ kiện|BRANCH_PICKUP|HOME_DELIVERY" docs/project-spec.html
rg -n "bảo hành|IMEI|serial|trả góp|lắp đặt" docs/project-spec.html
```

Expected: all current groups and fulfilment modes appear; deferred terms occur only in a future/out-of-scope section.

- [ ] **Step 5: Record the token-test evidence boundary**

Write a compact result containing only file path, acceptance-criterion mapping, command/exit/signal, and remaining risks. Do not paste HTML or full diff into the result or dispatch prompt.

- [ ] **Step 6: Run independent direct review**

Call `/docs-review TASK-NNN` after the `/docs` call completes. The reviewer reads the task, scoped diff, result path, and approved sources. Afterward, record the request count and input/output tokens shown by 9Router; do not estimate missing usage.

## Completion verification

After all three tasks are independently approved, run:

```powershell
git diff --check -- README.md docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md docs/architecture/erd.md docs/project-spec.html
git status --short
```

Expected: only approved documentation and compact AI evidence paths are changed; no application, migration, OpenAPI, workflow, or agent configuration file is introduced by the documentation migration.

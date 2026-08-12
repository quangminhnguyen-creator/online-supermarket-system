---
description: Lightly review one approved documentation-only task
agent: docs-review
subtask: true
---

Review the approved documentation-only task identified by:

`$ARGUMENTS`

Arguments contain only task ID, immutable commit SHA when available, result path, and unresolved finding IDs for R2. Read the scoped working-tree diff when recovering a missing R1 artifact. Write the numeric round artifact before any verdict (`TASK-001-DR1.md` for R1; `TASK-001-DR2.md` for R2), never a literal `DRN` filename. A response-only verdict is invalid. Do not edit maintained documentation. End with exactly `APPROVED` or `CHANGES_REQUIRED`.

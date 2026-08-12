---
description: Independently review one task and its current diff
agent: review
subtask: true
---

Review the approved task and current implementation identified by:

`$ARGUMENTS`

Use the task, scoped diff, relevant decisions/source, and exact Action evidence. Write the numeric report before returning a verdict (`TASK-001-R1.md` for R1; `TASK-001-R2.md` for R2). R3 requires explicit user approval. Do not edit implementation files. End with exactly `APPROVED` or `CHANGES_REQUIRED`.

# TASK-NNN Review R1

## Inputs reviewed

- Task: `.ai/tasks/TASK-NNN.md`
- Relevant decisions: `None` or exact paths
- Diff: state the exact range or source
- Relevant source: exact files and symbols
- Test evidence: `.ai/results/TASK-NNN-ACTION.md`

## Acceptance criteria

- Record each task criterion as `MET` or `NOT MET` with evidence.

## Blocking findings

Use one section per P0-P2 finding. Remove this instruction and write `None` when no blocking finding exists.

### REV-NNN

- Severity: `P0 Critical | P1 High | P2 Medium`
- File: `path/to/file`
- Location: `line range or symbol`
- Problem: concrete incorrect behavior
- Evidence: technical proof from the diff, source, or test
- Required fix: bounded correction
- Verification: observable checks and exact commands

## Previous finding status

For review round 2 or 3, mark every prior finding ID as `RESOLVED` or `UNRESOLVED` and provide evidence. For round 1, write `Not applicable`.

## Non-blocking suggestions

List P3 suggestions separately. They do not change the verdict. Write `None` when there are none.

## Verdict

End the report with exactly one of these verdicts:

`APPROVED`

`CHANGES_REQUIRED`

Use `APPROVED` only when every acceptance criterion is met, required checks pass, no P0-P2 finding remains, and every known blocker is disclosed.

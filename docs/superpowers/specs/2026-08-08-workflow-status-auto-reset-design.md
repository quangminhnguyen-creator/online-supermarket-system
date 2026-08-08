# Workflow Status Auto-Reset Design

## Goal

Automatically return `.ai/STATUS.md` to its neutral state after a workflow finishes successfully, while preserving actionable state when execution is blocked.

## Success path

After Review returns `APPROVED`, the Workflow agent must:

1. invoke Docs after `APPROVED` when public behavior, setup, API, or maintained documentation changed;
2. update the durable task artifact to `DONE`;
3. collect the final implementation, verification, review, and documentation evidence for its response;
4. reset `.ai/STATUS.md` to the neutral state below;
5. return the final report to the user.

```markdown
# Workflow Status

- Task: `NONE`
- Stage: `DONE`
- Review round: `0/3`
- Last verdict: `NONE`
- Blocking findings: `NONE`
- Next agent: `workflow`
```

The task, result, and review artifacts remain available as durable history after the status board resets.

## Blocked path

The Workflow agent must not reset `.ai/STATUS.md` when the workflow ends in `BLOCKED`. It must preserve:

- the active task ID;
- `Stage: BLOCKED`;
- the current review round;
- the latest verdict;
- unresolved finding IDs or other blockers;
- the next actor or user decision required.

This keeps `/status` useful for diagnosis and resumption.

## Scope

The implementation changes only:

- `.ai/WORKFLOW.md` — define terminal-state behavior and the neutral-state contract;
- `.opencode/agents/workflow.md` — require the correct completion sequence;
- `AGENTS.md` — record the repository-level rule.

No application source, model routing, provider configuration, agent permissions, commands, task templates, result templates, or review templates change.

## Verification

The change is complete when:

1. all three instruction files require neutral reset only after successful completion;
2. all three explicitly preserve `BLOCKED` state;
3. the neutral fields match `.ai/STATUS.md` exactly;
4. OpenCode still resolves `workflow` as the default primary agent with permission to edit `.ai/STATUS.md`;
5. no API key literal or smoke-test artifact is introduced.

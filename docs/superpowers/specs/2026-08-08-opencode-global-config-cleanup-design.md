# OpenCode Global Config Cleanup Design

Date: 2026-08-08

## Goal

Remove obsolete global OpenCode workflow entries that leak into this project's resolved configuration while preserving unrelated Google/Antigravity setup and keeping the cleanup recoverable.

## Scope

The cleanup modifies only `C:\Users\manh\.config\opencode\opencode.json` after creating a timestamped backup beside it.

It must:

- remove the global `explorer` agent;
- remove the global 9Router model aliases `workflow-plan`, `workflow-action`, `workflow-review`, and `workflow-docs`;
- replace the global 9Router literal API key with `{env:NINE_ROUTER_API_KEY}`;
- preserve the Google provider, its model definitions, and Antigravity account data;
- preserve every unrelated global OpenCode setting;
- leave the repository-local `opencode.json` unchanged.

The cleanup must not delete `antigravity-accounts.json`, provider packages, caches, or account data.

## Safety and rollback

Before editing, copy the global config to a timestamped `.bak` file in the same directory. Do not print or copy the API key into repository files, command output, or logs.

Apply a structural JSON edit rather than a text replacement. If validation fails, restore the backup and report the failed check.

## Acceptance criteria

After cleanup:

1. the global config parses as JSON;
2. `agent.explorer` is absent globally;
3. the four global `workflow-*` aliases are absent;
4. the global 9Router API key is exactly `{env:NINE_ROUTER_API_KEY}`;
5. Google/Antigravity configuration remains present and unchanged;
6. inside this repository, OpenCode resolves exactly agents `action`, `docs`, `review`, `workflow`;
7. inside this repository, OpenCode resolves exactly model aliases `action`, `docs`, `plan`, `review` and commands `action`, `docs`, `feature`, `review`, `status`;
8. the resolved 9Router API key is non-empty without printing its value;
9. repository Git status remains clean apart from this specification and its implementation plan commits.

## Runtime note

This cleanup does not start 9Router or install Docker. The 9Router endpoint can be tested separately after the 9Router service is running.

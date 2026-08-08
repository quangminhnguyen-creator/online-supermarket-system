# OpenCode Global Config Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove obsolete global OpenCode workflow entries without affecting Google/Antigravity configuration or repository-local routing.

**Architecture:** Perform one atomic structural JSON migration against the validated global config path. Keep an exact timestamped backup only during the migration, restore it automatically on any failed check, and delete it after all global and project-resolved assertions pass so the old literal key is not retained in plaintext.

**Tech Stack:** OpenCode 1.2.25, PowerShell 7/Windows PowerShell JSON APIs, Git.

## Global Constraints

- Modify only `C:\Users\manh\.config\opencode\opencode.json` outside the repository.
- Remove global agent `explorer` and global aliases `workflow-plan`, `workflow-action`, `workflow-review`, `workflow-docs`.
- Set the global 9Router API key to exactly `{env:NINE_ROUTER_API_KEY}`.
- Preserve Google provider configuration and `antigravity-accounts.json` byte-for-byte.
- Preserve every unrelated global OpenCode setting.
- Do not print any API key or account data.
- Do not modify repository-local `opencode.json`.
- Do not require the 9Router service to be running for config validation.

---

### Task 1: Atomically clean and validate the global config

**Files:**

- Modify: `C:\Users\manh\.config\opencode\opencode.json`
- Create temporarily: `C:\Users\manh\.config\opencode\opencode.json.<yyyyMMdd-HHmmss>.bak`
- Inspect: `C:\Users\manh\.config\opencode\antigravity-accounts.json`
- Inspect: `opencode.json`

**Interfaces:**

- Consumes: existing global JSON, environment variable `NINE_ROUTER_API_KEY`, and repository-local OpenCode config.
- Produces: project-resolved agents `action,docs,review,workflow`, model aliases `action,docs,plan,review`, and commands `action,docs,feature,review,status`.

- [ ] **Step 1: Verify the failing baseline without printing secrets**

Run outside the filesystem sandbox:

```powershell
$globalPath = 'C:\Users\manh\.config\opencode\opencode.json'
$global = Get-Content -Raw -LiteralPath $globalPath | ConvertFrom-Json

$failures = @()
if ($global.agent.PSObject.Properties.Name -contains 'explorer') {
  $failures += 'global explorer remains'
}

$aliases = @($global.provider.'9router'.models.PSObject.Properties.Name)
$oldAliases = @('workflow-plan','workflow-action','workflow-review','workflow-docs')
if (@($aliases | Where-Object { $_ -in $oldAliases }).Count -gt 0) {
  $failures += 'global workflow aliases remain'
}

if ([string]$global.provider.'9router'.options.apiKey -ne '{env:NINE_ROUTER_API_KEY}') {
  $failures += 'global 9Router key is not environment-backed'
}

if ($failures.Count -eq 0) { throw 'Baseline unexpectedly clean' }
$failures | ForEach-Object { Write-Output "EXPECTED FAIL: $_" }
```

Expected: all three obsolete conditions are reported; no key value is printed.

- [ ] **Step 2: Validate exact targets and capture preservation hashes**

```powershell
$globalPath = [IO.Path]::GetFullPath('C:\Users\manh\.config\opencode\opencode.json')
$expectedPath = 'C:\Users\manh\.config\opencode\opencode.json'
if ($globalPath -ne $expectedPath) { throw "Unexpected global path: $globalPath" }

$accountPath = 'C:\Users\manh\.config\opencode\antigravity-accounts.json'
if (-not (Test-Path -LiteralPath $globalPath -PathType Leaf)) { throw 'Global config missing' }
if (-not (Test-Path -LiteralPath $accountPath -PathType Leaf)) { throw 'Antigravity account file missing' }
if ([string]::IsNullOrWhiteSpace($env:NINE_ROUTER_API_KEY)) { throw 'NINE_ROUTER_API_KEY is not set' }

$before = Get-Content -Raw -LiteralPath $globalPath | ConvertFrom-Json
$googleBefore = $before.provider.google | ConvertTo-Json -Depth 100 -Compress
$accountHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $accountPath).Hash
```

Expected: all preconditions pass without outputting provider or account contents.

- [ ] **Step 3: Create a temporary exact backup and apply the structural edit atomically**

Run this in the same PowerShell process as Step 2:

```powershell
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = "$globalPath.$stamp.bak"
$tempPath = "$globalPath.$stamp.tmp"
Copy-Item -LiteralPath $globalPath -Destination $backupPath -ErrorAction Stop

try {
  $config = Get-Content -Raw -LiteralPath $globalPath | ConvertFrom-Json

  if ($config.agent) {
    $config.agent.PSObject.Properties.Remove('explorer')
    if (@($config.agent.PSObject.Properties).Count -eq 0) {
      $config.PSObject.Properties.Remove('agent')
    }
  }

  $models = $config.provider.'9router'.models
  foreach ($alias in @('workflow-plan','workflow-action','workflow-review','workflow-docs')) {
    if ($models) { $models.PSObject.Properties.Remove($alias) }
  }
  if ($models -and @($models.PSObject.Properties).Count -eq 0) {
    $config.provider.'9router'.PSObject.Properties.Remove('models')
  }

  $config.provider.'9router'.options.apiKey = '{env:NINE_ROUTER_API_KEY}'
  $json = $config | ConvertTo-Json -Depth 100
  [IO.File]::WriteAllText($tempPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
  $null = Get-Content -Raw -LiteralPath $tempPath | ConvertFrom-Json
  Move-Item -Force -LiteralPath $tempPath -Destination $globalPath
} catch {
  if (Test-Path -LiteralPath $backupPath) {
    Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  }
  Remove-Item -Force -LiteralPath $tempPath -ErrorAction SilentlyContinue
  throw
}
```

Expected: the target is replaced only after the temporary JSON parses successfully.

- [ ] **Step 4: Validate the global config and preserved data**

Run in the same process while `$backupPath`, `$googleBefore`, and `$accountHashBefore` remain available:

```powershell
try {
  $after = Get-Content -Raw -LiteralPath $globalPath | ConvertFrom-Json
  if ($after.agent.PSObject.Properties.Name -contains 'explorer') { throw 'explorer remains' }

  $remainingModels = @($after.provider.'9router'.models.PSObject.Properties.Name)
  $forbidden = @('workflow-plan','workflow-action','workflow-review','workflow-docs')
  if (@($remainingModels | Where-Object { $_ -in $forbidden }).Count -gt 0) {
    throw 'obsolete workflow alias remains'
  }

  if ([string]$after.provider.'9router'.options.apiKey -ne '{env:NINE_ROUTER_API_KEY}') {
    throw 'global API key is not environment-backed'
  }

  $googleAfter = $after.provider.google | ConvertTo-Json -Depth 100 -Compress
  if ($googleAfter -ne $googleBefore) { throw 'Google provider changed' }

  $accountHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $accountPath).Hash
  if ($accountHashAfter -ne $accountHashBefore) { throw 'Antigravity account file changed' }
} catch {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw
}
```

Expected: obsolete entries are absent, the key uses the environment, and unrelated provider/account state is unchanged.

- [ ] **Step 5: Validate the project-resolved OpenCode configuration**

Run from `C:\Users\manh\Documents\Project3\online-supermarket-system` outside the sandbox:

```powershell
$ErrorActionPreference = 'Continue'
$raw = & opencode.cmd debug config 2>$null | Out-String
$exitCode = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($exitCode -ne 0 -or [string]::IsNullOrWhiteSpace($raw)) {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw "opencode debug config failed: $exitCode"
}

$resolved = $raw | ConvertFrom-Json
$agents = @($resolved.agent.PSObject.Properties.Name | Sort-Object)
$models = @($resolved.provider.'9router'.models.PSObject.Properties.Name | Sort-Object)
$commands = @($resolved.command.PSObject.Properties.Name | Sort-Object)

if (Compare-Object $agents @('action','docs','review','workflow')) {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw "Resolved agents differ: $($agents -join ',')"
}
if (Compare-Object $models @('action','docs','plan','review')) {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw "Resolved models differ: $($models -join ',')"
}
if (Compare-Object $commands @('action','docs','feature','review','status')) {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw "Resolved commands differ: $($commands -join ',')"
}
if ([string]::IsNullOrWhiteSpace($resolved.provider.'9router'.options.apiKey)) {
  Copy-Item -Force -LiteralPath $backupPath -Destination $globalPath
  throw 'Resolved 9Router API key is empty'
}

Write-Output 'PASS: 4 agents, 4 model aliases, 5 commands, environment-backed key'
```

Expected: OpenCode exits 0 and every exact inventory assertion passes without printing the key.

- [ ] **Step 6: Remove the plaintext backup only after success and verify repository state**

```powershell
Remove-Item -Force -LiteralPath $backupPath
if (Test-Path -LiteralPath $backupPath) { throw 'Temporary backup remains' }

$final = Get-Content -Raw -LiteralPath $globalPath | ConvertFrom-Json
if ([string]$final.provider.'9router'.options.apiKey -ne '{env:NINE_ROUTER_API_KEY}') {
  throw 'Final key contract failed'
}

git status --short --branch
git diff --check
```

Expected: no temporary plaintext backup remains; only the committed spec/plan make `main` ahead of `origin/main`, with no uncommitted repository changes.

- [ ] **Step 7: Commit the implementation plan before execution**

```powershell
git add -- 'docs/superpowers/plans/2026-08-08-opencode-global-config-cleanup.md'
git commit -m "docs: plan OpenCode global config cleanup"
```

Expected: plan commit succeeds; the later global config mutation is intentionally outside Git.

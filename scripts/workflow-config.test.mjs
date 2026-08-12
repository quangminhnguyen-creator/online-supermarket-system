import test from "node:test";
import assert from "node:assert/strict";
import { access, readFile, stat } from "node:fs/promises";
import { join } from "node:path";

const root = join(import.meta.dirname, "..");

async function exists(path) {
  try { await access(path); return true; } catch { return false; }
}

test("direct docs workflow has compact budgets and no custom dispatcher", async () => {
  const expected = { workflow: 12, action: 10, review: 8, docs: 6, "docs-review": 6 };
  for (const [name, steps] of Object.entries(expected)) {
    const text = await readFile(join(root, ".opencode", "agents", `${name}.md`), "utf8");
    assert.match(text, new RegExp(`steps: ${steps}\\b`));
  }
  const workflow = await readFile(join(root, ".opencode", "agents", "workflow.md"), "utf8");
  assert.doesNotMatch(workflow, /task-workflow_/);
  assert.equal(await exists(join(root, ".opencode", "tools", "task-workflow.ts")), false);
  assert.equal(await exists(join(root, "scripts", "workflow-task.mjs")), false);
});

test("docs artifacts are capped and review names use numeric rounds", async () => {
  const docs = await readFile(join(root, ".opencode", "agents", "docs.md"), "utf8");
  const review = await readFile(join(root, ".opencode", "agents", "docs-review.md"), "utf8");
  assert.match(docs, /1-3 KB/);
  assert.match(docs, /do not copy source passages or the full diff/i);
  assert.match(review, /1-3 KB/);
  assert.match(review, /TASK-001-DR1\.md/);
  assert.match(review, /TASK-001-DR2\.md/);
  assert.match(review, /artifact before returning the final verdict/);
});

test("code and docs reviews share a two-round automatic cap", async () => {
  const canonical = await readFile(join(root, ".ai", "WORKFLOW.md"), "utf8");
  const workflow = await readFile(join(root, ".opencode", "agents", "workflow.md"), "utf8");
  const review = await readFile(join(root, ".opencode", "agents", "review.md"), "utf8");
  const reviewCommand = await readFile(join(root, ".opencode", "commands", "review.md"), "utf8");
  const guide = await readFile(join(root, "AGENTS.md"), "utf8");
  assert.match(canonical, /Code review stops after two automatic rounds/);
  assert.match(canonical, /R3 requires explicit user approval/);
  assert.doesNotMatch(canonical, /three code-review rounds|third code-review round/);
  assert.match(workflow, /R3 requires explicit user approval/);
  assert.match(workflow, /\/docs TASK-NNN DOCS_REVIEW_FIX/);
  assert.match(review, /TASK-001-R1\.md/);
  assert.match(review, /TASK-001-R2\.md/);
  assert.match(reviewCommand, /TASK-001-R1\.md/);
  assert.match(reviewCommand, /TASK-001-R2\.md/);
  assert.doesNotMatch(review, /rounds 2 and 3/);
  assert.match(guide, /TASK-NNN-R1\.md/);
  assert.match(guide, /TASK-NNN-R2\.md/);
  assert.doesNotMatch(guide, /TASK-NNN-RN\.md/);
});

test("TASK-001 durable artifacts are compact and canonical", async () => {
  assert.equal(await exists(join(root, ".ai", "reviews", "TASK-001-DRN.md")), false);
  assert.ok((await stat(join(root, ".ai", "results", "TASK-001-DOCS.md"))).size <= 3072);
  assert.ok((await stat(join(root, ".ai", "reviews", "TASK-001-DR1.md"))).size <= 3072);
  const task = await readFile(join(root, ".ai", "tasks", "TASK-001.md"), "utf8");
  assert.match(task, /TASK-001-DR1\.md/);
  assert.doesNotMatch(task, /TASK-001-DRN\.md/);
});

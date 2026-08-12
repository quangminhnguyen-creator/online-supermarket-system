# TASK-001

- Type: DOCS_ONLY
- Goal: Create a standalone Vietnamese HTML specification for the Online Electronics Supermarket and balanced four-person ownership.
- Approved: YES
- Approved by user: YES
- Status: DONE
- Final verdict: APPROVED
- Docs review: `.ai/reviews/TASK-001-DR1.md`
- Result evidence: `.ai/results/TASK-001-DOCS.md`
- Allowed paths:
  - docs/project-spec.html
- Acceptance criteria:
  - AC-1: One offline semantic HTML5 file uses `lang="vi"`, embedded CSS, responsive layout, and print styles without scripts or external resources.
  - AC-2: Ten sections cover goals, scope/roles, seven electronics groups and flows, architecture, data, API/security, testing, fulfilment/roadmap, four-person ownership, and dependencies/risks/definition of done.
  - AC-3: Current scope includes `BRANCH_PICKUP` and `HOME_DELIVERY`; warranty, serial/IMEI, installments, installation, carrier integration, and route optimization are future work.
  - AC-4: Current Sprint 1 implementation is distinguished from target capabilities, and card number, CVV, or expiry are never stored.
  - AC-5: Members 1-4 receive balanced vertical ownership across UI, API, persistence, and testing, with shared review/integration duties.
- Tests/checks:
  - `git diff --check -- docs/project-spec.html` — exit 0.
  - PowerShell structural check from the approved implementation plan — one doctype, Vietnamese lang, four members, and no external/script dependency.
  - `rg` scope checks from the approved implementation plan — all groups, fulfilment modes, and deferred features appear with correct status.
- Approved sources:
  - AGENTS.md — architecture, stack, repository constraints, and current verification commands.
  - README.md — current Sprint 1 implementation and local operation.
  - docs/superpowers/specs/2026-08-12-online-electronics-supermarket-scope-design.md — authoritative electronics scope and fulfilment decisions.
  - docs/superpowers/specs/2026-08-12-online-supermarket-html-project-spec-design.md — HTML content, layout, and team-assignment design.
  - docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md — target commerce requirements.
  - docs/architecture/erd.md — target data relationships.
  - docs/api/openapi.json — current implemented API boundary only.

Result evidence belongs at `.ai/results/TASK-001-DOCS.md`. Do not paste the HTML or full diff into the result.

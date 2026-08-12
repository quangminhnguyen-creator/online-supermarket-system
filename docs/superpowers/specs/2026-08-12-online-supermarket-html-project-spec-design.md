# Online Supermarket HTML Project Specification Design

**Date:** 2026-08-12
**Status:** Approved design; awaiting written-spec review

## Goal

Create one standalone Vietnamese HTML document at `docs/project-spec.html` that describes the complete Online Electronics Supermarket project and assigns balanced feature ownership to a four-person team. This is also a bounded test of the direct `/docs` then `/docs-review` workflow.

## Scope

The document covers project goals, actors, electronics product groups, functional modules, architecture, data model, API boundaries, security, fulfilment, testing, delivery phases, acceptance criteria, risks, and team assignments. It must distinguish currently implemented Sprint 1 foundations from planned capabilities and must not claim planned features already exist.

The current scope includes mobile phones, laptops, televisions, refrigeration and climate appliances, household electrical appliances, networking equipment, and accessories. Customers can choose pickup at a selected branch or home delivery. Warranty claims, serial/IMEI tracking, installment financing, installation services, carrier integrations, and route optimization are future work.

The task changes only:

- `docs/project-spec.html`
- `.ai/results/TASK-NNN-DOCS.md`

No application source, generated OpenAPI contract, workflow configuration, or repository setup changes are included.

## Document design

The output is a standalone semantic HTML5 page in Vietnamese with embedded CSS. It requires no JavaScript, external font, CDN, image, or network resource. The layout is responsive and printable, with a navigation summary, readable sections, status labels, tables that remain usable on narrow screens, and sufficient color contrast.

Required sections:

1. Project overview and measurable goals.
2. Scope, actors, assumptions, and exclusions.
3. Electronics product groups, functional modules, and representative user flows.
4. Existing modular-monolith architecture and technology stack.
5. Data domains and key relationships, including branch-specific inventory.
6. API, validation, security, payment, and operational constraints.
7. Testing and acceptance strategy.
8. Pickup/home-delivery scope and delivery roadmap separating implemented foundations from future work.
9. Four-person responsibility matrix.
10. Dependencies, collaboration rules, risks, and definition of done.

## Balanced team assignment

Work is divided by vertical feature ownership so each member participates in UI, API, persistence, and testing. Relative complexity, integration surface, and operational risk are used to balance work rather than raw feature count.

| Member | Primary feature group |
|---|---|
| Member 1 | Accounts, authentication, profiles, addresses, ratings, and comments |
| Member 2 | Electronics catalog, categories, brands, technical attributes, search, comparison, and product administration |
| Member 3 | Branches, inventory, cart, and promotions |
| Member 4 | Checkout, branch pickup, home delivery, orders, payments, and reporting |

Shared foundation, integration, review, documentation, and demo duties rotate across the four members. The document must state cross-team dependencies and require peer review for changes that affect shared contracts.

## Source boundaries

The documentation agent may read only these approved sources:

- `AGENTS.md` for repository architecture, workflow, commands, and constraints.
- `README.md` for current implementation and local operation.
- `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md` for approved whole-system requirements.
- `docs/superpowers/specs/2026-08-12-online-electronics-supermarket-scope-design.md` for the approved electronics scope and fulfilment decisions.
- `docs/architecture/erd.md` for target data relationships.
- `docs/api/openapi.json` only to distinguish the checked-in current API contract from planned endpoints.

Long source passages must not be copied into the dispatch prompt. The agent receives only the task ID and `DOCS_ONLY_IMPLEMENTATION` mode, then reads the named files itself.

## Verification

The documentation implementation must run:

```powershell
git diff --check -- docs/project-spec.html
```

It must also verify locally that the file contains one `<!doctype html>`, a Vietnamese `lang` attribute, the ten required sections, all seven electronics product groups, both fulfilment methods, all four member assignments, no external resource references, and no wording that presents deferred features as implemented.

The independent docs reviewer reads the compact task, the generated HTML, its scoped diff, the compact result, and only the approved sources above. It returns `APPROVED` or bounded findings without scanning the repository.

## Success criteria

- A reader can understand the complete project scope and current-versus-planned status from one HTML file.
- Each of four members has a coherent vertical feature group and comparable expected workload.
- Shared responsibilities and inter-feature dependencies are explicit.
- The HTML works offline, is responsive and printable, and uses semantic structure.
- Normal execution requires exactly one `/docs` call and one `/docs-review` call.
- The 9Router usage record can be compared with the previous documentation run to evaluate request count and input-token reduction.

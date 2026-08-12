# Online Electronics Supermarket Scope Design

**Date:** 2026-08-12
**Status:** Approved design; awaiting written-spec review

## Goal

Refocus the project documentation from a general online supermarket to a simplified **online electronics supermarket** while preserving the current modular-monolith architecture and secure payment boundary. This phase updates specifications and maintained documentation only; application code, migrations, and the checked-in OpenAPI contract remain unchanged.

## Approved source

The external reference `ONLINE SUPERMARKET SYSTEM.md` supplied by the user defines the baseline roles and commerce capabilities: Guest, Customer, Admin, catalog browsing, search and filtering, product comparison, cart, checkout, order history, ratings, promotions, administration, and sales reports. Its example database tables are advisory rather than authoritative.

The project must not adopt the reference document's unsafe suggestion to store card numbers or expiry dates. Card number, CVV, and expiry remain outside project persistence and are handled only by the payment provider or sandbox integration.

## Current product scope

The storefront sells these product groups:

- mobile phones;
- laptops;
- televisions;
- refrigeration and climate appliances;
- household electrical appliances;
- networking equipment;
- accessories.

Current-version capabilities are:

- guest browsing, categorized navigation, search, filters, pagination, and product comparison;
- customer registration, authentication, profile and address management;
- product details, images, technical attributes, branch-specific price and stock;
- shopping cart and basic promotions;
- checkout with server-side recalculation of price, promotions, and inventory;
- payment sandbox integration without storing card data;
- customer choice between pickup at a selected branch and home delivery;
- order history, ratings, and comments;
- administration of users, categories, brands, products, branches, inventory, and promotions;
- daily, weekly, monthly, brand, and category sales reports;
- About Us and branch contact information.

## Fulfilment

Checkout requires exactly one fulfilment method:

1. `BRANCH_PICKUP`: the customer selects a branch that can fulfil the cart and receives the order there.
2. `HOME_DELIVERY`: the customer selects a saved or checkout delivery address.

The current specification defines the selection, validation, persistence, order status, and customer-facing display of these methods. Route optimization, carrier integrations, installation scheduling, and advanced delivery pricing are not required in this phase.

## Deferred capabilities

The following electronics-specific capabilities are explicitly future work:

- warranty claim management;
- per-unit serial number or IMEI tracking;
- installment financing;
- installation services;
- carrier integration and delivery-route optimization.

Documentation may identify extension points for these capabilities but must not present them as implemented or required for the current release.

## Architecture and data implications

The system remains an ASP.NET Core and React modular monolith backed by MySQL. Inventory and price remain branch-specific through `BranchInventory`. Product specifications should support category-appropriate technical attributes without introducing serial/IMEI inventory in the current phase.

Order design must distinguish pickup branch from delivery address and retain an immutable fulfilment snapshot sufficient to display historical orders. Exact schema and API changes are design targets until a later approved code task implements them.

## Documentation migration

The documentation-only migration updates:

- `README.md` for the revised project identity, scope, current status, and deferred features;
- `docs/superpowers/specs/2026-08-05-online-supermarket-system-design.md` for roles, modules, fulfilment, acceptance criteria, and non-goals;
- `docs/architecture/erd.md` for electronics terminology and target fulfilment relationships;
- `docs/project-spec.html` as a standalone Vietnamese project specification and four-person assignment artifact.

Historical workflow designs and implementation plans remain historical records and are not mass-rewritten merely because they contain the repository's original name. `docs/api/openapi.json` remains implementation-derived and is not edited during this documentation-only phase.

## Four-person ownership model

Each member owns a vertical feature group across UI, API, persistence, and testing:

| Member | Primary ownership |
|---|---|
| Member 1 | Accounts, authentication, profiles, addresses, ratings, and comments |
| Member 2 | Electronics catalog, categories, brands, technical attributes, search, comparison, and product administration |
| Member 3 | Branches, branch inventory and pricing, cart, and promotions |
| Member 4 | Checkout, pickup, home delivery, orders, payment, and reporting |

Shared foundation work, integration, review, documentation, and demonstrations rotate. Shared-contract changes require peer review by the owner of the consuming feature group. Workload is balanced by complexity and integration risk rather than feature count alone.

## Verification strategy

The documentation task must:

- search maintained target documents for contradictory general-grocery or pharmacy scope;
- verify all seven electronics groups appear in the main specification and HTML artifact;
- verify both fulfilment methods are current-version capabilities;
- verify warranty, serial/IMEI, installments, installation, and advanced logistics are marked future work;
- verify no document instructs the system to store card data;
- run `git diff --check` on every changed documentation file;
- keep current implementation claims distinct from target design claims.

## Success criteria

1. Maintained project documentation consistently describes an online electronics supermarket.
2. The current scope includes branch pickup and home delivery.
3. Deferred electronics-specific capabilities are clearly identified and not implied to exist.
4. Security guidance explicitly rejects persistent card data.
5. The four-person plan assigns coherent vertical ownership with visible shared dependencies.
6. No application source, migration, generated API contract, or workflow-history document changes as part of this documentation-only migration.

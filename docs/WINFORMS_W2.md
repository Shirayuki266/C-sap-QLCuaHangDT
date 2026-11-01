# WinForms W2 — products and lookups

Base: merged W1 PR #103 at `8b35722c70c898a79684f22e4c3182db4f887fa7`; [develop CI SUCCESS](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41302833604). Issue #104; branch `feature/104-admin-catalog`. Official inputs remain the latest user 08/09 `(2)` files: 113 IDs, 48 P0 / 59 P1 / 6 P2. W2 has 16 IDs, 6 P0 / 10 P1. No automatic merge.

## Behavior and boundaries

The existing MainForm now opens product, brand and category management. Product list uses actual `/api/admin/products` query/brandId/active/page/pageSize (20 by default), including inactive products by default. Names resolve through all bounded ADMIN lookup pages, retaining inactive rows. No category filter is invented. Lookup management loads the bounded ADMIN pages and displays the complete lookup list in a scrollable grid; its layout does not claim server-supported state filtering.

Product details expose ADMIN cost/profit/price, H/R, available presentation and version, plus image URLs and descriptive fields. ProductEditor has general/variants/images tabs. Existing variants cannot be deleted, their cost and server price are read-only, and every retained ID/version is sent in a dedicated request DTO. New variants receive initial cost; server returns actual price and zero initial stock. No client selling-price calculation exists. Local form checks assist required fields, positive numbers, percent precision/range and image URL syntax; the server remains authoritative and its rejection leaves the draft intact.

Creation is POST201 `{id}` then GET; updates are PUT204 then GET. All writes obtain fresh CSRF through the W1 cookie client. Lookup requests always contain both name and active; refresh uses lists because no lookup-by-id API exists. No DELETE request or delete action is offered. Product/variant state changes and lookup stop/reopen require confirmation. Lookup warning explicitly states that inactive brand/category hides related public products (accepted DEC.03), while history remains intact.

After an uncertain POST/PUT, subsequent writes are blocked until an authoritative read; product create recovery searches exact normalized code using actual server query pages, without resending. If a successful201/204 is followed by a failed GET, CatalogWorkspace retains the known product ID or lookup readback guard and blocks another write until GET/list succeeds. Form reload recovers instead of recreating. No new backend behavior or idempotency guarantee is invented.

`WIN.A05.04` is **NOT RUN, W4**. Typed PurchaseHandoff/PurchaseSuggestion and an event carry persisted variant IDs/SKUs and positive suggested quantities. The purchase button is disabled and labeled W4. No purchase draft is opened/persisted and no inventory is changed by this handoff. `WIN.BASE.15` also remains NOT RUN as a whole: product/lookup confirmation is now wired, but customer/order/purchase/stock confirmations belong to later stages.

## Run and verify

Use the W1/README backend and external QA settings; never target demo or live databases. New checks create a random `PhoneStore_QA_Admin_...` database, run the accepted migration and drop only that database. Generated fixture credentials remain in memory and ephemeral child-process environment, never command arguments/evidence.

```powershell
dotnet build PhoneStore.slnx -c Release
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build
# Windows only, after both runners are built, with matching external QA variables:
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --desktop-catalog
dotnet run --project src/PhoneStore.Admin -c Release --no-build
```

The client runner retains 13 W1 groups and adds 11 W2 groups with real HTTP/SQL. The Windows runner retains 8 W1 groups and adds 5 W2 groups with actual STA controls/fake API. A separate actual WinForms -> Kestrel -> SQL smoke creates a product through ProductEditor and checks its list, server price12,000,000, H0/R0, no orders/payments/journal and revoked logout session. Local Release solution build has zero warnings/errors. CI reruns unchanged backend/MVC regression plus all client/form groups; exact final head/run is bound in the PR verification comment, not inferred from old CI.

### Evidence interpretation

- [SQL/HTTP results](evidence/admin-w2/sql-results.json): request fields/status and full before/after rows for 11 groups. Accounts/hashes/cookies/tokens/connection strings are excluded. `rejectedWrites` compares SHA256 of canonical, ordered full SQL snapshots for each rejected write, proving no partial state/version change.
- The rate-change group's outer snapshot includes explicit QA stock setup and actual customer checkout. Its nested `priceChanges.before/after` isolates the subsequent rate PUT: order snapshot price12,000,000, total, payment, history, stock H5/R1 and journal are identical; live variant price becomes12,500,000. Do not mistake fixture setup for an inventory effect of editing the product.
- Deliberate transport faults (lost committed response, synthetic readback503) are test-only. Ordinary validation/conflict/duplicate outcomes come from the real backend. A stripped PUT token is rejected400 without SQL changes. Snapshot records preserve the real backend status; an injected readback failure replaces the client response after that backend read.
- [Windows control captures](evidence/admin-w2/form-results.json) label fake API explicitly; [actual QA captures](evidence/admin-w2/real-form-results.json) are separate. Eight fake and two real screenshots cover list, empty/error, editor general/variants, detail, inactive lookup and confirmation. All are test evidence, not product assets or proof of the deferred W6 DPI matrix.
- [Desktop SQL result](evidence/admin-w2/desktop-sql-results.json) corroborates the real editor/list smoke. [Checks](evidence/admin-w2/checks.txt), independent visual review/verdict and native design documentation are retained alongside the captures. The reviewer scored the single header-wrap fix resolved; this is not a new whole-app/WCAG acceptance claim.

No discrepancy requiring backend/API repair was found. The private Word UI08/prototype and raw logs are not tracked. Protected diff from the W1 merge is empty for Core, Infrastructure, Web and previous PhoneStore.Checks, including services/schema/migrations/API/MVC. Existing Ubuntu workflow steps are unchanged; its existing new-client step now discovers W2 checks. The Windows artifact upload includes W1 and W2 evidence.

See [the official-priority ledger](WINFORMS_CHECKLIST_RESULTS.md) for all 113 IDs, exact priorities, evidence and reasons for remaining NOT RUN rows. The W1 report is historical acceptance evidence; this report describes the added W2 behavior.

# Proposed contract to implemented backend

`01_API_CONTRACT.md` is a proposal, not an instruction to replace working service behavior. The Word report, checklist and accepted decisions govern backend acceptance. Priority labels come exclusively from CHECKLIST_BACKEND_TOAN_BO_USECASE_PRIORITY.md:114P0/89P1/21P2. Do not define extra P0 groups from DEC numbers.

## Backend closeout and accepted MVC transfers

The user accepted the following scope decisions. They change the acceptance ledger, not runtime behavior. This closeout does not merge or certify the separate MVC foundation PR87.

| ID | Priority | Backend status | Accepted scope |
| --- | --- | --- | --- |
| K09.02 | P1 | PASS (API) | Missing/discontinued product IDs return404; public detail contains only its own variants. The API does not bind a selected variantId query. **Kiểm tra cặp sản phẩm–biến thể chuyển sang WEB.K09.02, khác biệt được chấp nhận.** Do not claim an API selection-pair guard exists. |
| K10.03 | P0 | NOT RUN | **Chuyển sang MVC**, WEB.K10.03/WEB.K10.04: browser storage, client cleanup and real guest merge. Existing preview/merge/checkout server proofs are retained. |
| BASE.12 | P1 | NOT RUN | **Chuyển sang MVC**, WEB.BASE.07/WEB.K10.03/WEB.K10.04: browser state versus persisted cart/draft and no cache authority. |
| FE.08 | P2 | NOT RUN | **Chuyển sang MVC** for browser/forms/responsive/confirmation evidence; physical printer behavior remains with the applicable client stage. Not accepted by service tests alone. |
| GATE.01 | P0 | PASS (server) | All37 server use cases have retained evidence; the three UI rows above are explicitly transferred. This does not accept MVC pages or printing. |
| GATE.03 | P0 | PASS (backend) | No remaining backend correctness FAIL/BLOCKED in the accepted scope; selection-pair behavior is an accepted transfer, not a pending backend repair. |
| BASE.05 | P0 | N/A (legacy upgrade) | **Dựng DB mới + chạy lại: PASS; nâng cấp từ DB cũ: N/A.** Chỉ hỗ trợ dựng DB mới, không có đường nâng cấp từ DB cũ. No claim of running a legacy003 upgrade or preserving legacy prices through an upgrade is made. |

The actual clean lineage is `rebuild/001_initial_store`, from001_InitialStore.sql. It creates the final schema including Cost/ProfitPercent/Price. Its ledger is **dbo.__PhoneStoreMigrations**. There are no rebuild002/003 migrations; none are fabricated. The closeout checks INFORMATION_SCHEMA/sys for the final19-table schema, uniqueness, CHECK constraints, NO ACTION foreign keys and rowversions.

Final224-ID counts after these scope decisions: P0=112PASS/1NOT RUN/1N/A; P1=88PASS/1NOT RUN; P2=20PASS/1NOT RUN. Total220PASS,0FAIL,0BLOCKED,3NOT RUN,1N/A. See [the final ledger](BACKEND_CHECKLIST_FINAL.md).

## Routes and requests

| Proposed contract | Actual backend | Scope and adaptation |
| --- | --- | --- |
| `/api/admin/auth/login` | POST `/api/auth/login` with `surface="admin"` | CUSTOMER credentials cannot obtain an ADMIN session. The surface is validated by the service. |
| Admin logout | POST `/api/auth/logout` | Revokes the current authenticated session. |
| Admin password | POST `/api/account/password` | One shared self-password operation serves K05 and A03. CUSTOMER may change its own password, never choose an ADMIN account/role. |
| Account/Catalog/Cart/Orders MVC actions | Corresponding `/api/auth/*`, `/api/account/*`, `/api/catalog`, `/api/cart`, `/api/orders` services/endpoints | Equivalent server operations only. MVC/Razor pages, form binding, ModelState/PRG and browser redirects are deferred and have not been implemented or accepted by this mapping. MVC calls must use the shared services directly, not loopback HTTP. |
| `/Cart/Quote` | POST `/api/cart/preview` | Array `[{variantId,quantity}]`, not `{lines:[...]}`. No persistence/reservation. |
| `/Cart/Merge` | POST `/api/cart/merge` | Array input. Approved DEC.02 now implements max/cap/drop and returns 200 `{cart,adjusted}`. Existing POST cart add remains additive; only merge policy changed. |
| `/api/admin/brands`, `/categories` | `/api/admin/lookups/brands`, `/lookups/categories` | GET/POST and PUT `/{id}`. Full-state request includes `name,active`. |
| Product `/active` | GET product, then PUT `/api/admin/products/{id}` | Send retained variant IDs/versions and current product fields with desired `active`. There is no product-level rowversion; retained variant versions protect aggregate edits. |
| Customer `/active` | POST `/api/admin/customers/{id}/state` | `{active,expectedActive}`. Lock atomically revokes every session; unlock requires new login. |
| Supplier `/active` | PUT `/api/admin/suppliers/{id}` | Full allowed supplier fields including active. No hard delete. |
| `/api/admin/inventory` | GET `/api/admin/stock` | `query,brandId,below,page,pageSize`; below means available <= explicit threshold. No invented default low-stock threshold. |
| Inventory `/{id}/log` | GET `/api/admin/stock/movements?variantId={id}` | Supports operationKey and bounded paging. |
| `countedQuantity` | `counted` | Same absolute physical count, reason/version/operationKey required. |
| Order `ship`, `receive-back` | `dispatch`, `receive-return` | Same physical/reserved effects and failed-state return guard. Return reason is optional, trimmed and limited to 1000 characters; cancellation/failure reasons remain required. |
| Checkout `confirmedLines` | `lines` | `{variantId,quantity,price}` are confirmation values; persisted cart/live SQL remain authoritative. |
| Version-only transition commands | `{version,operationKey,reason?}` | Nonempty operationKey is required by the current command contract. Version/state prevent repeated transitions; stock effects also have unique journal keys. |
| Purchase create without key | Purchase create with `requestKey` | Stable creation retry identity; edits need version. This guard is not silently removed to match the draft. |

## Query, enum and response shape

| Proposed | Actual / adapter | Limits |
| --- | --- | --- |
| `q` | `query` | Search products/suppliers/customers; never send q expecting the actual endpoint to bind it. |
| Order `code` or customer text | `query` | Single keyword searches code/recipient/customer name/phone. Separate simultaneous code AND customer predicates are not supported and are not claimed equivalent. |
| Internal product `active` | `active` | Implemented functional correction for A04.02. Omitted state includes both; public remains active-only. |
| pageSize default12/max50 | default20/max100 | Current documented bounded policy. Adapter may explicitly request 12 and limit its own UI to 50. Oversized actual requests >100 are rejected. |
| `name_asc,price_asc,price_desc` | `name,price-asc,price-desc` | newest is unchanged; stable ID tie-breakers. |
| report `type=bestseller/inventory` | `kind=bestsellers/stock` | revenue unchanged. Explicit from/to uses UTC+7 inclusive dates; current stock ignores periods. |
| `groupBy=month`, columns/rows/totals/chart | Typed daily/bestsellers/stock rows from one StoreReport | Client may group daily revenue rows by month and derive table/chart from the same result. There is no native groupBy parameter. No claim of server validation for an unsupported parameter. |
| Dashboard DTO | Revenue period + today queries, pending-order total, stock-below total | Client composition using existing authorized queries; each query is coherent. Cross-query dashboard snapshot atomicity is not promised. Period and low-stock threshold must be explicit. |
| POST returns full DTO | 201 `{id}` followed by authorized GET | Applies to products, suppliers and purchases. |
| PUT/transition returns updated DTO | 204 followed by authorized GET | After uncertain responses reconcile the object/requestKey before a new operation. |
| Flat InvoiceData | `{title,notice,order}` | Flatten order snapshot and rename line total as needed; preserve internal/non-tax-document notice. |
| `paymentStatus`, `lineTotal` | `payment`, line `total` | COD is the only method. Collected amount is total only when DA_THU; unpaid amount is zero. CompletedAt matches CollectedAt by invariant. |
| Admin variant available | onHand-reserved | Public variants already return available and redact internal balances/cost. |
| brandName/categoryName | Resolve brandId/categoryId through ADMIN lookup lists | ADMIN lookup lists retain inactive rows; do not use public-active lists for historical labels. |
| CUSTOMER history excludes actor | Current OrderView history includes actorId/actor | Word A.3 forbids hashes/passwords/privilege overposting, not these history labels. Owner boundaries still apply. This is a proposed DTO minimization policy, not a demonstrated authorization bypass. If adopted, redaction must be server-side; hiding fields in UI is not security enforcement. |

Successful readiness now includes environment and a deterministic credential-free application-target fingerprint required by Word A.1.2. It is not a TLS certificate fingerprint; remote clients must validate HTTPS separately.

## Error conventions

| Proposed code | Actual code / behavior |
| --- | --- |
| UNAUTHENTICATED, INVALID_CREDENTIALS, ACCOUNT_LOCKED | UNAUTHORIZED/401 with generic login feedback; locked/invalid/wrong-surface create no session. |
| VALIDATION_FAILED, PRICE_OUT_OF_RANGE, DUPLICATE_VALUE | INVALID_INPUT/400; unique SQL constraints and atomic rollback remain enforced. No field-error array is promised. |
| STALE_VERSION | CONCURRENT_UPDATE/409 |
| ALREADY_APPLIED | INVALID_STATE or CONCURRENT_UPDATE/409 depending version/state; no repeated effect. |
| INSUFFICIENT_STOCK | OUT_OF_STOCK/409 |
| CSRF_INVALID/403 | INVALID_INPUT/400; authorization may run before CSRF on protected routes. Both reject without business writes. |
| BACKEND_NOT_READY | DATABASE_UNAVAILABLE/503 |
| INTERNAL_ERROR | SERVER_ERROR/500 with Vietnamese generic message and traceId |
| PRICE_CHANGED, FORBIDDEN, NOT_FOUND, INVALID_STATE | Same functional meaning; customer nonowner order remains 404. |

Inactive cart/recipient/empty-cart validations can share INVALID_INPUT. Read the current cart's issue flags and reconcile after errors; do not claim a one-to-one stable code mapping where none exists.

On 2026-10-31 the user explicitly accepted A03.02 (shared own-password operation) and FE.04 (non-bijective checkout error mapping) as accepted differences. They are not unresolved backend blockers. Exact count replay with a refreshed version and unchanged count returns INVALID_INPUT/400, also explicitly accepted; the ledger still prevents repeated effects.

## Pending business decisions and non-equivalent proposals

- DEC.02 approved on 2026-10-30: max/cap/drop merge with adjustment results; unchanged-input retries are idempotent under unchanged live availability. No durable merge-intent ledger is required by this approved policy.
- DEC.03 confirmed on 2026-10-30: retain the existing public visibility policy; inactive brand/category excludes its products. The alternative proposal is not adopted.
- DEC.07/FE.01 approved: public `GET /api/catalog/bestsellers` returns current UTC+7 month/top-eight products by quantity, snapshot revenue and product ID. Homepage rendering remains a later client task.
- DEC.01: stripping phone punctuation, minimum password8 and rejecting the same new password remain proposals. Current named validator uses trimmed phone, 10-128 password characters with letter/digit and optional normalized email.
- DEC.06: same key with changed payload returns a conflict under the current policy. Source-required same-intent/owner retry is supported; returning an old order for changed content is an unapproved change.
- Exact supplier-ID/date purchase-list filters and category-ID filter are not native parameters. Do not simulate these by filtering one already-paginated page and report correct totals; their proposed capability has no equivalent mapping yet.
- Legacy migration003 price preservation is N/A by the user's final scope decision: only new DB creation is supported, with no upgrade path from an old DB. The rebuild rejects legacy/unmanaged schemas without modifying them.

Checklist results distinguish PASS/FAIL/BLOCKED/NOT RUN/N/A by the original IDs. A source-backed test pass does not approve every proposed policy or certify frontend behavior.

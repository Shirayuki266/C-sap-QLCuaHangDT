# Commerce backend contracts

The revised report's K06-K15/A04-A22 server flows share the SQL-backed services in `PhoneStore.Infrastructure/Commerce`. Account contracts are in [ACCOUNTS](ACCOUNTS.md). All UI and physical printing remain separate work.

## Transport and authorization

JSON is camelCase and rejects unknown members. All cookie-authenticated writes, guest cart previews, login and registration require the token from `GET /api/security/token` in `X-CSRF-TOKEN`, preserving its cookie. Refresh the token after login/logout. Successful edits/transitions return 204; creations return 201. Auth failures return JSON 401/403 without redirects. Private order IDs belonging to another customer return 404.

Lists use one-based `page`, default `pageSize=20`, maximum 100 and `{items,total,page,pageSize}`. Stable ID tie-breakers apply. Dates are ISO `yyyy-MM-dd`, interpreted as UTC+7 calendar dates with inclusive start and exclusive following-day end. Persisted timestamps/serialized offsets represent UTC. Invalid/reversed/unrepresentable periods return 400, and no matches return successful empty data.

Eight-byte SQL rowversions are Base64 strings named `version`. Missing/malformed versions return 400, stale versions 409. State commands use `{version,operationKey,reason?}`; keys are nonempty GUIDs. Clients must reconcile current data after uncertain responses before choosing a new key. Business writes serialize across processes using a transaction-owned SQL application lock and conditional inventory writes. Multi-query commerce reads hold a shared lock in one transaction; they cannot mix pre-transition headers with post-transition payments/history.

## Routes

| Methods | Route | Access / purpose |
| --- | --- | --- |
| GET | `/api/catalog`, `/api/catalog/{id}` | Public active products/variants; cost and percentage redacted |
| GET | `/api/lookups/{kind}` | Public active brands/categories |
| GET, POST | `/api/admin/products` | ADMIN internal list / creation |
| GET, PUT | `/api/admin/products/{id}` | ADMIN detail / aggregate edit or trading state |
| GET, POST | `/api/admin/lookups/{kind}` | ADMIN list / creation; kind brands or categories |
| PUT | `/api/admin/lookups/{kind}/{id}` | ADMIN edit/disable, no hard delete |
| GET, POST | `/api/admin/suppliers` | ADMIN list/search / create |
| PUT | `/api/admin/suppliers/{id}` | ADMIN edit/disable, retain receipts |
| GET, POST | `/api/admin/purchases` | ADMIN list / persistent draft creation |
| GET, PUT | `/api/admin/purchases/{id}` | ADMIN detail / draft edit |
| POST | `/api/admin/purchases/{id}/confirm`, `/cancel` | ADMIN draft finalization |
| GET | `/api/admin/stock`, `/api/admin/stock/movements` | ADMIN balances and journal |
| POST | `/api/admin/stock/{id}/adjust` | ADMIN physical count and reason |
| POST | `/api/cart/preview` | Public read-only validation of guest inputs |
| GET, POST, PUT | `/api/cart` | CUSTOMER own read / additive add / replace or explicit zero-delete |
| POST | `/api/cart/merge` | CUSTOMER atomic max/cap/drop guest merge |
| POST | `/api/orders` | CUSTOMER COD checkout / same-key same-content retry |
| GET | `/api/orders/by-request/{key}` | CUSTOMER reconcile an owned checkout result |
| GET | `/api/orders`, `/api/orders/{id}` | CUSTOMER own history/detail |
| POST | `/api/orders/{id}/cancel` | CUSTOMER pending-only cancellation |
| GET | `/api/admin/orders`, `/api/admin/orders/{id}` | ADMIN history/detail |
| POST | `/api/admin/orders/{id}/{action}` | ADMIN confirm, cancel, dispatch, complete, fail, receive-return |
| GET | `/api/admin/customers`, `/api/admin/customers/{id}` | ADMIN safe customer list/detail |
| PUT | `/api/admin/customers/{id}` | ADMIN normalized profile edit |
| POST | `/api/admin/customers/{id}/state` | ADMIN `{active,expectedActive}` lock/unlock |
| GET | `/api/admin/customers/{id}/orders` | ADMIN exact existing customer history |
| GET | `/api/admin/orders/{id}/invoice` | ADMIN internal sales document snapshots |
| GET | `/api/admin/reports?kind=...` | ADMIN revenue, bestsellers or current stock |

Catalog queries accept `query,brandId,minPrice,maxPrice,storageGb,color,sort,page,pageSize`. ADMIN internal queries additionally accept optional `active=true/false`; omitted state includes both trading and discontinued products. Sort is name/newest/price-asc/price-desc; combined filters match the same variant. Public products require active product, brand, category and matching variant. Inventory accepts `query,brandId,below,page,pageSize`, where below means available stock <= threshold, including discontinued items. Journal queries accept `variantId,operationKey,page,pageSize`. Order filters use query/status/from/to and ADMIN-only customerId.

## Product and purchasing commands

Product input is `{code,name,brandId,categoryId,specifications,description,imageUrl,active,variants,images?}`. Each variant is `{id,sku,color,storageGb,cost,profitPercent,active,version?}`. New variants have id=0 and zero stock. Every existing variant must remain in an edit with its current version; disable variants instead of deleting them. `active` and `profitPercent` must be explicit. Existing cost is ignored; percentage changes reprice the persisted cost. Client sale-price/stock fields are rejected. Common metadata changes advance every retained variant version.

Price uses `Round(cost*(1+profitPercent/100),2,AwayFromZero)`, decimal(18,2), positive money and percentage 0-10000 with at most two decimal places. New current cost comes only from a confirmed receipt. Confirmation order determines latest cost, independent of draft creation order. Old order snapshots never reprice.

At most 100 variants, 12 images, valid HTTPS or `/images/` URLs, nonnegative image sort and unique canonical image URLs are technical request bounds. Omitted images preserve the existing gallery; an explicit empty list clears it. Lookup and supplier full-state inputs also require explicit active booleans.

Purchase input is `{supplierId,lines:[{variantId,quantity,cost}],version?,requestKey}`. Creation requires requestKey; same key/content reconciles to one draft. Drafts may be empty, duplicate variant lines are rejected, and saves/cancellation never change stock or price. Confirmation requires active supplier, nonempty lines, current version/key and valid repricing of every line. Stock, latest cost/price, receipt and journal commit together; overflow or any later SQL failure rolls back the entire transaction. Finalized receipts are immutable.

Stock adjustment is `{counted,reason,version,operationKey}`. Counted is an explicitly supplied integer, nonnegative, different from current physical stock and at least reserved. The server derives the physical delta, preserves reservations and journals the reason exactly once.

## Cart, checkout and fulfillment

Cart inputs contain explicit integer variantId/quantity. POST adds, PUT replaces and PUT quantity=0 deletes. Preview/read returns missing/discontinued/shortage issues rather than silently dropping lines. Approved DEC.02 merge uses max(server, guest), consolidates duplicate guest IDs with max, caps quantities at current availability and removes missing/discontinued/zero-available lines. It returns 200 `{cart,adjusted}` with per-variant requested/final quantities and reasons. Repeating identical inputs with unchanged availability does not accumulate quantity. The full union, including existing cart rows, is reconciled in one transaction; at most 100 retained variants are allowed. Cart changes never reserve stock.

Checkout is `{requestKey,recipient,phone,address,paymentMethod:"COD",lines:[{variantId,quantity,price}]}`. Lines are the customer's explicitly confirmed server prices. A changed cart/price returns 409 `{code:"PRICE_CHANGED",message,traceId,details:<current CartView>}` and commits nothing. Other errors omit details. Reconfirm fresh values before retrying. Sufficient stock, immutable recipient/line/name/SKU/prices, unpaid COD, reservation/journal/history and own-cart clearing form one transaction. Same owner/key/immutable content returns the existing order even after its cart is cleared; changed content conflicts and another owner cannot reuse the key.

| Action | Allowed state | Effect |
| --- | --- | --- |
| Confirm ADMIN | CHO_XAC_NHAN | DA_XAC_NHAN; no stock/payment effect |
| Cancel CUSTOMER | Own CHO_XAC_NHAN | DA_HUY; release reserved only; reason required |
| Cancel ADMIN | Pending or confirmed | DA_HUY; release reserved only; reason required |
| Dispatch ADMIN | DA_XAC_NHAN | DANG_GIAO; decrement physical and reserved together |
| Complete ADMIN | DANG_GIAO | HOAN_TAT; collect full COD, collector and completion time atomically |
| Fail ADMIN | DANG_GIAO | GIAO_THAT_BAI; reason required; remains unpaid; no restock |
| Receive return ADMIN | GIAO_THAT_BAI, not received before | Restore physical stock once, no reservation/refund; keep failed status and record history/reason |

All transitions check live role, state, version and nonempty operation key. `returnReceived` derives from the NHAN_LAI journal. The schema has no invented return-status column or invoice table. There is no online payment, refund or external carrier integration.

Locking a CUSTOMER sets active=false and revokes every SQL session in the same transaction. Unlocking never restores sessions. Profile edits reuse existing normalization/uniqueness without touching passwords, sessions or historical order snapshots. ADMIN accounts cannot be edited through customer routes.

## Invoice and reports

Invoice data is eligible only for confirmed/shipping/completed orders, preserves checkout snapshots and explicitly identifies an internal sales document, not an electronic tax invoice. Reads/reprints do not alter order, payment, stock or history. Sending a printer job belongs to the future WinForms UI.

Revenue/bestsellers include only completed orders with collected COD matching total, grouped by completion time. Header revenue is summed once, never multiplied by joined lines. Daily rows and snapshot-priced bestseller revenue reconcile; bestsellers sort by quantity, then variant ID. Labels use current product name/SKU while historical prices remain snapshots. `kind=stock` always returns current physical/reserved/available, ignoring date inputs. The response supplies one result for future table/chart rendering. No actual profit or export feature is claimed. Homepage bestseller default period remains undecided; explicit report periods work independently.

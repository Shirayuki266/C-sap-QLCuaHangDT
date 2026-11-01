# MVC boundary audit — final acceptance

Scope: the five customer controllers, customer models/views, the MVC adapter and checkout quote protection. This is a source review plus real HTTP/SQL verification, not a claim based on naming conventions.

## Method and findings

Read AccountController, HomeController, CatalogController, CartController, OrdersController, MvcStoreAdapter and CheckoutConfirmation. Search the controller/model/view/adapter boundary with:

```powershell
rg -n 'HttpClient|IHttpClientFactory|DbContext|SqlConnection|ExecuteSql|SaveChanges|Cost|ProfitPercent|OnHand|Reserved|Html.Raw' src/PhoneStore.Web/Controllers src/PhoneStore.Web/Models src/PhoneStore.Web/Views src/PhoneStore.Web/Storefront/MvcStoreAdapter.cs
```

The search returns no matches. Controllers call IStoreService in process. MvcStoreAdapter delegates to existing catalog/cart/checkout/account/order services. No SQL access, authoritative price calculation, inventory reservation, state transition or ownership decision is implemented in that boundary.

The approved read-only product–variant association check in MvcStoreAdapter runs before CartService and returns NOT_FOUND for a mismatch. Customer order projections deliberately remove actor/admin identity. Cancel uses the existing OrderWorkflow; its operation GUID stays internal.

Authorization attributes and current-session extraction are HTTP access controls; existing services still enforce the actor/role/ownership. Required/range validation is form shape validation. Pending-only cancellation buttons, available-quantity limits, formatted totals and a line subtotal derived from server unit price are display affordances, not authoritative domain decisions. A direct forged POST is still checked by the service.

CheckoutConfirmation protects the displayed server quote with ASP.NET Data Protection and binds it to account and requestKey. It does not accept posted money as authority or compute selling prices. The existing checkout service checks the protected comparison and stock transactionally. Error aliases and status rendering translate service outcomes without changing those outcomes.

## Runtime boundary verification

Compare the acceptance branch with merged PR99, develop commit `3bc247dd79ee3eab7919cdfe60fa34671a448f26`:

```powershell
git diff 3bc247dd79ee3eab7919cdfe60fa34671a448f26 -- src/PhoneStore.Infrastructure src/PhoneStore.Core src/PhoneStore.Web/Program.cs src/PhoneStore.Web/Endpoints src/PhoneStore.Web/Security
```

Expected and observed: empty. Existing backend services, schema/migrations, admin endpoints and authentication implementation are unchanged. MVC presentation/error translation and tests are the only runtime changes in this step. The full executable regression suite and actual browser/admin-API/SQL flow evidence are recorded separately in the final acceptance ledger.

## Changed-file security review

Review scope is the29 changed text files in this acceptance diff, plus the three local acceptance/regression logs; it is not a new all-history scan. PowerShell/regex searched literal GitHub tokens, private-key headers, bearer credentials and password assignments. The two candidate files contained password-field clearing and preexisting deliberately fake error/redaction test inputs, not credentials. New fixture credentials remain explicitly inside QA tests. The fixture password was absent from all three logs. Filtered PASS-only logs and QA screenshots/SQL summaries are published; raw logs, connection configuration, cookies, tokens, private Word/prototype inputs and browser-context files are excluded from the commit.

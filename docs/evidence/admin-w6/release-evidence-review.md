Evidence-validity result: valid for the declared agent-assisted Release walkthrough, with the limitations below. This is an additional evidence check, not a new UI defect review or whole-phase approval.

Read manifest.json, captures.json, all19 records in sql-results.json through structured summaries, ReleaseWalkthroughChecks.cs and Walkthrough.Release.ps1. The initial evidence pass opened all21 PNGs individually. After the final physical-input recapture, this correction reopened login-ready.png and reread the updated manifests and all19 checkpoint summaries; it does not claim a second visual inspection of the other20 replaced PNGs. Ignored stale failure.png, failure-tree.json and diagnostic artifacts. Wrote no shipping code and made no git changes.

The manifest declares actualReleaseApplication=true, formHarness=false, fakeApi=false and QA-only operation. Source corroborates launching the actual Release exe, starting the real QA backend against isolated SQL, invoking Admin operations with native Windows/UIAutomation controls, and recording SQL checkpoints. The final captures.json records21 captures from process25152 using “Win32 input + UIAutomation + PrintWindow”. The helper uses PMv2 thread coordinates (SetThreadDpiAwarenessContext(-4)), foreground activation and physical mouse down/up at live control bounds for button clicks; capture dimensions come from live HWND bounds immediately before PrintWindow, with screen capture fallback. Customer cart/checkout uses the real customer API as the accepted official08 exception; these captures do not claim a customer browser walkthrough. Manifest exe/DLL SHA256 values remain identical to the values verified against the Release binaries in the initial pass. This is corroboration from artifacts/source, not a claim that the reviewer personally operated or reran the process.

The initial21-file visual pass found the expected rendered native QA stages, with no blank/unrendered task area, password text, or unrelated form. The customer-order1/2/3 captures intentionally show refreshed Admin dashboard aftermath; their checkout creation is evidenced by real API/source and SQL records, not by a customer screen. The selected MainForm title may retain the prior navigation name while showing the dashboard; this does not change the captured stage. The final reopened login-ready.png is complete1280×800: heading, QA readiness, empty credential fields, login/connection/settings/close actions and footer are all present; the former startup crop limitation is resolved. Updated captures.json records all20 main forms at1280×800 and the expiry warning at568×232. Narrow nonclient black frame strips do not hide the inspected task content. Per the limited correction request, other capture states are carried from the initial visual pass and the parent's unchanged-state declaration, supported by the repeated SQL checkpoints.

| Opened capture | Visible state checked |
|---|---|
| login-ready.png | Final recapture reopened: complete1280×800 startup login readiness with real loopback QA URL, Testing fingerprint, empty credential fields, all actions and footer. |
| login.png | Authenticated Admin MainForm with QA identity; metrics initially zero. |
| product-created.png | Saved product variant: cost10M, margin20%, server price12M; H/R/available0. |
| draft-saved.png | Saved draft: quantity10 at8M, total80M; status Nháp and explicit stock/price unchanged. |
| receipt-confirmed.png | Confirmed receipt: server cost8M, price9.6M, H10/R0/available10. |
| customer-order1.png | Admin dashboard refreshed after real customer order1 creation: one waiting order. |
| order1-confirmed.png | FLOW01 confirmed:2×9.6M=19.2M; COD uncollected. |
| order1-dispatched.png | FLOW01 dispatched: Đang giao; COD uncollected. |
| order1-completed.png | FLOW01 completed: Hoàn tất; COD collected19.2M. |
| revenue-seen.png | Revenue report:19.2M, one completed order, two units; matching day table and bar. |
| customer-order2.png | Admin dashboard refreshed after real customer order2 creation: one waiting; completed revenue19.2M. |
| order2-confirmed.png | FLOW02 confirmed:19.2M; COD uncollected. |
| order2-dispatched.png | FLOW02 dispatched: Đang giao; COD uncollected. |
| order2-failed.png | FLOW02 failed: Giao thất bại; return not yet received; COD0. |
| order2-returned.png | FLOW02 returned: still failed order status, explicit Đã nhận lại; COD0; return action absent. |
| customer-order3.png | Admin dashboard refreshed after real customer order3 creation: one waiting; returns-needed0; revenue remains19.2M. |
| order3-confirmed.png | CANCEL confirmed:19.2M; COD uncollected. |
| order3-cancelled.png | CANCEL cancelled: Đã hủy; COD0; no transition action remains. |
| expire-before-save.png | New unsaved purchase: quantity7 at8M; no saved draft total; unsaved-lines message. |
| expired-draft-warning.png | Native Nháp chưa lưu warning: session ended, unsaved content not retained, log in and re-enter; no automatic replay. |
| expired-returned-login.png | Complete1280×800 returned login with empty credential fields and same QA URL/fingerprint. |

SQL/visual cross-checks:

| Stage | H / R / available after checkpoint | SQL fact consistent with visible state |
|---|---|---|
| login | no inventory | One active Admin session; no products, purchases or orders. |
| product-created |0 /0 /0 | One product/variant; cost10M and price12M. |
| draft-saved |0 /0 /0 | One NHAP receipt;10 units at8M, total80M; no inventory mutation. |
| receipt-confirmed |10 /0 /10 | DA_XAC_NHAN receipt, one NHAP journal row +10H; server cost8M, price9.6M. |
| customer-order1 |10 /2 /8 | Order1 CHO_XAC_NHAN, total19.2M; GIU +2R; COD0. |
| order1-confirmed |10 /2 /8 | DA_XAC_NHAN; no extra stock movement; COD0. |
| order1-dispatched |8 /0 /8 | DANG_GIAO; XUAT_GIAO -2H/-2R; COD0. |
| order1-completed |8 /0 /8 | HOAN_TAT; DA_THU19.2M and completion/collection timestamps. |
| revenue-seen |8 /0 /8 | Same completed order/payment; report visibly19.2M, one order, two units. |
| customer-order2 |8 /2 /6 | Order2 waiting; GIU +2R; COD0. |
| order2-confirmed |8 /2 /6 | Confirmed; COD0; no extra stock movement. |
| order2-dispatched |6 /0 /6 | Dispatched; XUAT_GIAO -2H/-2R. |
| order2-failed |6 /0 /6 | Failed with supplied reason; failure alone does not restore stock; COD0. |
| order2-returned |8 /0 /8 | NHAN_LAI +2H and failed→failed history entry corroborate visible received-return flag; COD0. |
| customer-order3 |8 /2 /6 | Order3 waiting; GIU +2R; COD0. |
| order3-confirmed |8 /2 /6 | Confirmed; COD0. |
| order3-cancelled |8 /0 /8 | DA_HUY with supplied reason; HUY_GIU -2R; COD0. |
| expire-before-save |8 /0 /8 | Session deliberately expired in isolated QA; active sessions0; still only one saved receipt. |
| expired-returned-login |8 /0 /8 | Active sessions0; complete business snapshot equals the preceding snapshot; no expired draft persisted. |

Across all19 snapshots, Available=H−R. The final rerun's19 checkpoint summaries repeat the table's H/R/available, order/payment and history/journal counts. Final totals remain three orders, only order1 paid19.2M, one saved receipt,12 history rows and8 movement rows. Order2 remains GIAO_THAT_BAI with return receipt established by the NHAN_LAI movement and the initially inspected visible flag; order3 remains DA_HUY. Expiry still ends at activeAdminSessions0 with the final before/after business snapshots equal. History progresses through the statuses checked in the initial images. The SQL JSON contains the supplied QA business snapshot, not passwords/session tokens/connection strings; inspected screenshots show no password text. SQL is read from supplied result artifacts for this review, not independently queried live.

Limits: no certification of all113 gates, full API matrix, CI, every negative path, concurrency/idempotency, manual keyboard operation, print preview, or broader backend correctness. The expiry condition was deliberately injected by expiring the QA session before the UI save; this demonstrates the resulting UI/rejection evidence rather than waiting for natural elapsed expiry. Initial lookup/supplier preparation and customer API setup are preconditions, not Admin UI actions covered by this walkthrough. The accepted deliverable is agent-assisted UI operation; the user will manually run again. Prior finish-review/finish-verdict visual scope remains separate.

No recapture is required for this declared evidence-validity scope.

# Demo price assumptions — approved 2026-11-04 (UTC+7)

The user approved: “Dùng giá/cost mô phỏng có khoảng tham chiếu, mốc thời gian và giả định ghi rõ; không coi là báo giá hiện hành hay giá mua thật”. This governs DATA.CATALOG.05, CLEAN.INTEG.02 and CLEAN.REAL.03 for this educational dataset. It is not a current retail-price survey or a claim to know a supplier's real purchase cost.

## Reference and time basis

The reference bands come from section2b of the original [data specification](sources/11_DAC_TA_DU_LIEU_DEMO.md). They are scenario-design ranges in VND, not copied manufacturer/reseller quotes. For this closeout the reproducible snapshot uses `--as-of 2026-11-04`; its cutoff is midnight at the beginning of that date in UTC+7. Initial acquisition assumptions apply at each model's simulated shop introduction, not the manufacturer's actual release price. Product model/specification facts and licensed photographs remain real; customer identities, orders, suppliers and amounts remain simulated.

| Segment | Reference initial cost band (VND) | Generator base cost before variant increment | ProfitPercent |
|---|---:|---:|---:|
| Budget | 2,500,000–5,500,000 | 2,500,000 + deterministic0..17 ×100,000 | 15 |
| Midrange | 5,000,000–10,000,000 | 5,000,000 + deterministic0..17 ×200,000 | 12 |
| Premium | 14,000,000–30,000,000 | 14,000,000 + deterministic0..17 ×500,000 | 10 |
| Gaming | 12,000,000–22,000,000 | 12,000,000 + deterministic0..17 ×350,000 | 11 |
| Foldable | 20,000,000–35,000,000 | 20,000,000 + deterministic0..17 ×500,000 | 9 |

Variants add0/250,000/500,000 VND. All resulting initial inputs stay in their reference bands. Categories represent the shop's scenario segmentation; older real models do not imply present-day new-stock availability or present-day price tiers.

## Historical effects and independent checks

Each confirmed receipt applies a0.95 multiplier per completed calendar quarter since the simulated shop launch, rounded to2 decimals AwayFromZero. Consequently the final snapshot cost may be lower than the initial reference band. Draft/cancelled receipts do not change cost or stock. Sale prices are always calculated by the existing business service as `Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`; order lines retain the price effective at order time. No runtime service, price formula, schema or historical snapshot update is introduced by this policy.

Acceptance must run SQL checks for positive costs/prices, valid profit percentages, the formula, latest confirmed purchase cost, receipt/order sums, stock/COD/history consistency, and decreasing historical costs. It must report quarterly actual counts and revenue. These checks substantiate the simulation, not a market-price assertion. A future commercial deployment needs a separately sourced current-price dataset and supplier agreements; this demo does not supply either.

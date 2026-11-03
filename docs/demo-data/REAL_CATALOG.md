# Real-product demo catalog

The user's instruction after D3 replaces fictional option A with real-world phones and real images. Issue [#119](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/119) tracks this follow-up. The implementation is isolated to the demo tool, its tests, new static images and documentation; the previous D1–D3 source documents and evidence remain historical records.

Use `--catalog real` with the existing seed command. The CLI keeps `fictional` as its default so earlier scripts and immutable SVGs continue to work. Both profiles still use the existing business services and fake `TimeProvider`, isolated allowlisted demo/QA databases and environment-only passwords. Real-product data gets new internal codes `RL001`–`RL050`; new images are `rl*.svg`, leaving all original `dm*.svg` bytes unchanged.

## Facts and images

[real-products.json](../../tools/PhoneStore.DemoData/Catalog/real-products.json) is the frozen source of 50 actual models across ten manufacturer families: Samsung, Xiaomi (including POCO), realme, OPPO, vivo, HONOR, Apple, OnePlus, ASUS and REDMAGIC. Each record contains the source specification URL, source-page digest, supported capacity/color lists and three image URLs/digests. Samsung's public product-card API supplies exact-SKU gallery photographs when an archived product page no longer embeds its gallery. Photos were inspected as a contact sheet; generic brand logos and lazy-load thumbnails were excluded.

Specifications cover display, processor, physical RAM information, storage, rear cameras, battery, charging, operating-system family and mobile connectivity. Descriptions are original Vietnamese summaries of those facts. Apple does not publish RAM capacity in the linked specification pages; the catalog says so instead of inventing a number. Specifications/colors/capacities refer to the linked manufacturer's regional listing, rather than a promise that every international configuration is sold in Vietnam. Variant combinations use only the listed capacities and colors, with no duplicate product/capacity/color tuple. Exactly 125 variants are distributed across the full catalog; models with only two supported combinations retain two variants.

The existing schema identifies variants by storage and color; it has no per-variant RAM column. RAM therefore describes the manufacturer's product listing rather than an independently selectable SKU dimension.

All 150 image views are packaged locally and embedded into the separate demo tool, so seeding and CI do not access the internet. They come from 132 distinct manufacturer bitmaps: where additional publisher images are unrelated navigation, blank backgrounds or neighboring models, the gallery instead shows clearly attributed detail viewports of the same verified original photograph. An SVG container embeds the **unaltered downloaded bitmap bytes** as a data URI; its viewport removes surrounding whitespace or frames the selected detail. It contains no generated replacement phone drawing, script or external image reference. The source bitmap SHA256 and final wrapper SHA256 are recorded. These are manufacturer product/gallery/technical illustrations, with the original manufacturer copyright; source attribution is retained for the local demonstration. Manufacturer CDN size/format variants reduce the complete image set to approximately 29.6 MiB before Git compression. Website runtime code and serving behavior are unchanged.

## What stays simulated

The customer identities, suppliers, receipts, orders, COD, inventory and costs/profit margins remain synthetic. Prices are computed by existing services; they are not advertised as live market prices. Product activation dates describe the demo shop's introduction of a model, and four inactive products represent the shop's retirement decisions, not a claim about manufacturer discontinuation. The selected models date from 2022–2024. Requiring `--as-of >= 2026-10-01` ensures their eight-quarter simulated history begins after those introductions. The snapshot still ends at midnight at the beginning of `--as-of` in UTC+7.

## Synchronization checks

The verifier independently reads the frozen source JSON and joins it to persisted SQL rows. It does not call catalog generation. It checks:

- Product code/name/manufacturer and all nine specification facts; descriptions must contain the real model name.
- Every variant's capacity/color against that model's supported values.
- Every gallery URL/order and the product's primary image against the exact model's frozen image files.
- Names and SKUs in order-line snapshots against the corresponding real product/variant.
- All referenced image files against their frozen wrapper digests.

`--verify-demo-data` reports `RealProductMismatches`, `RealVariantMismatches`, `RealPhotoMismatches`, `RealSnapshotMismatches` and `RealPhotoDigestMismatches`; every value must be zero. Normal timeline, price, COD, stock, history, scale and manifest checks also run. A detected mismatch returns a nonzero exit. The existing `--audit-db` remains unchanged.

The QA suite preserves the earlier fictional-profile checks and adds real full/small plans, offline photo byte/hash checks, strict profile/date arguments, actual real-profile SQL seeding/reset reproducibility, HTTP 200/content checks for all 150 new images, and deliberate model-name corruption followed by restoration. Full-data evidence is recorded separately from historical D3 evidence in [real-products-summary.json](../evidence/demo-data/real-products-summary.json).

To generate a fresh full database, follow [DEMO_DATA.md](../DEMO_DATA.md), explicitly adding `--catalog real`. To replace a tool-owned demo snapshot, repeat the same command with `--reset`; otherwise use a new dedicated demo database. Do not apply this to `PhoneStore_Rebuild` or a valuable database. Verification needs the matching seed manifest; the real profile deliberately produces a different hash from the historical fictional catalog.

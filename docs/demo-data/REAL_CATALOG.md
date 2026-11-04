# Real-product demo catalog

The user's instruction after D3 replaces fictional option A with real-world phones and real images. Issue [#119](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/119) tracks this follow-up. The implementation is isolated to the demo tool, its tests, new static images and documentation; the previous D1–D3 source documents and evidence remain historical records.

Use `--catalog real` with the existing seed command. The separate CLI defaults to `real` and refuses `fictional`; reproduce archived fictional runs at their recorded commits instead. The current profile uses the existing business services and fake `TimeProvider`, isolated allowlisted demo/QA databases and environment-only passwords. Real-product data gets new internal codes `LC001`–`LC050`; new images are `lc*.svg`, and remove the150 unreferenced historical `dm*.svg` files from active static serving; their original bytes remain in Git history. The existing demo-phone.svg is retained solely for the explicitly approved unchanged legacy Web seeder.

## Facts and images

[real-products.json](../../tools/PhoneStore.DemoData/Catalog/real-products.json) freezes50 actual models across Samsung, Xiaomi, realme, OPPO, vivo, HONOR, Apple, OnePlus, ASUS and nubia. Each record has primary specification URLs, supported capacity/color lists and three locally licensed image-view records. Prior fact-page digests are retained only for unchanged source records; new/changed model facts have a recorded review date and primary URLs, without inventing a fetched-page digest. See [all model sources and five original Vietnamese examples](MODEL_SOURCES.md). Photographs now come from the separately licensed Commons sources in [IMAGE_CREDITS.md](IMAGE_CREDITS.md), not the former manufacturer gallery API.

Specifications cover display, processor, physical RAM information, storage, rear cameras, battery, charging, operating-system family and mobile connectivity. Descriptions are original Vietnamese summaries of those facts. Apple does not publish RAM capacity in the linked specification pages; the catalog says so instead of inventing a number. Specifications/colors/capacities refer to the linked manufacturer's regional listing, rather than a promise that every international configuration is sold in Vietnam. Variant combinations use only the listed capacities and colors, with no duplicate product/capacity/color tuple. Exactly 125 variants are distributed across the full catalog; models with only two supported combinations retain two variants.

The existing schema identifies variants by storage and color; it has no per-variant RAM column. RAM therefore describes the manufacturer's product listing rather than an independently selectable SKU dimension.

All150 local image views use licensed real photographs from Wikimedia Commons, with explicit source, author, license URL, downloaded-byte digest, crop and wrapper digest. There are49 distinct photographs for50 models: the iPhone15 Pro/Pro Max share one source but crop different labelled devices. Each model has a primary view and two clearly labelled details of that same photograph; these are not three distinct camera angles. Mi11 Lite4G uses an identified blue-camera close-up. The native SVG embeds unchanged downloaded raster bytes, and contains no script or external image dependency. See the complete [attribution register](IMAGE_CREDITS.md). Manufacturer model facts are independently sourced from the linked primary specification pages.

The superseded RL manufacturer images remain in immutable Git history; no redistribution license had been documented for them. They are removed from the release tree. A deliberately ignored local RL cache may remain in an existing preview workspace to avoid breaking that user-owned database; it is excluded from fresh clones and release acceptance. The legacy demo-phone.svg is the documented runtime compatibility exception.

## What stays simulated

The customer identities, suppliers, receipts, orders, COD, inventory and costs/profit margins remain synthetic. Prices are computed by existing services; they are not advertised as live market prices. Product activation dates describe the demo shop's introduction of a model, and four inactive products represent the shop's retirement decisions, not a claim about manufacturer discontinuation. The selected models include older2019–2021 models and the2025 nubia Z80 Ultra. Requiring `--as-of >= 2026-10-01` bounds the scenario; the nubia model has an explicit earliest simulated shop introduction of2026-01-01, after the manufacturer announcement of2025-10-22. This lower bound is a simulation assumption, not a claimed release date. The snapshot still ends at midnight at the beginning of `--as-of` in UTC+7.

## Synchronization checks

The verifier independently reads the frozen source JSON and joins it to persisted SQL rows. It does not call catalog generation. It checks:

- Product code/name/manufacturer and all nine specification facts; descriptions must contain the real model name.
- Every variant's capacity/color against that model's supported values.
- Every gallery URL/order and the product's primary image against the exact model's frozen image files.
- Names and SKUs in order-line snapshots against the corresponding real product/variant.
- All referenced image files against their frozen wrapper digests.

`--verify-demo-data` reports `RealProductMismatches`, `RealVariantMismatches`, `RealPhotoMismatches`, `RealSnapshotMismatches` and `RealPhotoDigestMismatches`; every value must be zero. Normal timeline, price, COD, stock, history, scale and manifest checks also run. A detected mismatch returns a nonzero exit. The existing `--audit-db` remains unchanged.

The QA suite retains safety, lifecycle and corruption regression checks on the real profile, rejects legacy generation, and includes real full/small plans, offline photo byte/hash checks, strict profile/date arguments, actual real-profile SQL seeding/reset reproducibility, HTTP 200/content checks for all 150 new images, and deliberate model-name corruption followed by restoration. Full-data evidence is recorded separately from historical D3 evidence in [real-products-summary.json](../evidence/demo-data/real-products-summary.json).

To generate a fresh full database, follow [DEMO_DATA.md](../DEMO_DATA.md), explicitly adding `--catalog real`. To replace a tool-owned demo snapshot, repeat the same command with `--reset`; otherwise use a new dedicated demo database. Do not apply this to `PhoneStore_Rebuild` or a valuable database. Verification needs the matching seed manifest; the real profile deliberately produces a different hash from the historical fictional catalog.

Price/cost acceptance follows the user-approved [simulation policy](PRICE_ASSUMPTIONS.md), not a claim about current market quotes or actual supplier costs. Historical real-products-summary.json predates this licensed closeout and cannot establish acceptance of the current catalog.

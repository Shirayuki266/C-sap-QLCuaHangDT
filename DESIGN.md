---
name: Monophone MVC foundation
description: The implemented optionA customer frame and error recovery, PR1 of six.
colors:
  ink: "#19263d"
  muted: "#52647f"
  blue: "#2454dc"
  navy: "#142d5c"
  paper: "#fff"
  canvas: "#f4f6fb"
  line: "#d5deed"
  soft: "#eaf0ff"
  blue-hover: "#1946c2"
  action-light: "#f8faff"
  action-light-hover: "#dfe9ff"
  welcome-accent: "#aac5ff"
  welcome-copy: "#d2def5"
  welcome-type: "#f7faff"
  focus: "#cf6c13"
typography:
  display:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "clamp(2.3rem,4.8vw,4rem)"
    fontWeight: 700
    lineHeight: 1.17
    letterSpacing: "-.035em"
  headline:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "clamp(1.65rem,3vw,2.35rem)"
    fontWeight: 700
    lineHeight: 1.17
    letterSpacing: "-.02em"
  body:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", sans-serif'
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.6
  brand:
    fontSize: "1.9rem"
    fontWeight: 800
    letterSpacing: "-.04em"
  action:
    fontSize: "1rem"
    fontWeight: 650
    lineHeight: 1.6
rounded:
  control: "10px"
  radius: "14px"
  welcome: "20px"
  search: "100px"
spacing:
  compact: "16px"
  control: "22px"
  standard: "24px"
  masthead: "28px"
  section: "32px"
  spacious: "56px"
  section-large: "64px"
components:
  button-primary:
    backgroundColor: "{colors.blue}"
    textColor: "{colors.paper}"
    typography: "{typography.action}"
    rounded: "{rounded.control}"
    padding: "13px 22px"
  button-primary-hover:
    backgroundColor: "{colors.blue-hover}"
  button-light:
    backgroundColor: "{colors.action-light}"
    textColor: "{colors.navy}"
    typography: "{typography.action}"
    rounded: "{rounded.control}"
    padding: "13px 22px"
  button-light-hover:
    backgroundColor: "{colors.action-light-hover}"
  search:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    rounded: "{rounded.search}"
    padding: "4px 6px 4px 18px"
  welcome:
    backgroundColor: "{colors.navy}"
    textColor: "{colors.paper}"
    rounded: "{rounded.welcome}"
    padding: "clamp(28px,4vw,56px)"
---

# Design System: Monophone MVC foundation

## Overview

**Creative North Star: "OptionA customer frame"**

The established optionA world uses navy ink, blue actions, a white navigation frame and a pale page surface. Rounded controls and readable Vietnamese system typography support a familiar phone shop. Monophone is illustrative, configurable branding. This describes the current build, not a new identity proposal.

This is PR1 of six: the shared customer frame, a limited welcome/about/contact homepage and standalone error recovery. It does not establish complete UI03–UI07/UI13 or FLOW, or implemented account, catalog, cart, checkout and order screens. Links to those destinations are present before their controllers. No raster assets ship.

Evidence: `src/PhoneStore.Web/wwwroot/css/storefront.css`, `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml` and `Views/Shared/WebsiteError.cshtml` under that project; the four foundation/error404 desktop/mobile captures in `.artifacts/mvc-audit/screenshots/`; and the accepted PR1 finish review. Private Word optionA images `image90.png`/`image80.png` and the supplied prototype are visual authority, not deployable assets. The ship disposition resolves the CTA SVG finding within this partial foundation boundary.

**Key Characteristics:**

- Navy welcome panel within a light, flat customer frame.
- Blue action and navigation states; plain Vietnamese text.
- Responsive stacking, visible keyboard focus and stroke SVG icons.

## Colors

The palette combines strong navy and blue with cool, pale surfaces.

### Primary

- **Action Blue** (`blue`): default links, current navigation and recovery button; `blue-hover` deepens the filled action.
- **Welcome Navy** (`navy`): welcome panel and light-button lettering.
- **Welcome Accent** (`welcome-accent`): second headline line and decorative wordmark accent; `welcome-copy` supports panel body and secondary link, and `welcome-type` the decorative wordmark.

### Neutral

- **Ink** (`ink`): text, branding and narrow service strip.
- **Muted Slate** (`muted`): secondary copy and search placeholder.
- **Paper / Canvas** (`paper`, `canvas`): white header/footer and pale page/search surfaces.
- **Line / Soft** (`line`, `soft`): fine separators and notice/search-hover fill.
- **Light Action** (`action-light`, `action-light-hover`): the welcome CTA's light fill and hover.
- **Focus Amber** (`focus`): keyboard outlines, a state color rather than a brand accent.

**The Visible State Rule.** Preserve the current navigation underline and keyboard outline; color alone does not communicate these states.

## Typography

**Display and body font:** the same installed system stack in frontmatter; no webfont ships. The build uses the browser's standard bold heading weight. This inherited optionA foundation has no separate display-font asset; that is an observed implementation limit, not a prescription for unrelated future identities.

### Hierarchy

- **Display:** responsive homepage h1; at widths up to 600px the welcome h1 is fixed at 2.35rem.
- **Headline:** h2 for about/contact sections.
- **Body:** inherited default 1rem with 1.6 line height; paragraphs have a 70ch cap, welcome copy 43ch.
- **Brand:** heavy, tightly spaced shop name; reduces to 1.55rem on mobile.
- **Action / navigation:** weight 650; account links use .88rem, reducing to .82rem on mobile.
- **Error:** h1 uses `clamp(2rem,5vw,3.4rem)`; status uses 3.5rem, weight 800, line height 1 and tabular numerals. Description is 1.05rem, trace .85rem.
- **Decorative welcome wordmark:** `clamp(3.8rem,8vw,7.2rem)`, weight 800, line height .92, letter spacing -.04em, rotated -8 degrees; aria-hidden and absent on mobile.

## Layout

The shared container is capped at 1200px and otherwise viewport minus 48px. At up to 600px it becomes viewport minus 32px. The main frame has 32px/64px top/bottom spacing, becoming 20px/40px on mobile, with a 55vh minimum height.

Desktop places brand, flexible search and account links in one row, then a horizontal navigation row. At up to 1000px the masthead wraps, search takes the last full row and account links move right. At up to 600px the order is brand, full-width search, then a full-width account row. Navigation remains a horizontal, scrollable strip; it does not become a hamburger menu.

The welcome panel uses a 1.3fr/1fr grid with a 32px gap, becoming 1.4fr/1fr at the intermediate breakpoint. At up to 600px it becomes a block with 28px/24px padding and hides the decorative wordmark. The secondary welcome link becomes a separate line at up to 1000px. About uses equal columns with a 56px gap, stacking at up to 600px. Footer content wraps below 1000px and its copy takes a full row on mobile.

Errors omit the shop header/footer. Their column is capped at 700px, otherwise viewport minus 48px, and has vertical margin `clamp(64px,14vh,160px)`. Mobile uses viewport minus 40px. Screenshot evidence covers 1440px desktop and 390px mobile; these are examples, not additional breakpoints.

## Elevation & Depth

No box shadows are implemented. White, pale and navy surface changes, thin borders and spacing supply separation. The welcome wordmark rotation belongs to this panel's decoration, not a general transform convention.

## Shapes

The welcome panel has broad corners, reducing to the shared radius on mobile. Filled actions have smaller control corners; search is a pill with a circular icon button. Header, footer and contact separators use a one-pixel line. Icons are inline, unfilled SVG strokes (22px, stroke width 1.8, rounded line caps), including the resolved CTA arrow.

## Components

### Buttons

Filled blue recovery and light welcome variants share 13px/22px padding, a 48px minimum height, weight 650 and a 22px internal gap. Hover changes fill. Focus uses a 3px amber outline offset 4px. Color/background transitions last .15s with ease-out only when reduced motion is not requested. No pressed treatment is defined.

### Search

A labeled search form combines a pale pill field, line border, transparent input and a minimum 44px square submit button. Placeholder uses muted text at full opacity. Submit hover uses the soft fill. The icon is inline SVG with an accessible name on the button. This foundation supplies the form appearance and catalog destination; catalog search is later work.

### Navigation

Dark links use a blue hover and a three-pixel blue underline plus blue text for `aria-current=page`. Account links underline on hover. A focus-revealed skip link moves into the viewport and targets the main landmark. The mobile account row wraps and main navigation scrolls horizontally.

### Welcome / notice containers

The navy welcome panel carries a two-tone heading and light CTA. The existing notice is a soft filled, shared-radius block with 16px/20px padding and `role=status`; it is not a product-card system.

### Error recovery

A numeric status, heading, explanation and home action form a standalone recovery surface. A trace identifier appears only when supplied (500 flow); its text wraps anywhere. No trace appears in the reviewed 404. Error states must retain explanatory text and a recovery action.

## Do's and Don'ts

### Do:

- **Do** retain the white frame, pale canvas, navy welcome surface and blue actions of optionA.
- **Do** keep Vietnamese labels, visible focus, named icon buttons and textual status/recovery messages.
- **Do** preserve the implemented stacking and horizontal mobile navigation.

### Don't:

- **Don't** present this foundation as complete shopping flows or completed UI03–UI07/UI13 acceptance.
- **Don't** publish private source Word/prototype files or treat audit screenshots as shipping imagery.
- **Don't** infer product-card, filter-drawer, checkout, account-form or order-state tokens from screens that are not implemented.

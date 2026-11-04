---
name: Monophone Admin W1 native foundation
description: Observed Option A extension for native administrative login, session and account forms.
colors:
  ink: "#19263d"
  navy: "#142d5c"
  blue: "#2454dc"
  canvas: "#f4f6fb"
  muted: "#52647f"
  paper: "#ffffff"
  navigation-selected: "#eaf0ff"
  login-error: "#aa1e2d"
typography:
  body:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 400
  input:
    fontFamily: "Segoe UI"
    fontSize: "12pt"
    fontWeight: 400
  login-heading:
    fontFamily: "Segoe UI"
    fontSize: "24pt"
    fontWeight: 700
  main-heading:
    fontFamily: "Segoe UI"
    fontSize: "23pt"
    fontWeight: 700
spacing:
  stack-default: "24px"
  form-inset: "32px"
  panel-inset: "28px"
components:
  button-primary:
    backgroundColor: "{colors.blue}"
    textColor: "{colors.paper}"
    padding: "5px 12px"
  button-secondary:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.ink}"
    padding: "5px 12px"
---

## Overview

**Creative North Star: "Option A, native administration"**

This artifact records the observed W1 WinForms surface in Operate mode. Navy navigation, blue actions, white form surfaces and a pale ground extend the accepted Option A world. Vietnamese copy and native Segoe UI support an ordinary desktop administration workflow. This is a record of the current build, not a new identity proposal.

Root PRODUCT.md and DESIGN.md describe the historical MVC phase. They and the existing .impeccable files are intentionally preserved; this scoped artifact does not refresh or reconcile that context. Current Admin authorization and composition are recorded in direction.md. The private Word UI01 reference is design authority only: do not copy or publish the reference or sample credentials.

Source basis: AdminTheme.cs, LoginForm.cs, MainForm.cs, PasswordForm.cs, ServerSettingsForm.cs and ConfirmationDialog.cs under src/PhoneStore.Admin. Representative real-QA login/main captures and the refreshed fake-API busy capture were opened for this documentation pass. The supplied finish review covered eight fake-form and two real-QA captures; its final ship verdict is confined to the sole loading-readability fix and visible regressions from that fix.

Evidence boundaries remain separate: 13 real client QA checks, eight STA form checks, one real UI QA login/logout flow and the unchanged 322-check older regression suite. These counts describe supplied evidence, not additional tests run by this documentation pass. Fake-API captures under forms/ are explicitly synthetic; real-forms/ captures use actual Kestrel/SQL QA. Neither screenshots nor a visual review alone establish runtime, security or SQL correctness. No formal WCAG conformance claim is made. A full 125%/150% DPI matrix is deferred to W6.

## Colors

Blue identifies the main submission and the login prelabel. Navy grounds the left navigation; ink supplies primary text. White form panels and secondary buttons stand against the pale canvas. Muted slate distinguishes readiness metadata, while login errors use a separate red text color.

Clicking a module gives its navigation button the pale selected fill and updates the content heading. Initial navigation buttons are white; do not claim that an initial selected treatment is implemented.

**The Visible Operation Rule.** Busy feedback must remain readable outside disabled actions. The refreshed login capture shows bold ink operation text on white; the disabled blue button is no longer the sole carrier of the loading message.

## Typography

The application uses installed Segoe UI with native regular and bold weights. The theme defines body and input styles above. Field labels are bold body size, login identity uses bold 11pt, dialog headings use bold 20pt, the confirmation heading uses bold 18pt and login footer text uses 9pt. The main placeholder also uses bold 18pt. There are no bundled webfonts or display-font assets.

Vietnamese headings, labels and action text carry the hierarchy. Keep endpoint and environment/fingerprint text legible and allow wrapping where the source sets a maximum width.

## Layout

Login and main windows default to and have a minimum outer size of 1280×800. Forms center on screen and declare DPI autoscaling. These source properties do not establish successful rendering across the deferred DPI matrix.

Post-review platform correction: the Windows runner initially constrained a form to 1044×788 and failed the strict minimum-size assertion. A Shown workaround proved ineffective because the Form.MinimumSize setter clamps to the screen working area; that workaround has been removed. Shipping AdminTheme.Frame retains its original 1280×800 default and minimum. The Windows CI job instead explicitly requests a 1920×1080 display with Set-DisplayResolution. The requirement remains at least 1280×800; the assertion was not relaxed. All ten captures were refreshed, and the supplied local reruns passed eight STA form checks and the one real UI QA login/logout flow. Reopened representative refreshed real-main and fake busy captures show complete surfaces at 1280×800, including the readable external progress label. These captures do not complete the deferred 125%/150% DPI matrix. The Windows CI rerun is pending at this note; no CI pass is claimed.

Login uses a white identity header, a centered white stack within a three-column pale layout (25% / 50% / 25%), and a small footer. The card has the form inset token, endpoint/readiness identity, two labeled credentials, progress/error text, a full-width primary action and a flowing row of connection/settings/close actions. Its heading prelabel and outer identity header follow the supplied UI01 authority.

The main frame has a fixed 230px navy sidebar, a flexible pale body and a 36px footer spanning both columns. The body contains the current module heading, session greeting, password/logout actions and a white placeholder panel. Sidebar controls are vertically stacked and scrollable. Settings, password and confirmation windows use outer sizes of 720×620, 660×500 and 620×330 respectively, with maximize disabled. Native table/flow layout and scrollable stacks provide adaptation; this is not a web breakpoint system.

## Elevation & Depth

Solid surface changes, native borders and spacing create separation. The application defines no custom shadows, gradients, imagery, texture or animation system. Native window chrome remains visible. No shipping raster assets are introduced; evidence PNGs are audit artifacts.

## Shapes

Flat rectangular buttons and native rectangular text fields supply the observed form language. Do not transplant the historical storefront's rounded CSS controls into this native record. Theme buttons have a minimum size of 120×42 and native flat styling; navigation is assigned 190px width. The source also assigns navigation height to 40px, subject to its shared minimum size and native layout behavior.

## Components

### Login and readiness

Named username/password controls sit below visible labels; the password uses the system password character. Enter submits and Escape closes. Connection checking and login share a busy guard, disable submission/check/configuration actions and show an enabled bold status label with a wait cursor. The close action remains available. Readiness exposes actual environment and fingerprint, or a visible not-ready message with recovery action. Validation and failure retain the username while clearing the password; this documentation does not reproduce credentials.

The original finish review found loading text dependent on a low-contrast disabled button. The fix adds the external progress label, also used for readiness checking. The final verdict resolves this sole finding and confirms the normal/error/offline recaptures retain complete layouts. Its disposition is ship for that fix scope only; it is not a fresh whole-surface or independent runtime audit. No HTML/CSS detector applies to this native surface.

### Navigation and session frame

The full named modules are Tổng quan, Điện thoại, Khách hàng, Nhập hàng, Tồn kho, Đơn hàng, Báo cáo, Hãng, Danh mục, Nhà cung cấp and Tài khoản. Clicks update the heading and selected fill. Most module content explicitly reads “Chưa triển khai”; account content points to session/password actions. No inventory, price or order logic lives in these forms. The footer identifies environment, fingerprint, user and ADMIN role. Logout shows an ending-session message, disables account actions and closes through the session lifecycle.

### Server settings

The modal settings stack provides server address, optional expected environment and optional expected fingerprint. Save validates the address; validation failure leaves entered values visible with an explanatory message. Save and cancel map to Enter and Escape. The form states the HTTPS requirement for another machine and that credentials are not saved.

### Password change

Two masked inputs collect current and new password, accompanied by the 10–128 character letter-and-number guidance and the warning that success revokes sessions and requires login again. Submission clears both inputs, guards duplicate submission, disables save and displays a separate progress label. Errors appear in text. Cancel/Escape and save/Enter are wired; the dialog also clears its inputs on close.

### Shared confirmation

The shared modal presents an action heading, supplied description, “Quay lại” and “Xác nhận”. Initial focus is on return; Escape cancels and Enter accepts through native dialog wiring. This is presentation infrastructure. W2–W5 business-command wiring is not implemented, and BASE15 will NOT RUN in W1. The fixture capture does not prove any shipped destructive business flow.

## Do's and Don'ts

- **Do** preserve the native Option A palette, Vietnamese labels, visible endpoint/environment identity and clear later-module placeholders.
- **Do** keep readable progress text independent of disabled action rendering, retain safe input on failure and clear password controls.
- **Do** preserve keyboard access and native DPI properties while treating untested scaling behavior as deferred.
- **Don't** publish private reference imagery, sample credentials or audit PNGs as product assets.
- **Don't** label synthetic form fixtures as real API evidence, combine the separate evidence counts into a completion claim, or infer formal accessibility conformance.
- **Don't** claim W2–W5 business workflows, BASE15 execution or W6 DPI completion from the W1 frame and confirmation foundation.

# Accessibility smoke test — Opening Times - Stores  
**Date:** 2026-08-13  
**Scope:** Partner Center question *“This product has been tested to meet accessibility guidelines.”*  
**Method:** Code review + build verification (WinForms .NET 8). Not a full WCAG / Microsoft Accessibility Insights lab.

## Partner Center answer (recommended)

**No** — do **not** check “tested to meet accessibility guidelines” as a pass.

This smoke pass finds **gaps**. That is OK for v1; you can ship without claiming a11y certification.

---

## What works (partial)

| Area | Notes |
|------|--------|
| **Keyboard (partial)** | Search/add text boxes work; suggestion list has ↑↓/Enter; theme toggle is `TabStop` |
| **Icon buttons** | Settings / menu / min / close are `TabStop = true` |
| **DPI** | `ApplicationHighDpiMode` PerMonitorV2 in csproj |
| **Execution level** | `asInvoker` (no admin required) |
| **Contrast (dark theme)** | Primary text on dark surfaces is generally strong; green status readable |

---

## Failures / gaps (why not to claim “meets guidelines”)

| Issue | Severity | Detail |
|-------|----------|--------|
| **No AccessibleName / AccessibleDescription** | High | Controls lack UIA names for Narrator/screen readers |
| **Owner-drawn store rows** | High | `StoreRowControl` paints text only — **not** exposed as readable automation tree content |
| **Name links** | Medium | Click-to-open URL is mouse-oriented; no clear keyboard-only “open store” |
| **Colour-only urgency** | Medium | Close timer green/yellow/orange/red is mainly colour (text still says “Closes in …”) — partial OK, not full |
| **Borderless chrome** | Medium | Custom min/close; system title bar a11y patterns not used |
| **Focus order / trap** | Medium | Not formally verified; suggestions focus not full form cycle |
| **High contrast / forced colors** | Not tested | Custom paint may ignore Windows High Contrast |
| **Screen reader end-to-end** | Not tested | Narrator not run in this session |
| **Accessibility Insights / AccChecker** | Not run | No automated full scan attached |

---

## Quick local checks you can do later (optional)

1. Windows **Narrator** (Win+Ctrl+Enter) — can it read store names and hours?  
2. **Tab** through header → search → list → settings.  
3. **Settings** — all fields reachable without mouse.  
4. Windows **High contrast** theme — does text stay visible?

---

## Bottom line

| Question | Answer |
|----------|--------|
| Tested lightly? | **Yes** (this smoke + code review) |
| Meets accessibility guidelines? | **No claim** — gaps remain |
| Block Store submit? | **No** — leave the a11y attestation **unchecked** / answer that it was not fully validated |

---

*Generated for Partner Center listing honesty. Improve a11y in a later version if desired.*

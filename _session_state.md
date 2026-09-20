# Session state — Opening Times

- **Updated:** 2026-09-20 22:40
- **This turn:** Push to `LinespottingOrg/Opening-Times-Stores`. README + privacy.html updated (yr.no/MET, in-widget weather). No dedicated linespotting.com product page — marketing is GitHub README + downloads.linespotting.com privacy.

## 2026-09-20 22:32
- Temperature color: green ≥ 0 °C, blue below 0. Reinstalled PC1.

## 2026-09-20 22:18
- Weather/hours text clip: UiText sizes draw rect to MeasureText.

## 2026-09-20 19:11
- Reinstalled on PC1. Rebuilt `payload.zip` (exe + `app.ico`), published `setup.exe`, ran `setup /reinstall /S` exit 0. Start Menu shortcuts now `exe,0` (Opening Times EU + Opening Times - Stores). Widget running from Local\Programs with bag icon.
- **Installed:** `C:\Users\User\AppData\Local\Programs\OpeningTimesStores\OpeningTimesWidget.exe`
- **Setup:** `...\dist\installer\1.0.1\setup.exe` (local only — not uploaded to R2).
- **Not done:** no Microsoft Store / Play upload this turn.

## 2026-09-20 19:07
- PC1 widget had no bag icon. Cause: `ApplicationIcon` commented out, no `.ico`, tray used `SystemIcons.Application`. Cropped WhatsApp bag JPEG, wrote multi-size `Assets\app.ico`, wired form/tray/exe, replaced Capacitor chevron leftovers in mobile source.

## 2026-08-19 11:51 (previous)
- **Why Submit greyed out (researched):** Microsoft requires ALL mandatory sections complete. Live PC shows Store listing English/Swedish = **Incomplete** because screenshots + 1:1 Store logo missing. Age ratings = Complete. Package URL 1.0.1 set. App status In draft — not production.
- Docs: learn.microsoft.com create-app-submission MSI/EXE checklist — Screenshots Required (min 1), Store logos 1:1 Required, Description + License Required.

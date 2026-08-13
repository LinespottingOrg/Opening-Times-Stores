# Microsoft Partner Center — Opening Times - Stores  
**Target submit window:** ~20 hours from 2026-08-13 (tomorrow session)  
**Dashboard:** https://partner.microsoft.com/en-US/dashboard/apps-and-games/overview  
**Code:** `C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores`  
**GitHub (if used):** https://github.com/LinespottingOrg/Opening-Times-Stores  

---

## What the product is

| | |
|--|--|
| **Type** | Windows desktop (WinForms .NET 8) full-trust MSIX |
| **Name** | Opening Times - Stores |
| **Package identity (draft)** | `Linespotting.OpeningTimesStores` |
| **Publisher display** | Linespotting |
| **Platforms** | Windows 10/11 desktop x64 (start with x64) |

**Capabilities in manifest:** `internetClient` + `runFullTrust`  
**Network:** xAI (optional weekly hours), Open-Meteo (sunrise/sunset + geocode).  
**Privacy:** local settings only for token; document network in privacy policy.

---

## Tonight / before sleep (prep — ~45 min)

1. **Partner Center login** as Linespotting account  
   https://partner.microsoft.com/en-US/dashboard/apps-and-games/overview  

2. **Reserve product name**  
   Apps and games → New product → **MSIX or PWA app** → name:  
   `Opening Times - Stores`  
   (if taken: `Opening Times Stores` / `OpeningTimes by Linespotting`)

3. **Copy Publisher CN**  
   Account settings → **Publisher management** / package identity →  
   paste into `Package\Package.appxmanifest` → `Identity Publisher="CN=..."`  

4. **Privacy policy URL** (required if network)  
   Host a short page (e.g. linespotting.com or GitHub Pages) covering:  
   - xAI token stored only locally  
   - Open-Meteo for sun times / geocoding  
   - No account system in v1  

5. **Screenshots** (desktop 1920×1080 or 1366×768)  
   Capture 3–5 from running app: list, search, settings, sun strip.  
   Save under `store-assets\`.

6. **Pause Dropbox** before any bulk package build tomorrow.

---

## Tomorrow (~20 h) — submit day checklist

### A. Package (MSIX)

**Preferred:** Visual Studio → open solution → add **Windows Application Packaging Project** targeting `OpeningTimesWidget`, set applications, create app packages for Store.

**Alt:** MSIX Packaging Tool (Microsoft Store) — package  
`publish\win-x64\OpeningTimesWidget.exe` after:

```
cd "C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores"
dotnet publish OpeningTimesWidget\OpeningTimesWidget.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64
```

Manifest template:  
`Package\Package.appxmanifest`  
(version `1.0.0.0` → bump for each Store upload)

### B. Partner Center listing

| Field | Suggested |
|-------|-----------|
| **Product name** | Opening Times - Stores |
| **Description (EN)** | Floating desktop helper for favourite store opening hours, specials/sales, search, and local sunrise/sunset. |
| **Description (SV optional)** | Desktopwidget för favoritbutikers öppettider, erbjudanden och soluppgång/nedgång. |
| **Category** | Productivity / Utilities |
| **Age rating** | Complete questionnaire (likely PEGI 3 / Everyone) |
| **Privacy policy URL** | (your hosted URL) |
| **Support contact** | your email |
| **Website** | linespotting.com or GitHub |

### C. Store images (minimum)

| Asset | Size |
|-------|------|
| Store logo | 50×50 |
| Square 44 / 71 / 150 / 310 | as named |
| Wide 310×150 | tile |
| Splash 620×300 | optional for desktop |
| **Screenshots** | at least 1 desktop (1920×1080 preferred) |

Source art: `logo\icon favicon app icon.png`, `logo\logo orgiginal.jpg`

### D. Submission

1. Packages → upload MSIX / MSIX bundle  
2. Store listings → EN (+ SV if ready)  
3. Properties → category, support  
4. Age ratings → save  
5. Privacy → URL  
6. **Submit for certification**

Certification often **1–3 days**.

---

## Identity (fill when known)

```
Package name:     Linespotting.OpeningTimesStores
Publisher CN:     CN=________________  (from Partner Center)
Package family:   (auto after first package)
Version:          1.0.0.0
```

---

## Pre-submit smoke (do before upload)

- [ ] App starts, 9 stores visible, no crash  
- [ ] Search does **not** resize window wildly  
- [ ] Settings: theme, token, sun place  
- [ ] Sun strip shows ↑ / ↓ for Kalmar  
- [ ] Store name click opens browser  
- [ ] No secrets in package (grep `xai-` in publish folder)  
- [ ] `api-passwords.md` **not** in package  

```
cd "C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores"
Get-ChildItem publish -Recurse | Select-String -Pattern "xai-" -SimpleMatch
```

---

## Notes / risks

- **Full trust** desktop apps are allowed but Partner Center may ask “why full trust” → local filesystem settings + WinForms desktop widget.  
- **Internet** capability required for sun + optional xAI.  
- Do **not** commit real xAI token; only LocalAppData.  
- GitHub org for code: **LinespottingOrg** only (never LinespottingPrivate as owner).

---

## Agent next session prompt (copy-paste)

```
Open Opening Times - Stores under Dropbox Microsoft Store APPS.
We submit to Partner Center tomorrow. Publisher CN is: <paste>.
Package MSIX for Store, fill Package.appxmanifest, verify no secrets,
and walk me through upload at partner.microsoft.com apps-and-games.
```

---

**Status 2026-08-13:** code publish folder prepared; manifest draft; listing plan ready. Package signing identity waits on Partner Center CN.

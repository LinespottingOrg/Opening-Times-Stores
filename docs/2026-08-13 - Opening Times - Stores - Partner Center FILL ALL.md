# Partner Center — FILL ALL (copy-paste pack)
**Product:** Opening Times - Stores · **v1.0.0** · **2026-08-13**  
**Dashboard:** https://partner.microsoft.com/en-US/dashboard/apps-and-games/overview  

Use this file as the single source while filling the form.  
**Accessibility attestation:** leave **unchecked / No** (smoke-tested; does not fully meet guidelines).

---

## 1) Product basics

| Field | Value |
|-------|--------|
| **Product name** | Opening Times - Stores |
| **Publisher display name** | Linespotting |
| **Package / product type** | Windows desktop (Win32 EXE via package URL) *or* MSIX if that path |
| **Primary language** | English (United States) — `en-US` |
| **Additional language** | Swedish (Sweden) — `sv-SE` *(optional listing; primary UI is English)* |
| **Category** | Productivity |
| **Secondary category** | Utilities & tools *(if asked)* |
| **Markets** | All markets / at least Sweden + United States |
| **Pricing** | Free |
| **This product has been tested to meet accessibility guidelines** | **No / leave unchecked** |

---

## 2) Package URL + installer

### Package URL *
```
https://downloads.linespotting.com/opening-times-stores/downloads/1.0.0/setup.exe
```

### Installer parameters *
```
/S
```

(Also valid: `/silent` `/quiet` `/q` `/VERYSILENT` `/qn`)

### Installer handling — documentation URL *
```
https://downloads.linespotting.com/opening-times-stores/docs/installer-return-codes.html
```

### EXE return codes (standard scenarios)

| Scenario | EXE return code |
|----------|-----------------|
| Installation cancelled by user | `1602` |
| Application already exists | `1638` |
| Installation already in progress | `1618` |
| Disk space is full | `112` |
| Reboot required | `3010` |
| Network failure | `1619` |
| Package rejected during installation | `1625` |
| Installation successful | `0` |

### Miscellaneous install failure

| Field | Value |
|-------|--------|
| **Code** | `1603` |
| **URL** | `https://downloads.linespotting.com/opening-times-stores/docs/installer-return-codes.html` |

---

## 3) Store listing — English (United States)

### Description (short / subtitle) — if separate field
```
Desktop widget for store opening hours, offers, and sun times.
```

### Description (full)
```
Opening Times - Stores is a lightweight Windows desktop widget that keeps your favourite shops’ opening hours in one place.

See today’s hours at a glance, specials and sale alerts, search and quick-add stores, and local sunrise and sunset for a place you choose (default Kalmar). Optional weekly refresh can use your own xAI API token stored only on your PC.

Features:
• Favourite stores with today’s open/closed status
• “Closes in…” timer with colour urgency near closing time
• Specials / sale indicators
• Search and suggestions for the right store name
• Click a store name to open its website or map
• Sunrise and sunset for your selected place
• Dark and light theme
• Settings for API token and sun location

Designed for everyday errands around Kalmar and beyond—pin the stores you actually use.
```

### What’s new (1.0.0)
```
Initial release: store hours widget, specials, search, sun times, dark/light theme, and settings.
```

### Keywords / search terms (if free text)
```
opening hours, store hours, shops, Kalmar, sunrise, sunset, desktop widget, specials, retail
```

### Support info

| Field | Value |
|-------|--------|
| **Support contact email** | davidrad@gmail.com |
| **Support / website** | https://linespotting.com |
| **Privacy policy URL** | https://downloads.linespotting.com/opening-times-stores/docs/privacy.html |

---

## 4) Store listing — Swedish (optional, if you added sv-SE)

### Kort beskrivning
```
Desktopwidget för butikers öppettider, erbjudanden och soltider.
```

### Full beskrivning
```
Opening Times - Stores är en enkel Windows-widget som samlar öppettider för dina favoritbutiker.

Se dagens tider, erbjudanden och reor, sök och lägg till butiker, samt soluppgång och solnedgång för valfri ort (standard Kalmar). Valfri veckouppdatering kan använda din egen xAI-nyckel som bara sparas lokalt på datorn.

Funktioner:
• Favoritbutiker med öppet/stängt i dag
• Timer “stänger om…” med färg nära stängning
• Erbjudanden / reor
• Sök och förslag på rätt butiksnamn
• Klicka på namn för webbplats eller karta
• Soluppgång och solnedgång
• Mörkt och ljust tema

Perfekt för vardagsärenden—håll koll på butikerna du faktiskt använder.
```

---

## 5) Age ratings / properties (typical answers)

| Topic | Suggested answer |
|-------|------------------|
| User account required | **No** |
| Third-party accounts | **No** (optional user-supplied API token only) |
| Online multiplayer | **No** |
| Sharing personal info | **No** |
| Unrestricted web access | **Limited** — opens store/map links; API for sun/hours if enabled |
| Location | **Approximate / user-chosen place name** for sun times only (not continuous GPS tracking) |
| In-app purchases | **No** |
| Advertising | **No** |
| Expected rating | **Everyone / PEGI 3** (complete the questionnaire truthfully) |

---

## 6) System requirements

| Field | Value |
|-------|--------|
| **OS** | Windows 10 version 1903 or later / Windows 11 |
| **Architecture** | x64 |
| **Notes** | Self-contained installer; no separate .NET install required |

---

## 7) Notes for “Notes for certification” (optional)

```
Win32 desktop widget (full trust). Installs per-user to %LocalAppData%\Programs\OpeningTimesStores with silent switch /S.
Internet used for Open-Meteo (sunrise/sunset + geocoding) and optional xAI if the user pastes their own token in Settings.
No forced account. Privacy policy: https://downloads.linespotting.com/opening-times-stores/docs/privacy.html
Installer return codes: https://downloads.linespotting.com/opening-times-stores/docs/installer-return-codes.html
```

---

## 8) Assets still on you (cannot remote-fill)

| Asset | Where |
|-------|--------|
| Screenshots (1–5 desktop) | Capture the running app; save under `store-assets\` |
| Store logos if separate from package | `Package\Images\` already generated for MSIX |
| Publisher CN if MSIX path | From Partner Center account |

---

## 9) Do / don’t

| Do | Don’t |
|----|--------|
| Use the **downloads.linespotting.com** package URL | Use GitHub release URL for Package URL |
| Languages: **en-US** primary | Claim full accessibility compliance |
| Privacy URL above | Put xAI token in listing text |

---

*End of fill pack — open Partner Center and paste field by field.*

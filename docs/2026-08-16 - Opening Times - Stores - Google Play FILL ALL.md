# Google Play FILL ALL — Opening Times - Stores

**Date:** 2026-08-16  
**Play Console:** app created (2026-08-16) · short ref **`app.openingtimes`**  
**Package (AAB / applicationId):** `app.openingtimes`  
**versionName:** `1.0.0` · **versionCode:** `1`  
**Binary:** local AAB only (`gradlew bundleRelease`) — **never EAS cloud**  
**targetSdk:** 35 · **compileSdk:** 36 · upload-key signed  

> **Lock rule:** The first AAB you upload sets the permanent package name in Play.  
> Our binary is **`app.openingtimes`**. If Console already forced a *different* package, say so — we rebuild before first upload.

---

## Create app (Play Console) — DONE

| Field | Value |
|-------|--------|
| App name | Opening Times - Stores |
| Short / internal ref | `app.openingtimes` |
| Default language | English (United States) — add Swedish after |
| App or game | App |
| Free or paid | Free |
| Status | **Created** — next: listing + AAB upload |

---

## Store listing (EN)

**Short description** (≤80 chars):

```
Today's store opening hours on your phone and Android home widget.
```

**Full description:**

```
Opening Times - Stores shows today's opening hours for your favourite stores — at a glance on your phone, and as an optional Android home-screen widget.

• See which stores are open, closing soon, or closed
• Colour status for open / closing soon / closed
• Optional specials (e.g. green sales) when listed
• Home widget: long-press home → Widgets → Opening Times - Stores
• No account required for core features
• No ads and no in-app purchases in this version

Publisher: Linespotting AB
Privacy: https://downloads.linespotting.com/opening-times-stores/docs/privacy.html
```

**Swedish short (optional):**

```
Dagens öppettider för dina favoritbutiker — app och hemskärms-widget.
```

---

## Graphics

| Asset | Path / note |
|-------|-------------|
| App icon (512×512) | Use `store-assets\storelogo-1080x1080.png` scaled, or `logo\` + `mobile\assets\icon.png` |
| Feature graphic 1024×500 | Create if missing (dark #1C1C20 + title) |
| Phone screenshots | Capture after install (list + widget) — min 2 |
| Hi-res icon | Must match launcher (L21) |

Canonical art:

```
C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores\logo\
C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores\store-assets\
C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores\mobile\assets\icon.png
```

---

## Privacy / User Data (L25) — required before upload

| Item | Value |
|------|--------|
| Privacy policy URL | `https://downloads.linespotting.com/opening-times-stores/docs/privacy.html` |
| In-app link | Footer **Privacy policy** (same URL) |
| Support email | `info@linespotting.com` |
| Data safety | No collection of personal data for ads/analytics; store list stays on device; no Play Billing |

**Data safety form (suggested):**

- Does your app collect or share user data? **No** (for this 1.0.0 mobile build: local store list only; no analytics SDKs, no accounts, no ads)
- If Console forces categories: only declare what the binary actually does — local app data / files if listed as “app activity” not shared

**Report line:**

```
PRIVACY: PASS — url=https://downloads.linespotting.com/opening-times-stores/docs/privacy.html · in_app=footer · names_app=yes · data_safety=no collection (1.0.0 mobile)
```

---

## App content

| Section | Answer |
|---------|--------|
| Ads | No |
| In-app products | No |
| Target audience | 13+ / general utility (not designed for children) |
| News app | No |
| COVID | No |
| Data safety | Match privacy (on-device only) |
| Government apps | No |
| Financial features | No |
| Health | No |

---

## Categories / contact

| Field | Value |
|-------|--------|
| Category | Tools / Lifestyle (pick one; Tools preferred) |
| Email | info@linespotting.com |
| Website | https://linespotting.com |
| Phone | optional |

---

## Upload binary

1. Play Console → **Create app** → Testing / Internal testing first recommended  
2. Upload AAB from:

```
C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores\mobile\android\app\build\outputs\bundle\release\app-release.aab
```

(or copy under `mobile\dist\` if staged there)

3. First upload enrolls **Play App Signing** with this upload certificate (SHA1 in vault)  
4. Do **not** lose keystore:

```
C:\Users\User\Dropbox\aiprojekt\APPAR\All Keys for all apps\03-android-keystores\Opening-Times-Stores\
```

---

## Rebuild (local only)

**Pause Dropbox sync first**, then PowerShell:

```
cd "C:\b\ots\android"
# if junction missing:
# cmd /c mklink /J "C:\b\ots" "C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores\mobile"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21.0.11.10-hotspot"
.\gradlew.bat bundleRelease
```

Short junction `C:\b\ots` avoids Windows path-length / ninja failures under the long Dropbox path.

**Never:** `eas build`

---

## Pre-upload gate

```
[ ] P01–P09 Privacy URL live + in-app
[ ] I01–I04 Icon matches logo / listing
[ ] T01 Local AAB (gradlew)
[ ] T02 targetSdk 35+
[ ] T03 Upload-key signed (not debug)
[ ] T04 versionCode 1 first release
[ ] T05 No secrets in AAB
[ ] L01 Screenshots current UI
```

---

## Identity lock

| | |
|--|--|
| Package | `app.openingtimes` |
| Display name | Opening Times - Stores |
| Upload alias | `ots-upload` |
| Org GitHub | LinespottingOrg only (if repo created) |

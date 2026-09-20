# File index

| Path | Description |
|------|-------------|
| OpeningTimesWidget.sln | Solution |
| OpeningTimesWidget/OpeningTimesWidget.csproj | WinForms net8.0-windows |
| OpeningTimesWidget/Program.cs | Entry |
| OpeningTimesWidget/MainForm.cs | Widget UI, desktop layer, context menu |
| OpeningTimesWidget/Models/StoreModels.cs | Store, watches, settings models |
| OpeningTimesWidget/Services/SettingsStore.cs | LocalAppData JSON + samples |
| OpeningTimesWidget/Services/XaiUpdateService.cs | Weekly xAI refresh |
| README.md | Run notes |
| .gitignore | bin/obj + secrets |

| claude design/2026-08-09 - Opening Times - Stores - Design Handover.md | Full UI design handover for Claude |
| claude design/README.md | Pointer to handover |

| OpeningTimesWidget/Theme/AppTheme.cs | Dark/light named color tokens (Claude 1c) |
| OpeningTimesWidget/Controls/StoreRowControl.cs | Large store row + chips + green rail |
| OpeningTimesWidget/Controls/ThemeToggle.cs | Sun/moon theme slider |
| OpeningTimesWidget/Assets/* | App logo/icon from logo/ |
| OpeningTimesWidget/Assets/app.ico | Multi-size Windows icon 16–256 (2026-09-20) |
| OpeningTimesWidget/Services/WeatherService.cs | MET/yr.no Locationforecast 2.0 + cache + outdoor sun windows |
| OpeningTimesWidget/Controls/WeatherBar.cs | yr-style current + hourly strip |
| OpeningTimesWidget/Controls/WeatherGlyph.cs | Sun/cloud/rain/snow glyphs |
| scripts/2026-09-20 - Opening Times - Make icons.py | Crop WhatsApp bag + emit ICO / Play / mipmap |
| logo/source/2026-09-20 - Opening Times - WhatsApp crop.png | Tight square crop of David WhatsApp JPEG |
| mobile/assets/icon.png | Was missing; Expo app icon (bag) |
| mobile/assets/android-icon-foreground.png | Was missing; adaptive foreground (bag) |
| claude design/2026-08-09 - Opening Times - Stores - Design Implementation Notes.md | What was implemented |

| claude design/2026-08-13 - Opening Times - Stores - Claude Coworker Handover.md | Full Partner Center + app handover for Claude |
| docs/2026-08-13 - Opening Times - Stores - Partner Center FILL ALL.md | Copy-paste field values |
| docs/privacy.html | Live privacy policy |
| docs/installer-return-codes.html | Live return codes doc |
| download-worker/ | CF Worker + R2 package host |
| Installer/ | Silent setup.exe project |

## 2026-08-16 Google Play
- mobile/app.json — versionCode 1, expo-build-properties compile 36 / target 35
- mobile/signing/* — upload keystore (gitignored)
- mobile/dist/OpeningTimesStores-1.0.0-vc1-release.aab — Play upload binary
- mobile/scripts/build-play-aab.ps1 — local rebuild helper
- docs/2026-08-16 - Opening Times - Stores - Google Play FILL ALL.md
- docs/privacy.html — multi-platform policy (R2 live)

- docs/2026-08-16 - Opening Times - Stores - Play API fill log.json — API fill results
- mobile/dist/OpeningTimes-1.0.0-vc1-app.openingtimes.aab — Play package app.openingtimes


- store-assets/play-screenshots/phone-01..08.png — Play phone SS (2026-08-17)


## 2026-08-18
- dist/installer/1.0.1/setup.exe — Opening Times EU silent setup 1.0.1
- docs/2026-08-18 - Opening Times EU - Partner Center FILL ALL.md
- docs/2026-08-18 - Opening Times EU - Windows Store READY.md
- docs/2026-08-18 - Opening Times - Stores - Mac iOS Handover.md
- store-assets/asc-icon-1024.png
- logo/QUARANTINE-DO-NOT-SHIP/ — bad Microsoft wordmark logo

- claude design/2026-08-18 - Opening Times EU - Claude Coworker Handover.md — SS/listing finish handover


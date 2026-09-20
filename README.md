# Opening Times EU

Desktop widget for **store opening hours** and **weather** — all in the window, no browser tabs.

## What it shows

- Favourite stores with **today’s hours**, closes-in / opens-at, and a **full week line**
- Specials / sale chips
- Weather on the **right**: Stora Frö, Kalmar, Karleby (yr.no / MET Norway)
- Temperature **green ≥ 0 °C**, **blue below 0 °C**
- Next 12 hours, sunrise / sunset, outdoor-sun hint
- Click a weather place to switch the hourly strip **inside** the widget

## Run (Windows)

```
cd "C:\Users\User\Dropbox\aiprojekt\Microsoft Store APPS\Opening Times - Stores"
dotnet run --project OpeningTimesWidget
```

## Privacy / downloads

```
https://downloads.linespotting.com/opening-times-stores/docs/privacy.html
https://downloads.linespotting.com/opening-times-stores/downloads/1.0.2/setup.exe
```

Weather uses MET Norway Locationforecast 2.0 (same data as yr.no). Sun times via Open-Meteo. No Linespotting account required.

## Token (local only)

Optional xAI weekly hours refresh. Stored in:

```
%LocalAppData%\OpeningTimesWidget\settings.json
```

Never commit tokens.

## Repo

```
https://github.com/LinespottingOrg/Opening-Times-Stores
```

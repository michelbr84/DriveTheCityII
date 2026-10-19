# START HERE — Play Drive the City II (private build)

| Field | Value |
|---|---|
| ID | DOC-START-HERE |
| Document status | Draft (kept current with the latest local package) |
| Last updated | 2026-10-19 |

This is a **private, local** build of a Cities: Skylines II code mod. Nothing here is published. The steps below were
verified on this machine (CS2 1.6.2f1, Steam) unless marked otherwise.

## 1. Install (one command)

Open **PowerShell** in the project folder and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\deploy.ps1 -Install
```

This copies the last good package (`%LOCALAPPDATA%\DriveTheCityII\packages\LATEST.txt`) into the game's local mods folder
`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DriveTheCityII\`. It refuses to run while the game
is open and never touches other mods or saves.

- Check: `tools\deploy.ps1 -Status`
- Undo the last update: `tools\deploy.ps1 -Rollback`
- Remove completely: `tools\deploy.ps1 -Uninstall`
- Rebuild from source first (optional): `tools\build.ps1` (needs the private .NET SDK described in
  [BUILD_AND_RUN](docs/engineering/BUILD_AND_RUN.md))

## 2. Start the game

Start Cities: Skylines II **from Steam as usual** and load any city. The mod does nothing until you press the
character-mode key. Do not install the Beta Test Driving Mod at the same time: Drive the City II detects it and stays
inactive.

Optional disposable test city (no risk to your saves): the developer test session
`tools\test-session.ps1 -Start` launches the game, and the bridge command `benchcity` loads the game's built-in benchmark
city as `DTC2-LAB-...`. Stop with `tools\test-session.ps1 -Stop` (restores the "Continue" pointer).

## 3. Controls (defaults; change them in Options → Drive the City II → Controls)

| Key | On foot | Driving |
|---|---|---|
| **F8** | Enter / leave character mode (also the "Drive the City" button, top left) | Leave (stop first) |
| W / S | Walk forward / back | Throttle / brake, then reverse when stopped |
| A / D | Walk left / right | Steer |
| Shift | Run | — |
| Mouse | Look around | Look around (camera returns behind the car) |
| **E** | Enter the vehicle you stand next to | Get out (stop first) |
| V | Third-person / first-person | Chase / hood camera |
| Space | — | Handbrake |
| Tab | Jobs, garage, company, challenges, profile panel (frees the mouse) | same |
| Backspace | Move to a safe spot (recovery) | same |
| Esc | Close the panel, otherwise the game's pause menu | same |
| Alt (hold) | Free the mouse cursor | same |

Gamepad bindings exist (left stick, triggers, A enter/exit, B handbrake, Y camera, X panel) but were **not tested** (no
gamepad was available).

## 4. What you can do

See [MORNING_REPORT](MORNING_REPORT.md) for exactly what is verified in game and what is not yet.

1. **Walk around your city.** Press F8 in any city. Your character appears on a nearby sidewalk. Walk with WASD, run
   with Shift, and look around with the mouse. V switches between third and first person.
2. **Get a car.** Press **Tab**, open **Garage**, and choose **Rent** on the *Basic rental compact*. It is always
   available, even with no money, because its fee is taken from what you earn. The car appears at the curb; walk to
   it and press **E**.
3. **Drive.** W/S accelerate and brake. Hold S at a standstill to reverse. A/D steer, Space is the handbrake, V
   changes the camera. City traffic stops behind you; buildings, poles and cars stop you.
4. **Take a job.** Tab → **Jobs**. Pick a career (Taxi to start), then **Accept** an offer. A marker and a route line
   show where to go. Stop inside the marker to pick up or drop off. The HUD card (top right) shows the objective,
   distance and pay. You are paid when the job ends; the breakdown appears on screen.
5. **Progress.** Jobs give XP per career and reputation. Higher levels unlock van rental and purchase, freight (rigid
   trucks), bus shifts, a taxi you can buy, and finally founding a company.
6. **Look after vehicles.** Owned and rented vehicles use fuel and wear down. The city has no fuel stations, so
   **service points** (blue markers: parking facilities and depots) are where you refuel, repair, rent, buy and sell.
   If you run dry or break down, Tab offers roadside help or a tow.
7. **Home, challenges, company.**
   - Profile tab: set the nearest house as your home and the current service point as your garage.
   - Challenges tab: timed checkpoint and parking runs with medals.
   - Company tab, once unlocked: hire drivers, buy fleet vehicles, take contracts. The company's results are simulated;
     its vehicles do not drive around the city.
8. **Saving.** Save the city normally. Your career is stored inside that save, so each save keeps its own money and
   progress. Your character and cars are never written into the city itself.
9. **Back to building.** Press F8 at any time when you are on foot, or after stopping the car.

## 5. If something goes wrong

- Stuck or in a strange place: **Backspace** (recovery), or **F8** to return to city building.
- The mod can be stopped for the session without uninstalling: Options → Drive the City II → Safety → Safe-disable.
- Language, HUD size, high contrast and vehicle sounds: Options → Drive the City II → General → Display. "Mod language"
  can be the game's language, English, or Português (Brasil).
- Logs: `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\DriveTheCityII.log`.
- Career backups: `...\Cities Skylines II\ModsData\DriveTheCityII\` (one file per save revision; invalid data is copied to
  `quarantine\`, never deleted).
- Your character and vehicles are never written into the city save; career data is stored inside each save, so loading an
  older save restores that save's money and progress.

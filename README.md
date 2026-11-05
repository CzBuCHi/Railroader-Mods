# Tweaks for Railroader

HUD, camera, map, and gameplay tweaks for **Railroader 0.6.x**.

## Locomotive controls

- **Control modes:** Manual, AE Road, AE Yard buttons replace dropdown.
- **Roster Shift-click:** follow locomotive and select it for control HUD.

## Train maintenance

Buttons beside speed display appear when selected train needs attention.

| Button | Click | Shift-click: whole coupled train |
| --- | --- | --- |
| Air problem | Follow faulty coupler, even while moving. | Connect hoses, open internal anglecocks, close free ends. |
| Handbrake | Show car with applied handbrake. | Release handbrakes. |
| Low oil / hotbox | Follow bearing oiling point. | Refill eligible cars to 100%. |

- Orange: oil below **50%** by default. Red: hotbox; takes priority.
- Oil refill: **5 MPH maximum**. Hotboxes still need repair.

## Car inspection and assignments

- Live wagon speed in MPH.
- Live bearing oil percentage on supported vehicles.
- Copy crew to eligible coupled vehicles. None clears assignments.
- Copy repair destination to eligible coupled cars.

## Map

### Vehicle display

Select mode in upper-right **Render** dropdown:

| Mode | Vehicles shown |
| --- | --- |
| Locomotives only | Locomotives. Default. |
| Locomotives and cabooses | Locomotives + cabooses. |
| All vehicles | All vehicles, including detached tenders. |

- Destination colors, including locomotive repair/depot destinations. Spotted cars darker; unassigned gray.
- Number labels on locomotives only.
- Steam locomotive + attached rear tender share one icon.

### Navigation

- **Follow selected car:** track selected vehicle. Map drag stops following.
- **Shift-click vehicle:** open car inspector.
- **Shift-click station:** open station window and refresh freight list, even from far away.

### Switch positions

- **Save:** record reversed switches.
- **Restore:** apply saved positions; unlisted switches become normal.
- Occupancy, CTC locks, and permissions respected.

## Points of interest

- **Alt+T:** open saved camera views (Shortcut configurable).
- Save first-person position/direction or tracking-camera position/rotation/zoom. Following a train still saves a fixed location.
- Group bookmarks. Add, rename, move, replace, delete. Reorder groups.
- Replace/delete require confirmation. Rename/move preserve saved view.
- Resizable main window and separate editor. Optional auto-open on world load.
- Distant jumps load terrain first.

## Distances, time, and pushing

- **Metric distances:** meters below 150 m; kilometers from 150 m, up to two decimals. Includes tutorial distance helpers and train length. Speed stays MPH.
- **Sleep:** advance to next enabled interchange service, including extra service.
- **Manual pushing:** default cap **6 MPH** instead of 3 MPH. Multiplayer: host needs Tweaks.

## License

[MIT](LICENSE).

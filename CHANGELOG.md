# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
The project has no released version yet; all changes are under "Unreleased".

## [Unreleased]

### Added
- Tray menu that lists every paired Bluetooth device (Classic and BLE) and connects or disconnects
  it with a click.
- Favourites: starred devices sit at the top, the rest under "Other devices"; without a star the
  list stays flat.
- Accessible "Manage Favorites" dialog built on a standard `CheckedListBox` (space bar toggles,
  screen reader announces the state).
- Per-entry state indicators (`●` connected, `○` not connected, `◌` attempt running, `★`
  favourite), each spelled out in `AccessibleName`.
- Windows notifications for the start, success and failure of a connection attempt.
- Settings dialog for language, colour mode and autostart. Language and colour mode in
  `%APPDATA%\StarTooth\settings.json`, autostart in the registry Run key.
- Localisation via satellite assemblies: English (neutral culture) and German, using the
  terminology of the Windows interface.
- Light/dark mode that follows the Windows setting or can be fixed.
- Icon drawn at runtime (a Bluetooth rune on a star).

### Known limitations
- The connect path (`BluetoothSetServiceState`) has not yet been tested on real hardware.
- The announcement of the notifications by NVDA has not yet been verified aloud.

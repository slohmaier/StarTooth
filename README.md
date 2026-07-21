# StarTooth

Ein Windows-Tray-Tool, das alle gepairten Bluetooth-Geräte auflistet und per Klick verbindet
oder trennt. Geräte lassen sich mit einem Stern markieren; Favoriten stehen dann oben in der
Liste, alle übrigen darunter unter „Weitere Geräte“.

![StarTooth](docs/icon.png)

## Bedienung

| Aktion | Wirkung |
| --- | --- |
| Links- oder Rechtsklick aufs Tray-Icon | Geräteliste öffnen |
| Klick oder Eingabetaste auf einem Gerät | verbinden bzw. trennen |
| „Favoriten verwalten…“ | Dialog zum Setzen der Sterne |

Verbundene Geräte werden fett und mit Haken dargestellt. Solange kein einziger Stern vergeben
ist, bleibt die Liste flach und ungruppiert.

### Barrierefreiheit

Das Menü kommt ohne Mausgesten und ohne Modifier-Tasten aus: Die Sterne werden nicht im Menü
selbst vergeben, sondern in einem eigenen Dialog mit einer Standard-`CheckedListBox`, in der die
Leertaste umschaltet und der Screenreader den Zustand von sich aus ansagt.

Fettdruck und Sternsymbol im Menü sind rein visuell und deshalb nicht die einzige Quelle für den
Zustand — jeder Eintrag trägt ihn zusätzlich im `AccessibleName` („OpenFit by Shokz, Favorit,
verbunden“) und im `AccessibleDescription`, das die Wirkung des Aktivierens beschreibt. Alle
Menüpunkte und Dialogelemente haben Zugriffstasten.

### Dark Mode

StarTooth folgt der Windows-Einstellung für App-Farben, auch bei einem Wechsel zur Laufzeit.
WinForms-Menüs bleiben unabhängig davon hell, deshalb bringt `ThemedMenuRenderer` eigene Farben
mit; die übrigen Steuerelemente laufen über `Application.SetColorMode`.

## Aufbau

| Datei | Zweck |
| --- | --- |
| `Native/BluetoothApis.cs` | P/Invoke auf `bthprops.cpl` (Radios, Geräte, `BluetoothSetServiceState`) |
| `Bluetooth/ClassicBluetooth.cs` | Classic-Geräte auflisten und verbinden |
| `Bluetooth/LowEnergyBluetooth.cs` | BLE über WinRT |
| `Bluetooth/DeviceService.cs` | führt beide Listen zusammen und cached sie |
| `Favorites.cs` | Sterne, gespeichert in `%APPDATA%\StarTooth\favorites.json` |
| `FavoritesForm.cs` | barrierefreier Dialog zum Vergeben der Sterne |
| `TrayApplicationContext.cs` | Tray-Icon und Menüaufbau |
| `TrayIcons.cs` | zeichnet das Icon zur Laufzeit |
| `Theme.cs`, `ThemedMenuRenderer.cs` | Light-/Dark-Mode |

Windows bietet keine allgemeine „Connect“-API. Für Classic-Geräte schaltet StarTooth deshalb
über `BluetoothSetServiceState` alle installierten Dienste des Geräts ein bzw. aus, was den
Verbindungsaufbau auslöst. Bei BLE gibt es auch das nicht: dort entsteht die Verbindung als
Nebenwirkung eines GATT-Zugriffs und hält nur, solange das Geräteobjekt am Leben bleibt.

## Bauen

```powershell
dotnet build
.\bin\Debug\net9.0-windows10.0.19041.0\StarTooth.exe
```

Benötigt das .NET 9 SDK und Windows 10 Build 19041 oder neuer.

### Diagnose

```powershell
StarTooth.exe --list                      # gepairte Classic-Geräte auflisten
StarTooth.exe --connect AA:BB:CC:DD:EE:FF # Verbindung testen
StarTooth.exe --disconnect AA:BB:CC:DD:EE:FF
StarTooth.exe --render-icon <verzeichnis> # Icon als PNG ausgeben
StarTooth.exe --render-dialog <datei.png> # Favoritendialog als PNG ausgeben
```

## Status

Enumeration ist gegen echte Hardware verifiziert. Der Connect-Pfad
(`BluetoothSetServiceState`) ist implementiert, aber noch nicht am Gerät getestet.

## Lizenz

MIT — siehe [LICENSE](LICENSE).

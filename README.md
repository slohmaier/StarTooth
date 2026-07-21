# StarTooth

Ein Windows-Tray-Tool, das alle gepairten Bluetooth-Geräte auflistet und per Klick verbindet
oder trennt. Geräte lassen sich mit einem Stern markieren; Favoriten stehen dann oben in der
Liste, alle übrigen darunter unter „Weitere Geräte“.

![StarTooth](docs/icon.png)

## Bedienung

| Aktion | Wirkung |
| --- | --- |
| Links- oder Rechtsklick aufs Tray-Icon | Geräteliste öffnen |
| Klick auf ein Gerät | verbinden bzw. trennen |
| **Strg + Klick** auf ein Gerät | Stern setzen oder entfernen |

Verbundene Geräte werden fett und mit Haken dargestellt. Solange kein einziger Stern vergeben
ist, bleibt die Liste flach und ungruppiert.

## Aufbau

| Datei | Zweck |
| --- | --- |
| `Native/BluetoothApis.cs` | P/Invoke auf `bthprops.cpl` (Radios, Geräte, `BluetoothSetServiceState`) |
| `Bluetooth/ClassicBluetooth.cs` | Classic-Geräte auflisten und verbinden |
| `Bluetooth/LowEnergyBluetooth.cs` | BLE über WinRT |
| `Bluetooth/DeviceService.cs` | führt beide Listen zusammen und cached sie |
| `Favorites.cs` | Sterne, gespeichert in `%APPDATA%\StarTooth\favorites.json` |
| `TrayApplicationContext.cs` | Tray-Icon und Menüaufbau |
| `TrayIcons.cs` | zeichnet das Icon zur Laufzeit |

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
```

## Status

Enumeration ist gegen echte Hardware verifiziert. Der Connect-Pfad
(`BluetoothSetServiceState`) ist implementiert, aber noch nicht am Gerät getestet.

## Lizenz

MIT — siehe [LICENSE](LICENSE).

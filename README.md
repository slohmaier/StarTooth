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
| „Einstellungen…“ | Sprache, Farbmodus, Autostart |

Jeder Eintrag trägt einen Indikator für seinen Zustand:

| Indikator | Bedeutung |
| --- | --- |
| `●` | verbunden (zusätzlich fett) |
| `○` | nicht verbunden |
| `◌` | Verbindungsversuch läuft gerade |
| `★` | Favorit |

Solange kein einziger Stern vergeben ist, bleibt die Liste flach und ungruppiert.

Ein Verbindungsversuch, sein Ergebnis und jeder Fehlschlag werden zusätzlich als Windows-
Benachrichtigung gemeldet. Das ist nötig, weil das Menü sich beim Aktivieren eines Eintrags
schließt und den Fortschritt deshalb nicht selbst anzeigen kann.

### Barrierefreiheit

Das Menü kommt ohne Mausgesten und ohne Modifier-Tasten aus: Die Sterne werden nicht im Menü
selbst vergeben, sondern in einem eigenen Dialog mit einer Standard-`CheckedListBox`, in der die
Leertaste umschaltet und der Screenreader den Zustand von sich aus ansagt.

Fettdruck und die Symbole `● ○ ◌ ★` sind rein visuell. Screenreader sprechen Sonderzeichen je
nach eingestellter Symbolebene unterschiedlich oder gar nicht aus, deshalb ist keines davon die
einzige Quelle für seinen Zustand: Jeder Eintrag führt ihn ausgeschrieben im `AccessibleName`
(„Shokz OpenFit, Favorit, Verbunden“) und die Wirkung des Aktivierens im `AccessibleDescription`.

Laufende Verbindungsversuche bleiben bewusst aktivierbar statt `Enabled = false` zu setzen:
ToolStrip überspringt deaktivierte Einträge bei der Tastaturnavigation, womit ausgerechnet der
laufende Versuch der einzige Zustand wäre, den man mit Tastatur oder Screenreader nie erreicht.
Ein zweiter Aufruf wird stattdessen im Aufrufer abgelehnt.

Die Überschrift „Weitere Geräte“ ist als deaktivierter Eintrag ebenfalls nicht anspringbar. Das
ist unkritisch, weil sie nichts trägt, was nicht schon im `AccessibleName` jedes Eintrags steht —
Favoriten sind dort als solche benannt.

Alle Menüpunkte und Dialogelemente haben Zugriffstasten.

### Einstellungen

Sprache, Farbmodus und Autostart stehen unter „Einstellungen…“. Sprache und Farbmodus liegen in
`%APPDATA%\StarTooth\settings.json` und wirken sofort — das Menü wird bei jedem Öffnen neu
aufgebaut, Dialoge werden ohnehin frisch erzeugt.

Der Autostart steht bewusst **nicht** in dieser Datei, sondern ausschließlich im
`Run`-Schlüssel der Registrierung (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`). Damit
gibt es nur eine Quelle der Wahrheit: Ein von Hand oder von einem anderen Werkzeug entfernter
Eintrag wird korrekt angezeigt, statt aus einer veralteten Kopie wiederhergestellt zu werden.
Lehnt Windows die Änderung ab, bleibt der Dialog offen und meldet das, statt eine Änderung zu
behaupten, die nicht stattgefunden hat.

Der gewählte Farbmodus wird auf zwei Stellen angewendet, die übereinstimmen müssen: die eigene
Palette in `Theme` und der WinForms-Farbmodus über `Application.SetColorMode`. Bliebe letzterer
auf „System“, während „Hell“ gewählt ist, kämen Auswahlfelder, Kontrollkästchen und Titelleiste
weiterhin dunkel.

### Sprachen

StarTooth folgt der Windows-Anzeigesprache. Enthalten sind Englisch (neutrale Kultur) und
Deutsch als Satellite Assembly; eine weitere Sprache ist genau eine zusätzliche
`Resources/Strings.<kultur>.resx`.

Übersetzt wird mit der Terminologie der jeweiligen Windows-Oberfläche, nicht wörtlich aus dem
Englischen. Im Deutschen heißt es deshalb „gekoppelte Geräte“ und nicht „gepairte“, und der
Zustand ist „Nicht verbunden“ statt „getrennt“ — so wie es in den Windows-Bluetooth-Einstellungen
steht. Die `.resx`-Dateien enthalten zu jedem mehrdeutigen Eintrag einen Kommentar mit der
Bedeutung der Platzhalter, damit spätere Übersetzungen nicht raten müssen.

Zum Prüfen lässt sich die Sprache erzwingen:

```powershell
StarTooth.exe --lang de
StarTooth.exe --lang en-US
```

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
| `TrayApplicationContext.cs` | Tray-Icon, Benachrichtigungen, Ablauf eines Versuchs |
| `DeviceMenuBuilder.cs` | baut die Geräteeinträge samt Indikatoren |
| `TrayIcons.cs` | zeichnet das Icon zur Laufzeit |
| `Theme.cs`, `ThemedMenuRenderer.cs` | Light-/Dark-Mode |
| `Settings.cs`, `SettingsForm.cs`, `Autostart.cs` | Einstellungen und ihre Speicherung |
| `Resources/Strings*.resx`, `Resources/Strings.cs` | Übersetzungen und typisierter Zugriff darauf |

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
StarTooth.exe --render-menu <datei.png>   # Menü mit allen Zuständen als PNG, plus
                                          # Ausgabe der Screenreader-Texte auf der Konsole
StarTooth.exe --render-settings <datei.png>
```

Die Renders taugen für Layout und Text, **nicht für Hintergrundfarben**: `DrawToBitmap` zeichnet
den Fensterhintergrund dunkel, auch wenn `BackColor` weiß und `Application.ColorMode` auf
`Classic` steht. Ob der helle Modus greift, sagen die Werte, die `--render-settings` mit ausgibt
— oder ein Blick auf den laufenden Dialog.

Alle Diagnoseausgaben sind englisch, weil sie sich an Entwickler richten und nicht an Benutzer;
lokalisiert ist nur, was in der Oberfläche erscheint. `--lang` wirkt auch auf `--render-dialog`.

## Status

Enumeration ist gegen echte Hardware verifiziert. Der Connect-Pfad
(`BluetoothSetServiceState`) ist implementiert, aber noch nicht am Gerät getestet.

## Lizenz

MIT — siehe [LICENSE](LICENSE).

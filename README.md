# StarTooth

<img src="docs/icon.png" alt="StarTooth-Symbol" width="64" align="left" hspace="12">

Ein Windows-Tray-Tool, das alle gekoppelten Bluetooth-Geräte auflistet und per Klick verbindet
oder trennt. Geräte lassen sich mit einem Stern markieren; Favoriten stehen dann oben in der
Liste, alle übrigen darunter unter „Weitere Geräte“.

<br clear="left">

![Das Tray-Menü von StarTooth mit Favoriten oben und weiteren Geräten darunter](docs/screenshot-menu.png)

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

![Der Dialog „Favoriten verwalten“ mit einer Kontrollkästchen-Liste aller Geräte](docs/screenshot-favorites.png)

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

![Der Einstellungsdialog mit Auswahl für Sprache, Farbmodus und Autostart](docs/screenshot-settings.png)

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

Zum Prüfen lässt sich die Sprache unabhängig von Windows und von den gespeicherten Einstellungen
erzwingen. Das gilt für die Anwendung wie für die Render-Befehle:

```powershell
StarTooth.exe --lang de
StarTooth.exe --lang en-US
```

**Eine Sprache hinzufügen:**

1. `Resources/Strings.resx` (Englisch) kopieren und nach dem Kulturkürzel benennen, z. B.
   `Resources/Strings.fr.resx` für Französisch oder `Strings.pl.resx` für Polnisch.
2. In der Kopie die `<value>`-Texte übersetzen. Die `<data name="…">`-Schlüssel und die
   Platzhalter (`{0}`, `{1}`) bleiben unverändert; die Kommentare erklären, wofür sie stehen.
3. Das kaufmännische Und (`&amp;`) markiert die Zugriffstaste eines Menüpunkts oder Feldes — im
   Ziel auf einen möglichst eindeutigen Buchstaben setzen, nicht wörtlich übernehmen.
4. Bauen. Das SDK erzeugt aus der neuen `.resx` automatisch eine Satellite Assembly
   (`<kultur>\StarTooth.resources.dll`); es ist kein Eintrag in der Projektdatei nötig.
5. Mit `StarTooth.exe --lang <kultur>` prüfen und die Auswahl in
   [`SettingsForm.cs`](SettingsForm.cs) (Methode `Populate`) um einen Eintrag ergänzen, damit die
   Sprache auch im Einstellungsdialog wählbar ist.

### Dark Mode

Der Farbmodus folgt entweder der Windows-Einstellung (auch bei einem Wechsel zur Laufzeit) oder
wird in den [Einstellungen](#einstellungen) fest auf Hell oder Dunkel gestellt. WinForms-Menüs
bleiben unabhängig davon hell, deshalb bringt `ThemedMenuRenderer` eigene Farben mit; die übrigen
Steuerelemente laufen über `Application.SetColorMode`.

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
| `AppSetup.cs` | wendet Sprache und Farbmodus auf den laufenden Prozess an |
| `Resources/Strings*.resx`, `Resources/Strings.cs` | Übersetzungen und typisierter Zugriff darauf |
| `Spike.cs` | Diagnose- und Render-Befehle (`--list`, `--render-*`), nicht Teil der Oberfläche |

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
StarTooth.exe --list                      # gekoppelte Classic-Geräte auflisten
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
lokalisiert ist nur, was in der Oberfläche erscheint. `--lang <kultur>` lässt sich jedem Aufruf
voranstellen und überschreibt für diesen Lauf sowohl die Windows-Sprache als auch die gespeicherte
Einstellung.

## Status

| Bereich | Stand |
| --- | --- |
| Geräte auflisten (Classic + BLE) | gegen echte Hardware verifiziert |
| Menü, Favoriten, Einstellungen, Sprachen, Dark Mode | verifiziert, u. a. per Render-Befehle |
| Verbinden / Trennen (`BluetoothSetServiceState`) | implementiert, **noch nicht am Gerät getestet** |
| Screenreader-Ausgabe (NVDA) | `AccessibleName`/`-Description` gesetzt und im Text geprüft, akustisch noch nicht gegengehört |

Der Connect-Pfad ist der letzte offene Punkt: `--connect <MAC>` bzw. ein Klick im Menü lösen ihn
aus, geprüft an echter Hardware ist er noch nicht. Siehe [CHANGELOG](CHANGELOG.md) für die
Entwicklung.

## Lizenz

MIT — siehe [LICENSE](LICENSE).

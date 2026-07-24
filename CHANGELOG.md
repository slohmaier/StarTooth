# Changelog

Format nach [Keep a Changelog](https://keepachangelog.com/de/1.1.0/).
Das Projekt hat noch keine veröffentlichte Version; alle Änderungen stehen unter „Unveröffentlicht“.

## [Unveröffentlicht]

### Hinzugefügt
- Tray-Menü, das alle gekoppelten Bluetooth-Geräte (Classic und BLE) auflistet und per Klick
  verbindet oder trennt.
- Favoriten: mit einem Stern markierte Geräte stehen oben, der Rest unter „Weitere Geräte“; ohne
  Stern bleibt die Liste flach.
- Barrierefreier Dialog „Favoriten verwalten“ auf Basis einer Standard-`CheckedListBox`
  (Leertaste schaltet um, Screenreader sagt den Zustand an).
- Zustandsindikatoren je Eintrag (`●` verbunden, `○` nicht verbunden, `◌` Versuch läuft, `★`
  Favorit), jeweils ausgeschrieben im `AccessibleName`.
- Windows-Benachrichtigungen für Beginn, Erfolg und Fehlschlag eines Verbindungsversuchs.
- Einstellungsdialog für Sprache, Farbmodus und Autostart. Sprache und Farbmodus in
  `%APPDATA%\StarTooth\settings.json`, Autostart im `Run`-Schlüssel der Registrierung.
- Mehrsprachigkeit über Satellite Assemblies: Englisch (neutrale Kultur) und Deutsch, mit der
  Terminologie der Windows-Oberfläche.
- Light-/Dark-Mode, das der Windows-Einstellung folgt oder fest gewählt werden kann.
- Zur Laufzeit gezeichnetes Icon (Bluetooth-Rune auf einem Stern).

### Bekannte Einschränkungen
- Der Verbindungspfad (`BluetoothSetServiceState`) ist noch nicht an echter Hardware getestet.
- Die Ansage der Benachrichtigungen durch NVDA ist noch nicht akustisch gegengehört.

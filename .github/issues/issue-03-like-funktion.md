# Like- bzw. Favoriten-Funktion pro Gerät

## Beschreibung
Zur Steigerung der Interaktivität sollen Gäste Fotos liken können. Jeder Gast darf pro Gerät nur einmal pro Bild reagieren. Dafür wird eine neue Tabelle für Likes benötigt, die mit der Geräte-UUID verknüpft ist, sowie passende UI-Elemente in der Galerie.

## Aufgaben
- Lege eine neue Tabelle (z. B. `gallery_item_likes`) in den SQL-Skripten (`SqlScripts/SQLite` und `SqlScripts/MySQL`) an und erweitere `IDatabaseHelper` plus Implementierungen um Methoden zum Setzen, Entfernen und Zählen von Likes.
- Implementiere im `GalleryController` einen Endpunkt wie `ToggleLike`, der anhand der Geräte-UUID und der Item-ID den Like-Status umschaltet und den aktuellen Zähler zurückliefert.
- Ergänze `PhotoGalleryImage` um Felder für Like-Anzahl und den aktuellen Status des Besuchers; stelle sicher, dass `GetAllGalleryItems` diese Daten mitliefert.
- Aktualisiere `Views/Gallery/GalleryWrapper.cshtml` und `wwwroot/js/gallery.js`, um Like-Buttons mit visuellem Feedback und Live-Aktualisierung der Zähler zu integrieren.

## Akzeptanzkriterien
- Likes werden pro Geräte-UUID eindeutig gespeichert; wiederholte Klicks toggeln den Status.
- Die Galerie zeigt die Gesamtzahl der Likes pro Bild und den eigenen Status (aktiv/inaktiv) an.
- Der Like-Zähler aktualisiert sich ohne vollständiges Seiten-Reload.

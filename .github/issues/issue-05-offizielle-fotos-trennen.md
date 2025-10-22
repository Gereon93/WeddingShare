# Offizielle Fotografenbilder von Gast-Uploads trennen

## Beschreibung
Aktuell werden alle freigegebenen Uploads in der gleichen Ansicht präsentiert. Für eine klare Trennung zwischen offiziellen Fotografenbildern und Beiträgen der Gäste braucht es ein zusätzliches Datenfeld sowie eigene Anzeige- und Review-Flows.

## Aufgaben
- Erweitere `GalleryItemModel` und die Datenbankskripte (`SqlScripts/SQLite`, `SqlScripts/MySQL`) um ein Flag wie `IsOfficial` oder eine entsprechende Statuserweiterung; aktualisiere `IDatabaseHelper`-Methoden.
- Stelle sicher, dass Fotograf:innen-Uploads automatisch als offiziell markiert werden, während Gastuploads standardmäßig als nicht offiziell gelten. Admins sollen den Status im Review-Prozess anpassen können.
- Teile `GalleryController.Index` sowie `Views/Gallery/GalleryWrapper.cshtml` in separate Bereiche (Offizielle Fotos vs. Gastbeiträge) auf und sorge für eine rein lesende öffentliche Galerie.
- Passe das Admin-Dashboard an, damit das Hochzeitspaar Gastordner freigeben/sperren und offizielle Inhalte gezielt hervorheben kann.

## Akzeptanzkriterien
- Die Datenbank speichert, ob ein Foto offiziell oder ein Gastbeitrag ist.
- Die öffentliche Galerie zeigt offizielle Fotos getrennt oder hervorgehoben von Gastuploads an.
- Admins können den Status eines Fotos jederzeit ändern.

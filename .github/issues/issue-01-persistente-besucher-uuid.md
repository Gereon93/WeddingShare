# Persistente Besucher-UUID für Uploads

## Beschreibung
Damit Gäste ohne Account wiedererkannt werden, soll bei jedem Zugriff auf eine Event- oder Galerie-Seite automatisch eine langlebige, zufällige UUID als Gerätekennung vergeben werden. Diese UUID muss sowohl im Browser-Cookie als auch in der Datenbank bei jedem Upload abgespeichert werden, damit spätere Aktionen (z. B. Likes oder Löschberechtigungen) eindeutig dem Gerät zugeordnet werden können.

## Aufgaben
- Konfiguriere in `Startup.cs` bzw. dem Nachfolger (`Program.cs`) die Middleware so, dass ein Cookie wie `.WeddingShare.Guest` mit einer UUID gesetzt wird, sobald ein Besucher `/event/{id}` oder die Galerie betritt.
- Erweitere `HomeController.SetIdentity`, `GalleryController` und die zugehörigen ViewModels, damit die Geräte-UUID aus dem Cookie geladen und an Upload-Aktionen weitergegeben wird.
- Füge in `GalleryItemModel` eine Spalte für die Geräte-ID hinzu und erweitere sowohl die SQLite- als auch MySQL-Skripte in `SqlScripts/` samt `IDatabaseHelper`-Implementierungen um das neue Feld.
- Stelle sicher, dass die UUID beim Speichern eines Uploads (`_database.AddGalleryItem`) mitpersistiert wird und im Frontend (z. B. `wwwroot/js/site.js`) zur Verfügung steht.

## Akzeptanzkriterien
- Beim ersten Besuch eines Events oder der Galerie wird ein Cookie mit einer gültigen UUID gesetzt.
- Neue Uploads enthalten die Geräte-UUID in der Datenbank.
- Wird das Cookie gelöscht, wird beim nächsten Besuch automatisch eine neue UUID erstellt.

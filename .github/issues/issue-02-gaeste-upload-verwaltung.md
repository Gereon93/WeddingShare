# Gäste-spezifische Upload-Verwaltung

## Beschreibung
Gäste sollen ihre eigenen Uploads sehen und bei Bedarf löschen können, ohne Zugriff auf fremde Fotos zu erhalten. Die bestehende Galerie zeigt jedoch alle freigegebenen Medien und erlaubt Löschaktionen nur über das Admin-Backend. Diese Aufgabe führt einen "Meine Uploads"-Bereich und passende API-Endpunkte ein.

## Aufgaben
- Ergänze `IDatabaseHelper` und die konkreten Implementierungen um Methoden, die Uploads per Galerie-ID und Geräte-UUID filtern sowie einzelne Datensätze sicher löschen.
- Implementiere im `GalleryController` oder einem neuen `GuestController` Endpunkte wie `GetMyUploads` und `DeleteMyUpload`, die anhand der Cookie-UUID verifizieren, ob ein Item zum aktuellen Gerät gehört.
- Erweitere `PhotoGalleryImage`/ViewModels um ein Flag, das markiert, ob ein Bild dem aktuellen Gast gehört, und passe `Views/Gallery/GalleryWrapper.cshtml` an, um einen separaten Bereich für eigene Uploads inklusive Lösch-Buttons bereitzustellen.
- Implementiere die erforderliche Client-Logik in `wwwroot/js/gallery.js`, um die neuen APIs via AJAX aufzurufen, Ergebnisse darzustellen und die Galerie nach Löschvorgängen zu aktualisieren.

## Akzeptanzkriterien
- Gäste sehen in der Galerie einen Abschnitt, der nur ihre eigenen Uploads enthält.
- Der Lösch-Button ist nur bei eigenen Fotos verfügbar und entfernt den Datensatz serverseitig.
- Versucht ein Gast ein fremdes Foto zu löschen, erhält er einen Fehler und der Datensatz bleibt bestehen.

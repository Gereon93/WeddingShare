# Sammelissue: Offene Punkte bei Gast-Uploads, Likes und Theme

## Hintergrund
Im Review der letzten Änderungen fiel auf, dass zentrale Anforderungen aus den Issues #1–#6 noch nicht umgesetzt oder unvollständig integriert sind. Zusätzlich wünscht der Auftraggeber, dass Gäste in einem eigenen Ordner (nach Name/UUID) hochladen und dass ganze Ordner freigegeben bzw. wieder gesperrt werden können. Dieses Sammelissue bündelt alle offenen Punkte, damit die Implementierung gezielt nachgearbeitet werden kann.

## Fehlende oder unvollständige Funktionen
1. **Identitätserfassung ohne View**
   - `GalleryController.CaptureIdentity` rendert `~/Views/Gallery/CaptureIdentity.cshtml`, die View-Datei existiert jedoch nicht. Gäste erhalten dadurch beim Redirect einen Fehler.
   - Maßnahme: View anlegen inkl. Formular zur Namens-/E-Mail-Eingabe und Weiterleitung zurück in die Galerie.

2. **Ordnerbasierte Gast-Freigaben fehlen**
   - Uploads landen weiterhin direkt im Galerie-Stamm (ggf. `Pending`-Unterordner); es gibt keine Gast-Unterordner.
   - Es existiert keine Logik, um beim Freigeben/Löschen ganze Gästemappen zu veröffentlichen oder erneut zu sperren, wenn neue Dateien hinzukommen.
   - Maßnahmen: Dateistruktur pro Gast einführen, Freigabe-Status pro Ordner speichern und bei Upload/Löschung aktualisieren, Download so anpassen, dass freigegebene Ordner sichtbar/ladbar sind.

3. **„Meine Uploads“-Bereich nur serverseitig vorbereitet**
   - `GetMyUploads`/`DeleteMyUpload` existieren, aber `PhotoGalleryImage`, Razor-Views und `gallery.js` kennen weder ein Eigenes-Upload-Flag noch UI-Elemente zum Anzeigen und Löschen.
   - Maßnahme: ViewModel um Besitz-/Status-Infos erweitern, separaten Abschnitt in der Galerie mit Löschbuttons einbauen und AJAX-Workflow implementieren.

4. **Like-Funktion ohne Oberfläche**
   - API (`ToggleLike`, `GetLikeInfo`) ist vorhanden, jedoch fehlen Felder im ViewModel und UI/JS-Interaktionen (Herz-Button, Counter, Zustandswechsel).
   - Maßnahmen: Like-Status & Zähler transportieren, Buttons/Icons darstellen, AJAX-Anbindung und optisches Feedback implementieren.

5. **Offizielle Fotografenfotos nicht getrennt dargestellt**
   - `IsOfficial` wird zwar gesetzt, aber Controller/View gruppieren nichts. Gäste sehen weiterhin nur eine gemischte Liste.
   - Maßnahmen: Im Modell trennen (z. B. Sektionen „Offiziell“ vs. „Community“), Filter/Badges ergänzen und Downloads berücksichtigen.

6. **Fotografen-Zuordnungen ohne Admin-Oberfläche**
   - Datenbank- und Helper-Methoden sind vorhanden, doch `Views/Account/Tabs/Users.cshtml` sowie zugehörige Partials/JS wurden nicht erweitert.
   - Maßnahmen: UI zum Zuweisen/Entziehen von Events bauen, entsprechende Controller-Aktionen und Validierungen ergänzen.

7. **Wedding-Theme erzwingt neues Layout**
   - `_Layout.cshtml` bindet `wedding-theme.css` immer ein. Dadurch überschreibt das neue Theme sämtliche Farbschemata, unabhängig von der Theme-Auswahl.
   - Maßnahmen: Theme optional machen (z. B. Umschalter in den Einstellungen) oder Styling in vorhandene Theme-Logik integrieren.

## Erwartete Ergebnisse
- Gäste können nur ihre Ordner sehen/bearbeiten; Admins entscheiden über Freigabe kompletter Gast-Ordner.
- Like- und „Meine Uploads“-Funktionen sind vollständig nutzbar.
- Offizielle Fotografenfotos erscheinen getrennt und Fotograf:innen verwalten ihre Inhalte ohne Admin-Freigabe.
- Das neue Wedding-Theme kann ein- und ausgeschaltet oder neben anderen Themes ausgewählt werden.

Bitte diese Punkte priorisiert bearbeiten und nach Umsetzung erneut reviewen.

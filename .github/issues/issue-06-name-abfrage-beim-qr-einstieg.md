# Pflicht zur Namenseingabe nach QR-Scan

## Beschreibung
Nach dem Scannen des Event-QR-Codes sollen Gäste direkt auf eine Seite geleitet werden, die sie zur Eingabe ihres Namens (optional auch Geräte-Namen) auffordert, bevor sie Fotos hochladen können. Der Name muss mit der Geräte-UUID verknüpft und für spätere Uploads wiederverwendet werden.

## Aufgaben
- Passe die Routing-Logik für `/event/{id}` an, sodass bei fehlender Namensinformation eine zwischengeschaltete View (z. B. `Views/Gallery/CaptureIdentity.cshtml`) angezeigt wird.
- Implementiere ein Formular, das Name (und optional weitere Felder) abfragt, die Geräte-UUID im Hintergrund an den Server sendet und die Werte in der Session sowie dauerhaft (z. B. `gallery_devices`- oder `gallery_items`-Tabelle) speichert.
- Aktualisiere `HomeController.SetIdentity` und die relevanten Razor-Views, damit Besucher ohne vollständige Identität nicht zum Upload-Formular gelangen.
- Stelle sicher, dass der eingegebene Name bei späteren Besuchen über den Geräte-Cookie vorausgefüllt wird und sich Gäste bei Bedarf umbenennen können.

## Akzeptanzkriterien
- Nach dem Aufruf von `/event/{id}` ohne gespeicherten Namen erscheint ein Pflichtdialog zur Namenseingabe.
- Erst nach erfolgreicher Eingabe gelangt der Gast zur Upload-Ansicht.
- Bei erneuten Besuchen wird der zuvor gespeicherte Name automatisch verwendet, lässt sich aber aktualisieren.

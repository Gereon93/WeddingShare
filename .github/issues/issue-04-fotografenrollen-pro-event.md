# Fotografenrollen auf einzelne Events einschränken

## Beschreibung
Derzeit gelten Benutzerrollen global. Fotograf:innen sollen jedoch nur auf die ihnen zugewiesenen Events zugreifen und dort offizielle Fotos hochladen können. Dafür braucht es eine Zuordnungstabelle und angepasste Berechtigungsprüfungen im Backend sowie UI-Anpassungen im Admin-Bereich.

## Aufgaben
- Ergänze das Datenmodell um eine Mapping-Tabelle (z. B. `gallery_user_roles`), passe die SQL-Skripte in `SqlScripts/` an und erweitere `IDatabaseHelper` sowie die konkreten Datenbankklassen um CRUD-Methoden für diese Zuordnungen.
- Aktualisiere die Authorisierungslogik (`RequiresRoleAttribute`, `GalleryController`, Upload-/Review-Aktionen), sodass Fotograf:innen nur dann Zugriff auf ein Event haben, wenn eine entsprechende Zuordnung besteht.
- Erweitere das Admin-Dashboard (`Views/Account/Tabs/Users.cshtml` und entsprechende Controller) um UI-Elemente, mit denen ein Event einem Fotografen zugewiesen bzw. entzogen werden kann.
- Passe Upload- und Review-Flows an, damit Fotograf:innen nur offizielle Inhalte für ihre Events hochladen und bearbeiten können.

## Akzeptanzkriterien
- Fotograf:innen sehen und bearbeiten ausschließlich die Events, denen sie zugeordnet sind.
- Admins können im Backend Event-Zuordnungen verwalten.
- Zugriffe auf nicht zugewiesene Events werden serverseitig blockiert.

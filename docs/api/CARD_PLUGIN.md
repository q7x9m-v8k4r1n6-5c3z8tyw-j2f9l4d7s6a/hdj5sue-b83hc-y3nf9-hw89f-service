# Card plugin API

Card definitions and race card state belong to `OVCMOVE2026.Plugin`. The core
API only exposes the `IPluginHub` event contract; it does not reference card or
workflow entities.

## MongoDB

Configure:

- `OVCMOVE_MongoDb__ConnectionString`
- `OVCMOVE_MongoDb__DatabaseName` (default `ovcmove`)
- `OVCMOVE_MongoDb__CollectionName` (default `race_cards`)

The plugin uses one `race_cards` collection. Each document has one `raceid`,
an `inventory`, embedded `teams`, card configuration, trap state, and scheduled
restocks. The document `_id` is the race ID, which prevents duplicate race
documents without adding a SQL table.

## Endpoints

Admin/organizer endpoints are under `/api/v1/plugin/cards/races/{raceId}`:

- `GET` — default card catalog and current stock
- `POST /store/open` and `POST /store/close`
- `POST /inventory/restock`
- `POST /inventory/schedule`
- `PUT /cards/{cardId}/config`
- `GET /cards/{cardId}/teams`
- `POST /cards/{cardId}/teams`
- `DELETE /cards/{cardId}/teams/{teamId}` with a required deletion reason

Team endpoints are under `/api/v1/plugin/cards/team/races/{raceId}/cards`.
The first built-in card is `TRAP`. Its `boothId` input identifies the booth;
using the card stores an active trap in MongoDB. The next successful booth-entry
request atomically claims that trap, then the plugin sends the existing core
score command with the configured negative points.

## Optional loading

The API loads `OVCMOVE2026.Plugin.dll` when it is available in the application
directory or `Plugins/` and `MongoDb:ConnectionString` is configured. If the
assembly or its MongoDB configuration is absent, the core keeps the no-op hub
and starts without plugin controllers or MongoDB registration. The SQL cleanup
migration `006_RemoveLegacyCardWorkflowTables.sql` removes the old
`FunctionCards`, `Workflows`, and `WorkflowRuns` tables from existing databases.

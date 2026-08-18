# Card workflows

Function cards and workflows are persisted by the backend. A function card
belongs to one race, starts with no assigned team, may be assigned to one team
in that race, and may own at most one active workflow.

The database enforces the card/workflow one-to-one relation with the filtered
unique index `UX_Workflows_CardId`. Removing a card soft-deletes its workflow
but retains workflow run history.

## Function card management

Management endpoints are under `/api/v1/function-cards`:

- `GET ?raceId=...` and `GET /{cardId}` read cards.
- `POST /races/{raceId}` creates an unassigned card.
- `PUT /{cardId}` updates card metadata and input definitions.
- `PUT /{cardId}/team` assigns one race team or sends `teamId: null` to unassign.
- `DELETE /{cardId}` soft-deletes the card and its workflow.

Card backgrounds are uploaded through `/api/v1/Image/upload`; only the returned
blob URL is stored with the card.

## Card backend integration

Inject `ICardWorkflowDispatcher` into the future card command handler and raise
one event after the card's own authorization/ownership/usage validation passes:

```csharp
await dispatcher.DispatchAsync(new CardWorkflowEvent(
    RaceId: raceId,
    CardKey: card.Code,
    TriggerType: WorkflowConstants.Trigger.Activated,
    EventId: $"card-use:{cardUseId}",
    ActorTeamId: actorTeamId,
    TargetTeamId: targetTeamId,
    Payload: JsonSerializer.SerializeToElement(new
    {
        cardUseId,
        inputs = new { target_team = targetTeamId }
    })));
```

Use `WorkflowConstants.Trigger.Attacked` when a card attack targets its owner.
`EventId` must be stable and unique for the business event. The database rejects
duplicate real executions before any workflow action runs.

The dispatcher returns `null` when no published workflow is configured. This is
not an error: the caller can continue the card command normally.

## Available nodes

- `logic.condition`: safe path/literal comparisons with true/false branches.
- `data.set_variable`, `data.random_number`: runtime data operations.
- `input.read_value`: reads `payload.inputs.<inputKey>` into a workflow variable.
- `team.adjust_score`: applies score changes through the existing score command.
- `notify.send_message`: persists and broadcasts race messages.
- `card.apply_effect`: returns a typed effect for the card backend to consume.
- `flow.stop`: terminates the selected branch.

Paths can read `event.actorTeamId`, `event.targetTeamId`, `event.cardKey`,
`event.triggerType`, `variables.<name>`, and `payload.<path>`. Message/reason text
supports placeholders such as `{{variables.ketQua}}`.

Management endpoints are under `/api/v1/workflows` and currently reuse
`race.manage`, so existing race administrators do not need a newly issued token.
The `workflow.manage` permission is seeded for a later dedicated RBAC rollout.
The admin builder uses simulation mode, which evaluates the
complete workflow but does not change score or send messages.

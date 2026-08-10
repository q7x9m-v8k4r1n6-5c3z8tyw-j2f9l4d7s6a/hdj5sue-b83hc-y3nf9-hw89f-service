# Card Effect Engine Foundation

## Status

Accepted for technical proof of concept.

## Context

MOVE cards are not limited to synchronous score changes. Some cards react to
future game events, keep state across requests, intercept an action before it
commits, or compensate a previous execution. A linear `Card Used -> If ->
Adjust Score` runner cannot cover those cases safely.

## Decision

Build a domain-specific, event-driven Card Effect Engine in .NET. React Flow is
an optional editor for the same typed definition; it is not the runtime.

The runtime vocabulary is deliberately small:

- Trigger: a whitelisted domain event type.
- Condition: a registered handler that reads a safe execution context.
- Action: a registered handler that calls an application capability.
- Execution policy: remaining uses, expiry, and distinct-target limits.

Definitions are immutable after publication and are versioned. Runtime state is
stored separately from definitions. The engine never evaluates arbitrary C#,
JavaScript, SQL, JSONPath, or HTTP supplied by a workflow author.

## Layering

- `OVCMOVE.Application`: contracts, validation, executor, and extension points.
- `OVCMOVE.Domain`: persistent entities after the proof of concept validates the
  model.
- `OVCMOVE.Infrastructure`: repositories, concurrency, idempotency, and journals.
- `OVCMOVE2026.Plugin`: MOVE 2026 condition/action handlers.
- Frontend: React Flow editor and simulator after backend acceptance tests pass.

## Schema v1 example

```json
{
  "schemaVersion": 1,
  "workflowVersionId": "00000000-0000-0000-0000-000000000001",
  "version": 1,
  "triggerType": "game_action.attempted",
  "conditions": [
    {
      "type": "condition.target_is_effect_owner",
      "config": {}
    },
    {
      "type": "condition.action_type_in",
      "config": {
        "allowed": ["steal_score", "trap"]
      }
    }
  ],
  "actions": [
    {
      "type": "action.cancel_current_action",
      "config": {}
    }
  ],
  "policy": {
    "maximumTriggers": 1,
    "consumeWhen": "action_succeeded"
  }
}
```

Consumption is an engine policy rather than a required graph node. This avoids
double consumption and keeps effect lifecycle semantics consistent.

## Proof-of-concept gates

1. Shield: the first matching attempted action is cancelled and consumes the
   effect; the second event is ignored.
2. Trap: an armed trap survives process restart, reacts to booth arrival, and
   applies a persisted pause deadline exactly once.
3. Rewind: a compensable execution can be reversed once inside 60 seconds and
   cannot be reversed outside the window.

Only gate 1 belongs to the foundation milestone. Gates 2 and 3 require the
persistence and action-journal milestones.

## Deliberately out of scope for the foundation

- Card catalog, inventory, purchase, and use endpoints.
- SQL tables and repositories.
- React Flow editor.
- Timers and background scheduling.
- Elsa, Microsoft RulesEngine, or dynamic expressions.
- Production handlers that mutate score, booth, or inventory.


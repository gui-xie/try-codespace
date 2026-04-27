# JSON Schema Reference

Schema v1 is available at `docs/schemas/permissions-v1.schema.json`.

Required root properties:

- `version`: must be `1`.
- `permissions`: array of permission definitions.

Optional root properties:

- `$schema`: schema URI.
- `namespace`: generated C# namespace override.
- `className`: generated permission constants class name.
- `catalogClassName`: generated permission catalog class name.
- `groups`: permission group metadata.

Permission codes must match:

```text
^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$
```

`requires` entries must reference existing permission codes and cannot form a cycle.

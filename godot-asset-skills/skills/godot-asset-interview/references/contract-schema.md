# Godot Asset Contract Schema

Use this schema for final contracts. Keep it concrete and executable.

```md
# Godot Asset Contract: <stable-id>

## Metadata
- Asset Type:
- Stable ID:
- Display Name:
- Source Requirement:
- Lifecycle State: draft | review-ready | execution-ready

## Intent
- Player/User-facing purpose:
- Design role:
- In scope:
- Non-goals:

## Godot Outputs
- Scene paths:
- Script paths:
- Resource paths:
- Test/preview paths:

## Runtime Boundary
- Owns:
- Reads:
- Emits:
- Must not own:

## Decision Boundaries
- AI may decide:
- AI must ask before:

## Acceptance Evidence
- Node/resource evidence:
- Visual evidence:
- Runtime evidence:
- Log/test evidence:

## Execution Readiness
- Blocking ambiguity:
- Required MCP/editor state:
- Safe to execute: true | false

## Asset-Type Specific Requirements
<Use the selected asset type guidance.>

## Residual Ambiguity
- Non-blocking assumptions:
- Blocking questions:
```

For `composite-feature`, add:

```md
## Child Contracts
| Asset Type | Stable ID | Contract Path | Dependency Role |
| --- | --- | --- | --- |
```

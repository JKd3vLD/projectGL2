# WorldGen Constraints [MVP Core]

**Purpose**: Defines constraints for world generation: connector compatibility, gateway anchoring, no floating doors, validation rules.

## Player-Facing Rules

- **Section Chaining**: Sections connect via compatible connectors (e.g., "ground" → "ground"). Ensures playable paths.
- **Gateway Anchoring**: Gateways (section transitions) must be anchored to solid ground. No floating transitions.
- **No Floating Doors**: Doors/portals must be accessible from ground. No doors floating in air.

## System Rules

- **Connector Compatibility**: `StageAssembler.EnforceConnectorCompatibility()` ensures adjacent sections have matching connectors. Defaults to "ground" if missing.
- **Gateway Validation**: During level load, validate gateway positions are on solid ground (future enhancement).
- **Door Validation**: Validate doors/portals are accessible (future enhancement).
- **Section Quotas**: Sections define quotas for secrets, bonus doors, chests. Used for SLOW stage assembly.

## Data Model

**Connector Types** (`GL2Project/World/SectionDef.cs`):
- `ConnectorsIn`: List<string> (e.g., ["ground", "air"])
- `ConnectorsOut`: List<string> (e.g., ["ground", "water"])

**SectionQuotas** (`GL2Project/World/SectionDef.cs`):
- `SecretSlots`: int
- `BonusDoorSlots`: int
- `ChestSlots`: int

**LevelFormat** (`GL2Project/Content/LevelFormat.cs`):
- `Blocks`: List<BlockEntry>
- `Entities`: List<EntityEntry>
- `CameraVolumes`: List<CameraVolumeEntry>

## Algorithms / Order of Operations

### Connector Compatibility

1. **Check Adjacent Sections**: For each pair `(sections[i], sections[i+1])`
2. **Find Match**: Check if `sections[i].ConnectorsOut` contains any element in `sections[i+1].ConnectorsIn`
3. **Default if Missing**: If no match AND connectors exist:
   - Add "ground" to `sections[i].ConnectorsOut` (if missing)
   - Add "ground" to `sections[i+1].ConnectorsIn` (if missing)
4. **Validate**: Ensure at least one connector match exists

### Gateway Anchoring (Future)

1. **Find Gateways**: Locate gateway entities in level data
2. **Check Ground**: Verify gateway position has solid ground below (within 64px)
3. **Reject if Floating**: If no ground found, log error, reject level or adjust position

### Door Validation (Future)

1. **Find Doors**: Locate door/portal entities in level data
2. **Check Accessibility**: Verify door reachable from ground (pathfinding or distance check)
3. **Reject if Inaccessible**: If unreachable, log error, reject level or adjust position

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `defaultConnector` | string | - | "ground" | Default connector if missing |
| `gatewayGroundCheckDistance` | float | 0-200 | 64 px | Distance to check for ground below gateway |
| `doorAccessibilityRadius` | float | 0-500 | 200 px | Radius to check door accessibility |

## Edge Cases + Counters

- **No connectors defined**: Default to "ground" connector for both sections.
- **Multiple connector matches**: Use first match (deterministic).
- **Gateway floating**: Log error, reject level or auto-adjust position (future enhancement).
- **Door unreachable**: Log error, reject level or auto-adjust position (future enhancement).

## Telemetry Hooks

- Log connector compatibility: `ConnectorCompatibility(sectionId1, sectionId2, connectorsMatched, defaultApplied, timestamp)`
- Log gateway validation: `GatewayValidation(gatewayId, anchored, position, timestamp)` (future)
- Log door validation: `DoorValidation(doorId, accessible, position, timestamp)` (future)

## Implementation Notes

**File**: `GL2Project/World/StageAssembler.cs`, `GL2Project/Content/LevelLoader.cs`

**Key Systems**:
- `StageAssembler`: Enforces connector compatibility during assembly
- `LevelLoader`: Validates level data (future: gateway/door validation)

**Deterministic Ordering**:
1. Assemble sections
2. Enforce connector compatibility
3. Load level data (future: validate gateways/doors)
4. Place flags
5. Create entities

**Validation**: Currently minimal. Future enhancements:
- Gateway anchoring validation
- Door accessibility validation
- Path validation (no dead ends)
- Quota validation (secrets/bonus doors match quotas)

**Connector Types**: Currently simple strings. Future: Structured connector types with properties (height, width, type).


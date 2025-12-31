# Telemetry & Debug [MVP Core]

**Purpose**: Defines what to log for debugging, telemetry, and deterministic replay. Logging hooks throughout systems.

## Logging Categories

- **Seed Events**: Seed resolution, stream usage, seed badge saves
- **World Generation**: Tier package generation, stage assembly, section selection
- **Stage Play**: Stage start/complete, pacing choice, flag passing
- **Currency**: XP collection, death drops, reward thresholds
- **Rewards**: Reward calculation, reward selection
- **Game Over**: Game over events, tier regression, lives reset
- **Performance**: System update times, entity counts, allocation counts (optional)

## System Rules

- **Deterministic Logging**: Log events in deterministic order. Same inputs produce same log sequence.
- **Structured Logs**: Log events as structured data (JSON or key-value pairs). Include timestamp, context.
- **Log Levels**: INFO (normal events), WARN (warnings), ERROR (errors). Filter by level.
- **Telemetry Hooks**: Systems call logging functions at key points. No performance impact in release builds.

## Data Model

**Log Event Structure**:
- `EventType`: string (e.g., "SeedResolved", "StageComplete")
- `Timestamp`: DateTime
- `Context`: Dictionary<string, object> (event-specific data)

**DebugOverlay** (`GL2Project/Debug/DebugOverlay.cs`):
- `Draw(spriteBatch, graphicsDevice)`: void
- Displays: FPS, fixed updates, dt, seed hashes, current tier, current biome signature

## Algorithms / Order of Operations

### Logging Flow

1. **System Event**: System detects event (e.g., flag passed, XP collected)
2. **Format Log Entry**: Create structured log entry with event type, timestamp, context
3. **Write to Log**: Write to log file or telemetry service (future)
4. **Debug Overlay**: Update debug overlay if event affects displayed info

### Debug Overlay Update

1. **Collect Metrics**: Gather FPS, fixed update count, delta time, seed hashes, tier, biome
2. **Format Display**: Format metrics as text strings
3. **Draw Overlay**: `DebugOverlay.Draw()` renders overlay on top of game

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `logLevel` | enum | INFO/WARN/ERROR | INFO | Minimum log level |
| `logToFile` | bool | - | true | Write logs to file |
| `logToTelemetry` | bool | - | false | Send to telemetry service (future) |
| `debugOverlayEnabled` | bool | - | true | Show debug overlay |

## Edge Cases + Counters

- **Log file full**: Rotate logs, keep last N files. Or truncate oldest entries.
- **Telemetry service down**: Queue logs, retry later. Or fall back to file only.
- **High log volume**: Throttle logging, sample events, or filter by level.

## Telemetry Hooks

### Seed Events
- `SeedResolved(tierIndex, symbolCodes, resolvedSeed, timestamp)`
- `RNGStreamUsed(streamName, usageCount, timestamp)` (optional)
- `SeedBadgeSaved(tierIndex, seedBadge, timestamp)`
- `RandomSeedGenerated(tierIndex, seed, timestamp)`

### World Generation
- `TierPackageGenerated(tierIndex, worldGenSeed, biomeIds, timestamp)`
- `StageAssembled(stageId, sectionCount, sidePocketCount, rampDistribution, timestamp)`
- `SectionSelected(sectionId, tier, signature, pacing, rampPosition, timestamp)`
- `SectionPoolExhausted(tier, signature, pacing, timestamp)`

### Stage Play
- `StageStart(stageId, pacingTag, timestamp)`
- `StageComplete(stageId, finishTime, rewards, timestamp)`
- `PacingChoice(stageId, pacingTag, timestamp)`
- `FlagPassed(flagId, flagType, timestamp)`
- `FlagConsumed(flagId, rewardMultiplier, timestamp)`

### Currency
- `XPCollected(collectibleId, value, currentXP, totalCollected, timestamp)`
- `RewardThresholdReached(currentXP, threshold, rewardsToGive, timestamp)`
- `DeathXP(previousXP, lostXP, dropPosition, timestamp)`
- `XPDropRetrieved(dropValue, currentXP, timestamp)`

### Rewards
- `RewardCalculated(stageId, pacingTag, baseReward, bonuses, totalReward, timestamp)`
- `RewardSelected(rewardOption, rewardId, timestamp)`

### Game Over
- `GameOver(reason, tierReached, livesRemaining, timestamp)`
- `TierRegression(fromTier, toTier, timestamp)`
- `TierUpgrade(fromTier, toTier, seedBadge, timestamp)`

### Performance (Optional)
- `SystemUpdate(systemName, updateTime, timestamp)`
- `EntityCount(total, active, timestamp)`
- `AllocationCount(count, timestamp)`

## Implementation Notes

**File**: `GL2Project/Debug/DebugOverlay.cs`

**Key Systems**:
- `DebugOverlay`: On-screen debug information display
- Logging system: Future implementation (structured logging)

**Deterministic Ordering**: Log events in same order as system updates. Ensures deterministic replay.

**Debug Overlay**: Shows real-time metrics. Toggle with debug key (TBD). Always visible in debug builds.

**Log Format**: Future: Structured JSON logs. Current: Console.WriteLine (temporary).

**Telemetry Service**: Future enhancement. Send logs to remote service for analysis.


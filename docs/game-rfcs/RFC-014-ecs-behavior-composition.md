# RFC-014: ECS Behavior Composition

- Start Date: 2025-01-16
- RFC Author: Team
- Status: Draft
- Depends On: RFC-001, RFC-002, RFC-003, RFC-004

## Summary

Define how the generic game/editor composes runtime behavior using an Entity Component System (ECS), with Arch ECS as the reference library, while preserving the 4-tier service architecture. Services remain engine-agnostic (Tier 1), profiles adapt behavior (Tier 3), and providers (Tier 4) implement concrete systems. Contracts do not become plugins; ECS is a composition mechanism behind services.

## Motivation

- Keep Tier 1 service APIs stable while enabling rich, data-driven behaviors.
- Simulate Unity/Godot behaviors inside a pure C# TUI application by mapping service intents to ECS systems.
- Reuse concepts from existing work (e.g., craft-sim) without binding the architecture to Unity-specific APIs.

## Design

### Roles per Tier

- Tier 1 (Contracts):
  - Define service intents and data contracts (e.g., `IAudioService`, `IGameLoop`, AI agent contracts).
  - No ECS dependency; no engine types.

- Tier 2 (Proxies/Infra):
  - Provide generated proxies and scheduling helpers.
  - Offer an ECS host abstraction (e.g., `IEcsWorldHost`) that is optional and hidden behind interfaces.

- Tier 3 (Profiles/Adapters):
  - Bind a chosen ECS runtime (Arch ECS) to service adapters.
  - Translate Tier 1 service intents into ECS archetypes, systems, and pipelines.
  - Define private provider-facing interfaces for Tier 4 systems as needed (do not leak upward).

- Tier 4 (Providers):
  - Implement concrete ECS systems and components (e.g., audio mixer system, input processing system, AI director system).
  - May be engine-specific or generic; never reference Tier 1 directly.

### ECS Integration Pattern

- Service → Intent → ECS:
  - A Tier 1 service call emits an intent DTO (pure data).
  - The Tier 3 adapter converts the intent into ECS operations: spawn entities with components, enable tags, enqueue commands.
  - Tier 4 systems process components each tick.

- Capabilities:
  - Optional features are modeled as capability facets discoverable from Tier 1 services (e.g., `ISpatialAudioCapability`).
  - ECS systems implementing these features live in Tier 4 and are wired by the active profile.

- Scheduling:
  - The ECS world tick is owned by the host game loop (TUI runtime). Systems run in well-defined phases (input → sim → render text → audio), all independent from Unity/Godot.

### Example (Sketch)

```
// Tier 1
public interface IAudioService {
    Task PlayAsync(string key, CancellationToken ct = default);
}

// Tier 3 adapter
public sealed class AudioServiceEcsAdapter : IAudioService {
    private readonly IEcsWorldHost _world;
    public Task PlayAsync(string key, CancellationToken ct = default) {
        _world.Enqueue(cmd => cmd.Spawn(new PlaySound { Key = key }));
        return Task.CompletedTask;
    }
}

// Tier 4 systems/components
public struct PlaySound { public string Key; }
public struct Playing { public int HandleId; }

public sealed class AudioSystem : IEcsSystem {
    public void Update(World w, float dt) {
        foreach (ref var ps in w.Query<PlaySound>()) {
            // resolve asset, start playback, add Playing; remove PlaySound
        }
    }
}
```

### Engine Simulation via Profiles

- Profiles register different system stacks:
  - Unity-like: add spatial audio, physics-driven footstep emitters.
  - Godot-like: add different input mapping and scene graph adapters.
- The UI remains TUI; rendering is text-based. Systems only depend on Arch ECS + .NET.

### Craft-Sim Alignment

- Crafting/economy behaviors (as in craft-sim) map naturally to ECS: recipes, inventories, jobs, and stations as components/systems.
- Tier 3 hosts these systems as a “gameplay module”; Tier 1 exposes neutral service contracts (e.g., `ICraftingService`).

## Operational Notes

- Determinism: keep ECS systems pure and time-step fixed where possible for replay/testing.
- Testing: unit-test systems with small worlds; service-level tests exercise adapter-to-system mapping.
- Config: profiles select pipelines and feature flags; contracts remain unchanged.

## Decision

- Adopt ECS (Arch ECS) as the primary behavior-composition mechanism behind services.
- Keep contracts stable in Tier 1; represent optional features as capabilities; use private Tier 3↔Tier 4 interfaces as needed.


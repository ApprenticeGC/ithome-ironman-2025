using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Procedural content generation director capability.
/// Provides AI-driven procedural content generation for game environments.
/// </summary>
public interface IAgentDirector : ICapabilityProvider
{
    /// <summary>
    /// Gets encounter intent based on current player state.
    /// </summary>
    /// <param name="snapshot">Current player snapshot for context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns encounter intent.</returns>
    Task<EncounterIntent> GetEncounterIntentAsync(PlayerSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates contextual flavor text for game situations.
    /// </summary>
    /// <param name="context">Game context for flavor text generation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns generated flavor text.</returns>
    Task<string> GetFlavorTextAsync(GameContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adapts dungeon parameters based on player progress.
    /// </summary>
    /// <param name="progress">Current player progress information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns adapted dungeon parameters.</returns>
    Task<DungeonParameters> AdaptDungeonAsync(PlayerProgress progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a loot table for the given encounter context.
    /// </summary>
    /// <param name="context">Encounter context for loot generation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the generated loot table.</returns>
    Task<LootTable> GenerateLootTableAsync(EncounterContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dialogue and narrative generation capability.
/// Provides AI-driven dialogue and story generation for interactive experiences.
/// </summary>
public interface IDialogueAgent : ICapabilityProvider
{
    /// <summary>
    /// Generates a contextual dialogue response.
    /// </summary>
    /// <param name="context">Dialogue context for response generation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the generated response.</returns>
    Task<string> GenerateResponseAsync(DialogueContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drafts a quest outline based on the provided brief.
    /// </summary>
    /// <param name="brief">Quest brief containing requirements and constraints.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the quest outline.</returns>
    Task<QuestOutline> DraftQuestAsync(QuestBrief brief, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates narrative text for the given story context.
    /// </summary>
    /// <param name="context">Story context for narrative generation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the generated narrative.</returns>
    Task<string> GenerateNarrativeAsync(StoryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands a dialogue tree from the given node.
    /// </summary>
    /// <param name="node">Starting dialogue node to expand.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the expanded dialogue tree.</returns>
    Task<DialogueTree> ExpandDialogueTreeAsync(DialogueNode node, CancellationToken cancellationToken = default);
}
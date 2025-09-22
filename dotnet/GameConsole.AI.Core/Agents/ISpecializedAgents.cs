using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core.Agents;

/// <summary>
/// Procedural content generation director agent.
/// Provides AI-driven content generation and adaptation capabilities.
/// </summary>
public interface IAgentDirector : ICapabilityProvider
{
    /// <summary>
    /// Gets encounter intent based on player snapshot for dynamic content generation.
    /// </summary>
    /// <param name="snapshot">Current player state and progress.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An encounter intent describing recommended content parameters.</returns>
    Task<EncounterIntent> GetEncounterIntentAsync(PlayerSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates contextual flavor text for game situations.
    /// </summary>
    /// <param name="context">The current game context requiring flavor text.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Generated flavor text appropriate for the context.</returns>
    Task<string> GetFlavorTextAsync(GameContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adapts dungeon parameters based on player progress.
    /// </summary>
    /// <param name="progress">Current player progress and statistics.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Adapted dungeon parameters for optimal challenge.</returns>
    Task<DungeonParameters> AdaptDungeonAsync(PlayerProgress progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates contextually appropriate loot tables.
    /// </summary>
    /// <param name="context">The encounter context for loot generation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A loot table appropriate for the encounter context.</returns>
    Task<LootTable> GenerateLootTableAsync(EncounterContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dialogue and narrative generation agent.
/// Provides AI-driven dialogue, quest, and story generation capabilities.
/// </summary>
public interface IDialogueAgent : ICapabilityProvider
{
    /// <summary>
    /// Generates a dialogue response based on conversation context.
    /// </summary>
    /// <param name="context">The current dialogue context and history.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A contextually appropriate dialogue response.</returns>
    Task<string> GenerateResponseAsync(DialogueContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drafts a quest outline based on brief requirements.
    /// </summary>
    /// <param name="brief">High-level quest requirements and constraints.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A detailed quest outline with objectives and narrative.</returns>
    Task<QuestOutline> DraftQuestAsync(QuestBrief brief, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates narrative content for story contexts.
    /// </summary>
    /// <param name="context">The story context requiring narrative content.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Generated narrative text appropriate for the context.</returns>
    Task<string> GenerateNarrativeAsync(StoryContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands a dialogue tree from a starting node.
    /// </summary>
    /// <param name="node">The dialogue node to expand from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An expanded dialogue tree with additional branches and responses.</returns>
    Task<DialogueTree> ExpandDialogueTreeAsync(DialogueNode node, CancellationToken cancellationToken = default);
}
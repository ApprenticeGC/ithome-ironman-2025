namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for discovering AI agents in the system.
/// Provides mechanisms to find and enumerate available AI agents based on various criteria.
/// </summary>
public interface IAIAgentDiscovery : IService
{
    /// <summary>
    /// Discovers all available AI agents in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of discovered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that match the specified criteria.
    /// </summary>
    /// <param name="criteria">The criteria to filter agents by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of matching AI agents.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAsync(AIAgentDiscoveryCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that provide specific capabilities.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of agents with the specified capability.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default) where TCapability : class;

    /// <summary>
    /// Discovers AI agents by category.
    /// </summary>
    /// <param name="categories">The categories to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of agents in the specified categories.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverByCategoryAsync(IEnumerable<string> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a specific AI agent by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to find.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent if found, or null if not found.</returns>
    Task<IAIAgent?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds AI agents by name (supports partial matching).
    /// </summary>
    /// <param name="name">The name or partial name to search for.</param>
    /// <param name="exactMatch">Whether to perform exact matching or partial matching.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of matching agents.</returns>
    Task<IEnumerable<IAIAgent>> FindByNameAsync(string name, bool exactMatch = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Criteria for filtering AI agents during discovery.
/// </summary>
public sealed class AIAgentDiscoveryCriteria
{
    /// <summary>
    /// Gets or sets the categories to filter by. Agents must match at least one category.
    /// </summary>
    public IList<string> Categories { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the required capabilities. Agents must provide all specified capabilities.
    /// </summary>
    public IList<Type> RequiredCapabilities { get; set; } = new List<Type>();

    /// <summary>
    /// Gets or sets the minimum status required for agents.
    /// </summary>
    public AIAgentStatus? MinimumStatus { get; set; }

    /// <summary>
    /// Gets or sets a name filter for partial name matching.
    /// </summary>
    public string? NameFilter { get; set; }

    /// <summary>
    /// Gets or sets a description filter for partial description matching.
    /// </summary>
    public string? DescriptionFilter { get; set; }

    /// <summary>
    /// Gets or sets a version filter for exact version matching.
    /// </summary>
    public string? VersionFilter { get; set; }

    /// <summary>
    /// Gets or sets additional custom filters as key-value pairs.
    /// </summary>
    public IDictionary<string, object> CustomFilters { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates an empty criteria instance.
    /// </summary>
    public static AIAgentDiscoveryCriteria Empty => new();

    /// <summary>
    /// Creates a criteria instance that matches agents with specific categories.
    /// </summary>
    /// <param name="categories">The categories to match.</param>
    /// <returns>A new criteria instance.</returns>
    public static AIAgentDiscoveryCriteria WithCategories(params string[] categories)
    {
        return new AIAgentDiscoveryCriteria { Categories = categories.ToList() };
    }

    /// <summary>
    /// Creates a criteria instance that matches agents with specific capabilities.
    /// </summary>
    /// <param name="capabilities">The capabilities to match.</param>
    /// <returns>A new criteria instance.</returns>
    public static AIAgentDiscoveryCriteria WithCapabilities(params Type[] capabilities)
    {
        return new AIAgentDiscoveryCriteria { RequiredCapabilities = capabilities.ToList() };
    }

    /// <summary>
    /// Creates a criteria instance that matches agents with a minimum status.
    /// </summary>
    /// <param name="minimumStatus">The minimum status required.</param>
    /// <returns>A new criteria instance.</returns>
    public static AIAgentDiscoveryCriteria WithMinimumStatus(AIAgentStatus minimumStatus)
    {
        return new AIAgentDiscoveryCriteria { MinimumStatus = minimumStatus };
    }
}
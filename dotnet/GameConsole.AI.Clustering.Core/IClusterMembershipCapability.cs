using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Capability interface for cluster membership management.
/// Enables services to participate in and monitor AI agent actor cluster membership.
/// </summary>
public interface IClusterMembershipCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the list of all cluster members.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of cluster members.</returns>
    Task<IEnumerable<ClusterMember>> GetMembersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets members with specific roles.
    /// </summary>
    /// <param name="roles">The roles to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns cluster members with the specified roles.</returns>
    Task<IEnumerable<ClusterMember>> GetMembersByRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers for cluster membership change notifications.
    /// </summary>
    /// <param name="callback">Callback to invoke when membership changes.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the registration operation.</returns>
    Task RegisterMembershipCallbackAsync(Func<ClusterMembershipChangedEventArgs, Task> callback, CancellationToken cancellationToken = default);
}
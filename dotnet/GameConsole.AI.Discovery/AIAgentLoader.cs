using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace GameConsole.AI.Discovery;

/// <summary>
/// Implementation of AI agent loader with resource validation and safe initialization.
/// </summary>
public class AIAgentLoader : IAIAgentLoader
{
    private readonly ILogger<AIAgentLoader> _logger;

    public AIAgentLoader(ILogger<AIAgentLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IAIAgent> LoadAgentAsync(AgentMetadata metadata, AgentInitializationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Loading agent: {AgentName} ({AgentId})", metadata.Name, metadata.Id);

        // Validate the agent can be loaded
        var validation = await ValidateAgentAsync(metadata, cancellationToken);
        if (!validation.IsValid)
        {
            throw new AgentLoadException($"Agent validation failed: {string.Join(", ", validation.Errors)}");
        }

        // Check resource availability
        var resourceCheck = await CheckResourceAvailabilityAsync(metadata.ResourceRequirements, cancellationToken);
        if (!resourceCheck.IsAvailable)
        {
            throw new AgentLoadException($"Insufficient resources: {string.Join(", ", resourceCheck.Messages)}");
        }

        try
        {
            // Create agent instance
            var agent = CreateAgentInstance(metadata);

            // Initialize with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(metadata.ResourceRequirements.InitializationTimeoutMs);

            var stopwatch = Stopwatch.StartNew();
            await agent.InitializeAsync(context, timeoutCts.Token);
            stopwatch.Stop();

            _logger.LogInformation("Agent {AgentId} initialized successfully in {ElapsedMs}ms", 
                metadata.Id, stopwatch.ElapsedMilliseconds);

            return agent;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Agent loading cancelled: {AgentId}", metadata.Id);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Agent initialization timeout: {AgentId} (timeout: {TimeoutMs}ms)", 
                metadata.Id, metadata.ResourceRequirements.InitializationTimeoutMs);
            throw new AgentLoadException($"Agent initialization timeout: {metadata.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent: {AgentId}", metadata.Id);
            throw new AgentLoadException($"Failed to load agent: {metadata.Id}", ex);
        }
    }

    /// <inheritdoc />
    public Task<AgentValidationResult> ValidateAgentAsync(AgentMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Check if type is valid
        if (metadata.AgentType == null)
        {
            errors.Add("Agent type is null");
        }
        else
        {
            // Check if type implements IAIAgent
            if (!typeof(IAIAgent).IsAssignableFrom(metadata.AgentType))
            {
                errors.Add($"Type {metadata.AgentType.Name} does not implement IAIAgent");
            }

            // Check if type is instantiable
            if (metadata.AgentType.IsAbstract)
            {
                errors.Add($"Type {metadata.AgentType.Name} is abstract and cannot be instantiated");
            }

            if (metadata.AgentType.IsInterface)
            {
                errors.Add($"Type {metadata.AgentType.Name} is an interface and cannot be instantiated");
            }

            // Check for public constructor
            var constructors = metadata.AgentType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                errors.Add($"Type {metadata.AgentType.Name} has no public constructors");
            }

            // Check if assembly is loadable
            try
            {
                var assembly = metadata.AgentType.Assembly;
                if (assembly == null)
                {
                    errors.Add("Agent assembly is not loadable");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Agent assembly load error: {ex.Message}");
            }

            // Check resource requirements
            if (metadata.ResourceRequirements.MinMemoryBytes < 0)
            {
                warnings.Add("Minimum memory requirement is negative");
            }

            if (metadata.ResourceRequirements.InitializationTimeoutMs <= 0)
            {
                warnings.Add("Initialization timeout is invalid");
            }
        }

        var result = new AgentValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.AsReadOnly(),
            Warnings = warnings.AsReadOnly(),
            IsInstantiable = metadata.AgentType != null && !metadata.AgentType.IsAbstract && !metadata.AgentType.IsInterface,
            ImplementsRequiredInterfaces = metadata.AgentType != null && typeof(IAIAgent).IsAssignableFrom(metadata.AgentType),
            IsAssemblyLoadable = metadata.AgentType?.Assembly != null
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ResourceAvailabilityResult> CheckResourceAvailabilityAsync(AgentResourceRequirements requirements, CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var isAvailable = true;

        // Check memory
        var availableMemory = GC.GetTotalMemory(false);
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;
        var approximateAvailableMemory = Math.Max(0, availableMemory - workingSet);

        if (requirements.MinMemoryBytes > approximateAvailableMemory)
        {
            isAvailable = false;
            messages.Add($"Insufficient memory: required {requirements.MinMemoryBytes:N0} bytes, available ~{approximateAvailableMemory:N0} bytes");
        }

        // Check CPU cores
        var availableCores = Environment.ProcessorCount;
        if (requirements.RequiredCpuCores > availableCores)
        {
            isAvailable = false;
            messages.Add($"Insufficient CPU cores: required {requirements.RequiredCpuCores}, available {availableCores}");
        }

        // Note: GPU and network checks are simplified for this implementation
        var result = new ResourceAvailabilityResult
        {
            IsAvailable = isAvailable,
            AvailableMemoryBytes = approximateAvailableMemory,
            AvailableCpuCores = availableCores,
            IsGpuAvailable = !requirements.RequiresGpu, // Assume no GPU for simplicity
            IsNetworkAvailable = true, // Assume network is available
            Messages = messages.AsReadOnly()
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task ShutdownAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        _logger.LogInformation("Shutting down agent: {AgentName} ({AgentId})", agent.Name, agent.Id);

        try
        {
            await agent.ShutdownAsync(cancellationToken);
            _logger.LogInformation("Agent shutdown completed: {AgentId}", agent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during agent shutdown: {AgentId}", agent.Id);
            throw;
        }
        finally
        {
            // Dispose if agent implements IDisposable
            if (agent is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private IAIAgent CreateAgentInstance(AgentMetadata metadata)
    {
        // Try to create instance using parameterless constructor
        var constructors = metadata.AgentType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var parameterlessConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);

        if (parameterlessConstructor != null)
        {
            var instance = Activator.CreateInstance(metadata.AgentType);
            if (instance is IAIAgent agent)
            {
                return agent;
            }
        }

        throw new AgentLoadException($"Cannot create instance of agent type: {metadata.AgentType.Name}");
    }
}

/// <summary>
/// Exception thrown when agent loading fails.
/// </summary>
public class AgentLoadException : Exception
{
    public AgentLoadException(string message) : base(message) { }
    public AgentLoadException(string message, Exception innerException) : base(message, innerException) { }
}
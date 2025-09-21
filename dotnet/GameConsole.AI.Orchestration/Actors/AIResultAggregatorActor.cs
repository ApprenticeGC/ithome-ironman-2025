using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Orchestration.Messages;
using GameConsole.AI.Orchestration.Services;

namespace GameConsole.AI.Orchestration.Actors;

/// <summary>
/// Actor responsible for aggregating AI results from multiple sources.
/// Provides intelligent result combination strategies and quality validation.
/// </summary>
public class AIResultAggregatorActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, AggregationContext> _activeAggregations = new();
    private readonly Dictionary<AggregationStrategy, IAggregationHandler> _strategyHandlers;

    public AIResultAggregatorActor()
    {
        // Initialize strategy handlers
        _strategyHandlers = new Dictionary<AggregationStrategy, IAggregationHandler>
        {
            { AggregationStrategy.Merge, new MergeAggregationHandler() },
            { AggregationStrategy.Consensus, new ConsensusAggregationHandler() },
            { AggregationStrategy.WeightedAverage, new WeightedAverageAggregationHandler() },
            { AggregationStrategy.BestResult, new BestResultAggregationHandler() },
            { AggregationStrategy.Custom, new CustomAggregationHandler() }
        };

        Receive<AggregateResults>(Handle);
        Receive<AddPartialResult>(Handle);
        Receive<AggregationCompleted>(Handle);
        Receive<AggregationFailed>(Handle);
        Receive<ValidateResults>(Handle);
        Receive<GetAggregationMetrics>(Handle);
        Receive<CleanupAggregation>(Handle);

        // Start periodic cleanup of completed aggregations
        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            Self,
            new CleanupAggregation(),
            ActorRefs.NoSender);
    }

    private void Handle(AggregateResults message)
    {
        _log.Info("Starting aggregation {AggregationId} with {ResultCount} partial results using {Strategy} strategy",
            message.AggregationId, message.PartialResults.Count(), message.Strategy);

        try
        {
            var context = new AggregationContext
            {
                Id = message.AggregationId,
                Strategy = message.Strategy,
                Requester = message.Sender,
                StartTime = DateTime.UtcNow,
                PartialResults = message.PartialResults.ToList(),
                Status = AggregationStatus.InProgress
            };

            _activeAggregations[message.AggregationId] = context;

            // Execute aggregation asynchronously
            ExecuteAggregationAsync(context);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to start aggregation {AggregationId}", message.AggregationId);
            message.Sender.Tell(new AggregationFailed(message.AggregationId, ex));
        }
    }

    private void Handle(AddPartialResult message)
    {
        if (_activeAggregations.TryGetValue(message.AggregationId, out var context))
        {
            _log.Info("Adding partial result to aggregation {AggregationId} from source {Source}",
                message.AggregationId, message.Source);

            context.PartialResults.Add(message.PartialResult);
            context.LastUpdated = DateTime.UtcNow;

            // Check if aggregation should be re-executed with new data
            if (context.Status == AggregationStatus.InProgress && context.Strategy == AggregationStrategy.Consensus)
            {
                // For consensus strategy, re-evaluate when new results arrive
                ExecuteAggregationAsync(context);
            }
        }
        else
        {
            _log.Warning("Cannot add partial result to aggregation {AggregationId} - aggregation not found",
                message.AggregationId);
        }
    }

    private void Handle(AggregationCompleted message)
    {
        if (_activeAggregations.TryGetValue(message.AggregationId, out var context))
        {
            context.Status = AggregationStatus.Completed;
            context.EndTime = DateTime.UtcNow;
            context.Result = message.AggregatedResult;

            _log.Info("Aggregation {AggregationId} completed in {ProcessingTime}",
                message.AggregationId, message.ProcessingTime);

            context.Requester?.Tell(message);
            UpdateAggregationMetrics(context, true);
        }
    }

    private void Handle(AggregationFailed message)
    {
        if (_activeAggregations.TryGetValue(message.AggregationId, out var context))
        {
            context.Status = AggregationStatus.Failed;
            context.EndTime = DateTime.UtcNow;
            context.Error = message.Error;

            _log.Error(message.Error, "Aggregation {AggregationId} failed", message.AggregationId);

            context.Requester?.Tell(message);
            UpdateAggregationMetrics(context, false);
        }
    }

    private void Handle(ValidateResults message)
    {
        _log.Info("Validating {ResultCount} results with criteria: MinConfidence={MinConfidence}, MaxAge={MaxAge}",
            message.Results.Count(), message.Criteria.MinConfidenceScore, message.Criteria.MaxAge);

        try
        {
            var validResults = new List<object>();
            var now = DateTime.UtcNow;

            foreach (var result in message.Results)
            {
                if (ValidateResult(result, message.Criteria, now))
                {
                    validResults.Add(result);
                }
            }

            _log.Info("Validation completed: {ValidCount}/{TotalCount} results passed validation",
                validResults.Count, message.Results.Count());

            message.Sender.Tell(validResults.AsEnumerable());
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to validate results");
            message.Sender.Tell(Enumerable.Empty<object>());
        }
    }

    private void Handle(GetAggregationMetrics message)
    {
        var totalAggregations = _aggregationMetrics.TotalAggregations;
        var successfulAggregations = _aggregationMetrics.SuccessfulAggregations;
        var averageTime = _aggregationMetrics.TotalProcessingTime.TotalSeconds > 0 
            ? TimeSpan.FromSeconds(_aggregationMetrics.TotalProcessingTime.TotalSeconds / Math.Max(1, totalAggregations))
            : TimeSpan.Zero;

        var metrics = new AggregationMetrics
        {
            TotalAggregations = totalAggregations,
            SuccessfulAggregations = successfulAggregations,
            AverageAggregationTime = averageTime,
            AverageResultQuality = _aggregationMetrics.TotalQualityScore / Math.Max(1, successfulAggregations),
            StrategyUsage = new Dictionary<AggregationStrategy, int>(_aggregationMetrics.StrategyUsage)
        };

        message.Sender.Tell(metrics);
    }

    private void Handle(CleanupAggregation message)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var completedAggregations = _activeAggregations
            .Where(kv => kv.Value.Status == AggregationStatus.Completed || 
                         kv.Value.Status == AggregationStatus.Failed)
            .Where(kv => kv.Value.EndTime < cutoffTime)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var aggregationId in completedAggregations)
        {
            _activeAggregations.Remove(aggregationId);
        }

        if (completedAggregations.Any())
        {
            _log.Info("Cleaned up {Count} completed aggregations", completedAggregations.Count);
        }
    }

    private async void ExecuteAggregationAsync(AggregationContext context)
    {
        try
        {
            if (!_strategyHandlers.TryGetValue(context.Strategy, out var handler))
            {
                throw new InvalidOperationException($"No handler found for aggregation strategy: {context.Strategy}");
            }

            _log.Info("Executing aggregation {AggregationId} with {ResultCount} results",
                context.Id, context.PartialResults.Count);

            // Execute aggregation in background
            await Task.Run(async () =>
            {
                var result = await handler.AggregateAsync(context.PartialResults, context.Parameters);
                var processingTime = DateTime.UtcNow - context.StartTime;

                Self.Tell(new AggregationCompleted(context.Id, result, processingTime));
            });
        }
        catch (Exception ex)
        {
            Self.Tell(new AggregationFailed(context.Id, ex));
        }
    }

    private bool ValidateResult(object result, ValidationCriteria criteria, DateTime now)
    {
        try
        {
            // Check age requirement
            if (result is ITimestamped timestamped)
            {
                var age = now - timestamped.Timestamp;
                if (age > criteria.MaxAge)
                {
                    return false;
                }
            }

            // Check confidence score
            if (result is IScoredResult scored)
            {
                if (scored.ConfidenceScore < criteria.MinConfidenceScore)
                {
                    return false;
                }
            }

            // Check required fields
            if (criteria.RequiredFields.Any())
            {
                var resultType = result.GetType();
                foreach (var field in criteria.RequiredFields)
                {
                    var property = resultType.GetProperty(field);
                    if (property == null || property.GetValue(result) == null)
                    {
                        return false;
                    }
                }
            }

            // Custom validators
            foreach (var validator in criteria.CustomValidators)
            {
                if (validator.Value is Func<object, bool> validatorFunc)
                {
                    if (!validatorFunc(result))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Error validating result");
            return false;
        }
    }

    private void UpdateAggregationMetrics(AggregationContext context, bool success)
    {
        _aggregationMetrics.TotalAggregations++;
        
        if (success)
        {
            _aggregationMetrics.SuccessfulAggregations++;
            
            // Calculate quality score (simplified)
            var qualityScore = CalculateQualityScore(context);
            _aggregationMetrics.TotalQualityScore += qualityScore;
        }

        if (context.EndTime.HasValue)
        {
            _aggregationMetrics.TotalProcessingTime += context.EndTime.Value - context.StartTime;
        }

        _aggregationMetrics.StrategyUsage.TryGetValue(context.Strategy, out var currentCount);
        _aggregationMetrics.StrategyUsage[context.Strategy] = currentCount + 1;
    }

    private double CalculateQualityScore(AggregationContext context)
    {
        // Simple quality scoring based on result count and processing time
        var resultScore = Math.Min(1.0, context.PartialResults.Count / 10.0);
        var timeScore = context.EndTime.HasValue ? 
            Math.Max(0.1, 1.0 - (context.EndTime.Value - context.StartTime).TotalSeconds / 60.0) : 0.5;
        
        return (resultScore + timeScore) / 2.0;
    }

    protected override void PreStart()
    {
        _log.Info("AIResultAggregatorActor started");
    }

    protected override void PostStop()
    {
        _log.Info("AIResultAggregatorActor stopped");
    }

    #region Internal Messages and State

    /// <summary>
    /// Internal message to validate results.
    /// </summary>
    /// <param name="Results">Results to validate.</param>
    /// <param name="Criteria">Validation criteria.</param>
    /// <param name="Sender">Actor that requested validation.</param>
    private record ValidateResults(
        IEnumerable<object> Results,
        ValidationCriteria Criteria,
        IActorRef Sender);

    /// <summary>
    /// Internal message to get aggregation metrics.
    /// </summary>
    /// <param name="Sender">Actor that requested metrics.</param>
    private record GetAggregationMetrics(IActorRef Sender);

    /// <summary>
    /// Internal message to clean up completed aggregations.
    /// </summary>
    private record CleanupAggregation();

    /// <summary>
    /// Context for an active aggregation operation.
    /// </summary>
    private class AggregationContext
    {
        public string Id { get; set; } = string.Empty;
        public AggregationStrategy Strategy { get; set; }
        public AggregationStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<object> PartialResults { get; set; } = new();
        public object? Result { get; set; }
        public Exception? Error { get; set; }
        public IActorRef? Requester { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Status of an aggregation operation.
    /// </summary>
    private enum AggregationStatus
    {
        InProgress,
        Completed,
        Failed
    }

    /// <summary>
    /// Aggregation metrics tracking.
    /// </summary>
    private readonly AggregationMetricsTracker _aggregationMetrics = new();

    private class AggregationMetricsTracker
    {
        public int TotalAggregations { get; set; }
        public int SuccessfulAggregations { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
        public double TotalQualityScore { get; set; }
        public Dictionary<AggregationStrategy, int> StrategyUsage { get; set; } = new();
    }

    #endregion

    #region Aggregation Strategy Handlers

    /// <summary>
    /// Interface for aggregation strategy handlers.
    /// </summary>
    private interface IAggregationHandler
    {
        Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters);
    }

    /// <summary>
    /// Handler for merge aggregation strategy.
    /// </summary>
    private class MergeAggregationHandler : IAggregationHandler
    {
        public Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters)
        {
            // Simple merge - combine all results into a collection
            var mergedResult = new
            {
                Results = results.ToList(),
                Count = results.Count(),
                MergedAt = DateTime.UtcNow,
                Strategy = "Merge"
            };

            return Task.FromResult<object>(mergedResult);
        }
    }

    /// <summary>
    /// Handler for consensus aggregation strategy.
    /// </summary>
    private class ConsensusAggregationHandler : IAggregationHandler
    {
        public Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters)
        {
            // Simple consensus - find most common result
            var resultGroups = results
                .GroupBy(r => r.ToString())
                .OrderByDescending(g => g.Count())
                .ToList();

            var consensus = resultGroups.FirstOrDefault();
            var consensusResult = new
            {
                ConsensusValue = consensus?.Key ?? "No consensus",
                Confidence = consensus != null ? (double)consensus.Count() / results.Count() : 0.0,
                TotalResults = results.Count(),
                Strategy = "Consensus"
            };

            return Task.FromResult<object>(consensusResult);
        }
    }

    /// <summary>
    /// Handler for weighted average aggregation strategy.
    /// </summary>
    private class WeightedAverageAggregationHandler : IAggregationHandler
    {
        public Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters)
        {
            var weightedResults = results.OfType<IWeightedResult>().ToList();
            
            if (!weightedResults.Any())
            {
                return Task.FromResult<object>(new { Error = "No weighted results available" });
            }

            var totalWeight = weightedResults.Sum(r => r.Weight);
            var weightedSum = weightedResults.Sum(r => r.Value * r.Weight);
            
            var averageResult = new
            {
                WeightedAverage = totalWeight > 0 ? weightedSum / totalWeight : 0,
                TotalWeight = totalWeight,
                ResultCount = weightedResults.Count,
                Strategy = "WeightedAverage"
            };

            return Task.FromResult<object>(averageResult);
        }
    }

    /// <summary>
    /// Handler for best result aggregation strategy.
    /// </summary>
    private class BestResultAggregationHandler : IAggregationHandler
    {
        public Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters)
        {
            var scoredResults = results.OfType<IScoredResult>().ToList();
            
            var bestResult = scoredResults
                .OrderByDescending(r => r.ConfidenceScore)
                .FirstOrDefault();

            if (bestResult != null)
            {
                return Task.FromResult<object>(new
                {
                    BestResult = bestResult,
                    Score = bestResult.ConfidenceScore,
                    Strategy = "BestResult"
                });
            }

            // Fall back to first result if no scored results
            return Task.FromResult<object>(new
            {
                BestResult = results.FirstOrDefault(),
                Score = 0.0,
                Strategy = "BestResult"
            });
        }
    }

    /// <summary>
    /// Handler for custom aggregation strategy.
    /// </summary>
    private class CustomAggregationHandler : IAggregationHandler
    {
        public Task<object> AggregateAsync(IEnumerable<object> results, Dictionary<string, object> parameters)
        {
            // Custom aggregation logic can be implemented here
            // For now, return a simple custom result
            var customResult = new
            {
                Results = results.ToList(),
                CustomProcessing = "Applied custom aggregation logic",
                ProcessedAt = DateTime.UtcNow,
                Strategy = "Custom"
            };

            return Task.FromResult<object>(customResult);
        }
    }

    #endregion

    #region Helper Interfaces

    /// <summary>
    /// Interface for timestamped results.
    /// </summary>
    private interface ITimestamped
    {
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// Interface for scored results.
    /// </summary>
    private interface IScoredResult
    {
        double ConfidenceScore { get; }
    }

    /// <summary>
    /// Interface for weighted results.
    /// </summary>
    private interface IWeightedResult
    {
        double Value { get; }
        double Weight { get; }
    }

    #endregion
}
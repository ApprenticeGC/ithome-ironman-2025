namespace TestLib.CQRS;

/// <summary>
/// Marker interface for queries in the CQRS system.
/// Queries represent read operations that return data without modifying state.
/// </summary>
/// <typeparam name="TResult">The type of data returned by the query.</typeparam>
public interface IQuery<TResult>
{
}
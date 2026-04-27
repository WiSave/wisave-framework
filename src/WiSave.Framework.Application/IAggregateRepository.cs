using WiSave.Framework.Domain;

namespace WiSave.Framework.Application;

public interface IAggregateRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, IAggregateStream<TId>, new()
{
    Task<TAggregate?> LoadAsync(TId id, CancellationToken ct = default);
    Task SaveAsync(TAggregate aggregate, CancellationToken ct = default);
}

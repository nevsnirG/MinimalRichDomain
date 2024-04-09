using MinimalDomainEvents.Core;

namespace MinimalRichDomain;
public abstract class AggregateRoot<TId> : IEntity<TId>
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public TId Id { get; }
    public int CurrentVersion { get; private set; }

    protected int NextVersion => CurrentVersion + 1;

    private readonly List<IDomainEvent> _domainEvents;

    protected AggregateRoot(TId id)
    {
        Id = id;
        _domainEvents = new();
    }

    protected AggregateRoot(TId id, IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        Id = id;
        _domainEvents = new(domainEvents.Count);
        Rehydrate(domainEvents);
    }

    protected virtual void Rehydrate(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        var domainEventsOrderedByVersion = domainEvents.OrderBy(de => de.Version);

        foreach (var domainEvent in domainEventsOrderedByVersion)
        {
            ApplyInternal(domainEvent);
        }
    }

    protected virtual void RaiseAndApplyDomainEvent(IDomainEvent domainEvent)
    {
        ApplyInternal(domainEvent);
        DomainEventTracker.RaiseDomainEvent(domainEvent);
    }

    protected void ApplyInternal(IDomainEvent domainEvent)
    {
        if (CanApply(domainEvent))
        {
            try
            {
                Apply(domainEvent);
                AppliedDomainEvent(domainEvent);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                throw new InvalidOperationException($"No Apply method has been implemented for type: {domainEvent.GetType().FullName}.");
            }
        }
        else
            throw new InvalidOperationException($"Cannot apply a domain event with version {domainEvent.Version} while the aggregate is at version {CurrentVersion}. Some aggregate history might be missing.");
    }

    protected virtual bool CanApply(IDomainEvent @event)
    {
        return @event.Version == NextVersion;
    }

    protected abstract void Apply(IDomainEvent @event);

    private void AppliedDomainEvent(IDomainEvent domainEvent)
    {
        ValidateState();
        _domainEvents.Add(domainEvent);
        IncrementVersion();
    }

    protected abstract void ValidateState();

    private void IncrementVersion()
    {
        CurrentVersion++;
    }
}
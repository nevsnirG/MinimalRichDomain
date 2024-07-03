using MinimalDomainEvents.Core;

namespace MinimalRichDomain;
public abstract class EventSourcedEntity<TId> : IEntity<TId>
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public TId Id { get; }
    public int CurrentVersion { get; private set; }
    public int NextVersion => CurrentVersion + 1;

    private readonly List<IDomainEvent> _domainEvents;

    protected EventSourcedEntity(TId id)
    {
        Id = id;
        _domainEvents = new();
    }

    protected EventSourcedEntity(TId id, IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        Id = id;
        _domainEvents = new(domainEvents.Count);
        Rehydrate(domainEvents);
    }

    private void Rehydrate(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        var domainEventsOrderedByVersion = domainEvents.OrderBy(de => de.EntityVersion);

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
            throw new InvalidOperationException($"Cannot apply a domain event with version {domainEvent.EntityVersion} while the aggregate is at version {CurrentVersion}. Some aggregate history might be missing.");
    }

    protected virtual bool CanApply(IDomainEvent @event)
    {
        return @event.EntityVersion == NextVersion;
    }

    protected abstract void Apply(IDomainEvent @event);

    private void AppliedDomainEvent(IDomainEvent domainEvent)
    {
        ValidateState();
        _domainEvents.Add(domainEvent);
        CurrentVersion++;
    }

    protected abstract void ValidateState();
}
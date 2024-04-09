using MinimalDomainEvents.Core;
using System.Reflection;

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
        ValidateHistory(domainEventsOrderedByVersion);

        foreach (var domainEvent in domainEventsOrderedByVersion)
        {
            Apply(domainEvent);
        }
    }

    private static void ValidateHistory(IOrderedEnumerable<IDomainEvent> domainEventsOrderedByVersion)
    {
        var lastVersion = 0;
        if (!domainEventsOrderedByVersion.All(de =>
        {
            if (de.Version - lastVersion == 1)
            {
                lastVersion = de.Version;
                return true;
            }
            else
                return false;
        }))
        {
            throw new InvalidOperationException($"Aggregate history incomplete. Missing domain event version {lastVersion + 1}.");
        }
    }

    protected virtual void RaiseAndApplyDomainEvent(IDomainEvent domainEvent)
    {
        Apply(domainEvent);
        DomainEventTracker.RaiseDomainEvent(domainEvent);
    }

    protected virtual void Apply(IDomainEvent domainEvent)
    {
        if (CanApply(domainEvent))
        {
            var eventType = domainEvent.GetType();
            var interfaceType = typeof(IApplyEvent<>).MakeGenericType(eventType);

            var applyMethod = GetType().GetInterfaceMap(interfaceType).TargetMethods
                .FirstOrDefault(m => m.Name.EndsWith(nameof(IApplyEvent<IDomainEvent>.Apply)));

            if (applyMethod is not default(MethodInfo))
            {
                applyMethod.Invoke(this, new object[] { domainEvent });
                AppliedDomainEvent(domainEvent);
            }
            else
                throw new InvalidOperationException($"No Apply method found for domain event type {domainEvent.GetType()}.");
        }
        else
            throw new InvalidOperationException($"Cannot apply a domain event with version {domainEvent.Version} while the aggregate is at version {CurrentVersion}. Some aggregate history might be missing.");
    }

    protected virtual bool CanApply(IDomainEvent @event)
    {
        return @event.Version == NextVersion;
    }

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
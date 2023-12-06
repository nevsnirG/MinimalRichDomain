using MinimalDomainEvents.Core;

namespace MinimalRichDomain;
public abstract class Entity<TId>
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public TId Id { get; }
    public int CurrentVersion { get; private set; }
    protected int NextVersion => CurrentVersion + 1;
    private readonly List<IDomainEvent> _domainEvents;

    protected Entity(TId id)
    {
        Id = id;
        _domainEvents = new();
    }

    protected Entity(TId id, IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        Id = id;
        Rehydrate(domainEvents);
        _domainEvents = new(domainEvents);
    }

    protected virtual void Rehydrate(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        foreach (var @event in domainEvents.OrderBy(de => de.Version))
        {
            Apply(@event);
        }

        ValidateRehydration();
    }

    protected void Apply(IDomainEvent @event)
    {
        if (CanApply(@event))
        {
            var eventType = @event.GetType();
            var interfaceType = typeof(IApplyEvent<>).MakeGenericType(eventType);

            var applyMethod = GetType().GetInterfaceMap(interfaceType).TargetMethods
                .FirstOrDefault(m => m.Name.EndsWith(nameof(IApplyEvent<IDomainEvent>.Apply)));

            applyMethod?.Invoke(this, new object[] { @event });
            IncrementVersion();
        }
        else
            throw new InvalidOperationException($"Cannot apply event with version {@event.Version} to entity version {CurrentVersion}. Some history might be missing.");
    }

    private bool CanApply(IDomainEvent @event)
    {
        return @event.Version == NextVersion;
    }

    protected abstract void ValidateRehydration();

    protected virtual void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent.Version != NextVersion)
            throw new InvalidOperationException($"Cannot raise a domain event for version {domainEvent.Version} while entity version is {CurrentVersion}.");

        Apply(domainEvent);
        DomainEventTracker.RaiseDomainEvent(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    private void IncrementVersion()
    {
        CurrentVersion++;
    }
}
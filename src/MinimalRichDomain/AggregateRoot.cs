using MinimalDomainEvents.Core;
using System.Reflection;

namespace MinimalRichDomain;
public abstract class AggregateRoot<TId>
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
        foreach (var domainEvent in domainEvents.OrderBy(de => de.Version))
        {
            Apply(domainEvent);
        }
    }

    protected virtual void RaiseAndApplyDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent.Version != NextVersion)
            throw new InvalidOperationException($"Cannot raise a domain event for version {domainEvent.Version} while entity version is {CurrentVersion}.");

        Apply(domainEvent);
        DomainEventTracker.RaiseDomainEvent(domainEvent);
    }

    public virtual void Apply(IDomainEvent domainEvent)
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
                return;
            }
        }

        throw new InvalidOperationException($"Cannot apply event with version {domainEvent.Version} to entity version {CurrentVersion}. Some history might be missing.");
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
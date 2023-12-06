namespace MinimalRichDomain;
public interface IDomainEvent : MinimalDomainEvents.Contract.IDomainEvent
{
    int Version { get; }
}
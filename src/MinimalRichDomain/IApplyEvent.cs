namespace MinimalRichDomain;
public interface IApplyEvent<TEvent> where TEvent : IDomainEvent
{
    void Apply(TEvent @event);
}
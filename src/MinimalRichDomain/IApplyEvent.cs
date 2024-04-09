using System.ComponentModel;

namespace MinimalRichDomain;
public interface IApplyEvent<TEvent> where TEvent : IDomainEvent
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    void Apply(TEvent @event);
}
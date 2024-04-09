namespace MinimalRichDomain;
public interface IEntity<TId>
{
    TId Id { get; }
}
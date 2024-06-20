namespace MinimalRichDomain;
public interface IEntity<out TId>
{
    TId Id { get; }
}
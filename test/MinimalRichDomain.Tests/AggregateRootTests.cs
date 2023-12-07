using FluentAssertions;

namespace MinimalRichDomain.Tests;

public class AggregateRootTests
{
    [Fact]
    public void CannotApplyEventWithLowerVersion()
    {
        var domainEvent = new TestDomainEvent(-1);
        var testEntity = new TestEntity();

        var action = () => testEntity.Apply(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply event with version -1 to entity version 0. Some history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithSameVersion()
    {
        var domainEvent = new TestDomainEvent(0);
        var testEntity = new TestEntity();

        var action = () => testEntity.Apply(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply event with version 0 to entity version 0. Some history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithTooHighVersion()
    {
        var domainEvent = new TestDomainEvent(2);
        var testEntity = new TestEntity();

        var action = () => testEntity.Apply(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply event with version 2 to entity version 0. Some history might be missing.");
    }

    [Fact]
    public void CanApplyEventWithIncrementalVersion()
    {
        var domainEvent = new TestDomainEvent(1);
        var testEntity = new TestEntity();

        testEntity.Apply(domainEvent);

        testEntity.CurrentVersion.Should().Be(1);
    }

    private sealed record class TestDomainEvent(int Version) : IDomainEvent;

    private class TestEntity : AggregateRoot<Guid>, IApplyEvent<TestDomainEvent>
    {
        public TestEntity() : base(Guid.NewGuid())
        {
        }

        protected override void ValidateState()
        {
        }

        void IApplyEvent<TestDomainEvent>.Apply(TestDomainEvent @event)
        {
        }
    }
}
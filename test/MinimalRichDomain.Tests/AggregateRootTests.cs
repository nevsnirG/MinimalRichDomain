using FluentAssertions;

namespace MinimalRichDomain.Tests;

public class AggregateRootTests
{
    [Fact]
    public void CannotApplyEventWithLowerVersion()
    {
        var domainEvent = new TestDomainEvent(-1);
        var testEntity = new TestEntity();

        var action = () => testEntity.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version -1 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithSameVersion()
    {
        var domainEvent = new TestDomainEvent(0);
        var testEntity = new TestEntity();

        var action = () => testEntity.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version 0 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithTooHighVersion()
    {
        var domainEvent = new TestDomainEvent(2);
        var testEntity = new TestEntity();

        var action = () => testEntity.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version 2 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CanApplyEventWithIncrementalVersion()
    {
        var domainEvent = new TestDomainEvent(1);
        var testEntity = new TestEntity();

        testEntity.ApplyForTest(domainEvent);

        testEntity.CurrentVersion.Should().Be(1);
    }

    [Fact]
    public void CanRehydrateWithCompleteHistory()
    {
        var domainEvent1 = new TestDomainEvent(1);
        var domainEvent2 = new TestDomainEvent(2);
        var domainEvent3 = new TestDomainEvent(3);
        var history = new List<TestDomainEvent> { domainEvent1, domainEvent2, domainEvent3 };

        TestEntity action() => new(history);

        FluentActions.Invoking(action).Should().NotThrow("the history is complete.");
    }

    private sealed record class TestDomainEvent(int Version) : IDomainEvent;

    private class TestEntity : AggregateRoot<Guid>
    {
        public TestEntity() : base(Guid.NewGuid())
        {
        }

        public TestEntity(IReadOnlyCollection<IDomainEvent> domainEvents) : base(Guid.NewGuid(), domainEvents) { }

        public void ApplyForTest(IDomainEvent domainEvent)
        {
            ApplyInternal(domainEvent);
        }

        protected override void Apply(IDomainEvent domainEvent)
        {
            Apply((dynamic)domainEvent);
        }

        protected override void ValidateState()
        {
        }

        private void Apply(TestDomainEvent @event)
        {
        }
    }
}
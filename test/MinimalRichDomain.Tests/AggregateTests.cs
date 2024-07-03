using FluentAssertions;

namespace MinimalRichDomain.Tests;

public class AggregateTests
{
    [Fact]
    public void CannotApplyEventWithLowerVersion()
    {
        var domainEvent = new TestDomainEvent(-1);
        var testAggregate = new TestAggregate();

        var action = () => testAggregate.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version -1 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithSameVersion()
    {
        var domainEvent = new TestDomainEvent(0);
        var testAggregate = new TestAggregate();

        var action = () => testAggregate.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version 0 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CannotApplyEventWithTooHighVersion()
    {
        var domainEvent = new TestDomainEvent(2);
        var testAggregate = new TestAggregate();

        var action = () => testAggregate.ApplyForTest(domainEvent);

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Cannot apply a domain event with version 2 while the aggregate is at version 0. Some aggregate history might be missing.");
    }

    [Fact]
    public void CanApplyEventWithIncrementalVersion()
    {
        var domainEvent = new TestDomainEvent(1);
        var testAggregate = new TestAggregate();

        testAggregate.ApplyForTest(domainEvent);

        testAggregate.CurrentVersion.Should().Be(1);
    }

    [Fact]
    public void CanRehydrateWithCompleteHistory()
    {
        var domainEvent1 = new TestDomainEvent(1);
        var domainEvent2 = new TestDomainEvent(2);
        var domainEvent3 = new TestDomainEvent(3);
        var history = new List<TestDomainEvent> { domainEvent1, domainEvent2, domainEvent3 };

        TestAggregate action() => new(history);

        FluentActions.Invoking(action).Should().NotThrow("the history is complete.");
    }

    [Fact]
    public void CanNotApplyEventThatsNotImplemented()
    {
        var domainEvent = new TestDomainEventNotImplemented(1);
        var testAggregate = new TestAggregate();

        void action() => testAggregate.ApplyForTest(domainEvent);

        FluentActions.Invoking(action).Should().Throw<InvalidOperationException>().WithMessage("No Apply method has been implemented for type: *.");
    }

    private sealed record class TestDomainEvent(int EntityVersion) : IDomainEvent;
    private sealed record class TestDomainEventNotImplemented(int EntityVersion) : IDomainEvent;

    private class TestAggregate : Aggregate<Guid>
    {
        public TestAggregate() : base(Guid.NewGuid())
        {
        }

        public TestAggregate(IReadOnlyCollection<IDomainEvent> domainEvents) : base(Guid.NewGuid(), domainEvents) { }

        public void ApplyForTest(IDomainEvent domainEvent)
        {
            ApplyInternal(domainEvent);
        }

        protected override void Apply(IDomainEvent domainEvent)
        {
            ApplyEvent((dynamic)domainEvent);
        }

        protected override void ValidateState()
        {
        }

        private void ApplyEvent(TestDomainEvent @event)
        {
        }
    }
}
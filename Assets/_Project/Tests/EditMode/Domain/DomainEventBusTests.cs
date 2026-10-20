using System.Collections.Generic;
using SugarDash.Domain.Events;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class DomainEventBusTests
    {
        private sealed class Ping { }
        private sealed class Pong { }

        [Test]
        public void Publish_DeliversOnlyToSubscribersOfThatType()
        {
            var bus = new DomainEventBus();
            int pings = 0, pongs = 0;
            bus.Subscribe<Ping>(_ => pings++);
            bus.Subscribe<Pong>(_ => pongs++);

            bus.Publish(new Ping());

            Assert.That(pings, Is.EqualTo(1));
            Assert.That(pongs, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_StopsDelivery()
        {
            var bus = new DomainEventBus();
            int count = 0;
            var subscription = bus.Subscribe<Ping>(_ => count++);

            subscription.Dispose();
            bus.Publish(new Ping());

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void UnsubscribingDuringPublish_DoesNotSkipOtherHandlers()
        {
            var bus = new DomainEventBus();
            var calls = new List<string>();
            System.IDisposable first = null;
            first = bus.Subscribe<Ping>(_ => { calls.Add("first"); first.Dispose(); });
            bus.Subscribe<Ping>(_ => calls.Add("second"));

            bus.Publish(new Ping());
            bus.Publish(new Ping());

            Assert.That(calls, Is.EqualTo(new[] { "first", "second", "second" }));
        }
    }
}

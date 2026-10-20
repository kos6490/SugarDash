using System;
using System.Collections.Generic;

namespace SugarDash.Domain.Events
{
    /// <summary>
    /// 순수 C# 이벤트 버스. 도메인 이벤트는 모두 이 버스로만 전달한다.
    /// 구독은 반환된 <see cref="IDisposable"/>로 해제한다.
    /// </summary>
    public sealed class DomainEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(typeof(T), list);
            }
            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        public void Publish<T>(T domainEvent)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // 처리 중 구독 해제가 일어나도 이번 발행의 나머지 구독자는 모두 받는다.
            foreach (var handler in list.ToArray())
            {
                ((Action<T>)handler)(domainEvent);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action _unsubscribe;

            public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}

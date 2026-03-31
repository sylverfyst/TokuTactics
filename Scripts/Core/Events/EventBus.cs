using System;
using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Core.Events
{
    /// <summary>
    /// Phased event bus with queue-and-dispatch model and subscriber-level priority.
    /// 
    /// TWO LEVELS OF PRIORITY SORTING:
    /// 
    /// 1. Event-level: When multiple events are queued, they dispatch in priority
    ///    order based on the EVENT's priority (set on the event class itself).
    ///    GameState events dispatch before Gameplay events, etc.
    /// 
    /// 2. Subscriber-level: When multiple handlers subscribe to the SAME event type,
    ///    they fire in priority order based on the SUBSCRIBER's declared priority.
    ///    A GameState handler fires before a Presentation handler for the same event.
    /// 
    /// This means a single FormDiedEvent can have:
    ///   - One GameState handler (does all state mutation: cooldowns, permadeath, demorph)
    ///   - Multiple Gameplay handlers (AI recalc, bond updates — read-only)
    ///   - Multiple Presentation handlers (animations, UI updates — read-only)
    ///   - Multiple Audio handlers (sound effects — read-only)
    /// All guaranteed to fire in that order.
    /// 
    /// CONVENTION: Only ONE GameState-priority subscriber per event type.
    /// GameState handlers do all state mutation. Lower tiers are read-only.
    /// Enable DebugMode to get warnings when this convention is violated.
    /// 
    /// QUEUE MODEL:
    /// - Events are NOT dispatched immediately — they accumulate in a queue.
    /// - Dispatch() processes them in batch with priority sorting.
    /// - Events published during dispatch go to an overflow queue for the next cycle.
    /// - No recursive cascades. Deterministic ordering.
    /// 
    /// Note: Subscriptions match by exact concrete type only.
    /// Subscribing to IGameEvent will NOT receive derived types.
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<SubscriptionEntry>> _subscribers = new();
        private readonly List<IGameEvent> _queue = new();
        private readonly List<IGameEvent> _overflowQueue = new();
        private bool _isDispatching;

        /// <summary>
        /// Maximum dispatch cycles per Dispatch() call.
        /// Prevents infinite loops if subscribers keep publishing events.
        /// </summary>
        public int MaxDispatchCycles { get; set; } = 10;

        /// <summary>
        /// Number of events discarded on the last Dispatch() due to MaxDispatchCycles.
        /// Zero if dispatch completed normally.
        /// </summary>
        public int DroppedEventCount { get; private set; }

        /// <summary>
        /// When true, logs warnings for convention violations:
        /// - Multiple GameState-priority subscribers on the same event type.
        /// Warnings are collected in DebugWarnings for inspection.
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// Warnings collected during debug mode. Cleared on each Dispatch().
        /// </summary>
        public List<string> DebugWarnings { get; } = new();

        // === Subscribe / Unsubscribe ===

        /// <summary>
        /// Subscribe to a specific event type at a given priority tier.
        /// 
        /// When the event dispatches, handlers fire in priority order:
        /// GameState first, then Gameplay, then Presentation, then Audio.
        /// Within the same priority, handlers fire in registration order.
        /// 
        /// Priority is required — no default. This forces every subscription
        /// to declare its tier, preventing silent ordering bugs from forgotten parameters.
        /// 
        /// CONVENTION: Only one GameState-priority subscriber per event type.
        /// Enable DebugMode to get warnings when violated.
        /// </summary>
        public EventSubscription Subscribe<T>(Action<T> handler, EventPriority priority) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<SubscriptionEntry>();

            // Debug: warn on multiple GameState subscribers for same event type
            if (DebugMode && priority == EventPriority.GameState)
            {
                int existingGameState = _subscribers[type]
                    .Count(e => e.Priority == EventPriority.GameState);
                if (existingGameState > 0)
                {
                    DebugWarnings.Add(
                        $"CONVENTION VIOLATION: Multiple GameState subscribers for {type.Name}. " +
                        $"Only one GameState handler should exist per event type. " +
                        $"Existing: {existingGameState}, adding another.");
                }
            }

            var entry = new SubscriptionEntry
            {
                EventType = type,
                Priority = priority,
                Handler = evt => handler((T)evt),
                Subscription = new EventSubscription()
            };
            entry.Subscription.Entry = entry;
            entry.Subscription.Bus = this;

            _subscribers[type].Add(entry);
            return entry.Subscription;
        }

        /// <summary>
        /// Remove a subscription. Called via the EventSubscription handle.
        /// </summary>
        internal void Unsubscribe(SubscriptionEntry entry)
        {
            if (entry == null) return;
            if (_subscribers.ContainsKey(entry.EventType))
            {
                _subscribers[entry.EventType].Remove(entry);
            }
        }

        /// <summary>
        /// Remove all subscriptions for a specific event type.
        /// </summary>
        public void UnsubscribeAll<T>() where T : IGameEvent
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Clear();
        }

        /// <summary>
        /// Remove ALL subscriptions. Used for cleanup between missions or tests.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _subscribers.Clear();
        }

        // === Publish ===

        /// <summary>
        /// Queue an event for dispatch. The event is NOT dispatched immediately.
        /// If called during a Dispatch() cycle, the event goes to the overflow
        /// queue and is processed in the next cycle.
        /// </summary>
        public void Publish(IGameEvent evt)
        {
            if (evt == null) return;

            if (_isDispatching)
                _overflowQueue.Add(evt);
            else
                _queue.Add(evt);
        }

        // === Dispatch ===

        /// <summary>
        /// Dispatch all queued events to subscribers.
        /// 
        /// Events sort by their event-level priority (GameState events before Gameplay, etc.).
        /// Within each event, subscribers sort by their subscriber-level priority.
        /// Within the same subscriber priority, handlers fire in registration order.
        /// 
        /// Events published during dispatch queue for the next cycle.
        /// If MaxDispatchCycles is reached, remaining events are discarded.
        /// 
        /// Returns the total number of events dispatched across all cycles.
        /// </summary>
        public int Dispatch()
        {
            int totalDispatched = 0;
            int cycles = 0;
            DroppedEventCount = 0;

            if (DebugMode)
                DebugWarnings.Clear();

            while (_queue.Count > 0 && cycles < MaxDispatchCycles)
            {
                cycles++;

                // Sort events by their event-level priority, stable within same priority
                var sorted = _queue
                    .Select((evt, index) => new { Event = evt, Index = index })
                    .OrderBy(x => (int)x.Event.Priority)
                    .ThenBy(x => x.Index)
                    .Select(x => x.Event)
                    .ToList();

                _queue.Clear();
                _isDispatching = true;

                try
                {
                    foreach (var evt in sorted)
                    {
                        DispatchSingle(evt);
                        totalDispatched++;
                    }
                }
                finally
                {
                    _isDispatching = false;

                    if (_overflowQueue.Count > 0)
                    {
                        _queue.AddRange(_overflowQueue);
                        _overflowQueue.Clear();
                    }
                }
            }

            if (_queue.Count > 0)
            {
                DroppedEventCount = _queue.Count;
                _queue.Clear();
                _overflowQueue.Clear();
            }

            return totalDispatched;
        }

        /// <summary>
        /// Dispatch a single event to all subscribers of its type,
        /// sorted by subscriber priority (GameState before Gameplay before
        /// Presentation before Audio). Within same priority, registration order.
        /// </summary>
        private void DispatchSingle(IGameEvent evt)
        {
            var type = evt.GetType();

            if (!_subscribers.ContainsKey(type))
                return;

            // Copy and sort by subscriber priority, stable within same priority
            var handlers = _subscribers[type]
                .Select((entry, index) => new { Entry = entry, Index = index })
                .OrderBy(x => (int)x.Entry.Priority)
                .ThenBy(x => x.Index)
                .Select(x => x.Entry)
                .ToList();

            foreach (var entry in handlers)
            {
                entry.Handler(evt);
            }
        }

        // === Inspection ===

        /// <summary>Number of events currently in the queue.</summary>
        public int QueuedCount => _queue.Count;

        /// <summary>Number of subscribers for a specific event type.</summary>
        public int SubscriberCount<T>() where T : IGameEvent
        {
            var type = typeof(T);
            return _subscribers.ContainsKey(type) ? _subscribers[type].Count : 0;
        }

        /// <summary>
        /// Number of subscribers at a specific priority for a specific event type.
        /// </summary>
        public int SubscriberCountAtPriority<T>(EventPriority priority) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) return 0;
            return _subscribers[type].Count(e => e.Priority == priority);
        }

        /// <summary>Peek at queued events without dispatching.</summary>
        public IReadOnlyList<IGameEvent> PeekQueue() => _queue.AsReadOnly();

        /// <summary>Clear the queue without dispatching.</summary>
        public void ClearQueue()
        {
            _queue.Clear();
            _overflowQueue.Clear();
        }
    }

    /// <summary>
    /// Internal tracking for a subscription.
    /// </summary>
    internal class SubscriptionEntry
    {
        public Type EventType { get; set; }
        public EventPriority Priority { get; set; }
        public Action<IGameEvent> Handler { get; set; }
        public EventSubscription Subscription { get; set; }
    }

    /// <summary>
    /// Handle returned by Subscribe(). Call Dispose() or Unsubscribe() to remove.
    /// </summary>
    public class EventSubscription : IDisposable
    {
        internal SubscriptionEntry Entry { get; set; }
        internal EventBus Bus { get; set; }

        public void Unsubscribe()
        {
            Bus?.Unsubscribe(Entry);
            Bus = null;
            Entry = null;
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}

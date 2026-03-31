using System.Collections.Generic;
using TokuTactics.Core.Events;

namespace TokuTactics.Tests.Core.Events
{
    public class EventBusTests
    {
        // === Test event types (event-level priority) ===

        private class GameStateEvent : IGameEvent
        {
            public EventPriority Priority => EventPriority.GameState;
            public string Data { get; set; }
        }

        private class GameplayEvent : IGameEvent
        {
            public EventPriority Priority => EventPriority.Gameplay;
            public string Data { get; set; }
        }

        private class PresentationEvent : IGameEvent
        {
            public EventPriority Priority => EventPriority.Presentation;
            public string Data { get; set; }
        }

        private class AudioTestEvent : IGameEvent
        {
            public EventPriority Priority => EventPriority.Audio;
            public string Data { get; set; }
        }

        // === Publish and Queue ===

        public void Publish_QueuesEvent_DoesNotDispatch()
        {
            var bus = new EventBus();
            bool called = false;
            bus.Subscribe<GameStateEvent>(e => called = true, EventPriority.GameState);

            bus.Publish(new GameStateEvent());

            Assert(!called, "Should not dispatch immediately");
            Assert(bus.QueuedCount == 1, "Should have 1 queued event");
        }

        public void Publish_MultipleEvents_AllQueued()
        {
            var bus = new EventBus();

            bus.Publish(new GameStateEvent { Data = "a" });
            bus.Publish(new GameplayEvent { Data = "b" });
            bus.Publish(new PresentationEvent { Data = "c" });

            Assert(bus.QueuedCount == 3, "Should have 3 queued events");
        }

        public void Publish_Null_IsIgnored()
        {
            var bus = new EventBus();

            bus.Publish(null);

            Assert(bus.QueuedCount == 0, "Null should not be queued");
        }

        // === Dispatch Basics ===

        public void Dispatch_CallsSubscriber()
        {
            var bus = new EventBus();
            string received = null;
            bus.Subscribe<GameStateEvent>(e => received = e.Data, EventPriority.GameState);

            bus.Publish(new GameStateEvent { Data = "hello" });
            bus.Dispatch();

            Assert(received == "hello", "Subscriber should receive the event data");
        }

        public void Dispatch_ClearsQueue()
        {
            var bus = new EventBus();
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(bus.QueuedCount == 0, "Queue should be empty after dispatch");
        }

        public void Dispatch_ReturnsEventCount()
        {
            var bus = new EventBus();
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Publish(new GameStateEvent());
            int count = bus.Dispatch();

            Assert(count == 2, "Should return 2 dispatched events");
        }

        public void Dispatch_EmptyQueue_ReturnsZero()
        {
            var bus = new EventBus();
            int count = bus.Dispatch();

            Assert(count == 0, "Should return 0 for empty queue");
        }

        public void Dispatch_NoSubscribers_StillClearsQueue()
        {
            var bus = new EventBus();

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(bus.QueuedCount == 0, "Queue should be clear even with no subscribers");
        }

        // === Event-Level Priority Ordering ===

        public void Dispatch_EventPriority_GameStateBeforeGameplay()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<GameplayEvent>(e => order.Add("gameplay"), EventPriority.Gameplay);
            bus.Subscribe<GameStateEvent>(e => order.Add("gamestate"), EventPriority.GameState);

            // Publish in reverse priority order
            bus.Publish(new GameplayEvent());
            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(order[0] == "gamestate", "GameState event should dispatch first");
            Assert(order[1] == "gameplay", "Gameplay event should dispatch second");
        }

        public void Dispatch_EventPriority_AllFourTiers()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<AudioTestEvent>(e => order.Add("audio"), EventPriority.Audio);
            bus.Subscribe<PresentationEvent>(e => order.Add("presentation"), EventPriority.Presentation);
            bus.Subscribe<GameplayEvent>(e => order.Add("gameplay"), EventPriority.Gameplay);
            bus.Subscribe<GameStateEvent>(e => order.Add("gamestate"), EventPriority.GameState);

            bus.Publish(new AudioTestEvent());
            bus.Publish(new PresentationEvent());
            bus.Publish(new GameplayEvent());
            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(order[0] == "gamestate", "GameState first");
            Assert(order[1] == "gameplay", "Gameplay second");
            Assert(order[2] == "presentation", "Presentation third");
            Assert(order[3] == "audio", "Audio last");
        }

        public void Dispatch_SameEventPriority_PreservesQueueOrder()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<GameStateEvent>(e => order.Add(e.Data), EventPriority.GameState);

            bus.Publish(new GameStateEvent { Data = "first" });
            bus.Publish(new GameStateEvent { Data = "second" });
            bus.Publish(new GameStateEvent { Data = "third" });
            bus.Dispatch();

            Assert(order[0] == "first", "First queued should dispatch first");
            Assert(order[1] == "second", "Second queued should dispatch second");
            Assert(order[2] == "third", "Third queued should dispatch third");
        }

        // === Subscriber-Level Priority Ordering (PATH 3) ===

        public void Dispatch_SubscriberPriority_GameStateBeforePresentationOnSameEvent()
        {
            var bus = new EventBus();
            var order = new List<string>();

            // Subscribe in REVERSE priority order — subscriber sort should fix it
            bus.Subscribe<GameStateEvent>(e => order.Add("presentation"), EventPriority.Presentation);
            bus.Subscribe<GameStateEvent>(e => order.Add("gamestate"), EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => order.Add("audio"), EventPriority.Audio);
            bus.Subscribe<GameStateEvent>(e => order.Add("gameplay"), EventPriority.Gameplay);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(order[0] == "gamestate", "GameState subscriber should fire first");
            Assert(order[1] == "gameplay", "Gameplay subscriber should fire second");
            Assert(order[2] == "presentation", "Presentation subscriber should fire third");
            Assert(order[3] == "audio", "Audio subscriber should fire last");
        }

        public void Dispatch_SubscriberPriority_SameTier_PreservesRegistrationOrder()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<GameStateEvent>(e => order.Add("first"), EventPriority.Presentation);
            bus.Subscribe<GameStateEvent>(e => order.Add("second"), EventPriority.Presentation);
            bus.Subscribe<GameStateEvent>(e => order.Add("third"), EventPriority.Presentation);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(order[0] == "first", "Same priority: first registered fires first");
            Assert(order[1] == "second", "Same priority: second registered fires second");
            Assert(order[2] == "third", "Same priority: third registered fires third");
        }

        public void Dispatch_SubscriberPriority_StateHandlerMutatesBeforePresentationReads()
        {
            var bus = new EventBus();
            string stateAtPresentationTime = null;
            string sharedState = "before";

            // GameState handler mutates state
            bus.Subscribe<GameStateEvent>(e =>
            {
                sharedState = "after";
            }, EventPriority.GameState);

            // Presentation handler reads state — should see "after"
            bus.Subscribe<GameStateEvent>(e =>
            {
                stateAtPresentationTime = sharedState;
            }, EventPriority.Presentation);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(stateAtPresentationTime == "after",
                "Presentation should see state AFTER GameState handler mutated it");
        }

        // === Cascade Prevention ===

        public void Dispatch_EventsDuringDispatch_QueuedForNextCycle()
        {
            var bus = new EventBus();
            var order = new List<string>();

            bus.Subscribe<GameStateEvent>(e =>
            {
                order.Add("gamestate:" + e.Data);
                if (e.Data == "trigger")
                {
                    bus.Publish(new GameplayEvent { Data = "cascaded" });
                }
            }, EventPriority.GameState);

            bus.Subscribe<GameplayEvent>(e =>
            {
                order.Add("gameplay:" + e.Data);
            }, EventPriority.Gameplay);

            bus.Publish(new GameStateEvent { Data = "trigger" });
            bus.Dispatch();

            Assert(order.Count == 2, "Should have 2 events total");
            Assert(order[0] == "gamestate:trigger", "Original event first");
            Assert(order[1] == "gameplay:cascaded", "Cascaded event in next cycle");
        }

        public void Dispatch_MultipleCascadeCycles()
        {
            var bus = new EventBus();
            int cycleCount = 0;

            bus.Subscribe<GameStateEvent>(e =>
            {
                cycleCount++;
                if (cycleCount < 3)
                {
                    bus.Publish(new GameStateEvent { Data = $"cycle{cycleCount}" });
                }
            }, EventPriority.GameState);

            bus.Publish(new GameStateEvent { Data = "start" });
            bus.Dispatch();

            Assert(cycleCount == 3, "Should process 3 cycles");
        }

        public void Dispatch_MaxCycleLimit_PreventsInfiniteLoop()
        {
            var bus = new EventBus();
            bus.MaxDispatchCycles = 5;
            int callCount = 0;

            bus.Subscribe<GameStateEvent>(e =>
            {
                callCount++;
                bus.Publish(new GameStateEvent());
            }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(callCount == 5, "Should stop at MaxDispatchCycles");
            Assert(bus.DroppedEventCount > 0, "Should report dropped events");
            Assert(bus.QueuedCount == 0, "Queue should be cleared");
        }

        public void Dispatch_MaxCycles_ClearsOrphanedEvents()
        {
            var bus = new EventBus();
            bus.MaxDispatchCycles = 3;

            bus.Subscribe<GameStateEvent>(e =>
            {
                bus.Publish(new GameStateEvent());
            }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(bus.QueuedCount == 0, "Orphaned events should be cleared");
            Assert(bus.DroppedEventCount > 0, "Should report dropped events");
        }

        public void Dispatch_NormalCompletion_DroppedCountIsZero()
        {
            var bus = new EventBus();
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(bus.DroppedEventCount == 0, "Normal dispatch should drop nothing");
        }

        // === Multiple Subscribers ===

        public void Dispatch_MultipleSubscribers_AllCalled()
        {
            var bus = new EventBus();
            int callCount = 0;

            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.Gameplay);
            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.Presentation);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(callCount == 3, "All 3 subscribers should be called");
        }

        public void Dispatch_DifferentEventTypes_OnlyMatchingSubscribersCalled()
        {
            var bus = new EventBus();
            bool gamestateCalled = false;
            bool gameplayCalled = false;

            bus.Subscribe<GameStateEvent>(e => gamestateCalled = true, EventPriority.GameState);
            bus.Subscribe<GameplayEvent>(e => gameplayCalled = true, EventPriority.Gameplay);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(gamestateCalled, "GameState subscriber should be called");
            Assert(!gameplayCalled, "Gameplay subscriber should NOT be called");
        }

        // === Exception Safety ===

        public void Dispatch_ExceptionInHandler_BusRemainsUsable()
        {
            var bus = new EventBus();
            bool secondHandlerCalled = false;

            bus.Subscribe<GameStateEvent>(e =>
            {
                throw new System.InvalidOperationException("Handler error");
            }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());

            bool threw = false;
            try { bus.Dispatch(); }
            catch (System.InvalidOperationException) { threw = true; }

            Assert(threw, "Exception should propagate");

            bus.Subscribe<GameplayEvent>(e => secondHandlerCalled = true, EventPriority.Gameplay);
            bus.Publish(new GameplayEvent());
            bus.Dispatch();

            Assert(secondHandlerCalled, "Bus should be usable after exception");
        }

        public void Dispatch_ExceptionInHandler_OverflowEventsPreserved()
        {
            var bus = new EventBus();

            bus.Subscribe<GameStateEvent>(e =>
            {
                bus.Publish(new GameplayEvent { Data = "from_overflow" });
                throw new System.InvalidOperationException("Handler error");
            }, EventPriority.GameState);

            bool overflowProcessed = false;
            bus.Subscribe<GameplayEvent>(e => overflowProcessed = true, EventPriority.Gameplay);

            bus.Publish(new GameStateEvent());

            try { bus.Dispatch(); } catch { }

            Assert(bus.QueuedCount > 0, "Overflow events should be in queue");

            bus.Dispatch();
            Assert(overflowProcessed, "Overflow events should dispatch on next call");
        }

        // === Unsubscribe ===

        public void Unsubscribe_PreventsCallback()
        {
            var bus = new EventBus();
            bool called = false;
            var sub = bus.Subscribe<GameStateEvent>(e => called = true, EventPriority.GameState);

            sub.Unsubscribe();

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(!called, "Should not be called after unsubscribe");
        }

        public void Unsubscribe_ViaDispose()
        {
            var bus = new EventBus();
            bool called = false;
            var sub = bus.Subscribe<GameStateEvent>(e => called = true, EventPriority.GameState);

            sub.Dispose();

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(!called, "Should not be called after dispose");
        }

        public void Unsubscribe_DuringDispatch_SafeForCurrentCycle()
        {
            var bus = new EventBus();
            var order = new List<string>();
            EventSubscription subToRemove = null;

            // Both at same priority so registration order determines firing order
            bus.Subscribe<GameStateEvent>(e =>
            {
                order.Add("first");
                subToRemove?.Unsubscribe();
            }, EventPriority.GameState);

            subToRemove = bus.Subscribe<GameStateEvent>(e =>
            {
                order.Add("second");
            }, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            // Handler list was copied, so both fire this cycle
            Assert(order.Count == 2, "Both should fire in current cycle");

            // Next dispatch, removed handler should not fire
            order.Clear();
            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(order.Count == 1, "Only first handler should remain");
        }

        public void UnsubscribeAll_RemovesAllForType()
        {
            var bus = new EventBus();
            int callCount = 0;

            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.Presentation);

            bus.UnsubscribeAll<GameStateEvent>();

            bus.Publish(new GameStateEvent());
            bus.Dispatch();

            Assert(callCount == 0, "No subscribers should remain");
        }

        public void ClearAllSubscriptions_RemovesEverything()
        {
            var bus = new EventBus();
            int callCount = 0;

            bus.Subscribe<GameStateEvent>(e => callCount++, EventPriority.GameState);
            bus.Subscribe<GameplayEvent>(e => callCount++, EventPriority.Gameplay);

            bus.ClearAllSubscriptions();

            bus.Publish(new GameStateEvent());
            bus.Publish(new GameplayEvent());
            bus.Dispatch();

            Assert(callCount == 0, "No subscribers should remain");
        }

        // === Debug Mode ===

        public void DebugMode_WarnsOnMultipleGameStateSubscribers()
        {
            var bus = new EventBus { DebugMode = true };

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState); // Violation!

            Assert(bus.DebugWarnings.Count == 1, "Should have 1 warning");
            Assert(bus.DebugWarnings[0].Contains("CONVENTION VIOLATION"),
                "Warning should mention convention violation");
            Assert(bus.DebugWarnings[0].Contains("GameStateEvent"),
                "Warning should mention the event type");
        }

        public void DebugMode_NoWarningForDifferentPriorities()
        {
            var bus = new EventBus { DebugMode = true };

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.Presentation);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.Audio);

            Assert(bus.DebugWarnings.Count == 0, "No warnings for different priorities");
        }

        public void DebugMode_NoWarningWhenDisabled()
        {
            var bus = new EventBus { DebugMode = false };

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);

            Assert(bus.DebugWarnings.Count == 0, "No warnings when debug mode is off");
        }

        public void DebugMode_WarningsClearedOnDispatch()
        {
            var bus = new EventBus { DebugMode = true };

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            Assert(bus.DebugWarnings.Count == 1, "Should have warning");

            bus.Dispatch(); // Clears warnings

            Assert(bus.DebugWarnings.Count == 0, "Warnings should be cleared after dispatch");
        }

        // === Inspection ===

        public void SubscriberCount_TracksCorrectly()
        {
            var bus = new EventBus();

            Assert(bus.SubscriberCount<GameStateEvent>() == 0, "Should start at 0");

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.Presentation);

            Assert(bus.SubscriberCount<GameStateEvent>() == 2, "Should be 2");
            Assert(bus.SubscriberCount<GameplayEvent>() == 0, "Other type should be 0");
        }

        public void SubscriberCountAtPriority_FiltersCorrectly()
        {
            var bus = new EventBus();

            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.GameState);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.Presentation);
            bus.Subscribe<GameStateEvent>(e => { }, EventPriority.Presentation);

            Assert(bus.SubscriberCountAtPriority<GameStateEvent>(EventPriority.GameState) == 1,
                "Should have 1 GameState subscriber");
            Assert(bus.SubscriberCountAtPriority<GameStateEvent>(EventPriority.Presentation) == 2,
                "Should have 2 Presentation subscribers");
            Assert(bus.SubscriberCountAtPriority<GameStateEvent>(EventPriority.Audio) == 0,
                "Should have 0 Audio subscribers");
        }

        public void PeekQueue_ShowsQueuedEvents()
        {
            var bus = new EventBus();

            bus.Publish(new GameStateEvent { Data = "a" });
            bus.Publish(new GameplayEvent { Data = "b" });

            var queue = bus.PeekQueue();

            Assert(queue.Count == 2, "Should show 2 queued events");
        }

        public void ClearQueue_RemovesWithoutDispatching()
        {
            var bus = new EventBus();
            bool called = false;
            bus.Subscribe<GameStateEvent>(e => called = true, EventPriority.GameState);

            bus.Publish(new GameStateEvent());
            bus.ClearQueue();
            bus.Dispatch();

            Assert(!called, "Should not dispatch cleared events");
            Assert(bus.QueuedCount == 0, "Queue should be empty");
        }

        // === Real Game Scenario ===

        public void Scenario_FormDeathCascade_SubscriberPriorityOrdersCorrectly()
        {
            var bus = new EventBus();
            var order = new List<string>();

            // Multiple systems subscribe to the SAME event at different priorities.
            // Subscriber priority guarantees ordering without needing cascade events.

            // Audio subscriber (registered FIRST but should fire LAST)
            bus.Subscribe<FormDiedEvent>(e =>
            {
                order.Add("audio:demorph_sound");
            }, EventPriority.Audio);

            // Presentation subscriber
            bus.Subscribe<FormDiedEvent>(e =>
            {
                order.Add("presentation:demorph_anim");
            }, EventPriority.Presentation);

            // GameState subscriber (the ONE authoritative state handler)
            bus.Subscribe<FormDiedEvent>(e =>
            {
                order.Add("state:form_died");
                // State handler cascades a demorph event for other systems
                bus.Publish(new RangerDemorphedEvent { RangerId = e.RangerId });
            }, EventPriority.GameState);

            // Gameplay subscriber reacts to the cascaded demorph
            bus.Subscribe<RangerDemorphedEvent>(e =>
            {
                order.Add("gameplay:ai_recalc");
            }, EventPriority.Gameplay);

            bus.Publish(new FormDiedEvent { RangerId = "red", FormId = "tank" });
            bus.Dispatch();

            // Cycle 1: FormDiedEvent dispatches to all subscribers sorted by subscriber priority
            //   GameState fires first (does mutation + publishes RangerDemorphedEvent to overflow)
            //   Presentation fires second (reads final state)
            //   Audio fires third
            // Cycle 2: RangerDemorphedEvent dispatches from overflow
            //   Gameplay handler fires

            Assert(order.Count == 4, $"Should have 4 entries, got {order.Count}");
            Assert(order[0] == "state:form_died", "GameState subscriber fires first");
            Assert(order[1] == "presentation:demorph_anim", "Presentation fires second");
            Assert(order[2] == "audio:demorph_sound", "Audio fires third");
            Assert(order[3] == "gameplay:ai_recalc", "Cascaded event in next cycle");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new EventBusTests();

            // Publish and Queue
            tests.Publish_QueuesEvent_DoesNotDispatch();
            tests.Publish_MultipleEvents_AllQueued();
            tests.Publish_Null_IsIgnored();

            // Dispatch Basics
            tests.Dispatch_CallsSubscriber();
            tests.Dispatch_ClearsQueue();
            tests.Dispatch_ReturnsEventCount();
            tests.Dispatch_EmptyQueue_ReturnsZero();
            tests.Dispatch_NoSubscribers_StillClearsQueue();

            // Event-Level Priority
            tests.Dispatch_EventPriority_GameStateBeforeGameplay();
            tests.Dispatch_EventPriority_AllFourTiers();
            tests.Dispatch_SameEventPriority_PreservesQueueOrder();

            // Subscriber-Level Priority (Path 3)
            tests.Dispatch_SubscriberPriority_GameStateBeforePresentationOnSameEvent();
            tests.Dispatch_SubscriberPriority_SameTier_PreservesRegistrationOrder();
            tests.Dispatch_SubscriberPriority_StateHandlerMutatesBeforePresentationReads();

            // Cascade Prevention
            tests.Dispatch_EventsDuringDispatch_QueuedForNextCycle();
            tests.Dispatch_MultipleCascadeCycles();
            tests.Dispatch_MaxCycleLimit_PreventsInfiniteLoop();
            tests.Dispatch_MaxCycles_ClearsOrphanedEvents();
            tests.Dispatch_NormalCompletion_DroppedCountIsZero();

            // Multiple Subscribers
            tests.Dispatch_MultipleSubscribers_AllCalled();
            tests.Dispatch_DifferentEventTypes_OnlyMatchingSubscribersCalled();

            // Exception Safety
            tests.Dispatch_ExceptionInHandler_BusRemainsUsable();
            tests.Dispatch_ExceptionInHandler_OverflowEventsPreserved();

            // Unsubscribe
            tests.Unsubscribe_PreventsCallback();
            tests.Unsubscribe_ViaDispose();
            tests.Unsubscribe_DuringDispatch_SafeForCurrentCycle();
            tests.UnsubscribeAll_RemovesAllForType();
            tests.ClearAllSubscriptions_RemovesEverything();

            // Debug Mode
            tests.DebugMode_WarnsOnMultipleGameStateSubscribers();
            tests.DebugMode_NoWarningForDifferentPriorities();
            tests.DebugMode_NoWarningWhenDisabled();
            tests.DebugMode_WarningsClearedOnDispatch();

            // Inspection
            tests.SubscriberCount_TracksCorrectly();
            tests.SubscriberCountAtPriority_FiltersCorrectly();
            tests.PeekQueue_ShowsQueuedEvents();
            tests.ClearQueue_RemovesWithoutDispatching();

            // Scenario
            tests.Scenario_FormDeathCascade_SubscriberPriorityOrdersCorrectly();

            System.Console.WriteLine("EventBusTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}

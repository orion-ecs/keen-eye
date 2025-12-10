namespace KeenEyes.Tests;

/// <summary>
/// Tests for the inter-system messaging feature including immediate message sending,
/// subscriptions, message queuing for deferred delivery, and typed message channels.
/// </summary>
public class MessagingTests
{
    #region Send Tests

    [Fact]
    public void Send_DeliversMessageToSubscriber()
    {
        using var world = new World();

        var receivedMessages = new List<DamageMessage>();

        using var subscription = world.Subscribe<DamageMessage>(msg =>
        {
            receivedMessages.Add(msg);
        });

        world.Send(new DamageMessage(42, 25));

        Assert.Single(receivedMessages);
        Assert.Equal(42, receivedMessages[0].TargetId);
        Assert.Equal(25, receivedMessages[0].Amount);
    }

    [Fact]
    public void Send_DeliversToMultipleSubscribers()
    {
        using var world = new World();

        var handler1Count = 0;
        var handler2Count = 0;

        using var sub1 = world.Subscribe<DamageMessage>(_ => handler1Count++);
        using var sub2 = world.Subscribe<DamageMessage>(_ => handler2Count++);

        world.Send(new DamageMessage(1, 10));

        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
    }

    [Fact]
    public void Send_WithNoSubscribers_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw
        world.Send(new DamageMessage(1, 10));
    }

    [Fact]
    public void Send_PreservesMessageData()
    {
        using var world = new World();

        var received = default(CollisionMessage);

        using var subscription = world.Subscribe<CollisionMessage>(msg =>
        {
            received = msg;
        });

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        world.Send(new CollisionMessage(entity1, entity2, 0.5f));

        Assert.Equal(entity1, received.Entity1);
        Assert.Equal(entity2, received.Entity2);
        Assert.Equal(0.5f, received.PenetrationDepth);
    }

    [Fact]
    public void Send_DeliversMultipleMessages()
    {
        using var world = new World();

        var messageCount = 0;

        using var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        world.Send(new DamageMessage(1, 10));
        world.Send(new DamageMessage(2, 20));
        world.Send(new DamageMessage(3, 30));

        Assert.Equal(3, messageCount);
    }

    #endregion

    #region Subscribe Tests

    [Fact]
    public void Subscribe_ReturnsDisposableSubscription()
    {
        using var world = new World();

        var subscription = world.Subscribe<DamageMessage>(_ => { });

        Assert.NotNull(subscription);
        Assert.IsAssignableFrom<IDisposable>(subscription);

        subscription.Dispose();
    }

    [Fact]
    public void Subscribe_ThrowsOnNullHandler()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.Subscribe<DamageMessage>(null!));
    }

    [Fact]
    public void Subscribe_DisposeUnsubscribesHandler()
    {
        using var world = new World();

        var messageCount = 0;

        var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        world.Send(new DamageMessage(1, 10));
        Assert.Equal(1, messageCount);

        subscription.Dispose();

        world.Send(new DamageMessage(2, 20));
        Assert.Equal(1, messageCount); // Should not have increased
    }

    [Fact]
    public void Subscribe_DisposeIsIdempotent()
    {
        using var world = new World();

        var messageCount = 0;

        var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        // Dispose multiple times should not throw
        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        world.Send(new DamageMessage(1, 10));
        Assert.Equal(0, messageCount);
    }

    [Fact]
    public void Subscribe_SameHandlerCanBeRegisteredMultipleTimes()
    {
        using var world = new World();

        var messageCount = 0;
        Action<DamageMessage> handler = _ => messageCount++;

        using var sub1 = world.Subscribe(handler);
        using var sub2 = world.Subscribe(handler);

        world.Send(new DamageMessage(1, 10));

        Assert.Equal(2, messageCount); // Handler should be called twice
    }

    [Fact]
    public void Subscribe_CanUnsubscribeDuringMessageDelivery()
    {
        using var world = new World();

        EventSubscription? subscription = null;
        var messageCount = 0;

        subscription = world.Subscribe<DamageMessage>(_ =>
        {
            messageCount++;
            subscription?.Dispose();
        });

        world.Send(new DamageMessage(1, 10));
        world.Send(new DamageMessage(2, 20));
        world.Send(new DamageMessage(3, 30));

        // Only the first message should have been processed
        Assert.Equal(1, messageCount);
    }

    #endregion

    #region HasMessageSubscribers Tests

    [Fact]
    public void HasMessageSubscribers_ReturnsFalseWhenNoSubscribers()
    {
        using var world = new World();

        Assert.False(world.HasMessageSubscribers<DamageMessage>());
    }

    [Fact]
    public void HasMessageSubscribers_ReturnsTrueWithSubscriber()
    {
        using var world = new World();

        using var subscription = world.Subscribe<DamageMessage>(_ => { });

        Assert.True(world.HasMessageSubscribers<DamageMessage>());
    }

    [Fact]
    public void HasMessageSubscribers_ReturnsFalseAfterUnsubscribe()
    {
        using var world = new World();

        var subscription = world.Subscribe<DamageMessage>(_ => { });

        Assert.True(world.HasMessageSubscribers<DamageMessage>());

        subscription.Dispose();

        Assert.False(world.HasMessageSubscribers<DamageMessage>());
    }

    [Fact]
    public void HasMessageSubscribers_IsPerMessageType()
    {
        using var world = new World();

        using var subscription = world.Subscribe<DamageMessage>(_ => { });

        Assert.True(world.HasMessageSubscribers<DamageMessage>());
        Assert.False(world.HasMessageSubscribers<CollisionMessage>());
    }

    #endregion

    #region GetMessageSubscriberCount Tests

    [Fact]
    public void GetMessageSubscriberCount_ReturnsZeroWithNoSubscribers()
    {
        using var world = new World();

        Assert.Equal(0, world.GetMessageSubscriberCount<DamageMessage>());
    }

    [Fact]
    public void GetMessageSubscriberCount_ReturnsCorrectCount()
    {
        using var world = new World();

        using var sub1 = world.Subscribe<DamageMessage>(_ => { });
        Assert.Equal(1, world.GetMessageSubscriberCount<DamageMessage>());

        using var sub2 = world.Subscribe<DamageMessage>(_ => { });
        Assert.Equal(2, world.GetMessageSubscriberCount<DamageMessage>());

        using var sub3 = world.Subscribe<DamageMessage>(_ => { });
        Assert.Equal(3, world.GetMessageSubscriberCount<DamageMessage>());
    }

    [Fact]
    public void GetMessageSubscriberCount_DecreasesAfterUnsubscribe()
    {
        using var world = new World();

        var sub1 = world.Subscribe<DamageMessage>(_ => { });
        var sub2 = world.Subscribe<DamageMessage>(_ => { });

        Assert.Equal(2, world.GetMessageSubscriberCount<DamageMessage>());

        sub1.Dispose();
        Assert.Equal(1, world.GetMessageSubscriberCount<DamageMessage>());

        sub2.Dispose();
        Assert.Equal(0, world.GetMessageSubscriberCount<DamageMessage>());
    }

    #endregion

    #region QueueMessage Tests

    [Fact]
    public void QueueMessage_DoesNotDeliverImmediately()
    {
        using var world = new World();

        var messageCount = 0;

        using var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        world.QueueMessage(new DamageMessage(1, 10));

        Assert.Equal(0, messageCount); // Should not be delivered yet
    }

    [Fact]
    public void QueueMessage_IncreasesQueuedCount()
    {
        using var world = new World();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());

        world.QueueMessage(new DamageMessage(1, 10));
        Assert.Equal(1, world.GetQueuedMessageCount<DamageMessage>());

        world.QueueMessage(new DamageMessage(2, 20));
        Assert.Equal(2, world.GetQueuedMessageCount<DamageMessage>());
    }

    [Fact]
    public void QueueMessage_PreservesMessageOrder()
    {
        using var world = new World();

        var receivedMessages = new List<DamageMessage>();

        using var subscription = world.Subscribe<DamageMessage>(msg =>
        {
            receivedMessages.Add(msg);
        });

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new DamageMessage(3, 30));

        world.ProcessQueuedMessages();

        Assert.Equal(3, receivedMessages.Count);
        Assert.Equal(1, receivedMessages[0].TargetId);
        Assert.Equal(2, receivedMessages[1].TargetId);
        Assert.Equal(3, receivedMessages[2].TargetId);
    }

    #endregion

    #region ProcessQueuedMessages Tests

    [Fact]
    public void ProcessQueuedMessages_DeliversAllQueuedMessages()
    {
        using var world = new World();

        var receivedMessages = new List<DamageMessage>();

        using var subscription = world.Subscribe<DamageMessage>(msg =>
        {
            receivedMessages.Add(msg);
        });

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new DamageMessage(3, 30));

        Assert.Empty(receivedMessages);

        world.ProcessQueuedMessages();

        Assert.Equal(3, receivedMessages.Count);
    }

    [Fact]
    public void ProcessQueuedMessages_ClearsQueueAfterProcessing()
    {
        using var world = new World();

        using var subscription = world.Subscribe<DamageMessage>(_ => { });

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));

        Assert.Equal(2, world.GetQueuedMessageCount<DamageMessage>());

        world.ProcessQueuedMessages();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
    }

    [Fact]
    public void ProcessQueuedMessages_WithNoQueuedMessages_DoesNotThrow()
    {
        using var world = new World();

        using var subscription = world.Subscribe<DamageMessage>(_ => { });

        // Should not throw
        world.ProcessQueuedMessages();
    }

    [Fact]
    public void ProcessQueuedMessages_WithNoSubscribers_ClearsQueue()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));

        Assert.Equal(2, world.GetQueuedMessageCount<DamageMessage>());

        world.ProcessQueuedMessages();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
    }

    [Fact]
    public void ProcessQueuedMessagesTyped_OnlyProcessesSpecificType()
    {
        using var world = new World();

        var damageCount = 0;
        var collisionCount = 0;

        using var damageSub = world.Subscribe<DamageMessage>(_ => damageCount++);
        using var collisionSub = world.Subscribe<CollisionMessage>(_ => collisionCount++);

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));

        world.ProcessQueuedMessages<DamageMessage>();

        Assert.Equal(2, damageCount);
        Assert.Equal(0, collisionCount); // Collision messages should still be queued
        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
        Assert.Equal(1, world.GetQueuedMessageCount<CollisionMessage>());
    }

    [Fact]
    public void ProcessQueuedMessagesTyped_LeavesOtherTypesQueued()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));
        world.QueueMessage(new SpawnMessage(123));

        world.ProcessQueuedMessages<DamageMessage>();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
        Assert.Equal(1, world.GetQueuedMessageCount<CollisionMessage>());
        Assert.Equal(1, world.GetQueuedMessageCount<SpawnMessage>());
    }

    #endregion

    #region GetQueuedMessageCount Tests

    [Fact]
    public void GetQueuedMessageCount_ReturnsZeroForEmptyQueue()
    {
        using var world = new World();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
    }

    [Fact]
    public void GetQueuedMessageCount_IsPerMessageType()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));

        Assert.Equal(2, world.GetQueuedMessageCount<DamageMessage>());
        Assert.Equal(1, world.GetQueuedMessageCount<CollisionMessage>());
        Assert.Equal(0, world.GetQueuedMessageCount<SpawnMessage>());
    }

    [Fact]
    public void GetTotalQueuedMessageCount_SumsAllTypes()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));
        world.QueueMessage(new SpawnMessage(123));

        Assert.Equal(4, world.GetTotalQueuedMessageCount());
    }

    [Fact]
    public void GetTotalQueuedMessageCount_ReturnsZeroForEmptyQueues()
    {
        using var world = new World();

        Assert.Equal(0, world.GetTotalQueuedMessageCount());
    }

    #endregion

    #region ClearQueuedMessages Tests

    [Fact]
    public void ClearQueuedMessages_ClearsAllQueues()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));

        Assert.Equal(3, world.GetTotalQueuedMessageCount());

        world.ClearQueuedMessages();

        Assert.Equal(0, world.GetTotalQueuedMessageCount());
        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
        Assert.Equal(0, world.GetQueuedMessageCount<CollisionMessage>());
    }

    [Fact]
    public void ClearQueuedMessagesTyped_OnlyClearsSpecificType()
    {
        using var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.1f));

        world.ClearQueuedMessages<DamageMessage>();

        Assert.Equal(0, world.GetQueuedMessageCount<DamageMessage>());
        Assert.Equal(1, world.GetQueuedMessageCount<CollisionMessage>());
    }

    [Fact]
    public void ClearQueuedMessages_DoesNotDeliverMessages()
    {
        using var world = new World();

        var messageCount = 0;

        using var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));

        world.ClearQueuedMessages();

        Assert.Equal(0, messageCount); // Messages should be discarded, not delivered
    }

    #endregion

    #region Multiple Message Types Tests

    [Fact]
    public void Messaging_SupportsMultipleMessageTypes()
    {
        using var world = new World();

        var damageReceived = new List<DamageMessage>();
        var collisionReceived = new List<CollisionMessage>();

        using var damageSub = world.Subscribe<DamageMessage>(msg => damageReceived.Add(msg));
        using var collisionSub = world.Subscribe<CollisionMessage>(msg => collisionReceived.Add(msg));

        world.Send(new DamageMessage(1, 10));
        world.Send(new DamageMessage(2, 20));
        world.Send(new CollisionMessage(Entity.Null, Entity.Null, 0.5f));

        Assert.Equal(2, damageReceived.Count);
        Assert.Single(collisionReceived);
    }

    [Fact]
    public void Messaging_TypesAreIndependent()
    {
        using var world = new World();

        var damageCount = 0;
        var collisionCount = 0;

        using var damageSub = world.Subscribe<DamageMessage>(_ => damageCount++);
        using var collisionSub = world.Subscribe<CollisionMessage>(_ => collisionCount++);

        world.Send(new DamageMessage(1, 10));

        Assert.Equal(1, damageCount);
        Assert.Equal(0, collisionCount);

        world.Send(new CollisionMessage(Entity.Null, Entity.Null, 0.5f));

        Assert.Equal(1, damageCount);
        Assert.Equal(1, collisionCount);
    }

    [Fact]
    public void Messaging_CanMixImmediateAndDeferredDelivery()
    {
        using var world = new World();

        var receivedMessages = new List<string>();

        using var damageSub = world.Subscribe<DamageMessage>(_ => receivedMessages.Add("damage"));
        using var collisionSub = world.Subscribe<CollisionMessage>(_ => receivedMessages.Add("collision"));

        world.Send(new DamageMessage(1, 10)); // Immediate
        world.QueueMessage(new CollisionMessage(Entity.Null, Entity.Null, 0.5f)); // Deferred
        world.Send(new DamageMessage(2, 20)); // Immediate

        Assert.Equal(2, receivedMessages.Count);
        Assert.Equal(["damage", "damage"], receivedMessages);

        world.ProcessQueuedMessages();

        Assert.Equal(3, receivedMessages.Count);
        Assert.Equal("collision", receivedMessages[2]);
    }

    #endregion

    #region Inter-System Communication Scenario Tests

    [Fact]
    public void Messaging_SupportsCombatSystemScenario()
    {
        // Simulates the scenario from the issue: multiple systems communicating via messages
        using var world = new World();

        // Track what each "system" received
        var combatSystemDamage = new List<int>();
        var audioSystemDamage = new List<int>();
        var uiSystemDamage = new List<int>();

        // Subscribe handlers (simulating different systems)
        using var combatSub = world.Subscribe<DamageMessage>(msg =>
            combatSystemDamage.Add(msg.Amount));
        using var audioSub = world.Subscribe<DamageMessage>(msg =>
            audioSystemDamage.Add(msg.Amount));
        using var uiSub = world.Subscribe<DamageMessage>(msg =>
            uiSystemDamage.Add(msg.Amount));

        // Projectile system sends damage message
        world.Send(new DamageMessage(TargetId: 42, Amount: 25));

        // All systems should receive the message
        Assert.Equal([25], combatSystemDamage);
        Assert.Equal([25], audioSystemDamage);
        Assert.Equal([25], uiSystemDamage);
    }

    [Fact]
    public void Messaging_SupportsDeferredProcessingDuringUpdate()
    {
        using var world = new World();

        var processedMessages = new List<DamageMessage>();

        using var subscription = world.Subscribe<DamageMessage>(msg =>
            processedMessages.Add(msg));

        // Simulate game loop: queue messages during "systems update"
        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));
        world.QueueMessage(new DamageMessage(3, 30));

        // No messages processed yet
        Assert.Empty(processedMessages);

        // Process all at end of frame
        world.ProcessQueuedMessages();

        // All messages processed in order
        Assert.Equal(3, processedMessages.Count);
        Assert.Equal(10, processedMessages[0].Amount);
        Assert.Equal(20, processedMessages[1].Amount);
        Assert.Equal(30, processedMessages[2].Amount);
    }

    [Fact]
    public void Messaging_SupportsOptimizationViaHasSubscribers()
    {
        using var world = new World();

        var expensiveOperationCalled = false;

        // Simulate expensive message creation that should be skipped if no subscribers
        void TrySendExpensiveMessage()
        {
            if (world.HasMessageSubscribers<DamageMessage>())
            {
                expensiveOperationCalled = true;
                world.Send(new DamageMessage(1, 10));
            }
        }

        // No subscribers - expensive operation should be skipped
        TrySendExpensiveMessage();
        Assert.False(expensiveOperationCalled);

        // Add subscriber
        using var subscription = world.Subscribe<DamageMessage>(_ => { });

        // Now expensive operation should run
        TrySendExpensiveMessage();
        Assert.True(expensiveOperationCalled);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Messaging_WorksWithRecordStructMessages()
    {
        using var world = new World();

        RecordStructMessage? received = null;

        using var subscription = world.Subscribe<RecordStructMessage>(msg =>
        {
            received = msg;
        });

        world.Send(new RecordStructMessage("test", 42));

        Assert.NotNull(received);
        Assert.Equal("test", received.Value.Name);
        Assert.Equal(42, received.Value.Value);
    }

    [Fact]
    public void Messaging_WorksWithClassMessages()
    {
        using var world = new World();

        ClassMessage? received = null;

        using var subscription = world.Subscribe<ClassMessage>(msg =>
        {
            received = msg;
        });

        var sent = new ClassMessage { Name = "test", Data = [1, 2, 3] };
        world.Send(sent);

        Assert.Same(sent, received); // Should be same reference for classes
    }

    [Fact]
    public void Messaging_EmptyQueuesAfterMultipleProcessCalls()
    {
        using var world = new World();

        var messageCount = 0;

        using var subscription = world.Subscribe<DamageMessage>(_ => messageCount++);

        world.QueueMessage(new DamageMessage(1, 10));
        world.ProcessQueuedMessages();

        Assert.Equal(1, messageCount);

        // Second process should not re-deliver
        world.ProcessQueuedMessages();

        Assert.Equal(1, messageCount);
    }

    [Fact]
    public void Messaging_QueueAndImmediateSendWorkTogether()
    {
        using var world = new World();

        var receivedOrder = new List<int>();

        using var subscription = world.Subscribe<DamageMessage>(msg =>
            receivedOrder.Add(msg.TargetId));

        world.QueueMessage(new DamageMessage(1, 10)); // Queued
        world.Send(new DamageMessage(2, 20)); // Immediate
        world.QueueMessage(new DamageMessage(3, 30)); // Queued
        world.Send(new DamageMessage(4, 40)); // Immediate

        // Only immediate messages delivered so far
        Assert.Equal([2, 4], receivedOrder);

        world.ProcessQueuedMessages();

        // All messages delivered
        Assert.Equal([2, 4, 1, 3], receivedOrder);
    }

    #endregion

    #region World Dispose Tests

    [Fact]
    public void WorldDispose_ClearsAllSubscriptions()
    {
        var world = new World();

        var messageCount = 0;

        world.Subscribe<DamageMessage>(_ => messageCount++);
        world.Subscribe<DamageMessage>(_ => messageCount++);

        Assert.Equal(2, world.GetMessageSubscriberCount<DamageMessage>());

        world.Dispose();

        // Can't verify directly since we can't use world after dispose
        // But ensure no exception during cleanup
    }

    [Fact]
    public void WorldDispose_ClearsQueuedMessages()
    {
        var world = new World();

        world.QueueMessage(new DamageMessage(1, 10));
        world.QueueMessage(new DamageMessage(2, 20));

        world.Dispose();

        // Can't verify directly since we can't use world after dispose
        // But ensure no exception during cleanup
    }

    #endregion

    #region Test Message Types

    private readonly record struct DamageMessage(int TargetId, int Amount);

    private readonly record struct CollisionMessage(Entity Entity1, Entity Entity2, float PenetrationDepth);

    private readonly record struct SpawnMessage(int EntityId);

    private readonly record struct RecordStructMessage(string Name, int Value);

    private sealed class ClassMessage
    {
        public string Name { get; set; } = "";
        public int[] Data { get; set; } = [];
    }

    #endregion
}

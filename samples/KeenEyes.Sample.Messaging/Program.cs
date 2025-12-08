using KeenEyes;
using KeenEyes.Sample.Messaging;

// =============================================================================
// KEEN EYES ECS - Inter-System Messaging Demo
// =============================================================================
// This sample demonstrates the inter-system messaging feature:
// 1. Immediate message delivery with Send<T>()
// 2. Message subscription with Subscribe<T>()
// 3. Deferred message delivery with QueueMessage<T>() and ProcessQueuedMessages()
// 4. Multiple systems responding to the same message type
// 5. Message chaining (death -> score change -> UI update)
// 6. HasMessageSubscribers() optimization pattern
// =============================================================================

Console.WriteLine("KeenEyes ECS - Inter-System Messaging Demo");
Console.WriteLine(new string('=', 60));

using var world = new World();

// =============================================================================
// Register Systems
// =============================================================================

Console.WriteLine("\n[Setup] Registering systems...");

world
    .AddSystem<SpawnerSystem>()      // EarlyUpdate - handles spawn requests
    .AddSystem<CombatSystem>()       // Update - sends damage messages
    .AddSystem<HealthSystem>()       // Update - responds to damage, sends death messages
    .AddSystem<ScoreSystem>()        // Update - responds to death, sends score messages
    .AddSystem<AudioSystem>()        // LateUpdate - plays sounds for damage/death/pickup
    .AddSystem<UISystem>()           // LateUpdate - updates UI for score/death
    .AddSystem<AnalyticsSystem>();   // PostRender - sends analytics if anyone listens

Console.WriteLine("  Systems registered.");

// =============================================================================
// Part 1: Basic Message Send/Subscribe
// =============================================================================

Console.WriteLine("\n[1] Basic Message Send/Subscribe\n");

// Create a player
var player = world.Spawn("Player")
    .WithPosition(0, 0)
    .WithHealth(100, 100)
    .WithPlayer()
    .Build();

Console.WriteLine($"Created player: {player}");

// Create an enemy with an attack targeting the player
var enemy = world.Spawn("Enemy")
    .WithPosition(10, 10)
    .WithHealth(50, 50)
    .With(new Attack { Damage = 25, Range = 15 })
    .With(new Target { Entity = player })
    .WithEnemy()
    .Build();

Console.WriteLine($"Created enemy: {enemy}");

// Run one update - this will trigger:
// 1. CombatSystem sends DamageMessage
// 2. HealthSystem receives it, updates health
// 3. AudioSystem receives it, plays hit sound
Console.WriteLine("\nRunning update (enemy attacks player)...");
world.Update(0.016f);

// Check player health
ref readonly var playerHealth = ref world.Get<Health>(player);
Console.WriteLine($"\nPlayer health after attack: {playerHealth.Current}/{playerHealth.Max}");

// =============================================================================
// Part 2: Message Chaining (Death -> Score -> UI)
// =============================================================================

Console.WriteLine("\n[2] Message Chaining\n");

// Create another enemy that will die
var weakEnemy = world.Spawn("WeakEnemy")
    .WithPosition(5, 5)
    .WithHealth(20, 20)
    .With(new Attack { Damage = 5, Range = 10 })
    .With(new Target { Entity = player })
    .WithEnemy()
    .Build();

// Player attacks weak enemy (manual damage message)
Console.WriteLine("Player attacks weak enemy (sending damage message)...");
world.Send(new DamageMessage(weakEnemy, 25, player));

// This triggers:
// 1. HealthSystem reduces health to 0, marks Dead, sends DeathMessage
// 2. AudioSystem receives DeathMessage, plays death sound
// 3. ScoreSystem receives DeathMessage, updates score, sends ScoreChangedMessage
// 4. UISystem receives ScoreChangedMessage, updates display
// 5. UISystem also receives DeathMessage, shows notification

// =============================================================================
// Part 3: Message Queuing (Deferred Delivery)
// =============================================================================

Console.WriteLine("\n[3] Message Queuing (Deferred Delivery)\n");

// Queue multiple spawn requests
Console.WriteLine("Queueing spawn requests...");
world.QueueMessage(new SpawnEnemyRequest(20, 20, 30));
world.QueueMessage(new SpawnEnemyRequest(25, 25, 40));
world.QueueMessage(new SpawnEnemyRequest(30, 30, 50));

Console.WriteLine($"  Queued message count: {world.GetQueuedMessageCount<SpawnEnemyRequest>()}");

// Messages are NOT delivered yet
Console.WriteLine("\nProcessing queued messages...");
world.ProcessQueuedMessages();

Console.WriteLine($"  Queued message count after processing: {world.GetQueuedMessageCount<SpawnEnemyRequest>()}");

// =============================================================================
// Part 4: Selective Message Processing
// =============================================================================

Console.WriteLine("\n[4] Selective Message Processing\n");

// Queue different message types
world.QueueMessage(new SpawnEnemyRequest(35, 35, 60));
world.QueueMessage(new CollisionMessage(player, enemy));

Console.WriteLine($"  Total queued messages: {world.GetTotalQueuedMessageCount()}");
Console.WriteLine($"  SpawnEnemyRequest count: {world.GetQueuedMessageCount<SpawnEnemyRequest>()}");
Console.WriteLine($"  CollisionMessage count: {world.GetQueuedMessageCount<CollisionMessage>()}");

// Process only spawn requests
Console.WriteLine("\nProcessing only SpawnEnemyRequest messages...");
world.ProcessQueuedMessages<SpawnEnemyRequest>();

Console.WriteLine($"  Remaining total: {world.GetTotalQueuedMessageCount()}");
Console.WriteLine($"  SpawnEnemyRequest count: {world.GetQueuedMessageCount<SpawnEnemyRequest>()}");
Console.WriteLine($"  CollisionMessage count: {world.GetQueuedMessageCount<CollisionMessage>()}");

// Clear remaining messages
world.ClearQueuedMessages();
Console.WriteLine("  Cleared remaining queued messages.");

// =============================================================================
// Part 5: HasMessageSubscribers Optimization
// =============================================================================

Console.WriteLine("\n[5] HasMessageSubscribers Optimization\n");

// Check if anyone is listening to analytics
var hasAnalyticsSubscribers = world.HasMessageSubscribers<AnalyticsMessage>();
Console.WriteLine($"Has analytics subscribers: {hasAnalyticsSubscribers}");

// Subscribe to analytics
Console.WriteLine("Subscribing to analytics...");
var analyticsSub = world.Subscribe<AnalyticsMessage>(msg =>
{
    Console.WriteLine($"  [Analytics] Frame {msg.Frame}: {msg.TotalEntities} entities, {msg.AliveEnemies} alive enemies");
});

hasAnalyticsSubscribers = world.HasMessageSubscribers<AnalyticsMessage>();
Console.WriteLine($"Has analytics subscribers now: {hasAnalyticsSubscribers}");

// Run update - AnalyticsSystem will send data because someone is listening
Console.WriteLine("\nRunning update with analytics subscriber...");
world.Update(0.016f);

// Unsubscribe
analyticsSub.Dispose();
Console.WriteLine("\nUnsubscribed from analytics.");

hasAnalyticsSubscribers = world.HasMessageSubscribers<AnalyticsMessage>();
Console.WriteLine($"Has analytics subscribers after dispose: {hasAnalyticsSubscribers}");

// Run update - AnalyticsSystem will skip expensive work
Console.WriteLine("\nRunning update without analytics subscriber (no output expected)...");
world.Update(0.016f);

// =============================================================================
// Part 6: Subscriber Count and Management
// =============================================================================

Console.WriteLine("\n[6] Subscriber Count and Management\n");

// Get current subscriber counts
Console.WriteLine($"DamageMessage subscribers: {world.GetMessageSubscriberCount<DamageMessage>()}");
Console.WriteLine($"DeathMessage subscribers: {world.GetMessageSubscriberCount<DeathMessage>()}");
Console.WriteLine($"ScoreChangedMessage subscribers: {world.GetMessageSubscriberCount<ScoreChangedMessage>()}");

// Add additional subscribers
Console.WriteLine("\nAdding extra damage subscriber...");
var extraDamageSub = world.Subscribe<DamageMessage>(_ =>
{
    Console.WriteLine("  [Extra] Damage logged!");
});

Console.WriteLine($"DamageMessage subscribers now: {world.GetMessageSubscriberCount<DamageMessage>()}");

// Test with the extra subscriber
Console.WriteLine("\nSending damage message...");
world.Send(new DamageMessage(player, 10, Entity.Null));

// Cleanup extra subscriber
extraDamageSub.Dispose();
Console.WriteLine($"\nDamageMessage subscribers after dispose: {world.GetMessageSubscriberCount<DamageMessage>()}");

// =============================================================================
// Summary
// =============================================================================

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("Inter-System Messaging Summary:");
Console.WriteLine("  - Send<T>(): Immediate delivery to all subscribers");
Console.WriteLine("  - Subscribe<T>(): Register handler, returns IDisposable");
Console.WriteLine("  - QueueMessage<T>(): Deferred delivery");
Console.WriteLine("  - ProcessQueuedMessages(): Deliver all queued messages");
Console.WriteLine("  - ProcessQueuedMessages<T>(): Deliver specific type only");
Console.WriteLine("  - HasMessageSubscribers<T>(): Optimization check");
Console.WriteLine("  - GetMessageSubscriberCount<T>(): Debug/testing");
Console.WriteLine("\nDemo complete!");

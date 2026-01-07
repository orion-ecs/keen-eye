using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Prompts;

/// <summary>
/// MCP prompts for common testing workflows.
/// </summary>
// S1118: MCP SDK requires non-static class for prompts registration, but methods are all static
#pragma warning disable S1118
[McpServerPromptType]
public sealed class WorkflowPrompts
#pragma warning restore S1118
{
    /// <summary>
    /// Prompt to connect to a game and explore its entities.
    /// </summary>
    [McpServerPrompt]
    [Description("Connect to game and explore entities")]
    public static ChatMessage ConnectAndExplore(
        [Description("Named pipe name (optional)")] string? pipeName = null)
    {
        var pipeArg = pipeName != null ? $" with pipe '{pipeName}'" : "";
        return new ChatMessage(ChatRole.User, $"""
            Help me connect to a KeenEyes game{pipeArg} and explore its entities.

            Steps:
            1. Use game_connect to establish connection
            2. Use state_get_entity_count to see how many entities exist
            3. Use state_query_entities to list entities with their components
            4. Summarize what you find
            """);
    }

    /// <summary>
    /// Prompt to guide through testing an input sequence.
    /// </summary>
    [McpServerPrompt]
    [Description("Guide through testing an input sequence")]
    public static ChatMessage TestInputSequence(
        [Description("What input to test")] string description)
    {
        return new ChatMessage(ChatRole.User, $"""
            Help me test this input sequence: {description}

            Steps:
            1. Ensure game is connected
            2. Capture initial screenshot
            3. Execute the input sequence
            4. Wait for game to process (game_wait_for_condition if needed)
            5. Capture result screenshot
            6. Compare and describe what changed
            """);
    }

    /// <summary>
    /// Prompt to capture a screenshot and describe the game scene.
    /// </summary>
    [McpServerPrompt]
    [Description("Capture screenshot and describe the game scene")]
    public static ChatMessage CaptureAndDescribe()
    {
        return new ChatMessage(ChatRole.User, """
            Capture a screenshot of the current game state and describe what you see.

            Include:
            - Overall scene description
            - Notable entities visible
            - UI elements if any
            - Suggestions for what to test next
            """);
    }

    /// <summary>
    /// Prompt to monitor an entity's state changes.
    /// </summary>
    [McpServerPrompt]
    [Description("Watch entity state changes")]
    public static ChatMessage MonitorEntity(
        [Description("Entity ID or name to monitor")] string entityIdOrName)
    {
        return new ChatMessage(ChatRole.User, $"""
            Help me monitor the entity '{entityIdOrName}' for state changes.

            Steps:
            1. Get the entity's current state using state_get_entity or state_get_entity_by_name
            2. Record all component values
            3. Perform some action or wait
            4. Re-query the entity
            5. Report what changed between snapshots
            """);
    }
}

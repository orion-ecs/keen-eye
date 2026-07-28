using System.Runtime.InteropServices;

namespace KeenEyes.Platform.Silk;

/// <summary>
/// Verifies that the OS window is created on the process main thread, which macOS requires.
/// </summary>
/// <remarks>
/// <para>
/// AppKit refuses to instantiate an <c>NSWindow</c> anywhere but the process main thread and
/// raises an Objective-C exception that terminates the process rather than a catchable managed
/// one. GLFW - and therefore Silk.NET - creates that <c>NSWindow</c> inside
/// <c>IWindow.Initialize()</c>, so the check runs immediately before that call.
/// </para>
/// <para>
/// Windows and Linux place no such restriction on window creation, so the guard is a no-op there.
/// </para>
/// </remarks>
internal static partial class WindowThreadGuard
{
    /// <summary>
    /// Throws if the current thread cannot legally create an OS window on this platform.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown on macOS when the calling thread is not the process main thread.
    /// </exception>
    internal static void EnsureWindowCreationThread()
    {
        // The P/Invoke below resolves a macOS-only library, so it must stay behind this check.
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        Validate(isMacOS: true, isProcessMainThread: PthreadMainNp() != 0);
    }

    /// <summary>
    /// Applies the guard's decision to already-observed platform and thread facts.
    /// </summary>
    /// <param name="isMacOS">Whether the process is running on macOS.</param>
    /// <param name="isProcessMainThread">Whether the calling thread is the process main thread.</param>
    /// <remarks>
    /// Split out from <see cref="EnsureWindowCreationThread"/> so the decision is testable on
    /// platforms where neither input can be reproduced.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="isMacOS"/> is <see langword="true"/> and
    /// <paramref name="isProcessMainThread"/> is <see langword="false"/>.
    /// </exception>
    internal static void Validate(bool isMacOS, bool isProcessMainThread)
    {
        if (!isMacOS || isProcessMainThread)
        {
            return;
        }

        throw new InvalidOperationException(
            "macOS requires the OS window to be created on the process main thread, and this call "
            + $"is running on managed thread {Environment.CurrentManagedThreadId}, which is not it. "
            + "The most likely cause is an 'await' somewhere before Run(): an async Main resumes its "
            + "continuation on a thread-pool thread, so Run() - and the window creation inside it - "
            + "no longer runs where the process started. The remedy is to do that startup work "
            + "synchronously before Run() (for example 'StartAsync().GetAwaiter().GetResult()'), or "
            + "to move it after Run() returns. Windows and Linux accept window creation on any "
            + "thread, so a program that works there can still fail here.");
    }

    /// <summary>
    /// Returns non-zero when the calling thread is the process main thread.
    /// </summary>
    /// <remarks>
    /// Declared by <c>&lt;pthread.h&gt;</c> and exported from libSystem on macOS. This is the
    /// definitive answer AppKit itself relies on; managed thread identity cannot supply it.
    /// </remarks>
    [LibraryImport("libSystem.B.dylib", EntryPoint = "pthread_main_np")]
    private static partial int PthreadMainNp();
}

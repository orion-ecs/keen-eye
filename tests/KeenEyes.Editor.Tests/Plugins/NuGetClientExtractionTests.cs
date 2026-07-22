// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.IO.Compression;
using KeenEyes.Editor.Plugins.NuGet;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for <see cref="NuGetClient.ExtractArchiveEntries"/>, covering normal package
/// extraction and rejection of zip-slip (path traversal) archive entries.
/// </summary>
public sealed class NuGetClientExtractionTests : IDisposable
{
    private readonly DirectoryInfo tempRoot;

    public NuGetClientExtractionTests()
    {
        tempRoot = Directory.CreateTempSubdirectory("keeneyes-zipslip-tests-");
    }

    public void Dispose()
    {
        tempRoot.Delete(recursive: true);
    }

    [Fact]
    public void ExtractArchiveEntries_WithNormalEntries_ExtractsAllFiles()
    {
        var destination = Path.Combine(tempRoot.FullName, "extract");
        Directory.CreateDirectory(destination);

        using var archive = CreateArchive(
            ("MyPlugin.nuspec", "<package />"),
            ("lib/net10.0/MyPlugin.dll", "binary-content"));

        NuGetClient.ExtractArchiveEntries(archive, destination, TestContext.Current.CancellationToken);

        Assert.Equal("<package />", File.ReadAllText(Path.Combine(destination, "MyPlugin.nuspec")));
        Assert.Equal(
            "binary-content",
            File.ReadAllText(Path.Combine(destination, "lib", "net10.0", "MyPlugin.dll")));
    }

    [Fact]
    public void ExtractArchiveEntries_WithParentTraversalEntry_ThrowsInvalidDataException()
    {
        var destination = Path.Combine(tempRoot.FullName, "extract");
        Directory.CreateDirectory(destination);

        using var archive = CreateArchive(("../evil.dll", "malicious"));

        Assert.Throws<InvalidDataException>(
            () => NuGetClient.ExtractArchiveEntries(archive, destination, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(tempRoot.FullName, "evil.dll")));
    }

    [Fact]
    public void ExtractArchiveEntries_WithNestedTraversalEntry_ThrowsInvalidDataException()
    {
        var destination = Path.Combine(tempRoot.FullName, "extract");
        Directory.CreateDirectory(destination);

        using var archive = CreateArchive(("lib/../../evil.dll", "malicious"));

        Assert.Throws<InvalidDataException>(
            () => NuGetClient.ExtractArchiveEntries(archive, destination, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(tempRoot.FullName, "evil.dll")));
    }

    [Fact]
    public void ExtractArchiveEntries_WithRootedEntry_ThrowsInvalidDataException()
    {
        var destination = Path.Combine(tempRoot.FullName, "extract");
        Directory.CreateDirectory(destination);

        var rootedPath = Path.Combine(tempRoot.FullName, "outside", "evil.dll");
        using var archive = CreateArchive((rootedPath, "malicious"));

        Assert.Throws<InvalidDataException>(
            () => NuGetClient.ExtractArchiveEntries(archive, destination, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(rootedPath));
    }

    [Fact]
    public void ExtractArchiveEntries_WithSiblingPrefixEscape_ThrowsInvalidDataException()
    {
        // "../extract-evil/..." resolves to a sibling directory whose name starts with
        // the destination directory's name; a naive prefix check would let it through.
        var destination = Path.Combine(tempRoot.FullName, "extract");
        Directory.CreateDirectory(destination);

        using var archive = CreateArchive(("../extract-evil/evil.dll", "malicious"));

        Assert.Throws<InvalidDataException>(
            () => NuGetClient.ExtractArchiveEntries(archive, destination, TestContext.Current.CancellationToken));
        Assert.False(Directory.Exists(Path.Combine(tempRoot.FullName, "extract-evil")));
    }

    private static ZipArchive CreateArchive(params (string EntryName, string Content)[] entries)
    {
        var stream = new MemoryStream();

        using (var writer = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = writer.CreateEntry(entryName);
                using var entryStream = entry.Open();
                using var textWriter = new StreamWriter(entryStream);
                textWriter.Write(content);
            }
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }
}

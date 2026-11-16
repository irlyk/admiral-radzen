namespace Admiral.TestRunner.Tests.Fixtures;

internal sealed class TemporaryRepository : IDisposable
{
    public RepositoryPaths Paths { get; } = new(
        Path.Combine(Path.GetTempPath(), $"admiral-runner-tests-{Guid.NewGuid():N}"));

    public TemporaryRepository()
    {
        Directory.CreateDirectory(Paths.TestDirectory);
        Directory.CreateDirectory(Path.Combine(Paths.Root, "Radzen.AdmiralComponents"));
        Directory.CreateDirectory(Path.Combine(Paths.Root, "samples/AdmiralConsumerSmoke/Pages"));
    }

    public void Write(string relativePath, string text)
    {
        var path = Path.Combine(Paths.Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    public void Dispose() => Directory.Delete(Paths.Root, recursive: true);
}

namespace CommentSense.TestHelpers;

public static class ProjectLayout
{
    private static string? _repositoryRoot;

    public static string RepositoryRoot => _repositoryRoot ??= FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "CommentSense.slnx")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("Could not find repository root (CommentSense.slnx).");
    }
}

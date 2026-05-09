namespace Axe2DEditor.Core.Projects;

public sealed class ProjectContext
{
    public ProjectContext(AxeProject project, string rootDirectory)
    {
        Project = project;
        RootDirectory = rootDirectory;
        ProjectFilePath = Path.Combine(rootDirectory, ProjectService.ProjectFileName);
    }

    public AxeProject Project { get; }

    public string RootDirectory { get; }

    public string ProjectFilePath { get; }
}

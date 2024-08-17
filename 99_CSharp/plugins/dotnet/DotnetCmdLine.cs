using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelExperiments.Helper;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.plugins.dotnet;

public class DotnetCommandExecutorConfig
{
    public string WorkingDirectory { get; set; }
}

[Description("Wraps dotnet command line utility to allow creating and manipulating projects")]
internal class DotnetCommandExecutor
{
    private const string DotnetCommand = "dotnet";
    private readonly ProcessHelper _processHelper;
    private readonly ILogger<DotnetCommandExecutor> _logger;

    public DotnetCommandExecutor(DotnetCommandExecutorConfig config, ILogger<DotnetCommandExecutor> logger)
    {
        _processHelper = new ProcessHelper(config.WorkingDirectory);
        if (!Directory.Exists(config.WorkingDirectory))
        {
            Directory.CreateDirectory(config.WorkingDirectory);
        }
        _logger = logger;
    }

    [KernelFunction("create_new_console_app")]
    [Description("Create a new console application.")]
    [return: Description("Result of creating a new console application.")]
    public async Task<ProcessResult> CreateNewConsoleApp(
        [Description("Name of the new project.")] string projectName,
        [Description("Output directory for the new project.")] string outputDirectory = null)
    {
        string arguments = $"new console -n {projectName}";
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            arguments += $" -o {outputDirectory}";
        }
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("create_new_class_library")]
    [Description("Create a new class library.")]
    [return: Description("Result of creating a new class library.")]
    public async Task<ProcessResult> CreateNewClassLibrary(
        [Description("Name of the new project.")] string projectName)
    {
        // By default we will create a project in a folder with the same name as the project
        string arguments = $"new classlib -n {projectName}";
        arguments += $" -o {projectName}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("build_project")]
    [Description("Build a dotnet project.")]
    [return: Description("Result of building the project.")]
    public async Task<ProcessResult> BuildProject(
        [Description("Path to the project to be built.")] string projectPath)
    {
        string arguments = $"build {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("run_project")]
    [Description("Run a dotnet project.")]
    [return: Description("Result of running the project.")]
    public async Task<ProcessResult> RunProject(
        [Description("Path to the project to be run.")] string projectPath)
    {
        string arguments = $"run --project {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("test_project")]
    [Description("Test a dotnet project.")]
    [return: Description("Result of testing the project.")]
    public async Task<ProcessResult> TestProject(
        [Description("Path to the project to be tested.")] string projectPath)
    {
        string arguments = $"test {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("publish_project")]
    [Description("Publish a dotnet project.")]
    [return: Description("Result of publishing the project.")]
    public async Task<ProcessResult> PublishProject(
        [Description("Path to the project to be published.")] string projectPath,
        [Description("Output directory for the published project.")] string outputDirectory)
    {
        string arguments = $"publish {projectPath} -o {outputDirectory}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("add_package")]
    [Description("Add a NuGet package to a project.")]
    [return: Description("Result of adding the NuGet package.")]
    public async Task<ProcessResult> AddPackage(
        [Description("Path to the project to add the package to.")] string projectPath,
        [Description("Name of the NuGet package to add.")] string packageName)
    {
        string arguments = $"add {projectPath} package {packageName}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("list_packages")]
    [Description("List all NuGet packages of a project.")]
    [return: Description("Result of listing the NuGet packages.")]
    public async Task<ProcessResult> ListPackages(
        [Description("Path to the project to list packages for.")] string projectPath)
    {
        string arguments = $"list {projectPath} package";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("clean_project")]
    [Description("Clean a dotnet project.")]
    [return: Description("Result of cleaning the project.")]
    public async Task<ProcessResult> CleanProject(
        [Description("Path to the project to be cleaned.")] string projectPath)
    {
        string arguments = $"clean {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("restore_project")]
    [Description("Restore NuGet packages for a project.")]
    [return: Description("Result of restoring the NuGet packages.")]
    public async Task<ProcessResult> RestoreProject(
        [Description("Path to the project to restore packages for.")] string projectPath)
    {
        string arguments = $"restore {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("create_solution")]
    [Description("Create a new solution.")]
    [return: Description("Result of creating the solution.")]
    public async Task<ProcessResult> CreateSolution(
        [Description("Name of the new solution.")] string solutionName,
        [Description("Output directory for the new solution.")] string outputDirectory = null)
    {
        string arguments = $"new sln -n {solutionName}";
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            arguments += $" -o {outputDirectory}";
        }
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("add_project_to_solution")]
    [Description("Add a project to a solution.")]
    [return: Description("Result of adding the project to the solution.")]
    public async Task<ProcessResult> AddProjectToSolution(
        [Description("Path to the solution file.")] string solutionPath,
        [Description("Path to the project to add to the solution.")] string projectPath)
    {
        string arguments = $"sln {solutionPath} add {projectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("add_nuget_reference")]
    [Description("Add a NuGet reference to a project.")]
    [return: Description("Result of adding the NuGet reference.")]
    public async Task<ProcessResult> AddNuGetReference(
        [Description("Path to the project to add the NuGet reference to.")] string projectPath,
        [Description("Name of the NuGet package to add.")] string packageName)
    {
        string arguments = $"add {projectPath} package {packageName}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("add_project_reference")]
    [Description("Add a project reference to another project.")]
    [return: Description("Result of adding the project reference.")]
    public async Task<ProcessResult> AddProjectReference(
        [Description("Path to the project to add the reference to.")] string projectPath,
        [Description("Path to the project to be referenced.")] string referencedProjectPath)
    {
        string arguments = $"add {projectPath} reference {referencedProjectPath}";
        _logger.LogTrace("Executing command: {Command} {Arguments}", DotnetCommand, arguments);
        return await _processHelper.InvokeProcessAsync(DotnetCommand, arguments);
    }

    [KernelFunction("create_solution_structure")]
    [Description("Create an entire solution structure.")]
    [return: Description("nothing interesting.")]
    public async Task<string> CreateSolutionStructure(
       [Description("Name of the solution")] string solutionName,
       [Description("List of the projects that are part of the solution.")] ProjectInfo[] projects)
    {
        await CreateSolution(solutionName);
        var realSolutionName = $"{solutionName}.sln";
        foreach (var project in projects)
        {
            await CreateNewClassLibrary(project.ProjectName);
            await AddProjectToSolution(realSolutionName, project.ProjectName);
            if (project.NugetPackages != null)
            {
                foreach (var nugetPackage in project.NugetPackages)
                {
                    await AddPackage(project.ProjectName, nugetPackage);
                }
            }
        }

        return "created";
    }
}

[Description("Information about a project.")]
public class ProjectInfo 
{
    [Description("Name of the project")]
    public string ProjectName { get; set; }

    [Description("List of all nuget packages we need to install with the project.")]
    public string[] NugetPackages { get; set; }
}

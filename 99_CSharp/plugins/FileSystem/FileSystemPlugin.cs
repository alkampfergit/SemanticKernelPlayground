using System;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SemanticKernelExperiments.plugins.FileSystem;
public class FileSystemPluginConfig
{
    public string BaseDirectory { get; set; }
}

[Description("Plugin to manipulate files in the file system.")]
public class FileSystemPlugin
{
    private readonly FileSystemPluginConfig _config;

    public FileSystemPlugin(FileSystemPluginConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    [KernelFunction("add_file")]
    [Description("Add a new file with content.")]
    [return: Description("Result of adding the file.")]
    public async Task<string> AddFile(
        [Description("Relative path of the new file.")] string relativePath,
        [Description("Content to write to the file.")] string content)
    {
        string fullPath = Path.Combine(_config.BaseDirectory, relativePath);
        await File.WriteAllTextAsync(fullPath, content);
        return $"File created at {fullPath}";
    }

    [KernelFunction("read_file")]
    [Description("Read content from a file.")]
    [return: Description("Content of the file.")]
    public async Task<string> ReadFile(
        [Description("Relative path of the file to read.")] string relativePath)
    {
        string fullPath = Path.Combine(_config.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at {fullPath}");
        }
        return await File.ReadAllTextAsync(fullPath);
    }

    [KernelFunction("append_to_file")]
    [Description("Append content to an existing file.")]
    [return: Description("Result of appending to the file.")]
    public async Task<string> AppendToFile(
        [Description("Relative path of the file to append to.")] string relativePath,
        [Description("Content to append to the file.")] string content)
    {
        string fullPath = Path.Combine(_config.BaseDirectory, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at {fullPath}");
        }
        await File.AppendAllTextAsync(fullPath, content);
        return $"Content appended to file at {fullPath}";
    }
}
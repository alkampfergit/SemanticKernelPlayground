using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.SemanticKernel;

namespace SemanticKernel.Orchestration.SamplePlugins;

[Description("Helps building a workflow adding blocks")]
public class WorkflowBuilderPlugin
{
    private readonly Dictionary<string, (string PreviousBlockId, Dictionary<string, object> Parameters)> _blocks = new ();

    /// <summary>
    /// Adds a new block to the workflow.
    /// </summary>
    /// <param name="blockId">Unique identifier for the block</param>
    /// <param name="previousBlockId">ID of the previous block. Use null for first block or to append to the end</param>
    /// <param name="parameters">Dictionary of parameters for the block</param>
    /// <returns>True if block was added successfully, false if blockId already exists</returns>
    [KernelFunction]
    [Description("Add a new block to the workflow with specified parameters")]
    public bool AddBlock(
        [Description("Unique identifier for the block")] string blockId,
        [Description("ID of the previous block (null for first block or to append)")] string? previousBlockId,
        [Description("Dictionary of parameters for the block")] Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(blockId) || _blocks.ContainsKey(blockId))
            return false;

        _blocks[blockId] = (previousBlockId ?? string.Empty, parameters);
        return true;
    }

    public string DumpMermaidDiagram()
    {
        var sb = new StringBuilder();
        sb.AppendLine("flowchart TD");
        
        foreach (var block in _blocks)
        {
            var blockId = block.Key;
            var (previousBlockId, parameters) = block.Value;
            
            // Add node definition
            sb.AppendLine($"    {blockId}[{blockId}]");
            
            // Add connection if there's a previous block
            if (!string.IsNullOrEmpty(previousBlockId))
            {
                sb.AppendLine($"    {previousBlockId} --> {blockId}");
            }
            
            // Add parameters as notes
            if (parameters.Count > 0)
            {
                var paramList = string.Join("<br/>", parameters.Select(p => $"{p.Key}: {p.Value}"));
                sb.AppendLine($"    {blockId}:::withParams");
                sb.AppendLine($"    {blockId}-. params .-> {blockId}_params([{paramList}])");
            }
        }
        
        return sb.ToString();
    }
}

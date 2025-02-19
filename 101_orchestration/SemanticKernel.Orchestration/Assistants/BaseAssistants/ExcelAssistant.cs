using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Orchestrators;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace SemanticKernel.Orchestration.Assistants.BaseAssistants;

class ExcelAssistant : BaseAssistant
{
    private readonly KernelStore _kernelStore;

    public ExcelAssistant(
        KernelStore kernelStore
    ) : base("ExcelAssistant")
    {
        RegisterFunctionDelegate(
            "ExportDataset",
            KernelFunctionFactory.CreateFromMethod(ExportDataset),
            async (args) => await ExportDataset(),
            isFinal: false);
        _kernelStore = kernelStore;
    }

    [Description("Export a dataset to an excel file")]
    private async Task<AssistantResponse> ExportDataset()
    {
        var datasets = KernelStore.GetAllPropertyValues<DataSet>();
        if (datasets.Count == 0)
        {
            return new AssistantResponse("No dataset found in the current conversation");
        }
        using var package = new ExcelPackage();

        var tempFilePath = Path.Combine(Path.GetTempPath(), "exported_dataset.xlsx");
        foreach (var dataset in datasets)
        {
            var prefix = dataset.Key;
            foreach (DataTable table in dataset.Value.Tables)
            {
                var worksheetName = $"{prefix}_{table.TableName}";
                var worksheet = package.Workbook.Worksheets.Add(worksheetName);
                worksheet.Cells["A1"].LoadFromDataTable(table, true);
            }
        }

        await package.SaveAsAsync(new FileInfo(tempFilePath));

        Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
        return new AssistantResponse($"Dataset exported successfully in file {tempFilePath}");

    }
}

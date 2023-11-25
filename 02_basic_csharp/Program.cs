using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;

namespace _02_basic_csharp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // var av = new AudioVideoPlugin();
            // av.ExtractAudio("/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4");

            // var python = new PythonWrapper("/Users/gianmariaricci/develop/github/SemanticKernelPlayground/skernel/bin/python3");
            // var script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "transcript_timeline.py");
            // var result = python.Execute(script, "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.wav");
            // Console.WriteLine(result);

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddConsole();
            });

            IKernel kernel = new KernelBuilder()
                .WithAzureOpenAIChatCompletionService( 
                    "GPT42",
                    "https://alkopenai2.openai.azure.com",
                    Environment.GetEnvironmentVariable("AI_KEY")
                )
                // Add a text or chat completion service using either:
                // .WithAzureTextCompletionService()
                // .WithAzureChatCompletionService()
                // .WithOpenAITextCompletionService()
                // .WithOpenAIChatCompletionService()
                .WithLoggerFactory(loggerFactory)
                .Build();

            var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");

            // Import the semantic functions
            var publishingPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "PublishingPlugin");
            var concreteAvPlugin = new _02_basic_csharp.AudioVideoPlugin.AudioVideoPlugin();
            var audioVideoPlugin = kernel.ImportFunctions(concreteAvPlugin, "AudioVideoPlugin");
            
            var result = await kernel.RunAsync(
                "/Users/gianmariaricci/develop/montaggi/UpdatingSSH.mp4",
                audioVideoPlugin["ExtractAudio"],
                audioVideoPlugin["TranscriptTimeline"],
                publishingPlugin["VideoTimelineCreator"]);

            Console.WriteLine(result.GetValue<string>());
        }
    }
}
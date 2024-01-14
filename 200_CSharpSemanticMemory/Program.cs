using Microsoft.Extensions.DependencyInjection;
using SemanticMemory.Helper.Pipeline;
using SemanticMemory.Samples;
using System;
using System.Threading.Tasks;

namespace LogIntercepting;

public static class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddSingleton<BookSample>();
        services.AddSingleton<SBertSample>();
        services.AddSingleton<BasicSample>();
        services.AddSingleton<TextCleanerHandler>();
        services.AddHttpClient();

        var serviceProvider = services.BuildServiceProvider();

        //now begin a loop that you can use to ask which sample to run
        ConsoleKeyInfo key;
        do
        {
            Console.WriteLine("Which sample do you want to run? \n1 - book\n2 - Custom Pipeline\n3 - Bert\nx - exit");
            key = Console.ReadKey();

            switch (key.KeyChar)
            {
                case '1':
                    await serviceProvider.GetRequiredService<BasicSample>().RunSample(@"c:\temp\advancedapisecurity.pdf");
                    break;

                case '2':
                    await serviceProvider.GetRequiredService<BookSample>().RunSample(@"c:\temp\advancedapisecurity.pdf");
                    break;

                case '3':
                    await serviceProvider.GetRequiredService<SBertSample>().RunSample(@"S:\OneDrive\B19553_11.pdf");
                    break;
            }

            //clear the console output
            Console.Clear();
        } while (key.KeyChar != 'x');
    }
}
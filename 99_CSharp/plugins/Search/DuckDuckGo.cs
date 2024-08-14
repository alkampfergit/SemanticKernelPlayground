using HtmlAgilityPack;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExperiments.plugins.Search;

[Description("Simple plugin to interact with Duck Duck Go")]
internal class DuckDuckGo
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DuckDuckGo(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [KernelFunction("search")]
    [Description("Search information in the internet")]
    [return: Description("A list of urls that contains keywords")]
    public async Task<List<string>> Evaluate([Description("keywords to search")] string keywords)
    {
        Console.WriteLine("Searching DDG: {0}", keywords);
        var client = _httpClientFactory.CreateClient();

        var response = await client.GetAsync($"https://html.duckduckgo.com/html/?q={keywords}");
        var content = await response.Content.ReadAsStringAsync();

        //now use html agility pack to load html and extract urls
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var result = new List<string>();
        //now all the links are anchor contained into nodes with class result_title
        var titlenodes = doc.DocumentNode.SelectNodes("//h2[@class='result__title']");
        foreach (var h2Node in titlenodes)
        {
            // Find all 'a' nodes within the 'h2' node
            var aNodes = h2Node.SelectNodes(".//a[@href]");

            if (aNodes != null)
            {
                foreach (var aNode in aNodes)
                {
                    // Extract the href attribute
                    string href = aNode.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        result.Add(href);
                    }
                }
            }
        }
        return result.Take(5).ToList();
    }
}

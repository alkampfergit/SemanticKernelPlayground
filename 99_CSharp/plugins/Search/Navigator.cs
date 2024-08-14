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

[Description("Simple plugin to navigate the web and extract text")]
internal class Navigator
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Navigator(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [KernelFunction("downloadtext")]
    [Description("Download a page and extract the text based on a question")]
    [return: Description("The text that should contains the answer of the question")]
    public async Task<string> DownloadText(
        [Description("Url to extract the text to solve the question")] string url,
        [Description("Question that you need to solve, it can be null.")] string question = null)
    {
        Console.WriteLine("Extracting text from: {0}", url);
        var client = _httpClientFactory.CreateClient();

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        //now use html agility pack to load html and extract urls
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var text = doc.DocumentNode.InnerText;
        //now we need to clean text, removing all the text that is small and surrounded by spaces
        var lines = text.Split('\n');
        var result = new StringBuilder(text.Length);
        foreach (var line in lines)
        {
            //remove all double spaces and trim
            var cleanedLine = Clean(line);
            if (cleanedLine.Length > 10)
            {
                result.AppendLine(cleanedLine);
            }
        }

        return result.ToString();
    }

    private string Clean(string line)
    {
        StringBuilder sb = new StringBuilder(line.Length);
        bool lastIsSpace = false;
        foreach (var c in line)
        {
            if (char.IsSeparator(c) || c == '\t' || c == '\n')
            {
                if (lastIsSpace)
                {
                    //consecutive spaces
                    continue;
                }
                lastIsSpace = true;
            }
            else 
            {
                lastIsSpace = false;
            }

            if (!char.IsLetterOrDigit(c) && !char.IsSeparator(c))
            {
                //remove all non letter or digit characters
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}

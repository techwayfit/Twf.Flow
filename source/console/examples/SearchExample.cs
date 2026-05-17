using Twf.Flow.Core;
using Twf.Flow.Nodes.IO;

public static class SearchExample
{
   

    public static async Task RunAsync(string apiKey, string searchQuery)
    {
        Workflow workflow= Workflow.Create("Search Engine");
       // string apiKey="31cc9b23c8dd747dd978b91fa5a856b8489b8c7f5bbaed6d49d04bd39d1db969";
        workflow.AddNode(new GoogleSearchNode(apiKey)); 
        WorkflowData input = new WorkflowData().Set("search_query", searchQuery);
        input.Set("search_results_count", 3);
        var result = await workflow.RunAsync(input);
        var searchResults = result.Data.Get<List<SearchResultItem>>("search_results")?? new List<SearchResultItem>();
        Console.WriteLine($"Search results for: {searchQuery}");
        foreach (var item in searchResults)        {
            Console.WriteLine($"- {item.Title}: {item.LinkedPage}");
        }

    }
}
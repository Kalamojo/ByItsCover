using AngleSharp;
using ListopiaParser.Configs;
using Microsoft.Extensions.Options;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ListopiaParser.Services;

public class ListopiaService
{
    private readonly HttpClient _client;
    private readonly ListopiaOptions _options;
    private readonly IBrowsingContext _context;

    public ListopiaService(HttpClient client, IOptions<ListopiaOptions> options)
    {
        _client = client;
        _options = options.Value;
        
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }
    
    public async Task<string[]> GetListopiaIsbns(int pageNumber, CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting listopia parse page " + pageNumber);
        var request = new HttpRequestMessage(HttpMethod.Get, ToAbsolute(_options.ListopiaURL, $"?page={pageNumber}"));
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var document = await _context.OpenAsync(req => req.Content(htmlContent), cancellationToken);
        var bookTitleElements = document.QuerySelectorAll("#all_votes tr a.bookTitle");
        var bookUrls = bookTitleElements.Select(x => ToAbsolute(_options.GoodreadsBase, x.GetAttribute("href"))).ToList();

        var isbnArray = await Task.WhenAll(bookUrls.Select(x => GetBookIsbn(x, cancellationToken)));
        return isbnArray;
    }

    private async Task<string> GetBookIsbn(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var document = await _context.OpenAsync(req => req.Content(htmlContent), cancellationToken);
        var scriptElement = document.QuerySelector("script#__NEXT_DATA__");

        if (scriptElement == null)
        {
            throw new ArgumentNullException(nameof(scriptElement), "Book page does not have script data to parse");
        }
        
        // var jsonData = (JObject?)JsonConvert.DeserializeObject(scriptElement.TextContent);
        // var isbn = jsonData?.Descendants().OfType<JObject>()
        //     .First(x => (string?)x["__typename"] == "BookDetails")["isbn13"];

        var jsonData = JsonDocument.Parse(scriptElement.TextContent);
        var temp = jsonData.RootElement.EnumerateObject();
        foreach (var jsonProperty in temp)
        {
            Console.WriteLine($"{jsonProperty.Name}: {jsonProperty.Value}");
        }

        var isbn = "";
        
        if (isbn == null)
        {
            throw new ArgumentNullException(nameof(isbn), "ISBN-13 was not found");
        }
        
        return isbn.ToString();
    }
    
    private static string ToAbsolute(string startingUrl, string? relativeUrl)
    {
        return new Uri(new Uri(startingUrl), relativeUrl).AbsoluteUri;
    }
}
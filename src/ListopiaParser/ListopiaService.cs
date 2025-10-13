using AngleSharp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ListopiaParser;

public class ListopiaService
{
    private readonly HttpClient _client;
    private readonly ListopiaOptions _options;
    private readonly IBrowsingContext _context;
    private const int PageSwitchDelay = 1000;

    public ListopiaService(HttpClient client, IOptions<ListopiaOptions> options)
    {
        _client = client;
        _options = options.Value;
        
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }

    public async Task<List<string>> GetIsbns(int pages, CancellationToken cancellationToken)
    {
        var isbnList = new List<string>();
        for (var i = 0; i < pages; i++)
        {
            var isbns = await GetListPageIsbns(i+1, cancellationToken);
            isbnList.AddRange(isbns);
            await Task.Delay(PageSwitchDelay, cancellationToken);
            Console.WriteLine("At page: " + (i+1));
        }
        return isbnList;
    }
    
    private async Task<string[]> GetListPageIsbns(int pageNumber, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ToAbsolute(_options.ListopiaURL, $"?page={pageNumber}"));
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var document = await _context.OpenAsync(req => req.Content(htmlContent), cancellationToken);
        var bookTitleElements = document.QuerySelectorAll("#all_votes tr a.bookTitle");
        var bookUrls = bookTitleElements.Select(x => ToAbsolute(_options.GoodreadsBase, x.GetAttribute("href"))).ToList();

        var isbnList = await Task.WhenAll(bookUrls.Select(x => GetBookIsbn(x, cancellationToken)));
        return isbnList;
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
        
        var jsonData = (JObject?)JsonConvert.DeserializeObject(scriptElement.TextContent);
        var isbn = jsonData?.Descendants().OfType<JObject>()
            .First(x => (string?)x["__typename"] == "BookDetails")["isbn13"]?
            .ToString();
        return isbn ?? string.Empty;
    }
    
    private static string ToAbsolute(string startingUrl, string? relativeUrl)
    {
        return new Uri(new Uri(startingUrl), relativeUrl).AbsoluteUri;
    }
}
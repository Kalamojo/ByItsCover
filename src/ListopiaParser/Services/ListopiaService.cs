using AngleSharp;
using ListopiaParser.Configs;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
    
    public async Task<List<string>> GetListopiaIsbns(int pageNumber, CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting listopia parse page " + pageNumber);
        var request = new HttpRequestMessage(HttpMethod.Get, ToAbsolute(_options.ListopiaURL, $"?page={pageNumber}"));
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var document = await _context.OpenAsync(req => req.Content(htmlContent), cancellationToken);
        var bookTitleElements = document.QuerySelectorAll("#all_votes tr a.bookTitle");
        var bookUrls = bookTitleElements.Select(x => ToAbsolute(_options.GoodreadsBase, x.GetAttribute("href"))).ToList();

        //var isbnArray = await Task.WhenAll(bookUrls.Select(x => GetBookIsbn(x, cancellationToken)));
        var isbnList = new List<string>();
        await foreach (var isbnTask in Task.WhenEach(bookUrls.Select(x => GetBookIsbn(x, cancellationToken))).WithCancellation(cancellationToken))
        {
            try
            {
                isbnList.Add(await isbnTask);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
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

        string? isbn = null;
        var jsonData = JsonDocument.Parse(scriptElement.TextContent);
        var stateNode = jsonData.RootElement
            .GetProperty("props")
            .GetProperty("pageProps")
            .GetProperty("apolloState")
            .EnumerateObject();
        foreach (var property in stateNode)
        {
            if (property.Name.StartsWith("Book:"))
            {
                isbn = property.Value.GetProperty("details").GetProperty("isbn13").GetString();
                //Console.WriteLine($"{property.Name}: {property.Value}");
                break;
            }
        }
        
        if (isbn == null)
        {
            throw new ArgumentNullException(nameof(isbn), "ISBN-13 was not found");
        }
        
        return isbn;
    }
    
    private static string ToAbsolute(string startingUrl, string? relativeUrl)
    {
        return new Uri(new Uri(startingUrl), relativeUrl).AbsoluteUri;
    }
}
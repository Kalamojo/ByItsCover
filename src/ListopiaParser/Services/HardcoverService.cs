using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using ListopiaParser.Configs;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Options;

namespace ListopiaParser.Services;

public class HardcoverService
{
    private readonly GraphQLHttpClient _client;
    
    public HardcoverService(HttpClient httpClient, IOptions<HardcoverOptions> hardcoverOptions)
    {
        var options = hardcoverOptions.Value;
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + options.Token);
        
        _client = new GraphQLHttpClient(
            new Uri(options.HardcoverURL),
            new SystemTextJsonSerializer(),
            httpClient
        );
    }

    public async Task<List<Edition>> GetBookEditions(string[] isbnArray, CancellationToken cancellationToken)
    {
        var editionsFromIsbnRequest = new GraphQLRequest
        {
            Query = """
                    query GetEditionsFromISBN($isbn_list: [String]) {
                        editions(where: { isbn_13: { _in: $isbn_list } }) {
                            id
                            isbn_13
                            image {
                                url
                            }
                        }
                    }
                    """,
            OperationName = "GetEditionsFromISBN",
            Variables = new
            {
                isbn_list = isbnArray
            }
        };

        var response = await _client.SendQueryAsync<EditionsResponse>(editionsFromIsbnRequest, cancellationToken);
        
        Console.WriteLine("Finally did the thing, yknow, with " + response.Data.Editions.Count + " editions");
        
        return response.Data.Editions;
    }
}
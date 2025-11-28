using System.Net.Http.Json;
using System.Text.Json;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Options;

namespace ListopiaParser.Services;

public class EmbedService : IEmbedService
{
    private readonly HttpClient _client;
    private readonly EmbedOptions _options;

    public EmbedService(HttpClient client, IOptions<EmbedOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IEnumerable<Cover>> GetCoverEmbeddings(List<Edition> editionList, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.EmbedUrl);
        request.Content = JsonContent.Create( new
        {
            image_urls = editionList.Select(x => x.Image?.Url)
        });
        var temp = await request.Content.ReadAsStringAsync();
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var embeddingsJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddings = JsonSerializer.Deserialize<EmbeddingsResponse>(embeddingsJson);

        if (embeddings == null)
        {
            throw new ArgumentNullException(nameof(embeddings), "Embeddings response was unable to be deserialized.");
        }

        var coverEmbeddings = embeddings.ImageEmbeddings.Zip(editionList)
            .Select(x => new Cover
            {
                CoverId = x.Second.Id,
                Isbn13 = x.Second.Isbn13,
                Url = x.Second.Image?.Url,
                Embedding = x.First
            });
        
        return coverEmbeddings;
    }
}
using ListopiaParser.ResponseTypes;
using ListopiaParser.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly ListopiaService _listopiaService;
    private readonly HardcoverService _hardcoverService;
    private readonly ClipService _clipService;
    private readonly PostgresVectorStore _vectorStore;
    private const int Pages = 2;

    public ListopiaParserRunner(ListopiaService listopiaService,  HardcoverService hardcoverService,  ClipService clipService,  PostgresVectorStore vectorStore)
    {
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _clipService = clipService;
        _vectorStore = vectorStore;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Howdy");

        var collection = _vectorStore.GetCollection<int, Cover>(Constants.CollectionName);
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        var embeddingsUploaded = 0;
        var pages = Enumerable.Range(1, Pages);
        var hardcoverTaskList = new List<Task<List<Edition>>>();
        var clipTaskList = new List<Task<IEnumerable<Cover>>>();
        
        var isbnsTaskList = pages.Select(x => _listopiaService.GetListopiaIsbns(x, cancellationToken));
        
        await foreach (var isbnsTask in Task.WhenEach(isbnsTaskList).WithCancellation(cancellationToken))
        {
            try
            {
                var editionsTask = _hardcoverService.GetBookEditions(await isbnsTask, cancellationToken);
                hardcoverTaskList.Add(editionsTask);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        await foreach (var hardcoverTask in Task.WhenEach(hardcoverTaskList).WithCancellation(cancellationToken))
        {
            try
            {
                var coverEmbeddingsTask = _clipService.GetCoverEmbeddings(await hardcoverTask, cancellationToken);
                clipTaskList.Add(coverEmbeddingsTask);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        
        await foreach (var clipTask in Task.WhenEach(clipTaskList).WithCancellation(cancellationToken))
        {
            try
            {
                var covers = (await clipTask).ToList();
                await collection.UpsertAsync(covers, cancellationToken);
                embeddingsUploaded += covers.Count(x => x.Embedding != null);
                // var embeddingsTask = _clipService.GetCoverEmbeddings(await hardcoverTask, cancellationToken);
                // clipTaskList.Add(embeddingsTask);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        
        Console.WriteLine("Number of embeddings to upload: " + embeddingsUploaded);
    }
}
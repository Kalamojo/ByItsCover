using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly IListopiaService _listopiaService;
    private readonly IHardcoverService _hardcoverService;
    private readonly IClipService _clipService;
    private readonly PostgresVectorStore _vectorStore;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly PgVectorOptions _pgVectorOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IListopiaService listopiaService,  IHardcoverService hardcoverService,
        IClipService clipService, PostgresVectorStore vectorStore, IOptions<ListopiaOptions> listopiaOptions,
        IOptions<PgVectorOptions> pgVectorOptions, ILogger<ListopiaParserRunner> logger)
    {
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _clipService = clipService;
        _vectorStore = vectorStore;
        _listopiaOptions = listopiaOptions.Value;
        _pgVectorOptions = pgVectorOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listopia Parser starting...");

        var collection = _vectorStore.GetCollection<int, Cover>(_pgVectorOptions.CollectionName);
        var exists = await collection.CollectionExistsAsync(cancellationToken);
        _logger.LogInformation($"Collection {_pgVectorOptions.CollectionName} exists status: {exists}");

        var embeddingsUploaded = 0;
        var pages = Enumerable.Range(1, _listopiaOptions.Pages);
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
                _logger.LogError(e, "Error: " + e.Message);
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
                _logger.LogError(e, "Error: " + e.Message);
            }
        }
        
        await foreach (var clipTask in Task.WhenEach(clipTaskList).WithCancellation(cancellationToken))
        {
            try
            {
                var covers = (await clipTask).ToList();
                await collection.UpsertAsync(covers, cancellationToken);
                embeddingsUploaded += covers.Count(x => x.Embedding != null);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error: " + e.Message);
            }
        }
        
        _logger.LogInformation("Number of embeddings uploaded: " + embeddingsUploaded);
        _logger.LogInformation("Listopia Parser completed");
    }
}
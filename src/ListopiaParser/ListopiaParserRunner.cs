using Microsoft.Extensions.Hosting;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly ListopiaService _listopiaService;

    public ListopiaParserRunner(ListopiaService listopiaService)
    {
        _listopiaService = listopiaService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Howdy");
        var isbns = await _listopiaService.GetIsbns(2, cancellationToken);
        Console.WriteLine(isbns);
    }
}
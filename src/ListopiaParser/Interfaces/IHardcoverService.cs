using ListopiaParser.ResponseTypes;

namespace ListopiaParser.Interfaces;

public interface IHardcoverService
{
    public Task<List<Edition>> GetBookEditions(IEnumerable<string> isbnList, CancellationToken cancellationToken);
}
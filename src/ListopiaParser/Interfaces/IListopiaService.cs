namespace ListopiaParser.Interfaces;

public interface IListopiaService
{
    public Task<List<string>> GetListopiaIsbns(int pageNumber, CancellationToken cancellationToken);
}
using ListopiaParser.ResponseTypes;

namespace ListopiaParser.Interfaces;

public interface IClipService
{
    public Task<IEnumerable<Cover>> GetCoverEmbeddings(List<Edition> editionList,
        CancellationToken cancellationToken);
}
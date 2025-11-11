using Microsoft.Extensions.VectorData;

namespace ListopiaParser;

public class Cover
{
    [VectorStoreKey(StorageName = "cover_id")]
    public required int CoverId { get; init; }
    
    [VectorStoreData(StorageName = "cover_isbn_13")]
    public required string Isbn13 { get; init; }
    
    [VectorStoreData(StorageName = "cover_url")]
    public string? Url { get; init; }
    
    [VectorStoreVector(Dimensions: Constants.VectorDimensions, StorageName = "cover_embedding")]
    public ReadOnlyMemory<float>? Embedding { get; init; }
}
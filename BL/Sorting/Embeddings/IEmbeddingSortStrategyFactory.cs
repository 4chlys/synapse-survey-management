namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

public interface IEmbeddingSortStrategyFactory
{
    bool IsEmbeddingKey(string key);
    IEmbeddingSortStrategy Get(string key);
}

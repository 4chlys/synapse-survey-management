namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

public interface IIdeaSortStrategyFactory
{
    bool IsEmbeddingKey(string key);
    IIdeaSortStrategy Get(string key);
}

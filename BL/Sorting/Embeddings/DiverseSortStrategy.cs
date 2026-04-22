using SynapseEnqueteManagement.BL.Domain.Project.Entities;

namespace SynapseEnqueteManagement.BL.Sorting.Embeddings;

// Maximum Marginal Relevance (Carbonell & Goldstein, 1998):
//   MMR(d) = λ · sim(d, q) − (1 − λ) · max_{s ∈ selected} sim(d, s)
// Greedy selection: on each step pick the un-selected idea with the highest
// MMR score, append it to `selected`, repeat. Balances relevance to q against
// novelty vs. what's already been returned, so the top-K isn't a cluster of
// near-duplicates.
//
// If no query embedding is supplied, we use the centroid of all embeddings as
// a pseudo-query — the result is a "representative spread" of the corpus.
//
// Complexity: O(n²) in the number of ideas with embeddings. Fine for ≤1000.
public class DiverseSortStrategy : IEmbeddingSortStrategy
{
    // 0.5 is the classic default: equal weight to relevance and diversity.
    // Higher → more relevance-biased; lower → more novelty-biased.
    private const float Lambda = 0.5f;

    public string Key => "diverse";

    public IReadOnlyList<Idea> Sort(IReadOnlyList<Idea> ideas, float[] queryEmbedding)
    {
        var embedded = ideas.Where(i => i.Embedding != null && !i.Embedding.Memory.IsEmpty).ToList();
        var unembedded = ideas.Where(i => i.Embedding == null || i.Embedding.Memory.IsEmpty).ToList();

        if (embedded.Count == 0) return ideas.ToList();

        // Fall back to the centroid when no query is given — picks items that
        // are broadly on-topic for the corpus while still maximising spread.
        float[] query = queryEmbedding ?? VectorMath.Centroid(embedded.Select(i => i.Embedding.Memory.ToArray()).ToList());

        // Pre-compute sim(d, q) once per idea; the relevance term never changes
        // during greedy selection, only the diversity term does.
        var relevance = new float[embedded.Count];
        for (int i = 0; i < embedded.Count; i++)
            relevance[i] = VectorMath.CosineSimilarity(embedded[i].Embedding.Memory.Span, query);

        // maxSimToSelected[i] = max_{s ∈ selected} sim(embedded[i], s).
        // Maintained incrementally so each step is O(n), total O(n²).
        var maxSimToSelected = new float[embedded.Count];
        Array.Fill(maxSimToSelected, float.NegativeInfinity);

        var remaining = new HashSet<int>(Enumerable.Range(0, embedded.Count));
        var ordered = new List<Idea>(embedded.Count);

        // Seed with the single most relevant item — standard MMR initialisation.
        int seed = Enumerable.Range(0, embedded.Count).MaxBy(i => relevance[i]);
        ordered.Add(embedded[seed]);
        remaining.Remove(seed);
        UpdateMaxSim(embedded, maxSimToSelected, seed, remaining);

        while (remaining.Count > 0)
        {
            int best = -1;
            float bestScore = float.NegativeInfinity;
            foreach (var i in remaining)
            {
                float diversityPenalty = maxSimToSelected[i] == float.NegativeInfinity ? 0f : maxSimToSelected[i];
                float score = Lambda * relevance[i] - (1f - Lambda) * diversityPenalty;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            ordered.Add(embedded[best]);
            remaining.Remove(best);
            UpdateMaxSim(embedded, maxSimToSelected, best, remaining);
        }

        // Embeddingless ideas tail the list — same reasoning as Similar/Different:
        // no vector means no basis for placing them among the ranked results.
        ordered.AddRange(unembedded);
        return ordered;
    }

    private static void UpdateMaxSim(IReadOnlyList<Idea> embedded, float[] maxSim, int justAdded, IEnumerable<int> remaining)
    {
        var addedVec = embedded[justAdded].Embedding.Memory.ToArray();
        foreach (var i in remaining)
        {
            float sim = VectorMath.CosineSimilarity(embedded[i].Embedding.Memory.Span, addedVec);
            if (sim > maxSim[i]) maxSim[i] = sim;
        }
    }
}

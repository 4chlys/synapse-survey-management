namespace SynapseEnqueteManagement.BL.Implementation;

internal static class TranslationUpserter
{
    public static void Upsert<TTranslation, TInput>(
        ICollection<TTranslation> existing,
        IEnumerable<TInput> inputs,
        Func<TInput, string> inputLanguageCode,
        Func<TTranslation, string> existingLanguageCode,
        Func<TInput, TTranslation> create,
        Action<TTranslation, TInput> update)
    {
        var inputList = inputs as IReadOnlyCollection<TInput> ?? inputs.ToList();
        var inputCodes = inputList.Select(inputLanguageCode).ToHashSet();

        foreach (var stale in existing.Where(t => !inputCodes.Contains(existingLanguageCode(t))).ToList())
            existing.Remove(stale);

        foreach (var input in inputList)
        {
            var code = inputLanguageCode(input);
            var match = existing.FirstOrDefault(t => existingLanguageCode(t) == code);
            if (match is null)
                existing.Add(create(input));
            else
                update(match, input);
        }
    }
}

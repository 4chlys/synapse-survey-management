using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ResponseValidatorFactory(IEnumerable<IResponseValidator> validators) : IResponseValidatorFactory
{
    private readonly Dictionary<QuestionFormat, IResponseValidator> _validators =
        validators.ToDictionary(v => v.Format);

    public IResponseValidator GetValidator(QuestionFormat format)
    {
        if (_validators.TryGetValue(format, out var validator))
            return validator;

        throw new NotSupportedException($"No validator registered for question format '{format}'.");
    }
}

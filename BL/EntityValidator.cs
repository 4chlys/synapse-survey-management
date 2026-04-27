using System.ComponentModel.DataAnnotations;

namespace SynapseEnqueteManagement.BL;

internal static class EntityValidator
{
    internal static void Validate<T>(T instance) where T : class
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(instance);
        if (!Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true))
            throw new ValidationException(results[0].ErrorMessage);
    }

    internal static void ValidateProperty<T>(T instance, object value, string propertyName) where T : class
    {
        var ctx = new ValidationContext(instance) { MemberName = propertyName };
        Validator.ValidateProperty(value, ctx);
    }
}

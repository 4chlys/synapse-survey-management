namespace SynapseEnqueteManagement.UI.Admin.Tests.ModelBinding;

// Exercises MVC's ComplexObjectModelBinder against a record type so we can verify
// that init-only setters are populated from form values — the one thing the C#
// compiler can't check for us after the class → record refactor.
public static class FormBinderTestHarness
{
    private static readonly IServiceProvider Services = BuildServices();

    private static IServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddOptions();
        sc.AddMvcCore().AddDataAnnotations();
        return sc.BuildServiceProvider();
    }

    public static async Task<(T Model, ModelStateDictionary ModelState)> BindAsync<T>(
        IDictionary<string, StringValues> formValues) where T : class
    {
        var metadataProvider = Services.GetRequiredService<IModelMetadataProvider>();
        var binderFactory = Services.GetRequiredService<IModelBinderFactory>();
        var validator = Services.GetRequiredService<IObjectModelValidator>();

        var metadata = metadataProvider.GetMetadataForType(typeof(T));
        var binder = binderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata,
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Form }
        });

        var form = new FormCollection(new Dictionary<string, StringValues>(formValues));
        var httpContext = new DefaultHttpContext { RequestServices = Services };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = form;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var valueProvider = new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture);
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            valueProvider,
            metadata,
            bindingInfo: null,
            modelName: string.Empty);

        await binder.BindModelAsync(bindingContext);

        var model = (T)bindingContext.Result.Model;
        if (model is not null)
            validator.Validate(actionContext, validationState: null, prefix: string.Empty, model);

        return (model, actionContext.ModelState);
    }
}

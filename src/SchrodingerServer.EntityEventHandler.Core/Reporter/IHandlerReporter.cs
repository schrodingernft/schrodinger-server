using Prometheus;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.EntityEventHandler.Core.Reporter;

public interface IHandlerReporter
{
    public void RecordAiImageGenAsync(string resourceName, int count = 1);
    public void RecordAiImageHandleAsync(string resourceName, int count = 1);
    public void RecordAiImageLimitExceedAsync(string resourceName, int count = 1);
}

public class DefinitionContants
{
    public const string AiImageGenName = "ai_image_gen";
    public const string AiImageHandleName = "ai_image_handle";
    public const string AiImageLimitExceedName = "ai_image_limit_exceed";
    public const string AiImageGenCallName = "ai_image_call";
    public static readonly string[] AiImageGenLabels = { "resource_name", "action" };
}

public class HandlerReporter : IHandlerReporter, ISingletonDependency
{
    private readonly Counter _aiImageGenCounter;

    public HandlerReporter()
    {
        _aiImageGenCounter = MetricsReporter.RegistryCounters(DefinitionContants.AiImageGenName, DefinitionContants.AiImageGenLabels);
    }

    public void RecordAiImageGenAsync(string resourceName, int count = 1)
    {
        _aiImageGenCounter.WithLabels(resourceName, DefinitionContants.AiImageGenCallName).Inc(count);
    }

    public void RecordAiImageHandleAsync(string resourceName, int count = 1)
    {
        _aiImageGenCounter.WithLabels(resourceName, DefinitionContants.AiImageHandleName).Inc(count);
    }

    public void RecordAiImageLimitExceedAsync(string resourceName, int count = 1)
    {
        _aiImageGenCounter.WithLabels(resourceName, DefinitionContants.AiImageLimitExceedName).Inc(count);
    }
}
using ExtensionComponents.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace ExtensionComponents;

public static class ExtensionStartup
{
    public static Action<string, string> Tracer { get; private set; }
    public static Action<Exception?, string> Logger { get; private set; }
    
    public static string? InitTime { get; set; }

    public static bool ExtensionWebhookInit { get; set; }
    public static WebhooksStorage? ALLWebhooks { get; set; }
    
    public static CallContext ContextInfo { get; set; }
    public static ExtensionCallback Callback = (name, function, data) => 0;

    public static IServiceProvider ServiceProvider { get; private set; }
    public static EntryDelegatesBase entryDelegates { get; private set; } 
    public static ILocalServices localServices { get; private set; }

    public static void InitConfiguration(
        Action<string, string> tracer,
        Action<Exception?, string> logger,
        IServiceProvider serviceProvider
    )
    {
        Tracer = tracer;
        Logger = logger;
        ServiceProvider = serviceProvider;
        
        try
        {
            localServices = serviceProvider.GetRequiredService<ILocalServices>();
            entryDelegates = serviceProvider.GetRequiredService<EntryDelegatesBase>();
            localServices.SetActionsMap(entryDelegates);
            Logger(null, $"({nameof(ExtensionStartup)}) Local Services Initialized");
        }
        catch (Exception e)
        {
            Logger(e, "Initialization Failed");
        }
    }
}

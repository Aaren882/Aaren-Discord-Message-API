using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Extensions;

public static class Arma3PayLoadExtension
{
    private static ILogger? Logger;
    private static IServiceScopeFactory? ServiceScopeFactory;
    
    public static async Task Invoke(
        this Arma3PayloadExtended action,
        IConnection connection,
        IDiscordBotService service
    )
    {
        if (Logger is null || ServiceScopeFactory is null) return;
        
        using var scope = ServiceScopeFactory.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
        Logger.LogInformation("Invoking : {Type}", action.Type);
        await action.Run(service, dbContext);
		
        //- Send back message to the client
        var msg = new Arma3PayloadText($"Invoked \"{action.Type}\"");
        await connection.SendArmaCallBackMessage(msg);
    }
    
    public static void Options(ILogger logger, IServiceScopeFactory serviceScopeFactory)
    {
	    if (Logger is not null && ServiceScopeFactory is not null) return;
	    Logger = logger;
	    ServiceScopeFactory = serviceScopeFactory;
    }

    public static string ToJsonString(this Arma3Payload payload)
    {
	    return JsonSerializer.Serialize(
		    payload,
		    Arma3PayloadJsonSerializerContext.Default.Arma3Payload
	    );
    }
}

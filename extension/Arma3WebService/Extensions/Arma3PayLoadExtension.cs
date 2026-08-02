using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Managers;
using Components.Entity;
using Microsoft.EntityFrameworkCore;
using Arma3WebService;
using Arma3WebService.Entity;

namespace Arma3WebService.Extensions;

public static class Arma3PayLoadExtension
{
	private static ILogger logger;
    
    public static async Task Invoke(
        this Arma3PayloadExtended action,
        IConnection connection,
        IServiceProvider serviceProvider,
        IDbContextFactory<ServiceDbContext> dbContextFactory
    )
    {
	    await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
        {
	        logger.LogInformation("Invoking : {Type}", action.Type);
	        await action.Run(serviceProvider, dbContext);
	        logger.LogInformation("Invoked : {Type}", action.Type);
        }
        
        //- Send back message to the client
        var msg = new Arma3PayloadText($"Invoked \"{action.Type}\"");
        await connection.SendArmaCallBackMessage(msg);
    }
    
    public static void Options()
    {
	    var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
	    logger = loggerFactory.CreateLogger<Arma3PayloadExtended>();
    }

    public static string ToJsonString(this Arma3Payload payload)
    {
	    return JsonSerializer.Serialize(
		    payload,
		    Arma3PayloadJsonSerializerContext.Default.Arma3Payload
	    );
    }
}

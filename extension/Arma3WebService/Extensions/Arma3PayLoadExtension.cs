using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Components.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.Extensions;

public static class Arma3PayLoadExtension
{
	private static ILogger logger;
    private static IDbContextFactory<ServiceDbContext> dbContextFactory;
    
    public static async Task Invoke(
        this Arma3PayloadExtended action,
        IConnection connection,
        IServiceProvider serviceProvider
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
    
    public static void Options(ILogger logger, IDbContextFactory<ServiceDbContext> dbContextFactory)
    {
	    logger = logger;
	    dbContextFactory = dbContextFactory;
    }

    public static string ToJsonString(this Arma3Payload payload)
    {
	    return JsonSerializer.Serialize(
		    payload,
		    Arma3PayloadJsonSerializerContext.Default.Arma3Payload
	    );
    }
}

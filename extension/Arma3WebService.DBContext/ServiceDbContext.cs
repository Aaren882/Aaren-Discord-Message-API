using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;


public sealed class ServiceDbContext: DbContext
{
	private ILogger<ServiceDbContext> _logger;
	private readonly IWebHostEnvironment _env;
	private readonly IConfiguration _configuration;
	
	public ServiceDbContext(
		DbContextOptions<ServiceDbContext> options,
		IConfiguration configuration,
		ILogger<ServiceDbContext> logger,IWebHostEnvironment env
	): base(options)
	{
		_logger = logger;
		_configuration = configuration;
	}
	
	public DbSet<ServerIdentity> ServerIdentities { get; set; }
	public DbSet<ServerInfoTemplate> ServerInfoList { get; set; }
	public DbSet<InternalManagement> InternalManagement { get; set; }

	public async Task<bool> CreateServerIdentityAsync(string profileName, string messageId)
	{
		var exist = ServerIdentities.FirstOrDefault(o => o.profileName == profileName);
				
		if (exist != null) return false;
		
		await ServerIdentities.AddAsync(new ServerIdentity
		{
			profileName = profileName,
			messageId = ulong.Parse(messageId),
		});
		await SaveChangesAsync();
		_logger.LogInformation("Create \"{profileName}\" ServerIdentity.", profileName);
		return true;
	}

	public async Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = ServerIdentities.FirstOrDefault(
			o => o.profileName == profileName
		);

		if (exist == null)
		{
			_logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
			return;
		}

		exist.messageId = ulong.Parse(serverInfoMessageId);
		await SaveChangesAsync();
	}

	public async Task UpsertServerInfoTemplateAsync(FileInfo fileInfo, string serverInfoMessageId)
	{
		var parsedId = ulong.Parse(serverInfoMessageId); 
		var exist = ServerInfoList.FirstOrDefault(o => o.messageId == parsedId);
		
		if (exist == null) {
			await ServerInfoList.AddAsync(new ServerInfoTemplate
			{
				messageId = parsedId,
				messageTemplatePath = fileInfo.FullName
			});
		} else {
			exist.messageTemplatePath = fileInfo.FullName;
			exist.lastUpdate = DateTime.Now;
		}
		
		await SaveChangesAsync();
	}
	
	public async Task<ServerIdentity?> GetServerIdentityFromProfileNameAsync(string profileName)
	{
		var exist = await ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist is null)
			_logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
		
		return exist;
	}
	public async Task<ServerIdentity?> GetServerIdentityFromMessageIdAsync(ulong messageId)
	{
		var exist = await ServerIdentities.FirstOrDefaultAsync(
			o => o.messageId == messageId
		);

		if (exist is null)
			_logger.LogError("\"{messageId}\" ServerIdentity  is not found !!", messageId);
		
		return exist;
	}

	
}

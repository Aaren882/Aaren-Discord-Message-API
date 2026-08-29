using Arma3WebService.DBContext.Entity;
using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Repositories;

public interface IServerInfoTemplateRepository
{
	ServiceDbContext DbContext { get; }
	// Returns a tuple: (The updated template, The associated identity)
	Task<(ServerInfoTemplate updatedTemplate, ServerIdentity identity)> GetOrCreateTemplateAndIdentityAsync(
		ulong messageId,
		Arma3ClientProfileConfiguration configuration);

	Task<ServerInfoTemplate?> GetByMessageIdAsync(ulong messageId);

	// Handles the update logic for an existing template
	Task UpdateTemplateAsync(ServerInfoTemplate existingTemplate, Arma3ClientProfileConfiguration updatedConfiguration);

	// Handles the creation of a new template
	Task AddTemplateAsync(ulong messageId, Arma3ClientProfileConfiguration configuration);
}

public class ServerInfoTemplateRepository(ServiceDbContext DbContext) : IServerInfoTemplateRepository
{
	public ServiceDbContext DbContext { get; } = DbContext;

	public async Task<(ServerInfoTemplate updatedTemplate, ServerIdentity identity)> GetOrCreateTemplateAndIdentityAsync(
		ulong messageId,
		Arma3ClientProfileConfiguration configuration)
	{
		// 1. Try to find the existing ServerInfoTemplate
		var existingTemplate = await DbContext.ServerInfoList
			.FirstOrDefaultAsync(x => x.messageId == messageId);

		if (existingTemplate != null)
		{
			// Template exists, update it
			await UpdateTemplateAsync(existingTemplate, configuration);

			// Retrieve the identity associated with this messageId
			var identity = await DbContext.ServerIdentities.FirstOrDefaultAsync(x => x.messageId == messageId);

			return (existingTemplate, identity ?? throw new NullReferenceException($"ServerIdentity not found for MessageId: {messageId}"));
		}
		else
		{
			// Template does not exist, create it
			var newTemplate = configuration.CreateInfoTemplate(messageId);
			DbContext.ServerInfoList.Add(newTemplate);

			// Create a new identity entry to track this messageId
			var newIdentity = new ServerIdentity
			{
				profileName = "TemplateProfile", // Placeholder or derive from context
				messageId = messageId,
				profileStateStamp = 0, // Initial stamp
			};
			DbContext.ServerIdentities.Add(newIdentity);
			await DbContext.SaveChangesAsync();

			return (newTemplate, newIdentity);
		}
	}
	public Task<ServerInfoTemplate?> GetByMessageIdAsync(ulong messageId)
	{
		// The repository is where the specific EF Core call lives
		return DbContext.ServerInfoList
			.FirstOrDefaultAsync(o => o.messageId == messageId);
	}

	public Task UpdateTemplateAsync(ServerInfoTemplate existingTemplate, Arma3ClientProfileConfiguration updatedConfiguration)
	{
		// Use CurrentValues to update properties on the tracked entity
		DbContext.Entry(existingTemplate).CurrentValues.SetValues(updatedConfiguration.CreateInfoTemplate(existingTemplate.messageId));
		return DbContext.SaveChangesAsync();
	}

	public Task AddTemplateAsync(ulong messageId, Arma3ClientProfileConfiguration configuration)
	{
		DbContext.ServerInfoList.Add(configuration.CreateInfoTemplate(messageId));
		return DbContext.SaveChangesAsync();
	}
}

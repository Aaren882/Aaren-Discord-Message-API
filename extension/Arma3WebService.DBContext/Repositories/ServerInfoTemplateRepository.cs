using Arma3WebService.DBContext.Entity;
using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Repositories;

public interface IServerInfoTemplateRepository
{
	ServiceDbContext DbContext { get; }

	Task<ServerInfoTemplate> GetOrCreateTemplateAsync(ulong messageId, Arma3ClientProfileConfiguration configuration);
	Task<ServerInfoTemplate?> GetByMessageIdAsync(ulong messageId);
	// Handles the update logic for an existing template
	Task<int> UpdateTemplateAsync(ServerInfoTemplate existingTemplate, Arma3ClientProfileConfiguration updatedConfiguration);
	// Handles the creation of a new template
	Task<int> AddTemplateAsync(ulong messageId, Arma3ClientProfileConfiguration configuration);
	Task<int> RemoveTemplateAsync(ServerInfoTemplate template);
}

public class ServerInfoTemplateRepository(ServiceDbContext DbContext) : IServerInfoTemplateRepository
{
	public ServiceDbContext DbContext { get; } = DbContext;

	public async Task<ServerInfoTemplate> GetOrCreateTemplateAsync(
		ulong messageId,
		Arma3ClientProfileConfiguration configuration)
	{
		// Try to find the existing ServerInfoTemplate
		var existingTemplate = await DbContext.ServerInfoList
			.FirstOrDefaultAsync(x => x.messageId == messageId);

		if (existingTemplate != null)
		{
			// Template exists, update it
			await UpdateTemplateAsync(existingTemplate, configuration);

			return existingTemplate;
		}
		else
		{
			// Template does not exist, create it
			var newTemplate = configuration.CreateInfoTemplate(messageId);
			DbContext.ServerInfoList.Add(newTemplate);

			return newTemplate;
		}
	}

	public Task<ServerInfoTemplate?> GetByMessageIdAsync(ulong messageId)
	{
		// The repository is where the specific EF Core call lives
		return DbContext.ServerInfoList
			.FirstOrDefaultAsync(o => o.messageId == messageId);
	}

	public Task<int> UpdateTemplateAsync(ServerInfoTemplate existingTemplate, Arma3ClientProfileConfiguration updatedConfiguration)
	{
		// Use CurrentValues to update properties on the tracked entity
		DbContext.Entry(existingTemplate).CurrentValues.SetValues(updatedConfiguration.CreateInfoTemplate(existingTemplate.messageId));
		return Task.FromResult(0);
	}

	public Task<int> AddTemplateAsync(ulong messageId, Arma3ClientProfileConfiguration configuration)
	{
		DbContext.ServerInfoList.Add(configuration.CreateInfoTemplate(messageId));
		return Task.FromResult(0);
	}

	public Task<int> RemoveTemplateAsync(ServerInfoTemplate template)
	{
		DbContext.ServerInfoList.Remove(template);
		return Task.FromResult(0);
	}
}

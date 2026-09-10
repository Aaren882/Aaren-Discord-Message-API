using System.Collections.Concurrent;
using System.Threading.Channels;
using Components.Entity;
using static Arma3WebService.Managers.BinaryStreamManager;

namespace Arma3WebService.Managers;

public sealed class BinaryStreamManager(
	ILogger<BinaryStreamManager> Logger,
	Channel<Arma3PayloadBinaryContent> _contentChannel,
	ConcurrentDictionary<string, Content> ContentDictionary,
	ConcurrentDictionary<string, Channel<Arma3PayloadBinaryContent>> ContentChannelDictionary
) : BackgroundService
{
	public sealed record class Content(
		Arma3PayloadBinary metaData,
		Stream writeStream,
		Action<Content>? action = null
	) : IDisposable
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			writeStream.Dispose();
		}
	};

	private bool TryGetBinaryValueInternal(string identifier, out Content? content)
		=> ContentDictionary.TryGetValue(identifier, out content);

	public bool TryAddBinaryValue(string identifier, Arma3PayloadBinary metaData, Stream writeStream, Action<Content>? action = null)
	{
		return ContentDictionary.TryAdd(identifier, new(metaData, writeStream, action));
	}
	public ValueTask PushBinaryContentAsync(Arma3PayloadBinaryContent content)
		=> _contentChannel.Writer.WriteAsync(content);

	public async Task<(string identifier, Content content)> AddBinaryAsync(string identifier, Arma3PayloadBinary metaData, Stream writeStream)
	{
		var content = ContentDictionary.GetOrAdd(identifier, _ => new(metaData, writeStream, null));

		await ReadAllContentAsync(identifier);
		return (identifier, content);
	}
	private async Task ReadAllContentAsync(string identifier)
	{
		if (!ContentChannelDictionary.TryGetValue(identifier, out var contentChannel))
			throw new ArgumentOutOfRangeException($"channel with identifier '{identifier}' not found.");

		try
		{
			await foreach (var binaryContent in contentChannel.Reader.ReadAllAsync())
			{
				var (_, bytes, EndOfContent) = binaryContent;
				if (!TryGetBinaryValueInternal(identifier, out var writtenContent))
				{
					Logger.LogWarning("Skip Binary value with identifier \"{identifier}\" not found.", identifier);
					continue;
				}

				var (_, writeStream, action) = writtenContent!;
				await writeStream.WriteAsync(bytes.AsMemory<byte>());

				if (EndOfContent)
				{
					writeStream.Position = 0;
					ContentDictionary.Remove(identifier, out _);
					action?.Invoke(writtenContent);

					writtenContent.Dispose(); //- Dispose content
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred while reading content for identifier '{identifier}'.", identifier);
		}
		finally
		{
			contentChannel.Writer.Complete();
			ContentChannelDictionary.Remove(identifier, out _);
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("{Service} service started. HashCode : {HashCode}", nameof(BinaryStreamManager), _contentChannel.GetHashCode());

		try
		{
			await foreach (var binaryContent in _contentChannel.Reader.ReadAllAsync(stoppingToken))
			{
				var (identifier, _, _) = binaryContent;

				var contentChannel = ContentChannelDictionary.GetOrAdd(identifier, _ => Channel.CreateBounded<Arma3PayloadBinaryContent>(100));
				await contentChannel.Writer.WriteAsync(binaryContent, stoppingToken);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred during binary stream processing.");
		}
		finally
		{
			Logger.LogCritical("Binary stream processing loop terminated.");
		}
	}
}

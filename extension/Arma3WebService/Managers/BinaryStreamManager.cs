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

	public async Task<(string identifier, Content content)> AddBinaryAsync(string identifier, Arma3PayloadBinary metaData, Stream writeStream, TimeSpan? timeout = null)
	{
		var content = ContentDictionary.GetOrAdd(identifier, _ => new(metaData, writeStream, null));

		try
		{
			var actualTimeout = timeout ?? TimeSpan.FromSeconds(15);
			using var cts = new CancellationTokenSource(actualTimeout);

			await ReadAllContentAsync(identifier, cts.Token);
			return (identifier, content);
		}
		catch (OperationCanceledException) //- On Timeout
		{
			Logger.LogWarning("Binary didn't assembled in time - identifier '{identifier}'.", identifier);
			content.Dispose();

			Logger.LogWarning("Cleaned up binary record & content - identifier '{identifier}'.", identifier);
			throw new TimeoutException($"Binary didn't assembled in time - identifier '{identifier}'.");
		}
		finally
		{
			ContentDictionary.Remove(identifier, out _);
		}
	}
	private async Task ReadAllContentAsync(string identifier, CancellationToken ct)
	{
		var contentChannel = ContentChannelDictionary.GetOrAdd(identifier, _ => Channel.CreateBounded<Arma3PayloadBinaryContent>(100));

		try
		{
			await foreach (var binaryContent in contentChannel.Reader.ReadAllAsync(ct))
			{
				var (_, bytes, EndOfContent) = binaryContent;
				if (!TryGetBinaryValueInternal(identifier, out var writtenContent))
				{
					Logger.LogWarning("Skip Binary value with identifier \"{identifier}\" not found.", identifier);
					continue;
				}

				var (_, writeStream, action) = writtenContent!;
				await writeStream.WriteAsync(bytes.AsMemory<byte>(), ct);

				if (EndOfContent)
				{
					contentChannel.Writer.Complete();
					writeStream.Position = 0;
					action?.Invoke(writtenContent);
				}
			}
		}
		catch (OperationCanceledException) { throw; }
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred while reading content for identifier '{identifier}'.", identifier);
		}
		finally
		{
			contentChannel.Writer.TryComplete();
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

using System.Collections.Concurrent;
using System.Threading.Channels;
using Components.Entity;
using static Arma3WebService.Managers.BinaryStreamManager;

namespace Arma3WebService.Managers;

public sealed class BinaryStreamManager(
	ILogger<BinaryStreamManager> Logger,
	Channel<Arma3PayloadBinaryContent> _contentChannel,
	ConcurrentDictionary<string, Content> ContentDictionary
) : BackgroundService
{
	// private readonly ConcurrentDictionary<string, Content> ContentDictionary = new();

	public sealed record class Content(
		Arma3PayloadBinary metaData,
		Stream writeStream,
		TaskCompletionSource<Content> tcs,
		Action<Content>? action = null
	) : IDisposable
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			writeStream.Dispose();
		}
	};

	public bool TryGetBinaryValue(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream)
	{
		var hasContent = ContentDictionary.TryGetValue(identifier, out var content);
		ArgumentNullException.ThrowIfNull(content);

		(metaData, writeStream, _, _) = content;
		return hasContent;
	}
	private bool TryGetBinaryValueInternal(string identifier, out Content content)
		=> ContentDictionary.TryGetValue(identifier, out content);
	public bool TryAddBinaryValue(string identifier, Arma3PayloadBinary metaData, Stream writeStream, Action<Content>? action = null)
	{
		return ContentDictionary.TryAdd(identifier, new(metaData, writeStream, new(TaskCreationOptions.RunContinuationsAsynchronously), action));
	}
	public async Task<(string identifier, Content content)> AddBinaryAsync(string identifier, Arma3PayloadBinary metaData, Stream writeStream)
	{
		Content content = new(metaData, writeStream, new(TaskCreationOptions.RunContinuationsAsynchronously), null);
		if (!ContentDictionary.TryAdd(identifier, content))
			throw new InvalidOperationException($"Binary value with identifier '{identifier}' already exists.");

		await WaitUntilBinaryStreamFinished(identifier);
		return (identifier, content);
	}
	public bool TryRemoveBinaryValue(string identifier, out Content? content)
		=> ContentDictionary.TryRemove(identifier, out content);
	public bool TryPushBinaryContent(in Arma3PayloadBinaryContent content)
	{
		var (Identifier, _, _) = content;
		if (!TryGetBinaryValue(Identifier, out _, out _))
			ArgumentOutOfRangeException.ThrowIfNullOrEmpty(nameof(content), $"Binary value with identifier '{content.Identifier}' not found.");
		return _contentChannel.Writer.TryWrite(content);
	}
	public ValueTask PushBinaryContentAsync(Arma3PayloadBinaryContent content)
	{
		var (Identifier, _, _) = content;
		if (!TryGetBinaryValue(Identifier, out _, out _))
			ArgumentOutOfRangeException.ThrowIfNullOrEmpty(nameof(content), $"Binary value with identifier '{content.Identifier}' not found.");

		return _contentChannel.Writer.WriteAsync(content);
	}
	public Task WaitUntilBinaryStreamFinished(string identifier)
	{
		if (!TryGetBinaryValueInternal(identifier, out var writtenContent))
			ArgumentOutOfRangeException.ThrowIfNullOrEmpty(nameof(identifier), $"Binary value with identifier '{identifier}' not found.");

		var (_, _, tcs, _) = writtenContent;

		return tcs.Task;
	}
	// private async Task DoLoop(CancellationToken stoppingToken)
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("{Service} service started. HashCode : {HashCode}", nameof(BinaryStreamManager), _contentChannel.GetHashCode());

		try
		{
			await foreach (var binaryContent in _contentChannel.Reader.ReadAllAsync(stoppingToken))
			{
				var (identifier, bytes, EndOfContent) = binaryContent;

				if (!TryGetBinaryValueInternal(identifier, out var writtenContent))
				{
					Logger.LogWarning("Skip Binary value with identifier \"{identifier}\" not found.", identifier);
					continue;
				}

				var (_, writeStream, tcs, action) = writtenContent;
				await writeStream.WriteAsync(bytes.AsMemory<byte>(), stoppingToken);

				if (EndOfContent)
				{
					writeStream.Position = 0;
					// semaphore.Release();
					ContentDictionary.Remove(identifier, out _);
					tcs.SetResult(writtenContent);
					action?.Invoke(writtenContent);

					writtenContent.Dispose(); //- Dispose content
				}
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

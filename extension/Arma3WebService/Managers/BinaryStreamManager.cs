using System.Collections.Concurrent;
using System.Threading.Channels;
using Components.Entity;

namespace Arma3WebService.Managers;

public sealed class BinaryStreamManager
{
	private readonly ILogger<BinaryStreamManager> Logger;
	private readonly Task _mainLoop;
	public BinaryStreamManager(ILogger<BinaryStreamManager> logger)
	{
		Logger = logger;
		_mainLoop = Task.Run(async () => await DoLoop());
	}
	private readonly Channel<Arma3PayloadBinaryContent> _contentChannel = Channel.CreateUnbounded<Arma3PayloadBinaryContent>();

	public readonly struct Content(
		Arma3PayloadBinary metaData,
		Stream writeStream,
		SemaphoreSlim semaphore,
		Action<Content>? action
	)
	{
		public void Deconstruct(
			out Arma3PayloadBinary MetaData,
			out Stream WriteStream,
			out SemaphoreSlim Semaphore,
			out Action<Content>? Action
		)
		{
			MetaData = metaData;
			WriteStream = writeStream;
			Semaphore = semaphore;
			Action = action;
		}
	}
	private readonly ConcurrentDictionary<string, Content> ContentDictionary = new();

	public bool TryGetBinaryValue(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream)
	{
		var hasContent = ContentDictionary.TryGetValue(identifier, out var content);

		(metaData, writeStream, _, _) = content;
		ArgumentNullException.ThrowIfNull(metaData);
		ArgumentNullException.ThrowIfNull(writeStream);

		return hasContent;
	}
	private bool TryGetBinaryValueInternal(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream, out SemaphoreSlim Semaphore, out Action<Content>? Action)
	{
		var hasContent = ContentDictionary.TryGetValue(identifier, out var content);

		(metaData, writeStream, Semaphore, Action) = content;
		ArgumentNullException.ThrowIfNull(metaData);
		ArgumentNullException.ThrowIfNull(writeStream);
		ArgumentNullException.ThrowIfNull(Semaphore);

		return hasContent;
	}
	public bool TryAddBinaryValue(string identifier, Arma3PayloadBinary metaData, Stream writeStream, Action<Content>? action)
	{
		return ContentDictionary.TryAdd(identifier, new(metaData, writeStream, new(0), action));
	}
	public bool TryRemoveBinaryValue(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream)
	{
		var removed = ContentDictionary.TryRemove(identifier, out var content);

		(metaData, writeStream, var semaphore, _) = content;
		semaphore.Dispose();
		ArgumentNullException.ThrowIfNull(metaData);
		ArgumentNullException.ThrowIfNull(writeStream);

		return removed;
	}
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
	public async ValueTask WaitUntilBinaryStreamFinished(string identifier)
	{
		if (!TryGetBinaryValueInternal(identifier, out _, out _, out var semaphore, out _))
			ArgumentOutOfRangeException.ThrowIfNullOrEmpty(nameof(identifier), $"Binary value with identifier '{identifier}' not found.");

		await semaphore.WaitAsync();
		Logger.LogInformation("Binary stream finished for identifier: {Identifier}", identifier);
	}
	private async Task DoLoop()
	{
		try
		{
			while (await _contentChannel.Reader.WaitToReadAsync())
			{
				while (_contentChannel.Reader.TryRead(out var content))
				{
					var (identifier, bytes, EndOfContent) = content;

					if (!TryGetBinaryValueInternal(identifier, out _, out var writeStream, out var semaphore, out var action))
					{
						Logger.LogWarning("Skip Binary value with identifier \"{identifier}\" not found.", identifier);
						continue;
					}

					await writeStream.WriteAsync(bytes.AsMemory<byte>());

					if (EndOfContent)
					{
						writeStream.Position = 0;
						semaphore.Release();
						ContentDictionary.Remove(identifier, out var WrittenContent);
						action?.Invoke(WrittenContent);
					}
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

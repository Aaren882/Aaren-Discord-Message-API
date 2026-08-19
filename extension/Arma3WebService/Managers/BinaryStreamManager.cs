using System.Collections.Concurrent;
using System.Threading.Channels;
using Components.Entity;

namespace Arma3WebService.Managers;

public sealed class BinaryStreamManager
{
	private readonly Channel<Arma3PayloadBinaryContent> _contentChannel = Channel.CreateUnbounded<Arma3PayloadBinaryContent>();
	private readonly ConcurrentDictionary<string, Arma3PayloadBinary> BinaryDictionary = new();
	private readonly ConcurrentDictionary<string, Stream> BinaryStreamDictionary = new();

	public bool TryGetBinaryValue(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream)
	{
		var meta = BinaryDictionary.TryGetValue(identifier, out metaData!);
		var stream = BinaryStreamDictionary.TryGetValue(identifier, out writeStream!);
		ArgumentNullException.ThrowIfNull(metaData);
		ArgumentNullException.ThrowIfNull(writeStream);

		return meta && stream;
	}
	public bool TryAddBinaryValue(string identifier, Arma3PayloadBinary metaData, Stream writeStream)
	{
		return BinaryDictionary.TryAdd(identifier, metaData) && BinaryStreamDictionary.TryAdd(identifier, writeStream);
	}
	public bool TryRemoveBinaryValue(string identifier, out Arma3PayloadBinary metaData, out Stream writeStream)
	{
		var meta = BinaryDictionary.TryRemove(identifier, out metaData!);
		var stream = BinaryStreamDictionary.TryRemove(identifier, out writeStream!);
		ArgumentNullException.ThrowIfNull(metaData);
		ArgumentNullException.ThrowIfNull(writeStream);

		return meta && stream;
	}
	public bool TryPushBinaryContent(in Arma3PayloadBinaryContent content)
	{
		var (Identifier, _, _) = content;
		if (!TryGetBinaryValue(Identifier, out _, out _))
			throw new ArgumentOutOfRangeException(nameof(content), $"Binary value with identifier '{content.Identifier}' not found.");
		return _contentChannel.Writer.TryWrite(content);
	}
	public async ValueTask WaitUntilBinaryStreamFinished(string identifier)
	{
		while (await _contentChannel.Reader.WaitToReadAsync())
		{
			if (!_contentChannel.Reader.TryRead(out var content))
				continue;

			var (_, bytes, EndOfContent) = content;
			if (!TryGetBinaryValue(identifier, out _, out var writeStream))
				return;

			await writeStream.WriteAsync(bytes);

			if (EndOfContent)
			{
				return;
			}
		}
	}
}

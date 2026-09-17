using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExtensionComponents.Entity;

public abstract class EntryDelegatesBase
{
	protected static ILogger<EntryDelegatesBase> Logger { get; set; } = NullLogger<EntryDelegatesBase>.Instance;
	protected EntryDelegatesBase(ILogger<EntryDelegatesBase> logger)
	{
		Logger = logger;
	}
	public required Dictionary<byte[], nint>.AlternateLookup<ReadOnlySpan<byte>> ActionsDict;

	public Dictionary<byte[], nint>.AlternateLookup<ReadOnlySpan<byte>> GetActionsMap<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] T>() where T : EntryDelegatesBase
	{
		var actionType = typeof(T);
		var methods = actionType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
			.Where(m => m.ReturnType == typeof(int));

#if DEBUG
		foreach (var method in methods)
			Logger.LogDebug("{ActionsMapName} : {MethodName}", nameof(GetActionsMap), method.Name);
#endif
		Logger.LogInformation("({FuncName}) => {ActionTypeName} registered {Count} key(s).", nameof(GetActionsMap), actionType.FullName, methods.Count());

		return methods.ToDictionary(
			prop => Encoding.UTF8.GetBytes(prop.Name),
			prop => prop.MethodHandle.GetFunctionPointer(),
			Utf8ByteArrayComparer.Ordinal
		).GetAlternateLookup<ReadOnlySpan<byte>>();
	}

	public sealed class Utf8ByteArrayComparer : IEqualityComparer<byte[]>, IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
	{
		public static Utf8ByteArrayComparer Ordinal { get; } = new();

		// Compare by the signature instead reference
		public bool Equals(byte[]? x, byte[]? y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (x == null || y == null) return false;
			return x.AsSpan().SequenceEqual(y);
		}

		public int GetHashCode(byte[] obj) => GetHashCode(obj.AsSpan());

		// Compare ReadOnlySpan<byte> with byte[] keys in Dictionary
		public bool Equals(ReadOnlySpan<byte> alternate, byte[] other)
		{
			return alternate.SequenceEqual(other);
		}

		// Hashing keys into standalone ones
		public int GetHashCode(ReadOnlySpan<byte> alternate)
		{
			HashCode hash = new();
			hash.AddBytes(alternate);
			return hash.ToHashCode();
		}

		// When GetAlternateLookup() it'll write Span as hashed byte[] and save into "AlternateLookup"
		public byte[] Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
	}
}

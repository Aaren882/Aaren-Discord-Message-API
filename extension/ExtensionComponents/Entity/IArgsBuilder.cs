using System.Runtime.InteropServices;

namespace ExtensionComponents.Entity;

public interface IArgsBuilder
{
	public nint SourcePtr { get; init; }
	public int ArgCount { get; init; }
	public string[] GetArgsStringArray();
}

public readonly record struct ArgsBuilder(nint SourcePtr, int ArgCount) : IArgsBuilder
{
	public unsafe string[] GetArgsStringArray()
	{
		if (SourcePtr == nint.Zero) return [];

		var source = (byte**)SourcePtr;
		var argCount = ArgCount;

		// Get the Buffer
		ReadOnlySpan<nint> pointerSpan = new(source, argCount);

		var result = new string[argCount];
		for (var i = 0; i < argCount; i++)
		{
			var rawString = Marshal.PtrToStringUTF8(pointerSpan[i]) ?? string.Empty;
			if (!string.IsNullOrEmpty(rawString))
			{
				//- Remove Arma quotations
				var cleanedSpan = rawString.AsSpan().Trim("\" ");
				result[i] = cleanedSpan.ToString().Replace("\"\"", "\"");
			}
		}

		return result;
	}
}

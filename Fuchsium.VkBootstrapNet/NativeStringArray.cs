using OpenTK.Core.Native;

namespace Fuchsium.VkBootstrapNet;

internal readonly struct NativeStringArray : IDisposable {
	public readonly nint Address;
	public readonly int Length;

	public NativeStringArray(nint address, int length) {
		Address = address;
		Length = length;
	}

	public static NativeStringArray Create(string[] strings) {
		return new NativeStringArray(MarshalTk.MarshalStringArrayToPtr(strings), strings.Length);
	}

	public void Dispose() {
		MarshalTk.FreeStringArrayPtr(Address, Length);
	}

	public static unsafe implicit operator byte**(NativeStringArray nativeStringArray) {
		return (byte**)nativeStringArray.Address;
	}
}
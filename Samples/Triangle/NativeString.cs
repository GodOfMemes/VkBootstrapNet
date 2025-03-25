using OpenTK.Core.Native;

namespace Triangle;

internal readonly struct NativeString : IDisposable {
	public readonly nint Address;

	public NativeString(nint address) {
		Address = address;
	}

	public static NativeString Create(string str) {
		return new NativeString(MarshalTk.MarshalStringToPtr(str));
	}

	public void Dispose() {
		MarshalTk.FreeStringPtr(Address);
	}

	public static explicit operator NativeString(string str) {
		return Create(str);
	}

	public static unsafe implicit operator byte*(NativeString nativeString) {
		return (byte*)nativeString.Address;
	}
}

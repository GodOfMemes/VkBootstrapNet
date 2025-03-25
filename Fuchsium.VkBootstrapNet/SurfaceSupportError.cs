
namespace Fuchsium.VkBootstrapNet;

internal enum SurfaceSupportError {
	SurfaceHandleNull,
	FailedGetSurfaceCapabilities,
	FailedEnumerateSurfaceFormats,
	FailedEnumeratePresentModes,
	NoSuitableDesiredFormat
}

internal sealed class SurfaceSupportException : EnumException<SurfaceSupportError> {
	public SurfaceSupportException(SurfaceSupportError error) : base(error) {
	}

	public SurfaceSupportException(SurfaceSupportError error, string message) : base(error, message) {
	}

	public SurfaceSupportException(SurfaceSupportError error, Exception? innerException) : base(error, innerException) {
	}

	public SurfaceSupportException(SurfaceSupportError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Surface support error.";
}

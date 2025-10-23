
namespace VkBootstrapNet;

public enum PhysicalDeviceError {
	NoSurfaceProvided,
    FailedEnumeratePhysicalDevices,
    NoPhysicalDevicesFound,
    NoSuitableDevice,
}

public sealed class PhysicalDeviceException : EnumException<PhysicalDeviceError> {
	public PhysicalDeviceException(PhysicalDeviceError error) : base(error) {
	}

	public PhysicalDeviceException(PhysicalDeviceError error, string message) : base(error, message) {
	}

	public PhysicalDeviceException(PhysicalDeviceError error, Exception? innerException) : base(error, innerException) {
	}

	public PhysicalDeviceException(PhysicalDeviceError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Physical device error.";
}
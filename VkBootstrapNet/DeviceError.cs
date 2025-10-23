
namespace VkBootstrapNet;

public enum DeviceError {
	FailedCreateDevice,
    VkPhysicalDeviceFeatures2InPNextChainWhileUsingAddRequiredExtensionFeatures,
}

public sealed class DeviceException : EnumException<DeviceError> {
	public DeviceException(DeviceError error) : base(error) {
	}

	public DeviceException(DeviceError error, string message) : base(error, message) {
	}

	public DeviceException(DeviceError error, Exception? innerException) : base(error, innerException) {
	}

	public DeviceException(DeviceError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Device error.";
}
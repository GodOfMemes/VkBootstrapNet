namespace VkBootstrapNet;

public enum InstanceError {
	VulkanUnavailable,
	VulkanVersionUnavailable,
	VulkanVersion11Unavailable,
	VulkanVersion12Unavailable,
	VulkanVersion13Unavailable,
	VulkanVersion14Unavailable,
	FailedCreateInstance,
	FailedCreateDebugMessenger,
	RequestedLayersNotPresent,
	RequestedExtensionsNotPresent,
	WindowingExtensionsNotPresent,
}

public sealed class InstanceException : EnumException<InstanceError> {
	public InstanceException(InstanceError error) : base(error) {
	}

	public InstanceException(InstanceError error, string message) : base(error, message) {
	}

	public InstanceException(InstanceError error, Exception? innerException) : base(error, innerException) {
	}

	public InstanceException(InstanceError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Instance error.";
}
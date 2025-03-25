using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public sealed class VkException : EnumException<VkResult> {
	public VkException(VkResult error) : base(error) {
	}

	public VkException(VkResult error, string message) : base(error, message) {
	}

	public VkException(VkResult error, Exception? innerException) : base(error, innerException) {
	}

	public VkException(VkResult error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "A Vulkan function returned an error.";
}
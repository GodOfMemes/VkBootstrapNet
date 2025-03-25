using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

internal struct SurfaceSupportDetails {
	public VkSurfaceCapabilitiesKHR Capabilities;
	public VkSurfaceFormatKHR[] Formats;
	public VkPresentModeKHR[] PresentModes;

	public SurfaceSupportDetails(VkSurfaceCapabilitiesKHR capabilities, VkSurfaceFormatKHR[] formats, VkPresentModeKHR[] presentModes) {
		Capabilities = capabilities;
		Formats = formats;
		PresentModes = presentModes;
	}
}
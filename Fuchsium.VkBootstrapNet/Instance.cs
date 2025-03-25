using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public unsafe struct Instance : IDisposable {
	public VkInstance VkInstance;
	public VkDebugUtilsMessengerEXT DebugMessenger;
	public VkAllocationCallbacks* AllocationCallbacks;
	public uint InstanceVersion = Vk.VK_API_VERSION_1_0;
	public uint ApiVersion = Vk.VK_API_VERSION_1_0;

	internal bool _headless;
	internal bool _properties2ExtEnabled;

	public Instance() {
	}

	public readonly void DestroySurface(VkSurfaceKHR surface) {
		Vk.DestroySurfaceKHR(VkInstance, surface, AllocationCallbacks);
	}

	public void Dispose() {
		Vk.DestroyInstance(VkInstance, AllocationCallbacks);
	}

	public static implicit operator VkInstance(Instance instance) {
		return instance.VkInstance;
	}
}
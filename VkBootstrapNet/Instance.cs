using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

public unsafe struct Instance : IDisposable {
	public VkInstance VkInstance;
	public VkInstanceApi InstanceApi;
	public VkDebugUtilsMessengerEXT DebugMessenger;
	public VkAllocationCallbacks* AllocationCallbacks;
	public VkVersion InstanceVersion = VkVersion.Version_1_0;
	public VkVersion ApiVersion = VkVersion.Version_1_0;

	internal bool _headless;
	internal bool _properties2ExtEnabled;

	public Instance() {
	}

	public readonly void DestroySurface(VkSurfaceKHR surface) {
		InstanceApi.vkDestroySurfaceKHR(VkInstance, surface, AllocationCallbacks);
	}

	public void Dispose() 
	{
		if(DebugMessenger.IsNotNull)
        {
            InstanceApi.vkDestroyDebugUtilsMessengerEXT(VkInstance,DebugMessenger);
        }
    
		InstanceApi.vkDestroyInstance(VkInstance, AllocationCallbacks);
	}

	public static implicit operator VkInstance(Instance instance) {
		return instance.VkInstance;
	}
  
  	public static implicit operator VkInstanceApi(Instance instance) {
		return instance.InstanceApi;
	}
}
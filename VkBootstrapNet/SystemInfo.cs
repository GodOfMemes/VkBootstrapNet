using System.Text;
using DotNext;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

public unsafe struct SystemInfo {
	public static Result<SystemInfo> GetSystemInfo() {
		try {
			vkInitialize();
		} catch(Exception e) {
			return Result.FromException<SystemInfo>(new InstanceException(InstanceError.VulkanUnavailable, e));
		}
		return new SystemInfo();
	}

	public readonly bool IsLayerAvailable(VkUtf8String layerName) {
		return Detail.CheckLayerSupported(AvailableLayers, layerName);
	}
	public readonly bool IsExtensionAvailable(VkUtf8String extensionName) {
		return Detail.CheckExtensionSupported(AvailableExtensions, extensionName);
	}
	public readonly bool IsInstanceVersionAvailable(uint majorApiVersion, uint minorApiVersion) {
		return InstanceApiVersion >= new VkVersion(majorApiVersion, minorApiVersion, 0);
	}
	public readonly bool IsInstanceVersionAvailable(VkVersion apiVersion) {
		return InstanceApiVersion >= apiVersion;
	}

	public VkLayerProperties[] AvailableLayers = [];
	public List<VkExtensionProperties> AvailableExtensions = [];
	public bool ValidationLayersAvailable;
	public bool DebugUtilsAvailable;

	public VkVersion InstanceApiVersion = VkVersion.Version_1_0;

	public SystemInfo() {
		var availableLayersRet = Detail.GetVector(out VkLayerProperties[] layerProperties, (p1, p2) => vkEnumerateInstanceLayerProperties((uint*)p1, (VkLayerProperties*)p2));
		AvailableLayers = availableLayersRet == VkResult.Success ? layerProperties : [];
    
		ValidationLayersAvailable = AvailableLayers.Any(x => (new VkUtf8String(x.layerName)) == Detail.ValidationLayerName);

		var availableExtensionsRet = Detail.GetVector(out VkExtensionProperties[] extensionProperties, (p1, p2) => vkEnumerateInstanceExtensionProperties(null, (uint*)p1, (VkExtensionProperties*)p2));
		AvailableExtensions = availableExtensionsRet == VkResult.Success ? extensionProperties.ToList() : [];

		foreach(var layer in AvailableLayers) {
			var layerExtensionsRet = Detail.GetVector(out extensionProperties, (p1, p2, p3) => vkEnumerateInstanceExtensionProperties((byte*)p1, (uint*)p2, (VkExtensionProperties*)p3), (nint)(&layer.layerName));
			if(layerExtensionsRet != VkResult.Success) {
				continue;
			}
			AvailableExtensions.AddRange(extensionProperties);
		}

		InstanceApiVersion = vkEnumerateInstanceVersion();

		DebugUtilsAvailable = AvailableExtensions.Any(x => (new VkUtf8String(x.extensionName)) == VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
	}
}

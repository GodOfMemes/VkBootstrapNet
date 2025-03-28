using System.Text;
using DotNext;
using OpenTK.Core.Native;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public unsafe struct SystemInfo {
	public static Result<SystemInfo> GetSystemInfo() {
		try {
			VKLoader.Init();
		} catch(Exception e) {
			return Result.FromException<SystemInfo>(new InstanceException(InstanceError.VulkanUnavailable, e));
		}
		return new SystemInfo();
	}

	public readonly bool IsLayerAvailable(string layerName) {
		return Detail.CheckLayerSupported(AvailableLayers, layerName);
	}
	public readonly bool IsExtensionAvailable(string extensionName) {
		return Detail.CheckExtensionSupported(AvailableExtensions, extensionName);
	}
	public readonly bool IsInstanceVersionAvailable(uint majorApiVersion, uint minorApiVersion) {
		return InstanceApiVersion >= Vk.MAKE_API_VERSION(0, majorApiVersion, minorApiVersion, 0);
	}
	public readonly bool IsInstanceVersionAvailable(uint apiVersion) {
		return InstanceApiVersion >= apiVersion;
	}

	public VkLayerProperties[] AvailableLayers = [];
	public List<VkExtensionProperties> AvailableExtensions = [];
	public bool ValidationLayersAvailable;
	public bool DebugUtilsAvailable;

	public uint InstanceApiVersion = Vk.VK_API_VERSION_1_0;

	public SystemInfo() {
		var availableLayersRet = Detail.GetVector(out VkLayerProperties[] layerProperties, (p1, p2) => Vk.EnumerateInstanceLayerProperties((uint*)p1, (VkLayerProperties*)p2));
		AvailableLayers = availableLayersRet == VkResult.Success ? layerProperties : [];

		ValidationLayersAvailable = AvailableLayers.Any(x => {
			string name = Encoding.UTF8.GetString(x.layerName);
			return name.Substring(0, name.IndexOf('\0')) == Detail.ValidationLayerName;
		});

		var availableExtensionsRet = Detail.GetVector(out VkExtensionProperties[] extensionProperties, (p1, p2) => Vk.EnumerateInstanceExtensionProperties(null, (uint*)p1, (VkExtensionProperties*)p2));
		AvailableExtensions = availableExtensionsRet == VkResult.Success ? extensionProperties.ToList() : [];

		foreach(var layer in AvailableLayers) {
			var layerExtensionsRet = Detail.GetVector(out extensionProperties, (p1, p2, p3) => Vk.EnumerateInstanceExtensionProperties((byte*)p1, (uint*)p2, (VkExtensionProperties*)p3), (nint)(&layer.layerName));
			if(layerExtensionsRet != VkResult.Success) {
				continue;
			}
			AvailableExtensions.AddRange(extensionProperties);
		}

		uint apiVersion;
		if(Vk.EnumerateInstanceVersion(&apiVersion) != VkResult.Success) {
			apiVersion = Vk.VK_API_VERSION_1_0;
		}

		DebugUtilsAvailable = AvailableExtensions.Any(x => {
			string name = Encoding.UTF8.GetString(x.extensionName);
			return name.Substring(0, name.IndexOf('\0')) == Vk.ExtDebugUtilsExtensionName;
		});
	}
}

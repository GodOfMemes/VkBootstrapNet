using VkBootstrapNet;

namespace SystemInfo;

internal class Program {
	static void Main(string[] args) {
		InstanceBuilder instanceBuilder = new();

		var systemInfo = VkBootstrapNet.SystemInfo.GetSystemInfo().Value;

		// check for a layer
		if(systemInfo.IsLayerAvailable("VK_LAYER_LUNARG_api_dump"u8)) {
			instanceBuilder.EnableLayer("VK_LAYER_LUNARG_api_dump"u8);
		}

		// of course dedicated variable for validation
		if(systemInfo.ValidationLayersAvailable) {
			instanceBuilder
				.EnableValidationLayers()
				.UseDefaultDebugMessenger();
		}

		// if you need an instance level extension
		if(systemInfo.IsExtensionAvailable("VK_KHR_get_physical_device_properties2"u8)) {
			instanceBuilder.EnableExtension("VK_KHR_get_physical_device_properties2"u8);
		}

		// Build instance now!
		using var inst = instanceBuilder.Build().Value;
	}
}

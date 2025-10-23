using VkBootstrapNet;
using Vortice.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace BasicUsage;

internal class Program {
	static unsafe bool InitVulkan() {
		InstanceBuilder builder = new();
		var instRet = builder
			.SetAppName("Example Vulkan Application")
			.RequestValidationLayers()
			.RequireApiVersion(VkVersion.Version_1_3)
			.UseDefaultDebugMessenger()
			.Build();
		if(!instRet.IsSuccessful) {
			Console.WriteLine(instRet.Error);
			return false;
		}
		var inst = instRet.Value;

		GLFW.Init();
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
		Window* window = GLFW.CreateWindow(1024, 1024, "Vulkan Triangle", null, null);

		VkResult glfwResult = (VkResult)GLFW.CreateWindowSurface(new VkHandle((ulong)inst.VkInstance.Handle), window, null, out VkHandle surfaceHandle);
		if(glfwResult != VkResult.Success) {
			Console.WriteLine(glfwResult);
			return false;
		}
		VkSurfaceKHR surface = new VkSurfaceKHR(surfaceHandle.Handle);

		PhysicalDeviceSelector selector = new(inst);
		var physRet = selector.SetSurface(surface).Select();
		if(!physRet.IsSuccessful) {
			Console.WriteLine(physRet.Error);
			return false;
		}

		DeviceBuilder deviceBuilder = new(inst, physRet.Value);
		// automatically propagate needed data from instance & physical device
		var devRet = deviceBuilder.Build();
		if(!devRet.IsSuccessful) {
			Console.WriteLine(devRet.Error);
			return false;
		}
		Device device = devRet.Value;

		var graphicsQueueRet = device.GetQueue(QueueType.Graphics);
		if(!graphicsQueueRet.IsSuccessful) {
			Console.WriteLine(graphicsQueueRet.Error);
			return false;
		}
		VkQueue graphicsQueue = graphicsQueueRet.Value;

		SwapchainBuilder swapchainBuilder = new(device);
		var swapRet = swapchainBuilder.Build();
		if(!swapRet.IsSuccessful) {
			Console.WriteLine(swapRet.Error);
			return false;
		}
		Swapchain swapchain = swapRet.Value;

		// We did it!
		// Turned 400-500 lines of boilerplate into a fraction of that.

		// Time to cleanup
		swapchain.Dispose();
		device.Dispose();
		inst.DestroySurface(surface);
		inst.Dispose();
		GLFW.DestroyWindow(window);
		GLFW.Terminate();
		return true;
	}

	static void Main(string[] args) {
		if(InitVulkan()) {
			Console.WriteLine("Boilerplate done, time to write your application!");
		}
	}
}

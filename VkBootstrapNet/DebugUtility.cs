using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace VkBootstrapNet;

public static unsafe class DebugUtility 
{
	[UnmanagedCallersOnly]
	public static uint DefaultDebugCallback(
			VkDebugUtilsMessageSeverityFlagsEXT severity,
			VkDebugUtilsMessageTypeFlagsEXT type,
			VkDebugUtilsMessengerCallbackDataEXT* pData,
			void* pUserData) 
	{
		var severityString = severity.ToString();
		var typeString = type.ToString();
    
    	VkUtf8String message = new VkUtf8String(pData->pMessage);

		if(type == VkDebugUtilsMessageTypeFlagsEXT.Validation) {
			Console.WriteLine($"[{severityString}: {typeString}] - {new VkUtf8String(pData->pMessageIdName)}\n{message}");
		} else {
			Console.WriteLine($"[{severityString}: {typeString}]\n{message}");
		}

		return 0;
	}
}

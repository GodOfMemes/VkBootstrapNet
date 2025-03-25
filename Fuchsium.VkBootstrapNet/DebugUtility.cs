using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public static unsafe class DebugUtility {
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	public static int DefaultDebugCallback(
			VkDebugUtilsMessageSeverityFlagBitsEXT severity,
			VkDebugUtilsMessageTypeFlagBitsEXT type,
			VkDebugUtilsMessengerCallbackDataEXT* pData,
			void* pUserData) {
		var severityString = severity.ToString();
		var typeString = type.ToString();

		if(type == VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt) {
			Console.WriteLine($"[{severityString}: {typeString}] - {MarshalTk.MarshalPtrToString((nint)pData->pMessageIdName)}\n{MarshalTk.MarshalPtrToString((nint)pData->pMessage)}");
		} else {
			Console.WriteLine($"[{severityString}: {typeString}]\n{MarshalTk.MarshalPtrToString((nint)pData->pMessage)}");
		}

		return 0;
	}
}

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DotNext;
using DotNext.Collections.Generic;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

internal static unsafe class Detail {
	public const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";

	public static bool CheckLayerSupported(IEnumerable<VkLayerProperties> availableLayers, string layerName) {
		foreach(var layerProperties in availableLayers) {
			string layerPropertiesName = Encoding.UTF8.GetString(layerProperties.layerName);
			layerPropertiesName = layerPropertiesName.Substring(0, layerPropertiesName.IndexOf('\0'));
			if(layerPropertiesName == layerName) {
				return true;
			}
		}
		return false;
	}
	public static bool CheckLayersSupported(IEnumerable<VkLayerProperties> availableLayers, IEnumerable<string> layerNames) {
		foreach(var layerName in layerNames) {
			if(!CheckLayerSupported(availableLayers, layerName)) {
				return false;
			}
		}
		return true;
	}
	public static bool CheckExtensionSupported(IEnumerable<VkExtensionProperties> availableExtensions, string extensionName) {
		foreach(var extensionProperties in availableExtensions) {
			string extensionPropertiesName = Encoding.UTF8.GetString(extensionProperties.extensionName);
			extensionPropertiesName = extensionPropertiesName.Substring(0, extensionPropertiesName.IndexOf('\0'));
			if(extensionPropertiesName == extensionName) {
				return true;
			}
		}
		return false;
	}
	public static bool CheckExtensionsSupported(IEnumerable<VkExtensionProperties> availableExtensions, IEnumerable<string> extensionNames) {
		foreach(var extensionName in extensionNames) {
			if(!CheckExtensionSupported(availableExtensions, extensionName)) {
				return false;
			}
		}
		return true;
	}
	public static IEnumerable<string> CheckDeviceExtensionSupport(IEnumerable<string> availableExtensions, IEnumerable<string> requiredExtensions) {
		foreach(var availExt in availableExtensions) {
			foreach(var reqExt in requiredExtensions) {
				if(availExt == reqExt) {
					yield return reqExt;
				}
			}
		}
	}
	public static void CombineFeatures(ref VkPhysicalDeviceFeatures dest, in VkPhysicalDeviceFeatures src) {
		dest.robustBufferAccess = dest.robustBufferAccess | src.robustBufferAccess;
		dest.fullDrawIndexUint32 = dest.fullDrawIndexUint32 | src.fullDrawIndexUint32;
		dest.imageCubeArray = dest.imageCubeArray | src.imageCubeArray;
		dest.independentBlend = dest.independentBlend | src.independentBlend;
		dest.geometryShader = dest.geometryShader | src.geometryShader;
		dest.tessellationShader = dest.tessellationShader | src.tessellationShader;
		dest.sampleRateShading = dest.sampleRateShading | src.sampleRateShading;
		dest.dualSrcBlend = dest.dualSrcBlend | src.dualSrcBlend;
		dest.logicOp = dest.logicOp | src.logicOp;
		dest.multiDrawIndirect = dest.multiDrawIndirect | src.multiDrawIndirect;
		dest.drawIndirectFirstInstance = dest.drawIndirectFirstInstance | src.drawIndirectFirstInstance;
		dest.depthClamp = dest.depthClamp | src.depthClamp;
		dest.depthBiasClamp = dest.depthBiasClamp | src.depthBiasClamp;
		dest.fillModeNonSolid = dest.fillModeNonSolid | src.fillModeNonSolid;
		dest.depthBounds = dest.depthBounds | src.depthBounds;
		dest.wideLines = dest.wideLines | src.wideLines;
		dest.largePoints = dest.largePoints | src.largePoints;
		dest.alphaToOne = dest.alphaToOne | src.alphaToOne;
		dest.multiViewport = dest.multiViewport | src.multiViewport;
		dest.samplerAnisotropy = dest.samplerAnisotropy | src.samplerAnisotropy;
		dest.textureCompressionETC2 = dest.textureCompressionETC2 | src.textureCompressionETC2;
		dest.textureCompressionASTC_LDR = dest.textureCompressionASTC_LDR | src.textureCompressionASTC_LDR;
		dest.textureCompressionBC = dest.textureCompressionBC | src.textureCompressionBC;
		dest.occlusionQueryPrecise = dest.occlusionQueryPrecise | src.occlusionQueryPrecise;
		dest.pipelineStatisticsQuery = dest.pipelineStatisticsQuery | src.pipelineStatisticsQuery;
		dest.vertexPipelineStoresAndAtomics = dest.vertexPipelineStoresAndAtomics | src.vertexPipelineStoresAndAtomics;
		dest.fragmentStoresAndAtomics = dest.fragmentStoresAndAtomics | src.fragmentStoresAndAtomics;
		dest.shaderTessellationAndGeometryPointSize = dest.shaderTessellationAndGeometryPointSize | src.shaderTessellationAndGeometryPointSize;
		dest.shaderImageGatherExtended = dest.shaderImageGatherExtended | src.shaderImageGatherExtended;
		dest.shaderStorageImageExtendedFormats = dest.shaderStorageImageExtendedFormats | src.shaderStorageImageExtendedFormats;
		dest.shaderStorageImageMultisample = dest.shaderStorageImageMultisample | src.shaderStorageImageMultisample;
		dest.shaderStorageImageReadWithoutFormat = dest.shaderStorageImageReadWithoutFormat | src.shaderStorageImageReadWithoutFormat;
		dest.shaderStorageImageWriteWithoutFormat = dest.shaderStorageImageWriteWithoutFormat | src.shaderStorageImageWriteWithoutFormat;
		dest.shaderUniformBufferArrayDynamicIndexing = dest.shaderUniformBufferArrayDynamicIndexing | src.shaderUniformBufferArrayDynamicIndexing;
		dest.shaderSampledImageArrayDynamicIndexing = dest.shaderSampledImageArrayDynamicIndexing | src.shaderSampledImageArrayDynamicIndexing;
		dest.shaderStorageBufferArrayDynamicIndexing = dest.shaderStorageBufferArrayDynamicIndexing | src.shaderStorageBufferArrayDynamicIndexing;
		dest.shaderStorageImageArrayDynamicIndexing = dest.shaderStorageImageArrayDynamicIndexing | src.shaderStorageImageArrayDynamicIndexing;
		dest.shaderClipDistance = dest.shaderClipDistance | src.shaderClipDistance;
		dest.shaderCullDistance = dest.shaderCullDistance | src.shaderCullDistance;
		dest.shaderFloat64 = dest.shaderFloat64 | src.shaderFloat64;
		dest.shaderInt64 = dest.shaderInt64 | src.shaderInt64;
		dest.shaderInt16 = dest.shaderInt16 | src.shaderInt16;
		dest.shaderResourceResidency = dest.shaderResourceResidency | src.shaderResourceResidency;
		dest.shaderResourceMinLod = dest.shaderResourceMinLod | src.shaderResourceMinLod;
		dest.sparseBinding = dest.sparseBinding | src.sparseBinding;
		dest.sparseResidencyBuffer = dest.sparseResidencyBuffer | src.sparseResidencyBuffer;
		dest.sparseResidencyImage2D = dest.sparseResidencyImage2D | src.sparseResidencyImage2D;
		dest.sparseResidencyImage3D = dest.sparseResidencyImage3D | src.sparseResidencyImage3D;
		dest.sparseResidency2Samples = dest.sparseResidency2Samples | src.sparseResidency2Samples;
		dest.sparseResidency4Samples = dest.sparseResidency4Samples | src.sparseResidency4Samples;
		dest.sparseResidency8Samples = dest.sparseResidency8Samples | src.sparseResidency8Samples;
		dest.sparseResidency16Samples = dest.sparseResidency16Samples | src.sparseResidency16Samples;
		dest.sparseResidencyAliased = dest.sparseResidencyAliased | src.sparseResidencyAliased;
		dest.variableMultisampleRate = dest.variableMultisampleRate | src.variableMultisampleRate;
		dest.inheritedQueries = dest.inheritedQueries | src.inheritedQueries;
	}
	public static bool SupportsFeatures(
	VkPhysicalDeviceFeatures supported,
	VkPhysicalDeviceFeatures requested,
	GenericFeatureChain extensionSupported,
	GenericFeatureChain extensionRequested) {
		if(requested.robustBufferAccess != 0 && supported.robustBufferAccess == 0) return false;
		if(requested.fullDrawIndexUint32 != 0 && supported.fullDrawIndexUint32 == 0) return false;
		if(requested.imageCubeArray != 0 && supported.imageCubeArray == 0) return false;
		if(requested.independentBlend != 0 && supported.independentBlend == 0) return false;
		if(requested.geometryShader != 0 && supported.geometryShader == 0) return false;
		if(requested.tessellationShader != 0 && supported.tessellationShader == 0) return false;
		if(requested.sampleRateShading != 0 && supported.sampleRateShading == 0) return false;
		if(requested.dualSrcBlend != 0 && supported.dualSrcBlend == 0) return false;
		if(requested.logicOp != 0 && supported.logicOp == 0) return false;
		if(requested.multiDrawIndirect != 0 && supported.multiDrawIndirect == 0) return false;
		if(requested.drawIndirectFirstInstance != 0 && supported.drawIndirectFirstInstance == 0) return false;
		if(requested.depthClamp != 0 && supported.depthClamp == 0) return false;
		if(requested.depthBiasClamp != 0 && supported.depthBiasClamp == 0) return false;
		if(requested.fillModeNonSolid != 0 && supported.fillModeNonSolid == 0) return false;
		if(requested.depthBounds != 0 && supported.depthBounds == 0) return false;
		if(requested.wideLines != 0 && supported.wideLines == 0) return false;
		if(requested.largePoints != 0 && supported.largePoints == 0) return false;
		if(requested.alphaToOne != 0 && supported.alphaToOne == 0) return false;
		if(requested.multiViewport != 0 && supported.multiViewport == 0) return false;
		if(requested.samplerAnisotropy != 0 && supported.samplerAnisotropy == 0) return false;
		if(requested.textureCompressionETC2 != 0 && supported.textureCompressionETC2 == 0) return false;
		if(requested.textureCompressionASTC_LDR != 0 && supported.textureCompressionASTC_LDR == 0) return false;
		if(requested.textureCompressionBC != 0 && supported.textureCompressionBC == 0) return false;
		if(requested.occlusionQueryPrecise != 0 && supported.occlusionQueryPrecise == 0) return false;
		if(requested.pipelineStatisticsQuery != 0 && supported.pipelineStatisticsQuery == 0) return false;
		if(requested.vertexPipelineStoresAndAtomics != 0 && supported.vertexPipelineStoresAndAtomics == 0) return false;
		if(requested.fragmentStoresAndAtomics != 0 && supported.fragmentStoresAndAtomics == 0) return false;
		if(requested.shaderTessellationAndGeometryPointSize != 0 && supported.shaderTessellationAndGeometryPointSize == 0) return false;
		if(requested.shaderImageGatherExtended != 0 && supported.shaderImageGatherExtended == 0) return false;
		if(requested.shaderStorageImageExtendedFormats != 0 && supported.shaderStorageImageExtendedFormats == 0) return false;
		if(requested.shaderStorageImageMultisample != 0 && supported.shaderStorageImageMultisample == 0) return false;
		if(requested.shaderStorageImageReadWithoutFormat != 0 && supported.shaderStorageImageReadWithoutFormat == 0) return false;
		if(requested.shaderStorageImageWriteWithoutFormat != 0 && supported.shaderStorageImageWriteWithoutFormat == 0) return false;
		if(requested.shaderUniformBufferArrayDynamicIndexing != 0 && supported.shaderUniformBufferArrayDynamicIndexing == 0) return false;
		if(requested.shaderSampledImageArrayDynamicIndexing != 0 && supported.shaderSampledImageArrayDynamicIndexing == 0) return false;
		if(requested.shaderStorageBufferArrayDynamicIndexing != 0 && supported.shaderStorageBufferArrayDynamicIndexing == 0) return false;
		if(requested.shaderStorageImageArrayDynamicIndexing != 0 && supported.shaderStorageImageArrayDynamicIndexing == 0) return false;
		if(requested.shaderClipDistance != 0 && supported.shaderClipDistance == 0) return false;
		if(requested.shaderCullDistance != 0 && supported.shaderCullDistance == 0) return false;
		if(requested.shaderFloat64 != 0 && supported.shaderFloat64 == 0) return false;
		if(requested.shaderInt64 != 0 && supported.shaderInt64 == 0) return false;
		if(requested.shaderInt16 != 0 && supported.shaderInt16 == 0) return false;
		if(requested.shaderResourceResidency != 0 && supported.shaderResourceResidency == 0) return false;
		if(requested.shaderResourceMinLod != 0 && supported.shaderResourceMinLod == 0) return false;
		if(requested.sparseBinding != 0 && supported.sparseBinding == 0) return false;
		if(requested.sparseResidencyBuffer != 0 && supported.sparseResidencyBuffer == 0) return false;
		if(requested.sparseResidencyImage2D != 0 && supported.sparseResidencyImage2D == 0) return false;
		if(requested.sparseResidencyImage3D != 0 && supported.sparseResidencyImage3D == 0) return false;
		if(requested.sparseResidency2Samples != 0 && supported.sparseResidency2Samples == 0) return false;
		if(requested.sparseResidency4Samples != 0 && supported.sparseResidency4Samples == 0) return false;
		if(requested.sparseResidency8Samples != 0 && supported.sparseResidency8Samples == 0) return false;
		if(requested.sparseResidency16Samples != 0 && supported.sparseResidency16Samples == 0) return false;
		if(requested.sparseResidencyAliased != 0 && supported.sparseResidencyAliased == 0) return false;
		if(requested.variableMultisampleRate != 0 && supported.variableMultisampleRate == 0) return false;
		if(requested.inheritedQueries != 0 && supported.inheritedQueries == 0) return false;

		return extensionSupported.MatchAll(extensionRequested);
	}

	public static uint GetFirstQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlagBits desiredFlags) {
		for(int i = 0; i < families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags) return (uint)i;
		}
		return uint.MaxValue;
	}
	public static uint GetSeperateQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlagBits desiredFlags, VkQueueFlagBits undesiredFlags) {
		uint index = uint.MaxValue;
		for(int i=0; i<families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags && ((families[i].queueFlags & VkQueueFlagBits.QueueGraphicsBit) == 0)) {
				if((families[i].queueFlags & undesiredFlags) == 0) {
					return (uint)i;
				} else {
					index = (uint)i;
				}
			}
		}
		return index;
	}
	public static uint GetDedicatedQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlagBits desiredFlags, VkQueueFlagBits undesiredFlags) {
		for(int i = 0; i < families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags && (families[i].queueFlags & VkQueueFlagBits.QueueGraphicsBit) == 0 && (families[i].queueFlags & undesiredFlags) == 0) {
				return (uint)i;
			}
		}
		return uint.MaxValue;
	}
	public static uint GetPresentQueueIndex(VkPhysicalDevice physDevice, VkSurfaceKHR surface, VkQueueFamilyProperties[] families) {
		for(int i = 0; i < families.Length; i++) {
			int presentSupport = 0;
			if(surface != default) {
				VkResult res = Vk.GetPhysicalDeviceSurfaceSupportKHR(physDevice, (uint)i, surface, &presentSupport);
				if(res != VkResult.Success) {
					return uint.MaxValue;
				}
			}
			if(presentSupport == 1) {
				return (uint)i;
			}
		}
		return uint.MaxValue;
	}

	public static VkResult GetVector<T>(out T[] result, Func<nint, nint, VkResult> f) where T : unmanaged {
		uint count = 0;
		VkResult error;
		do {
			error = f((nint)(&count), (nint)null);

			result = new T[count];
			fixed(T* pResult = result) {
				error = f((nint)(&count), (nint)pResult);
			}
		} while(error == VkResult.Incomplete);
		return error;
	}
	public static T[] GetVectorNoError<T>(Action<nint, nint> f) where T : unmanaged {
		uint count = 0;
		f((nint)(&count), (nint)null);

		T[] result = new T[count];
		fixed(T* pResult = result) {
			f((nint)(&count), (nint)pResult);
		}
		return result;
	}
	public static VkResult GetVector<T>(out T[] result, Func<nint, nint, nint, VkResult> f, nint p1) where T : unmanaged {
		uint count = 0;
		VkResult error;
		do {
			error = f(p1, (nint)(&count), (nint)null);

			result = new T[count];
			fixed(T* pResult = result) {
				error = f(p1, (nint)(&count), (nint)pResult);
			}
		} while(error == VkResult.Incomplete);
		return error;
	}

	public static void SetupPNextChain<T>(T* pStructure, List<nint> pNextChain) where T : unmanaged {
		SetPNext((nint)pStructure, null);
		if(pNextChain.Count == 0) {
			return;
		}
		for(int i = 0; i < pNextChain.Count - 1; i++) {
			SetPNext(pNextChain[i], (void*)pNextChain[i + 1]);
		}
		
		SetPNext((nint)pStructure, (void*)pNextChain[0]);
	}

	public static VkResult CreateDebugUtilsMessenger(
		VkInstance vkInstance,
		delegate* unmanaged[Cdecl]<VkDebugUtilsMessageSeverityFlagBitsEXT, VkDebugUtilsMessageTypeFlagBitsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, int> debugCallback,
		VkDebugUtilsMessageSeverityFlagBitsEXT debugMessageSeverity,
		VkDebugUtilsMessageTypeFlagBitsEXT debugMessageType,
		void* debugUserDataPointer,
		VkDebugUtilsMessengerEXT* v,
		VkAllocationCallbacks* allocationCallbacks) {
		if(debugCallback == null) {
			debugCallback = &DebugUtility.DefaultDebugCallback;
		}
		VkDebugUtilsMessengerCreateInfoEXT createInfo = new() {
			messageSeverity = debugMessageSeverity,
			messageType = debugMessageType,
			pfnUserCallback = debugCallback,
			pUserData = debugUserDataPointer
		};

		return Vk.CreateDebugUtilsMessengerEXT(vkInstance, &createInfo, allocationCallbacks, v);
	}

	public static Result<SurfaceSupportDetails> QuerySurfaceSupportDetails(VkPhysicalDevice physDevice, VkSurfaceKHR surface) {
		if(surface == default) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.SurfaceHandleNull));
		}

		VkSurfaceCapabilitiesKHR capabilities;
		VkResult res = Vk.GetPhysicalDeviceSurfaceCapabilitiesKHR(physDevice, surface, &capabilities);
		if(res != VkResult.Success) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.FailedGetSurfaceCapabilities, new VkException(res)));
		}

		var formatsRet = GetVector(out VkSurfaceFormatKHR[] formats, (p1, p2) => Vk.GetPhysicalDeviceSurfaceFormatsKHR(physDevice, surface, (uint*)p1, (VkSurfaceFormatKHR*)p2));
		if(formatsRet != VkResult.Success) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.FailedEnumerateSurfaceFormats, new VkException(formatsRet)));
		}
		var presentModesRet = GetVector(out VkPresentModeKHR[] presentModes, (p1, p2) => Vk.GetPhysicalDeviceSurfacePresentModesKHR(physDevice, surface, (uint*)p1, (VkPresentModeKHR*)p2));
		if(presentModesRet != VkResult.Success) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.FailedEnumeratePresentModes, new VkException(formatsRet)));
		}

		return new SurfaceSupportDetails(capabilities, formats, presentModes);
	}

	public static Result<VkSurfaceFormatKHR> FindDesiredSurfaceFormat(VkSurfaceFormatKHR[] availableFormats, VkSurfaceFormatKHR[] desiredFormats) {
		foreach(var desiredFormat in desiredFormats) {
			foreach(var availableFormat in availableFormats) {
				if(desiredFormat.format == availableFormat.format && desiredFormat.colorSpace == availableFormat.colorSpace) {
					return desiredFormat;
				}
			}
		}

		return Result.FromException<VkSurfaceFormatKHR>(new SurfaceSupportException(SurfaceSupportError.NoSuitableDesiredFormat));
	}

	public static VkSurfaceFormatKHR FindBestSurfaceFormat(VkSurfaceFormatKHR[] availableFormats, VkSurfaceFormatKHR[] desiredFormats) {
		var surfaceFormatRet = FindDesiredSurfaceFormat(availableFormats, desiredFormats);
		if(surfaceFormatRet.IsSuccessful) {
			return surfaceFormatRet.Value;
		}

		return availableFormats[0];
	}

	public static VkPresentModeKHR FindPresentMode(VkPresentModeKHR[] availablePresentModes, VkPresentModeKHR[] desiredPresentModes) {
		foreach(var desiredPresentMode in desiredPresentModes) {
			foreach(var availablePresentMode in availablePresentModes) {
				if(desiredPresentMode == availablePresentMode) {
					return desiredPresentMode;
				}
			}
		}

		return VkPresentModeKHR.PresentModeFifoKhr;
	}

	public static VkExtent2D FindExtent(in VkSurfaceCapabilitiesKHR capabilities, uint desiredWidth, uint desiredHeight) {
		if(capabilities.currentExtent.width != uint.MaxValue) {
			return capabilities.currentExtent;
		} else {
			return new VkExtent2D(
				uint.Clamp(desiredWidth, capabilities.minImageExtent.width, capabilities.maxImageExtent.width),
				uint.Clamp(desiredHeight, capabilities.minImageExtent.height, capabilities.maxImageExtent.height));
		}
	}

	private static void SetPNext(nint pStructure, void* value) {
		var pStructureCasted = (VkBaseInStructure*)pStructure;
		pStructureCasted->pNext = (VkBaseInStructure*)value;
	}
}
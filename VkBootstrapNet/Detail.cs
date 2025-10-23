using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DotNext;
using DotNext.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

internal static unsafe class Detail {
	public static VkUtf8String ValidationLayerName = "VK_LAYER_KHRONOS_validation"u8;

	public static bool CheckLayerSupported(IEnumerable<VkLayerProperties> availableLayers, VkUtf8String layerName) {
		foreach(var layerProperties in availableLayers) {
			if(new VkUtf8String(layerProperties.layerName) == layerName) {
				return true;
			}
		}
		return false;
	}
	public static bool CheckLayersSupported(IEnumerable<VkLayerProperties> availableLayers, IEnumerable<VkUtf8String> layerNames) {
		foreach(var layerName in layerNames) {
			if(!CheckLayerSupported(availableLayers, layerName)) {
				return false;
			}
		}
		return true;
	}
	public static bool CheckExtensionSupported(IEnumerable<VkExtensionProperties> availableExtensions, VkUtf8String extensionName) {
		foreach(var extensionProperties in availableExtensions) {
			if(new VkUtf8String(extensionProperties.extensionName) == extensionName) {
				return true;
			}
		}
		return false;
	}
	public static bool CheckExtensionsSupported(IEnumerable<VkExtensionProperties> availableExtensions, IEnumerable<VkUtf8String> extensionNames) {
		foreach(var extensionName in extensionNames) {
			if(!CheckExtensionSupported(availableExtensions, extensionName)) {
				return false;
			}
		}
		return true;
	}
	public static IEnumerable<VkUtf8String> CheckDeviceExtensionSupport(IEnumerable<VkUtf8String> availableExtensions, IEnumerable<VkUtf8String> requiredExtensions) {
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
		if(requested.robustBufferAccess != false && supported.robustBufferAccess == false) return false;
		if(requested.fullDrawIndexUint32 != false && supported.fullDrawIndexUint32 == false) return false;
		if(requested.imageCubeArray != false && supported.imageCubeArray == false) return false;
		if(requested.independentBlend != false && supported.independentBlend == false) return false;
		if(requested.geometryShader != false && supported.geometryShader == false) return false;
		if(requested.tessellationShader != false && supported.tessellationShader == false) return false;
		if(requested.sampleRateShading != false && supported.sampleRateShading == false) return false;
		if(requested.dualSrcBlend != false && supported.dualSrcBlend == false) return false;
		if(requested.logicOp != false && supported.logicOp == false) return false;
		if(requested.multiDrawIndirect != false && supported.multiDrawIndirect == false) return false;
		if(requested.drawIndirectFirstInstance != false && supported.drawIndirectFirstInstance == false) return false;
		if(requested.depthClamp != false && supported.depthClamp == false) return false;
		if(requested.depthBiasClamp != false && supported.depthBiasClamp == false) return false;
		if(requested.fillModeNonSolid != false && supported.fillModeNonSolid == false) return false;
		if(requested.depthBounds != false && supported.depthBounds == false) return false;
		if(requested.wideLines != false && supported.wideLines == false) return false;
		if(requested.largePoints != false && supported.largePoints == false) return false;
		if(requested.alphaToOne != false && supported.alphaToOne == false) return false;
		if(requested.multiViewport != false && supported.multiViewport == false) return false;
		if(requested.samplerAnisotropy != false && supported.samplerAnisotropy == false) return false;
		if(requested.textureCompressionETC2 != false && supported.textureCompressionETC2 == false) return false;
		if(requested.textureCompressionASTC_LDR != false && supported.textureCompressionASTC_LDR == false) return false;
		if(requested.textureCompressionBC != false && supported.textureCompressionBC == false) return false;
		if(requested.occlusionQueryPrecise != false && supported.occlusionQueryPrecise == false) return false;
		if(requested.pipelineStatisticsQuery != false && supported.pipelineStatisticsQuery == false) return false;
		if(requested.vertexPipelineStoresAndAtomics != false && supported.vertexPipelineStoresAndAtomics == false) return false;
		if(requested.fragmentStoresAndAtomics != false && supported.fragmentStoresAndAtomics == false) return false;
		if(requested.shaderTessellationAndGeometryPointSize != false && supported.shaderTessellationAndGeometryPointSize == false) return false;
		if(requested.shaderImageGatherExtended != false && supported.shaderImageGatherExtended == false) return false;
		if(requested.shaderStorageImageExtendedFormats != false && supported.shaderStorageImageExtendedFormats == false) return false;
		if(requested.shaderStorageImageMultisample != false && supported.shaderStorageImageMultisample == false) return false;
		if(requested.shaderStorageImageReadWithoutFormat != false && supported.shaderStorageImageReadWithoutFormat == false) return false;
		if(requested.shaderStorageImageWriteWithoutFormat != false && supported.shaderStorageImageWriteWithoutFormat == false) return false;
		if(requested.shaderUniformBufferArrayDynamicIndexing != false && supported.shaderUniformBufferArrayDynamicIndexing == false) return false;
		if(requested.shaderSampledImageArrayDynamicIndexing != false && supported.shaderSampledImageArrayDynamicIndexing == false) return false;
		if(requested.shaderStorageBufferArrayDynamicIndexing != false && supported.shaderStorageBufferArrayDynamicIndexing == false) return false;
		if(requested.shaderStorageImageArrayDynamicIndexing != false && supported.shaderStorageImageArrayDynamicIndexing == false) return false;
		if(requested.shaderClipDistance != false && supported.shaderClipDistance == false) return false;
		if(requested.shaderCullDistance != false && supported.shaderCullDistance == false) return false;
		if(requested.shaderFloat64 != false && supported.shaderFloat64 == false) return false;
		if(requested.shaderInt64 != false && supported.shaderInt64 == false) return false;
		if(requested.shaderInt16 != false && supported.shaderInt16 == false) return false;
		if(requested.shaderResourceResidency != false && supported.shaderResourceResidency == false) return false;
		if(requested.shaderResourceMinLod != false && supported.shaderResourceMinLod == false) return false;
		if(requested.sparseBinding != false && supported.sparseBinding == false) return false;
		if(requested.sparseResidencyBuffer != false && supported.sparseResidencyBuffer == false) return false;
		if(requested.sparseResidencyImage2D != false && supported.sparseResidencyImage2D == false) return false;
		if(requested.sparseResidencyImage3D != false && supported.sparseResidencyImage3D == false) return false;
		if(requested.sparseResidency2Samples != false && supported.sparseResidency2Samples == false) return false;
		if(requested.sparseResidency4Samples != false && supported.sparseResidency4Samples == false) return false;
		if(requested.sparseResidency8Samples != false && supported.sparseResidency8Samples == false) return false;
		if(requested.sparseResidency16Samples != false && supported.sparseResidency16Samples == false) return false;
		if(requested.sparseResidencyAliased != false && supported.sparseResidencyAliased == false) return false;
		if(requested.variableMultisampleRate != false && supported.variableMultisampleRate == false) return false;
		if(requested.inheritedQueries != false && supported.inheritedQueries == false) return false;

		return extensionSupported.MatchAll(extensionRequested);
	}

	public static uint GetFirstQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlags desiredFlags) {
		for(int i = 0; i < families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags) return (uint)i;
		}
		return uint.MaxValue;
	}
	public static uint GetSeperateQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlags desiredFlags, VkQueueFlags undesiredFlags) {
		uint index = uint.MaxValue;
		for(int i=0; i<families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags && ((families[i].queueFlags & VkQueueFlags.Graphics) == 0)) {
				if((families[i].queueFlags & undesiredFlags) == 0) {
					return (uint)i;
				} else {
					index = (uint)i;
				}
			}
		}
		return index;
	}
	public static uint GetDedicatedQueueIndex(VkQueueFamilyProperties[] families, VkQueueFlags desiredFlags, VkQueueFlags undesiredFlags) {
		for(int i = 0; i < families.Length; i++) {
			if((families[i].queueFlags & desiredFlags) == desiredFlags && (families[i].queueFlags & VkQueueFlags.Graphics) == 0 && (families[i].queueFlags & undesiredFlags) == 0) {
				return (uint)i;
			}
		}
		return uint.MaxValue;
	}
	public static uint GetPresentQueueIndex(VkInstanceApi instanceApi,PhysicalDevice physDevice, VkSurfaceKHR surface, VkQueueFamilyProperties[] families) 
	{
		for(int i = 0; i < families.Length; i++) {
			VkBool32 presentSupport = false;
			if(surface.IsNotNull) {
				VkResult res = instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(physDevice.VkPhysicalDevice, (uint)i, surface, &presentSupport);
				if(res != VkResult.Success) {
					return uint.MaxValue;
				}
			}
			if(presentSupport) 
			{
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
		Instance instance,
		delegate* unmanaged<VkDebugUtilsMessageSeverityFlagsEXT, VkDebugUtilsMessageTypeFlagsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, uint> debugCallback,
		VkDebugUtilsMessageSeverityFlagsEXT debugMessageSeverity,
		VkDebugUtilsMessageTypeFlagsEXT debugMessageType,
		void* debugUserDataPointer,
		VkDebugUtilsMessengerEXT* v,
		VkAllocationCallbacks* allocationCallbacks) 
	{
		if(debugCallback == null) {
			debugCallback = &DebugUtility.DefaultDebugCallback;
		}
		VkDebugUtilsMessengerCreateInfoEXT createInfo = new() {
			messageSeverity = debugMessageSeverity,
			messageType = debugMessageType,
			pfnUserCallback = debugCallback,
			pUserData = debugUserDataPointer
		};

		return instance.InstanceApi.vkCreateDebugUtilsMessengerEXT(instance.VkInstance, &createInfo, allocationCallbacks, v);
	}

	public static Result<SurfaceSupportDetails> QuerySurfaceSupportDetails(VkInstance instance, VkPhysicalDevice physDevice, VkSurfaceKHR surface) {
		if(surface.IsNull) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.SurfaceHandleNull));
		}

		var api = GetApi(instance);

		VkSurfaceCapabilitiesKHR capabilities;
		VkResult res = api.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physDevice, surface, &capabilities);
		if(res != VkResult.Success) {
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.FailedGetSurfaceCapabilities, new VkException(res)));
		}

		var formatsRet = GetVector(out VkSurfaceFormatKHR[] formats, (p1, p2) => api.vkGetPhysicalDeviceSurfaceFormatsKHR(physDevice, surface, (uint*)p1, (VkSurfaceFormatKHR*)p2));
		if(formatsRet != VkResult.Success)
		{
			return Result.FromException<SurfaceSupportDetails>(new SurfaceSupportException(SurfaceSupportError.FailedEnumerateSurfaceFormats, new VkException(formatsRet)));
		}
		var presentModesRet = GetVector(out VkPresentModeKHR[] presentModes, (p1, p2) => api.vkGetPhysicalDeviceSurfacePresentModesKHR(physDevice, surface, (uint*)p1, (VkPresentModeKHR*)p2));
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

		return VkPresentModeKHR.Fifo;
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
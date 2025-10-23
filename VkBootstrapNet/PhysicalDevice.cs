﻿using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

public struct PhysicalDevice {
	public VkUtf8String Name;
	public VkPhysicalDevice VkPhysicalDevice;
	public Instance Instance;
	public VkSurfaceKHR Surface;

	public VkPhysicalDeviceFeatures Features;
	public VkPhysicalDeviceProperties Properties;
	public VkPhysicalDeviceMemoryProperties MemoryProperties;

	internal VkVersion _instanceVersion;
	internal List<VkUtf8String> _extensionsToEnable = [];
	internal List<VkUtf8String> _availableExtensions = [];
	internal VkQueueFamilyProperties[] _queueFamilies = [];
	internal GenericFeatureChain _extendedFeaturesChain;

	internal bool _deferSurfaceInitialization;
	internal bool _properties2ExtEnabled;
	internal Suitable _suitable;

	public readonly bool HasDedicatedComputeQueue() {
		return Detail.GetDedicatedQueueIndex(_queueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer) != uint.MaxValue;
	}

	public readonly bool HasDedicatedTransferQueue() {
		return Detail.GetDedicatedQueueIndex(_queueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute) != uint.MaxValue;
	}

	public readonly bool HasSeperateComputeQueue() {
		return Detail.GetSeperateQueueIndex(_queueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer) != uint.MaxValue;
	}

	public readonly bool HasSeperateTransferQueue() {
		return Detail.GetSeperateQueueIndex(_queueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute) != uint.MaxValue;
	}

	public readonly IEnumerable<VkQueueFamilyProperties> GetQueueFamilies() {
		return _queueFamilies;
	}

	public readonly IEnumerable<VkUtf8String> GetExtensions() {
		return _extensionsToEnable;
	}

	public readonly IEnumerable<VkUtf8String> GetAvailableExtensions() {
		return _availableExtensions;
	}

	public readonly bool IsExtensionPresent(VkUtf8String extension) {
		return _availableExtensions.Any(x => x == extension);
	}

	public readonly bool AreExtensionFeaturesPresent<T>(in T features) where T : unmanaged {
		GenericFeaturesPNextNode.Create(features, out GenericFeaturesPNextNode result);
		return IsFeaturesNodePresent(result);
	}

	public bool EnableExtensionIfPresent(VkUtf8String extension) {
		if(IsExtensionPresent(extension)) {
			_extensionsToEnable.Add(extension);
			return true;
		}
		return false;
	}

	public bool EnableExtensionsIfPresent(IEnumerable<VkUtf8String> extensions) {
		foreach(var item in extensions) {
			if(!EnableExtensionIfPresent(item)) return false;
		}
		return true;
	}

	public unsafe bool EnableFeaturesIfPresent(in VkPhysicalDeviceFeatures featuresToEnable) {
		VkPhysicalDeviceFeatures actualPdf;
		Instance.InstanceApi.vkGetPhysicalDeviceFeatures(VkPhysicalDevice, &actualPdf);

		bool requiredFeaturesSupported = Detail.SupportsFeatures(actualPdf, featuresToEnable, new(), new());
		if(requiredFeaturesSupported) {
			Detail.CombineFeatures(ref Features, featuresToEnable);
		}
		return requiredFeaturesSupported;
	}

	public bool EnableExtensionFeaturesIfPresent<T>(in T featuresCheck) where T : unmanaged {
		GenericFeaturesPNextNode.Create(featuresCheck, out GenericFeaturesPNextNode result);
		return EnableFeaturesNodeIfPresent(result);
	}

	private readonly bool IsFeaturesNodePresent(in GenericFeaturesPNextNode node) {
		var requestedFeatures = new GenericFeatureChain();
		requestedFeatures.Nodes.Add(node);

		return _extendedFeaturesChain.FindAndMatch(requestedFeatures);
	}
	private unsafe bool EnableFeaturesNodeIfPresent(in GenericFeaturesPNextNode node) {
		var requestedFeatures = new GenericFeatureChain();
		requestedFeatures.Nodes.Add(node);

		var fillChain = requestedFeatures.Clone();
		fillChain.Nodes[0].DisableFields();

		bool requiredFeaturesSupported = false;

		uint instanceVersion = _instanceVersion;
		bool properties2ExtEnabled = _properties2ExtEnabled;
		VkPhysicalDevice vkPhysicalDevice = VkPhysicalDevice;
		GenericFeatureChain extendedFeaturesChain = _extendedFeaturesChain;

		var instanceApi = Instance.InstanceApi;
		fillChain.ChainUp((features) => {
			bool instanceIs11 = instanceVersion >= VkVersion.Version_1_1;
			if(instanceIs11 || properties2ExtEnabled) {
				if(instanceIs11) {
					instanceApi.vkGetPhysicalDeviceFeatures2(vkPhysicalDevice, &features);
				} else {
					instanceApi.vkGetPhysicalDeviceFeatures2KHR(vkPhysicalDevice, &features);
				}
				requiredFeaturesSupported = fillChain.MatchAll(requestedFeatures);
				if(requiredFeaturesSupported) {
					extendedFeaturesChain.Combine(requestedFeatures);
				}
			}
		});

		return requiredFeaturesSupported;
	}

	public PhysicalDevice() { }

	public static implicit operator VkPhysicalDevice(in PhysicalDevice physicalDevice) {
		return physicalDevice.VkPhysicalDevice;
	}

	public enum Suitable {
		Yes,
		Partial,
		No
	}
}
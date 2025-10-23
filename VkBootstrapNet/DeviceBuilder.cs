using System.Runtime.InteropServices;
using DotNext;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

public ref struct DeviceBuilder {
	private PhysicalDevice _physicalDevice;
	private Instance _instance; 
	private DeviceInfo _info = new();

	public DeviceBuilder(Instance instance, PhysicalDevice physicalDevice) {
		_physicalDevice = physicalDevice;
		_instance = instance;
	}

	public unsafe Result<Device> Build() {
		List<CustomQueueDescription> queueDescriptions = _info.QueueDescriptions.ToList();

		if(queueDescriptions.Count == 0) {
			for(int i = 0; i < _physicalDevice._queueFamilies.Length; i++) {
				queueDescriptions.Add(new CustomQueueDescription(i, [1f]));
			}
		}

		List<GCHandle> handles = [];

		try {
			List<VkDeviceQueueCreateInfo> queueCreateInfos = [];
			foreach(var item in queueDescriptions) {
				float[] priorities = item.Priorities.ToArray();
				GCHandle pPriorities = GCHandle.Alloc(priorities, GCHandleType.Pinned);
				handles.Add(pPriorities);

				queueCreateInfos.Add(new VkDeviceQueueCreateInfo() {
					queueFamilyIndex = (uint)item.Index,
					queueCount = (uint)item.Priorities.Count,
					pQueuePriorities = (float*)pPriorities.AddrOfPinnedObject()
				});
			}

			List<VkUtf8String> extensionsToEnable = [.._physicalDevice._extensionsToEnable];
			if(_physicalDevice.Surface.IsNotNull || _physicalDevice._deferSurfaceInitialization) {
				extensionsToEnable.Add(VK_KHR_SWAPCHAIN_EXTENSION_NAME);
			}

			List<nint> finalPNextChain = [];
			VkDeviceCreateInfo deviceCreateInfo = new();

			bool userDefinedPhysDevFeatures2 = false;
			foreach(var pnext in _info.PNextChain) {
				VkBaseOutStructure* pNext = (VkBaseOutStructure*)pnext;
				if(pNext->sType == VkStructureType.PhysicalDeviceFeatures2) {
					userDefinedPhysDevFeatures2 = true;
					break;
				}
			}

			if(userDefinedPhysDevFeatures2 && _physicalDevice._extendedFeaturesChain.Nodes.Count != 0) {
				return Result.FromException<Device>(new DeviceException(DeviceError.VkPhysicalDeviceFeatures2InPNextChainWhileUsingAddRequiredExtensionFeatures));
			}

			var physicalDeviceExtensionFeaturesCopy = _physicalDevice._extendedFeaturesChain.Clone();
			VkPhysicalDeviceFeatures2 localFeatures2 = new();
			localFeatures2.sType = VkStructureType.PhysicalDeviceFeatures2;
			localFeatures2.features = _physicalDevice.Features;
			if(!userDefinedPhysDevFeatures2) {
				if(_physicalDevice._instanceVersion >= VkVersion.Version_1_1 || _physicalDevice._properties2ExtEnabled) {
					GCHandle pLocalFeatures2 = GCHandle.Alloc(localFeatures2, GCHandleType.Pinned);
					handles.Add(pLocalFeatures2);

					finalPNextChain.Add(pLocalFeatures2.AddrOfPinnedObject());
					foreach(var featuresNode in physicalDeviceExtensionFeaturesCopy.Nodes) {
						GCHandle pFeaturesNode = GCHandle.Alloc(featuresNode);
						handles.Add(pLocalFeatures2);

						finalPNextChain.Add(pFeaturesNode.AddrOfPinnedObject());
					}
				} else {
					GCHandle pEnabledFeatures = GCHandle.Alloc(_physicalDevice.Features);
					handles.Add(pEnabledFeatures);

					deviceCreateInfo.pEnabledFeatures = (VkPhysicalDeviceFeatures*)pEnabledFeatures.AddrOfPinnedObject();
				}
			}

			finalPNextChain.AddRange(_info.PNextChain);

			DeviceInfo info = _info;
			PhysicalDevice physicalDevice = _physicalDevice;
			VkAllocationCallbacks* allocator = _info.AllocationCallbacks;

			Detail.SetupPNextChain(&deviceCreateInfo, finalPNextChain);
			deviceCreateInfo.flags = info.Flags;

			deviceCreateInfo.queueCreateInfoCount = (uint)queueCreateInfos.Count;
			fixed(VkDeviceQueueCreateInfo* pQueueCreateInfos = (queueCreateInfos.ToArray())) {
				deviceCreateInfo.pQueueCreateInfos = pQueueCreateInfos;

				deviceCreateInfo.enabledExtensionCount = (uint)extensionsToEnable.Count;
        		using var enabledExtensionNames = new VkStringArray(extensionsToEnable);
				deviceCreateInfo.ppEnabledExtensionNames = enabledExtensionNames;

				Device device = new();

				VkDeviceCreateInfo ciCopy = deviceCreateInfo;

				VkResult res = _instance.InstanceApi.vkCreateDevice(physicalDevice.VkPhysicalDevice, &ciCopy, allocator, &device.VkDevice);
				if(res != VkResult.Success) {
					return Result.FromException<Device>(new DeviceException(DeviceError.FailedCreateDevice, new VkException(res)));
				}

				device.DeviceApi = GetApi(_instance.VkInstance, device.VkDevice);
        		device.Instance = _instance;
				device.PhysicalDevice = physicalDevice;
				device.Surface = physicalDevice.Surface;
				device.QueueFamilies = physicalDevice._queueFamilies;
				device.AllocationCallbacks = info.AllocationCallbacks;
				device.InstanceVersion = physicalDevice._instanceVersion;
				return device;
			}
		} finally {
			foreach(var item in handles) {
				item.Free();
			}
		}
	}

	public unsafe ref DeviceBuilder CustomQueueSetup(IEnumerable<CustomQueueDescription> queueDescriptions) {
		_info.QueueDescriptions.AddRange(queueDescriptions);
		return ref this;
	}

	public unsafe ref DeviceBuilder AddPNext<T>(T* structure) where T : unmanaged {
		_info.PNextChain.Add((nint)structure);
		return ref this;
	}

	public unsafe ref DeviceBuilder SetAllocationCallbacks(VkAllocationCallbacks* callbacks) {
		_info.AllocationCallbacks = callbacks;
		return ref this;
	}

	private unsafe struct DeviceInfo {
		public VkDeviceCreateFlags Flags;
		public List<nint> PNextChain = [];
		public List<CustomQueueDescription> QueueDescriptions = [];
		public VkAllocationCallbacks* AllocationCallbacks;

		public DeviceInfo() {
		}
	}
}
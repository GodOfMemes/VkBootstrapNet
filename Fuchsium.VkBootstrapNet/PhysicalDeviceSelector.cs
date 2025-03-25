using System.Text;
using DotNext;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public unsafe ref struct PhysicalDeviceSelector {
	private InstanceInfo _instanceInfo;
	private SelectionCriteria _criteria = new();

	public PhysicalDeviceSelector(in Instance instance) : this(instance, default) {}
	public PhysicalDeviceSelector(in Instance instance, VkSurfaceKHR surface) {
		_instanceInfo = new();
		_instanceInfo.Instance = instance;
		_instanceInfo.Version = instance.InstanceVersion;
		_instanceInfo.Properties2ExtEnabled = instance._properties2ExtEnabled;
		_instanceInfo.Surface = surface;
		_criteria.RequirePresent = !instance._headless;
		_criteria.RequiredVersion = instance.ApiVersion;
	}

	public Result<PhysicalDevice> Select() {
		var selectedDevices = SelectImpl();
		if(!selectedDevices.IsSuccessful) {
			return Result.FromException<PhysicalDevice>(selectedDevices.Error);
		}
		if(selectedDevices.Value.Count == 0) {
			return Result.FromException<PhysicalDevice>(new PhysicalDeviceException(PhysicalDeviceError.NoSuitableDevice));
		}

		return selectedDevices.Value[0];
	}

	public Result<List<PhysicalDevice>> SelectDevices() {
		var selectedDevices = SelectImpl();
		if(!selectedDevices.IsSuccessful) {
			return Result.FromException<List<PhysicalDevice>>(selectedDevices.Error);
		}
		if(selectedDevices.Value.Count == 0) {
			return Result.FromException<List<PhysicalDevice>>(new PhysicalDeviceException(PhysicalDeviceError.NoSuitableDevice));
		}

		return selectedDevices;
	}

	public Result<List<string>> SelectDeviceNames() {
		var selectedDevices = SelectImpl();
		if(!selectedDevices.IsSuccessful) {
			return Result.FromException<List<string>>(selectedDevices.Error);
		}
		if(selectedDevices.Value.Count == 0) {
			return Result.FromException<List<string>>(new PhysicalDeviceException(PhysicalDeviceError.NoSuitableDevice));
		}

		return selectedDevices.Value.Select(x => x.Name).ToList();
	}

	public ref PhysicalDeviceSelector SetSurface(VkSurfaceKHR surface) {
		_instanceInfo.Surface = surface;
		return ref this;
	}

	public ref PhysicalDeviceSelector SetName(string name) {
		_criteria.Name = name;
		return ref this;
	}
	public ref PhysicalDeviceSelector PreferGpuDeviceType(PreferredDeviceType type = PreferredDeviceType.Discrete) {
		_criteria.PreferredType = type;
		return ref this;
	}
	public ref PhysicalDeviceSelector AllowAnyGpuDeviceType(bool allowAnyType = true) {
		_criteria.AllowAnyType = allowAnyType;
		return ref this;
	}

	public ref PhysicalDeviceSelector RequirePresent(bool require = true) {
		_criteria.RequirePresent = require;
		return ref this;
	}

	public ref PhysicalDeviceSelector RequireDedicatedComputeQueue() {
		_criteria.RequireDedicatedComputeQueue = true;
		return ref this;
	}
	public ref PhysicalDeviceSelector RequireDedicatedTransferQueue() {
		_criteria.RequireDedicatedTransferQueue = true;
		return ref this;
	}

	public ref PhysicalDeviceSelector RequireSeperateComputeQueue() {
		_criteria.RequireSeperateComputeQueue = true;
		return ref this;
	}
	public ref PhysicalDeviceSelector RequireSeperateTransferQueue() {
		_criteria.RequireSeperateTransferQueue = true;
		return ref this;
	}

	public ref PhysicalDeviceSelector RequiredDeviceMemorySize(ulong size) {
		_criteria.RequiredMemSize = size;
		return ref this;
	}

	public ref PhysicalDeviceSelector AddRequiredExtension(string extension) {
		_criteria.RequiredExtensions.Add(extension);
		return ref this;
	}
	public ref PhysicalDeviceSelector AddRequiredExtensions(IEnumerable<string> extensions) {
		_criteria.RequiredExtensions.AddRange(extensions);
		return ref this;
	}

	public ref PhysicalDeviceSelector SetMinimumVersion(uint major, uint minor) {
		_criteria.RequiredVersion = Vk.MAKE_API_VERSION(0, major, minor, 0);
		return ref this;
	}

	public ref PhysicalDeviceSelector DisablePortabilitySubset() {
		_criteria.EnablePortabilitySubset = false;
		return ref this;
	}

	public ref PhysicalDeviceSelector AddRequiredExtensionFeatures<T>(in T features) where T : unmanaged {
		_criteria.ExtendedFeaturesChain.Add(features);
		return ref this;
	}

	public ref PhysicalDeviceSelector SetRequiredFeatures(in VkPhysicalDeviceFeatures features) {
		Detail.CombineFeatures(ref _criteria.RequiredFeatures, features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures11(VkPhysicalDeviceVulkan11Features features) {
		features.sType = VkStructureType.StructureTypePhysicalDeviceVulkan11Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures12(VkPhysicalDeviceVulkan12Features features) {
		features.sType = VkStructureType.StructureTypePhysicalDeviceVulkan12Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures13(VkPhysicalDeviceVulkan13Features features) {
		features.sType = VkStructureType.StructureTypePhysicalDeviceVulkan13Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	//public PhysicalDeviceSelector SetRequiredFeatures14(in VkPhysicalDeviceVulkan14Features features) {

	//}

	public ref PhysicalDeviceSelector DeferSurfaceInitialization() {
		_criteria.DeferSurfaceInitialization = true;
		return ref this;
	}

	public ref PhysicalDeviceSelector SelectFirstDeviceUnconditionally(bool unconditionally = true) {
		_criteria.UseFirstGpuUnconditionally = unconditionally;
		return ref this;
	}

	private unsafe PhysicalDevice PopulateDeviceDetails(VkPhysicalDevice physDevice, in GenericFeatureChain srcExtendedFeaturesChain) {
		PhysicalDevice physicalDevice = new() {
			VkPhysicalDevice = physDevice,
			Surface = _instanceInfo.Surface,
			_deferSurfaceInitialization = _criteria.DeferSurfaceInitialization,
			_instanceVersion = _instanceInfo.Version
		};
		GetQueueFamilyProperties(physicalDevice, out VkQueueFamilyProperties[] families);
		physicalDevice._queueFamilies = families;

		ReadFeatures(physDevice, ref physicalDevice);

		physicalDevice.Name = Encoding.UTF8.GetString(physicalDevice.Properties.deviceName);
		physicalDevice.Name = physicalDevice.Name.Substring(0, physicalDevice.Name.IndexOf('\0'));

		var availableExtensionsRet = Detail.GetVector(out VkExtensionProperties[] availableExtensions, (p1, p2) => Vk.EnumerateDeviceExtensionProperties(physicalDevice, null, (uint*)p1, (VkExtensionProperties*)p2));
		if(availableExtensionsRet != VkResult.Success) {
			return physicalDevice;
		}
		physicalDevice._availableExtensions.AddRange(availableExtensions.Select(x => {
			string name = Encoding.UTF8.GetString(x.extensionName);
			name = name.Substring(0, name.IndexOf('\0'));
			return name;
		}));

		physicalDevice._properties2ExtEnabled = _instanceInfo.Properties2ExtEnabled;

		var fillChain = srcExtendedFeaturesChain;

		var instanceIs11 = _instanceInfo.Version >= Vk.VK_API_VERSION_1_1;
		if(fillChain.Nodes.Count > 0 && (instanceIs11 || _instanceInfo.Properties2ExtEnabled)) {
			fillChain.ChainUp((features) => {
				if(instanceIs11) {
					Vk.GetPhysicalDeviceFeatures2(physicalDevice, &features);
				} else {
					Vk.GetPhysicalDeviceFeatures2KHR(physicalDevice, &features);
				}
			});
			physicalDevice._extendedFeaturesChain = fillChain;
		} else {
			physicalDevice._extendedFeaturesChain = new GenericFeatureChain();
		}

			return physicalDevice;
	}

	private static unsafe void ReadFeatures(VkPhysicalDevice physDevice, ref PhysicalDevice physicalDevice) {
		fixed(VkPhysicalDeviceProperties* propertiesPtr = &physicalDevice.Properties)
		fixed(VkPhysicalDeviceFeatures* featuresPtr = &physicalDevice.Features)
		fixed(VkPhysicalDeviceMemoryProperties* memoryPropertiesPtr = &physicalDevice.MemoryProperties) {
			Vk.GetPhysicalDeviceProperties(physDevice, propertiesPtr);
			Vk.GetPhysicalDeviceFeatures(physDevice, featuresPtr);
			Vk.GetPhysicalDeviceMemoryProperties(physDevice, memoryPropertiesPtr);
		}
	}

	private static unsafe void GetQueueFamilyProperties(PhysicalDevice physicalDevice, out VkQueueFamilyProperties[] families) {
		families = Detail.GetVectorNoError<VkQueueFamilyProperties>((p1, p2) => Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, (uint*)p1, (VkQueueFamilyProperties*)p2));
	}

	private readonly unsafe PhysicalDevice.Suitable IsDeviceSuitable(in PhysicalDevice pd) {
		PhysicalDevice localPd = pd;
		var suitable = PhysicalDevice.Suitable.Yes;

		if(!string.IsNullOrEmpty(_criteria.Name) && _criteria.Name != pd.Name) return PhysicalDevice.Suitable.No;
		if(_criteria.RequiredVersion > pd.Properties.apiVersion) return PhysicalDevice.Suitable.No;

		bool dedicatedCompute = Detail.GetDedicatedQueueIndex(pd._queueFamilies, VkQueueFlagBits.QueueComputeBit, VkQueueFlagBits.QueueTransferBit) != uint.MaxValue;
		bool dedicatedTransfer = Detail.GetDedicatedQueueIndex(pd._queueFamilies, VkQueueFlagBits.QueueTransferBit, VkQueueFlagBits.QueueComputeBit) != uint.MaxValue;
		bool seperateTransfer = Detail.GetSeperateQueueIndex(pd._queueFamilies, VkQueueFlagBits.QueueTransferBit, VkQueueFlagBits.QueueComputeBit) != uint.MaxValue;
		bool seperateCompute = Detail.GetSeperateQueueIndex(pd._queueFamilies, VkQueueFlagBits.QueueComputeBit, VkQueueFlagBits.QueueTransferBit) != uint.MaxValue;

		bool presentQueue = Detail.GetPresentQueueIndex(pd, _instanceInfo.Surface, pd._queueFamilies) != uint.MaxValue;

		if(_criteria.RequireDedicatedComputeQueue && !dedicatedCompute) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireDedicatedTransferQueue && !dedicatedTransfer) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireSeperateComputeQueue && !seperateCompute) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireSeperateTransferQueue && !seperateTransfer) return PhysicalDevice.Suitable.No;

		var requiredExtensionsSupported = Detail.CheckDeviceExtensionSupport(pd._availableExtensions, _criteria.RequiredExtensions).ToArray();
		if(requiredExtensionsSupported.Length != _criteria.RequiredExtensions.Count) {
			return PhysicalDevice.Suitable.No;
		}

		if(!_criteria.DeferSurfaceInitialization && _criteria.RequirePresent) {
			var formatsRet = Detail.GetVector(out VkSurfaceFormatKHR[] formats, (p1, p2) => Vk.GetPhysicalDeviceSurfaceFormatsKHR(localPd, localPd.Surface, (uint*)p1, (VkSurfaceFormatKHR*)p2));
			var presentModesRet = Detail.GetVector(out VkPresentModeKHR[] presentModes, (p1, p2) => Vk.GetPhysicalDeviceSurfacePresentModesKHR(localPd, localPd.Surface, (uint*)p1, (VkPresentModeKHR*)p2));
		
			if(formatsRet != VkResult.Success || presentModesRet != VkResult.Success || formats.Length == 0 || presentModes.Length == 0) {
				return PhysicalDevice.Suitable.No;
			}
		}

		if(!_criteria.AllowAnyType && pd.Properties.deviceType != (VkPhysicalDeviceType)_criteria.PreferredType) {
			suitable = PhysicalDevice.Suitable.Partial;
		}

		bool requiredFeaturesSupported = Detail.SupportsFeatures(pd.Features, _criteria.RequiredFeatures, pd._extendedFeaturesChain, _criteria.ExtendedFeaturesChain);
		if(!requiredFeaturesSupported) {
			return PhysicalDevice.Suitable.No;
		}

		for(int i = 0; i < pd.MemoryProperties.memoryHeapCount; i++) {
			if((pd.MemoryProperties.memoryHeaps[i].flags & VkMemoryHeapFlagBits.MemoryHeapDeviceLocalBit) != 0) {
				if(pd.MemoryProperties.memoryHeaps[i].size < _criteria.RequiredMemSize) {
					return PhysicalDevice.Suitable.No;
				}
			}
		}

		return suitable;
	}

	private unsafe Result<List<PhysicalDevice>> SelectImpl() {
		if(_criteria.RequirePresent && !_criteria.DeferSurfaceInitialization) {
			if(_instanceInfo.Surface == default) {
				return Result.FromException<List<PhysicalDevice>>(new PhysicalDeviceException(PhysicalDeviceError.NoSurfaceProvided));
			}
		}

		VkInstance instance = _instanceInfo.Instance;

		var vkPhysicalDevicesRet = Detail.GetVector(out VkPhysicalDevice[] vkPhysicalDevices, (p1, p2) => Vk.EnumeratePhysicalDevices(instance, (uint*)p1, (VkPhysicalDevice*)p2));
		if(vkPhysicalDevicesRet != VkResult.Success) {
			return Result.FromException<List<PhysicalDevice>>(new PhysicalDeviceException(PhysicalDeviceError.FailedEnumeratePhysicalDevices, new VkException(vkPhysicalDevicesRet)));
		}
		if(vkPhysicalDevices.Length == 0) {
			return Result.FromException<List<PhysicalDevice>>(new PhysicalDeviceException(PhysicalDeviceError.NoPhysicalDevicesFound));
		}

		static void FillOutPhysDevWithCriteria(ref PhysicalDeviceSelector self, ref PhysicalDevice physDev) {
			physDev.Features = self._criteria.RequiredFeatures;
			physDev._extendedFeaturesChain = self._criteria.ExtendedFeaturesChain;
			bool portabilityExtAvailable = false;
			foreach(var item in physDev._availableExtensions) {
				if(self._criteria.EnablePortabilitySubset && item == "VK_KHR_portability_subset") {
					portabilityExtAvailable = true;
				}
			}

			physDev._extensionsToEnable = self._criteria.RequiredExtensions.ToList();
			if(portabilityExtAvailable) {
				physDev._extensionsToEnable.Add("VK_KHR_portability_subset");
			}
		}

		if(_criteria.UseFirstGpuUnconditionally && vkPhysicalDevices.Length > 0) {
			PhysicalDevice physicalDevice = PopulateDeviceDetails(vkPhysicalDevices[0], _criteria.ExtendedFeaturesChain);
			FillOutPhysDevWithCriteria(ref this, ref physicalDevice);
			return (List<PhysicalDevice>)[physicalDevice];
		}

		List<PhysicalDevice> physicalDevices = [];
		foreach(var item in vkPhysicalDevices) {
			PhysicalDevice physDev = PopulateDeviceDetails(item, _criteria.ExtendedFeaturesChain);
			physDev._suitable = IsDeviceSuitable(physDev);
			if(physDev._suitable != PhysicalDevice.Suitable.No) {
				physicalDevices.Add(physDev);
			}
		}

		physicalDevices.Sort((x, y) => x._suitable.CompareTo(y._suitable));

		for(int i = 0; i < physicalDevices.Count; i++) {
			var physDev = physicalDevices[i];
			FillOutPhysDevWithCriteria(ref this, ref physDev);
			physicalDevices[i] = physDev;
		}

		return physicalDevices;
	}

	struct InstanceInfo {
		public VkInstance Instance;
		public VkSurfaceKHR Surface;
		public uint Version;
		public bool Headless;
		public bool Properties2ExtEnabled;
	}

	struct SelectionCriteria {
		public string? Name;
		public PreferredDeviceType PreferredType = PreferredDeviceType.Discrete;
		public bool AllowAnyType = true;
		public bool RequirePresent = true;
		public bool RequireDedicatedTransferQueue = false;
		public bool RequireDedicatedComputeQueue = false;
		public bool RequireSeperateTransferQueue = false;
		public bool RequireSeperateComputeQueue = false;
		public ulong RequiredMemSize = 0;

		public List<string> RequiredExtensions = [];

		public uint RequiredVersion = Vk.VK_API_VERSION_1_0;

		public VkPhysicalDeviceFeatures RequiredFeatures;
		public VkPhysicalDeviceFeatures2 RequiredFeatures2;

		public GenericFeatureChain ExtendedFeaturesChain = new();
		public bool DeferSurfaceInitialization = false;
		public bool UseFirstGpuUnconditionally = false;
		public bool EnablePortabilitySubset = true;

		public SelectionCriteria() {
		}
	}
}
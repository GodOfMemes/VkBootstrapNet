using System.Text;
using DotNext;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

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

	public Result<List<VkUtf8String>> SelectDeviceNames() {
		var selectedDevices = SelectImpl();
		if(!selectedDevices.IsSuccessful) {
			return Result.FromException<List<VkUtf8String>>(selectedDevices.Error);
		}
		if(selectedDevices.Value.Count == 0) {
			return Result.FromException<List<VkUtf8String>>(new PhysicalDeviceException(PhysicalDeviceError.NoSuitableDevice));
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

	public ref PhysicalDeviceSelector AddRequiredExtension(VkUtf8String extension) {
		_criteria.RequiredExtensions.Add(extension);
		return ref this;
	}
	public ref PhysicalDeviceSelector AddRequiredExtensions(IEnumerable<VkUtf8String> extensions) {
		_criteria.RequiredExtensions.AddRange(extensions);
		return ref this;
	}

	public ref PhysicalDeviceSelector SetMinimumVersion(uint major, uint minor) {
		_criteria.RequiredVersion = new(0, major, minor, 0);
		return ref this;
	}

	public ref PhysicalDeviceSelector SetMinimumVersion(VkVersion version) {
		_criteria.RequiredVersion = version;
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
		features.sType = VkStructureType.PhysicalDeviceVulkan11Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures12(VkPhysicalDeviceVulkan12Features features) {
		features.sType = VkStructureType.PhysicalDeviceVulkan12Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures13(VkPhysicalDeviceVulkan13Features features) {
		features.sType = VkStructureType.PhysicalDeviceVulkan13Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}
	public ref PhysicalDeviceSelector SetRequiredFeatures14(VkPhysicalDeviceVulkan14Features features) {
		features.sType = VkStructureType.PhysicalDeviceVulkan14Features;
		AddRequiredExtensionFeatures(features);
		return ref this;
	}

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
			Instance = _instanceInfo.Instance,
			_deferSurfaceInitialization = _criteria.DeferSurfaceInitialization,
			_instanceVersion = _instanceInfo.Version
		};
		GetQueueFamilyProperties(physicalDevice, out VkQueueFamilyProperties[] families);
		physicalDevice._queueFamilies = families;

		ReadFeatures(physDevice, ref physicalDevice);

		var prop = physicalDevice.Properties;
		physicalDevice.Name = new VkUtf8String(prop.deviceName);

		var availableExtensionsRet = Detail.GetVector(out VkExtensionProperties[] availableExtensions, (p1, p2) => physicalDevice.Instance.InstanceApi.vkEnumerateDeviceExtensionProperties(physicalDevice, null, (uint*)p1, (VkExtensionProperties*)p2));
		if(availableExtensionsRet != VkResult.Success) {
			return physicalDevice;
		}
		physicalDevice._availableExtensions.AddRange(availableExtensions.Select(x => new VkUtf8String(x.extensionName)));

		physicalDevice._properties2ExtEnabled = _instanceInfo.Properties2ExtEnabled;

		var fillChain = srcExtendedFeaturesChain;

		var instanceIs11 = _instanceInfo.Version >= VkVersion.Version_1_1;
		if(fillChain.Nodes.Count > 0 && (instanceIs11 || _instanceInfo.Properties2ExtEnabled))
		{
			var instanceApi = physicalDevice.Instance.InstanceApi;
			fillChain.ChainUp((features) => {
				if(instanceIs11) {
					instanceApi.vkGetPhysicalDeviceFeatures2(physicalDevice, &features);
				} else {
					instanceApi.vkGetPhysicalDeviceFeatures2KHR(physicalDevice, &features);
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
		fixed(VkPhysicalDeviceMemoryProperties* memoryPropertiesPtr = &physicalDevice.MemoryProperties) 
		{
			physicalDevice.Instance.InstanceApi.vkGetPhysicalDeviceProperties(physDevice, propertiesPtr);
			physicalDevice.Instance.InstanceApi.vkGetPhysicalDeviceFeatures(physDevice, featuresPtr);
			physicalDevice.Instance.InstanceApi.vkGetPhysicalDeviceMemoryProperties(physDevice, memoryPropertiesPtr);
		}
	}

	private static unsafe void GetQueueFamilyProperties(PhysicalDevice physicalDevice, out VkQueueFamilyProperties[] families) {
		families = Detail.GetVectorNoError<VkQueueFamilyProperties>((p1, p2) => physicalDevice.Instance.InstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, (uint*)p1, (VkQueueFamilyProperties*)p2));
	}

	private readonly unsafe PhysicalDevice.Suitable IsDeviceSuitable(in PhysicalDevice pd) {
		PhysicalDevice localPd = pd;
		VkInstanceApi localApi = pd.Instance.InstanceApi;
		var suitable = PhysicalDevice.Suitable.Yes;

		if(!string.IsNullOrEmpty(_criteria.Name) && _criteria.Name != pd.Name.ToString()) return PhysicalDevice.Suitable.No;
		if(_criteria.RequiredVersion > pd.Properties.apiVersion) return PhysicalDevice.Suitable.No;

		bool dedicatedCompute = Detail.GetDedicatedQueueIndex(pd._queueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer) != uint.MaxValue;
		bool dedicatedTransfer = Detail.GetDedicatedQueueIndex(pd._queueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute) != uint.MaxValue;
		bool seperateTransfer = Detail.GetSeperateQueueIndex(pd._queueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute) != uint.MaxValue;
		bool seperateCompute = Detail.GetSeperateQueueIndex(pd._queueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer) != uint.MaxValue;

		bool presentQueue = Detail.GetPresentQueueIndex(pd.Instance, pd, _instanceInfo.Surface, pd._queueFamilies) != uint.MaxValue;

		if(_criteria.RequireDedicatedComputeQueue && !dedicatedCompute) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireDedicatedTransferQueue && !dedicatedTransfer) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireSeperateComputeQueue && !seperateCompute) return PhysicalDevice.Suitable.No;
		if(_criteria.RequireSeperateTransferQueue && !seperateTransfer) return PhysicalDevice.Suitable.No;

		var requiredExtensionsSupported = Detail.CheckDeviceExtensionSupport(pd._availableExtensions, _criteria.RequiredExtensions).ToArray();
		if(requiredExtensionsSupported.Length != _criteria.RequiredExtensions.Count) {
			return PhysicalDevice.Suitable.No;
		}

		if(!_criteria.DeferSurfaceInitialization && _criteria.RequirePresent) 
		{
			var formatsRet = Detail.GetVector(out VkSurfaceFormatKHR[] formats, (p1, p2) => localApi.vkGetPhysicalDeviceSurfaceFormatsKHR(localPd, localPd.Surface, (uint*)p1, (VkSurfaceFormatKHR*)p2));
			var presentModesRet = Detail.GetVector(out VkPresentModeKHR[] presentModes, (p1, p2) => localApi.vkGetPhysicalDeviceSurfacePresentModesKHR(localPd, localPd.Surface, (uint*)p1, (VkPresentModeKHR*)p2));
		
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
			if((pd.MemoryProperties.memoryHeaps[i].flags & VkMemoryHeapFlags.DeviceLocal) != 0) {
				if(pd.MemoryProperties.memoryHeaps[i].size < _criteria.RequiredMemSize) {
					return PhysicalDevice.Suitable.No;
				}
			}
		}

		return suitable;
	}

	private unsafe Result<List<PhysicalDevice>> SelectImpl() {
		if(_criteria.RequirePresent && !_criteria.DeferSurfaceInitialization) {
			if(_instanceInfo.Surface.IsNull) {
				return Result.FromException<List<PhysicalDevice>>(new PhysicalDeviceException(PhysicalDeviceError.NoSurfaceProvided));
			}
		}

		VkInstance instance = _instanceInfo.Instance;
		var instanceApi = _instanceInfo.Instance.InstanceApi;
		var vkPhysicalDevicesRet = Detail.GetVector(out VkPhysicalDevice[] vkPhysicalDevices, (p1, p2) => instanceApi.vkEnumeratePhysicalDevices(instance, (uint*)p1, (VkPhysicalDevice*)p2));
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
				if(self._criteria.EnablePortabilitySubset && item == VK_KHR_PORTABILITY_SUBSET_EXTENSION_NAME) 
				{
					portabilityExtAvailable = true;
				}
			}

			physDev._extensionsToEnable = self._criteria.RequiredExtensions.ToList();
			if(portabilityExtAvailable) {
				physDev._extensionsToEnable.Add(VK_KHR_PORTABILITY_SUBSET_EXTENSION_NAME);
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
		public Instance Instance;
		public VkSurfaceKHR Surface;
		public VkVersion Version;
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

		public List<VkUtf8String> RequiredExtensions = [];

		public VkVersion RequiredVersion = VkVersion.Version_1_0;

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
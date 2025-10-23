using System.Runtime.InteropServices;
using System.Text;
using DotNext;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

public unsafe ref struct InstanceBuilder {
	public ref InstanceBuilder SetAppName(string appName) {
		_info.AppName = appName;
		return ref this;
	}
	public ref InstanceBuilder SetEngineName(string engineName) {
		_info.EngineName = engineName;
		return ref this;
	}

	public ref InstanceBuilder SetAppVersion(uint version) {
		_info.AppVersion = new(version);
		return ref this;
	}
	public ref InstanceBuilder SetAppVersion(uint major, uint minor, uint patch = 0) {
		_info.AppVersion = new VkVersion(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder SetAppVersion(VkVersion version) {
		_info.AppVersion = version;
		return ref this;
	}

	public ref InstanceBuilder SetEngineVersion(uint version) {
		_info.EngineVersion = new(version);
		return ref this;
	}
	public ref InstanceBuilder SetEngineVersion(uint major, uint minor, uint patch = 0) {
		_info.EngineVersion = new VkVersion(0, major, minor, patch);
		return ref this;
	}
	
	public ref InstanceBuilder SetEngineVersion(VkVersion version) {
		_info.EngineVersion = version;
		return ref this;
	}

	public ref InstanceBuilder RequireApiVersion(uint version) {
		_info.RequiredApiVersion = new(version);
		return ref this;
	}
	public ref InstanceBuilder RequireApiVersion(uint major, uint minor, uint patch = 0) {
		_info.RequiredApiVersion = new VkVersion(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder RequireApiVersion(VkVersion version) {
		_info.RequiredApiVersion = version;
		return ref this;
	}

	public ref InstanceBuilder SetMinimumInstanceVersion(uint version) {
		_info.MinimumInstanceVersion = new(version);
		return ref this;
	}
	public ref InstanceBuilder SetMinimumInstanceVersion(uint major, uint minor, uint patch = 0) {
		_info.MinimumInstanceVersion = new VkVersion(0, major, minor, patch);
		return ref this;
	}
  
	public ref InstanceBuilder SetMinimumInstanceVersion(VkVersion version) {
		_info.MinimumInstanceVersion = version;
		return ref this;
	}

	public ref InstanceBuilder EnableLayer(VkUtf8String layerName) {
		_info.Layers.Add(layerName);
		return ref this;
	}
	public ref InstanceBuilder EnableExtension(VkUtf8String extensionName) {
		_info.Extensions.Add(extensionName);
		return ref this;
	}
	public ref InstanceBuilder EnableExtensions(IEnumerable<VkUtf8String> extensions) {
		_info.Extensions.AddRange(extensions);
		return ref this;
	}

	public ref InstanceBuilder SetHeadless(bool headless = true) {
		_info.HeadlessContext = headless;
		return ref this;
	}

	public ref InstanceBuilder EnableValidationLayers(bool requireValidation = true) {
		_info.EnableValidationLayers = requireValidation;
		return ref this;
	}
	public ref InstanceBuilder RequestValidationLayers(bool enableValidation = true) {
		_info.RequestValidationLayers = enableValidation;
		return ref this;
	}

	public ref InstanceBuilder UseDefaultDebugMessenger() {
		_info.UseDebugMessenger = true;
		_info.DebugCallback = &DebugUtility.DefaultDebugCallback;
		return ref this;
	}
	public ref InstanceBuilder SetDebugCallback(delegate* unmanaged<VkDebugUtilsMessageSeverityFlagsEXT, VkDebugUtilsMessageTypeFlagsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, uint> callback) {
		_info.UseDebugMessenger = true;
		_info.DebugCallback = callback;
		return ref this;
	}
	public ref InstanceBuilder SetDebugCallbackUserDataPointer(void* userDataPointer) {
		_info.DebugUserDataPointer = userDataPointer;
		return ref this;

	}
	public ref InstanceBuilder SetDebugMessengerSeverity(VkDebugUtilsMessageSeverityFlagsEXT severity) {
		_info.DebugMessageSeverity = severity;
		return ref this;
	}
	public ref InstanceBuilder AddDebugMessengerSeverity(VkDebugUtilsMessageSeverityFlagsEXT severity) {
		_info.DebugMessageSeverity |= severity;
		return ref this;
	}
	public ref InstanceBuilder SetDebugMessengerType(VkDebugUtilsMessageTypeFlagsEXT type) {
		_info.DebugMessageType = type;
		return ref this;
	}
	public ref InstanceBuilder AddDebugMessengerType(VkDebugUtilsMessageTypeFlagsEXT type) {
		_info.DebugMessageType |= type;
		return ref this;
	}

	public ref InstanceBuilder AddValidationDisable(VkValidationCheckEXT check) {
		_info.DisabledValidationChecks.Add(check);
		return ref this;
	}

	public ref InstanceBuilder AddValidationFeatureEnable(VkValidationFeatureEnableEXT enable) {
		_info.EnabledValidationFeatures.Add(enable);
		return ref this;
	}
	public ref InstanceBuilder AddValidationFeatureDisable(VkValidationFeatureDisableEXT disable) {
		_info.DisabledValidationFeatures.Add(disable);
		return ref this;
	}

	public ref InstanceBuilder SetAllocationCallbacks(VkAllocationCallbacks* callbacks) {
		_info.AllocationCallbacks = callbacks;
		return ref this;
	}

	public readonly Result<Instance> Build() {
		var sysInfoRet = SystemInfo.GetSystemInfo();
		if(!sysInfoRet.IsSuccessful) {
			return Result.FromException<Instance>(sysInfoRet.Error);
		}
		var system = sysInfoRet.Value;

		VkVersion instanceVersion = VkVersion.Version_1_0;

    	if(_info.MinimumInstanceVersion > VkVersion.Version_1_0 || _info.RequiredApiVersion > VkVersion.Version_1_0)
		{
			uint temp = 0;
			VkResult res = vkEnumerateInstanceVersion(&temp);
      		instanceVersion = new(temp);
			if(res != VkResult.Success && (_info.RequiredApiVersion > 0 || _info.MinimumInstanceVersion > 0)) 
			{
				return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersionUnavailable));
			}
      
			if (vkEnumerateInstanceVersion_ptr == null || (_info.MinimumInstanceVersion > 0 && instanceVersion < _info.MinimumInstanceVersion) || (_info.MinimumInstanceVersion == 0 && instanceVersion < _info.RequiredApiVersion)) 
			{
				VkVersion version_error = _info.MinimumInstanceVersion == 0 ? _info.RequiredApiVersion : _info.MinimumInstanceVersion;
				if (version_error.Minor == 4)
					return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersion14Unavailable));
				else if (version_error.Minor == 3)
					return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersion13Unavailable));
				else if (version_error.Minor == 2)
					return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersion12Unavailable));
				else if (version_error.Minor == 1)
					return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersion11Unavailable));
				else
					return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersionUnavailable));
			}
		}

		VkVersion apiVersion = instanceVersion < VkVersion.Version_1_1 ? instanceVersion : _info.RequiredApiVersion;

		VkUtf8ReadOnlyString appName = Encoding.UTF8.GetBytes(_info.AppName ?? "");
		VkUtf8ReadOnlyString engineName = Encoding.UTF8.GetBytes(_info.EngineName ?? "");

		VkApplicationInfo appInfo = new() {
			pApplicationName = appName,
			applicationVersion = _info.AppVersion,
			pEngineName = engineName,
			engineVersion = _info.EngineVersion,
			apiVersion = apiVersion
		};

		List<VkUtf8String> extensions = _info.Extensions.ToList();
		List<VkUtf8String> layers = _info.Layers.ToList();

		if(_info.DebugCallback != null && _info.UseDebugMessenger && system.DebugUtilsAvailable) {
			extensions.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
		}

		bool properties2ExtEnabled = apiVersion < VkVersion.Version_1_1 && Detail.CheckExtensionSupported(system.AvailableExtensions, VK_KHR_PORTABILITY_ENUMERATION_EXTENSION_NAME);
		if(properties2ExtEnabled) {
			extensions.Add(VK_KHR_GET_PHYSICAL_DEVICE_PROPERTIES_2_EXTENSION_NAME);
		}

		bool portabilityEnumerationSupport = Detail.CheckExtensionSupported(system.AvailableExtensions, VK_KHR_PORTABILITY_ENUMERATION_EXTENSION_NAME);
		if(portabilityEnumerationSupport) {
			extensions.Add(VK_KHR_PORTABILITY_ENUMERATION_EXTENSION_NAME);
		}

		if(!_info.HeadlessContext) {
			bool CheckAddWindowExt(VkUtf8String name) {
				if(!Detail.CheckExtensionSupported(system.AvailableExtensions, name)) {
					return false;
				}
				extensions.Add(name);
				return true;
			}
			bool khrSurfaceAdded = CheckAddWindowExt("VK_KHR_surface"u8);
			bool addedWindowExts = false;
			//TODO: Detect android & direct2display
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				addedWindowExts = CheckAddWindowExt("VK_KHR_win32_surface"u8);
			} else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
				// Make sure all three calls to check_add_window_ext, don't allow short circuiting
				addedWindowExts = CheckAddWindowExt("VK_KHR_xcb_surface"u8);
				addedWindowExts = CheckAddWindowExt("VK_KHR_xlib_surface"u8) || addedWindowExts;
				addedWindowExts = CheckAddWindowExt("VK_KHR_wayland_surface"u8) || addedWindowExts;
			} else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				addedWindowExts = CheckAddWindowExt("VK_EXT_metal_surface"u8);
			}

			if(!khrSurfaceAdded || !addedWindowExts) {
				return Result.FromException<Instance>(new InstanceException(InstanceError.WindowingExtensionsNotPresent));
			}
		}
		bool allExtensionsSupported = Detail.CheckExtensionsSupported(system.AvailableExtensions, extensions);
		if(!allExtensionsSupported) {
			return Result.FromException<Instance>(new InstanceException(InstanceError.RequestedExtensionsNotPresent));
		}

		if(_info.EnableValidationLayers || (_info.RequestValidationLayers && system.ValidationLayersAvailable)) {
			layers.Add(Detail.ValidationLayerName);
		}
		bool allLayersSupported = Detail.CheckLayersSupported(system.AvailableLayers, layers);
		if(!allLayersSupported) {
			return Result.FromException<Instance>(new InstanceException(InstanceError.RequestedLayersNotPresent));
		}

		List<nint> pNextChain = [];
		List<GCHandle> miscHandles = [];
		try {
			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = new();
			if(_info.UseDebugMessenger) {
				messengerCreateInfo.messageSeverity = _info.DebugMessageSeverity;
				messengerCreateInfo.messageType = _info.DebugMessageType;
				messengerCreateInfo.pfnUserCallback = _info.DebugCallback;
				messengerCreateInfo.pUserData = _info.DebugUserDataPointer;
				//pNextChain.Add(GCHandle.Alloc(messengerCreateInfo, GCHandleType.Pinned));
				pNextChain.Add((nint)(&messengerCreateInfo));
			}

			VkValidationFeaturesEXT features = new();
			if(_info.EnabledValidationFeatures.Count != 0 || _info.DisabledValidationFeatures.Count != 0) {
				features.enabledValidationFeatureCount = (uint)_info.EnabledValidationFeatures.Count;
				VkValidationFeatureEnableEXT[] enabledValidationFeatures = _info.EnabledValidationFeatures.ToArray();
				GCHandle pEnabledValidationFeatures = GCHandle.Alloc(enabledValidationFeatures, GCHandleType.Pinned);
				miscHandles.Add(pEnabledValidationFeatures);
				features.pEnabledValidationFeatures = (VkValidationFeatureEnableEXT*)pEnabledValidationFeatures.AddrOfPinnedObject();

				features.disabledValidationFeatureCount = (uint)_info.DisabledValidationFeatures.Count;
				VkValidationFeatureDisableEXT[] disabledValidationFeatures = _info.DisabledValidationFeatures.ToArray();
				GCHandle pDisabledValidationFeatures = GCHandle.Alloc(disabledValidationFeatures, GCHandleType.Pinned);
				miscHandles.Add(pDisabledValidationFeatures);
				features.pDisabledValidationFeatures = (VkValidationFeatureDisableEXT*)pDisabledValidationFeatures.AddrOfPinnedObject();

				//pNextChain.Add(GCHandle.Alloc(features, GCHandleType.Pinned));
				pNextChain.Add((nint)(&features));
			}

			VkValidationFlagsEXT checks = new();
			if(_info.DisabledValidationChecks.Count != 0) {
				checks.disabledValidationCheckCount = (uint)_info.DisabledValidationChecks.Count;
				VkValidationCheckEXT[] disabledValidationChecks = _info.DisabledValidationChecks.ToArray();
				GCHandle pDisabledValidationChecks = GCHandle.Alloc(disabledValidationChecks, GCHandleType.Pinned);
				miscHandles.Add(pDisabledValidationChecks);
				checks.pDisabledValidationChecks = (VkValidationCheckEXT*)pDisabledValidationChecks.AddrOfPinnedObject();
				pNextChain.Add((nint)(&checks));
			}

			return BuildRestOfIt(instanceVersion, apiVersion, appInfo, extensions, layers, properties2ExtEnabled, portabilityEnumerationSupport, pNextChain, miscHandles);
		} finally {
			//foreach(var item in pNextChain) {
				//item.Free();
			//}
			foreach(var item in miscHandles) {
				item.Free();
			}
		}
	}

	// Make this part of Build into a seperate method so C# doesnt complain about getting a pointer to instanceVersion
	// (Not neccessary anymore)
	private readonly Result<Instance> BuildRestOfIt(VkVersion instanceVersion, VkVersion apiVersion, VkApplicationInfo appInfo, List<VkUtf8String> extensions, List<VkUtf8String> layers, bool properties2ExtEnabled, bool portabilityEnumerationSupport, List<nint> pNextChain, List<GCHandle> miscHandles) {
		VkInstanceCreateInfo instanceCreateInfo = new();
		InstanceInfo info = _info;
		Detail.SetupPNextChain(&instanceCreateInfo, pNextChain);
		instanceCreateInfo.flags = info.flags;

		GCHandle pApplicationInfo = GCHandle.Alloc(appInfo, GCHandleType.Pinned);
		miscHandles.Add(pApplicationInfo);
		instanceCreateInfo.pApplicationInfo = (VkApplicationInfo*)pApplicationInfo.AddrOfPinnedObject();

		instanceCreateInfo.enabledExtensionCount = (uint)extensions.Count;
		using var ppEnabledExtensionNames = new VkStringArray(extensions);
		instanceCreateInfo.ppEnabledExtensionNames = ppEnabledExtensionNames;

		instanceCreateInfo.enabledLayerCount = (uint)layers.Count;
    	using var ppEnabledLayerNames = new VkStringArray(layers);
		instanceCreateInfo.ppEnabledLayerNames = ppEnabledLayerNames;

		if(portabilityEnumerationSupport) {
			instanceCreateInfo.flags |= VkInstanceCreateFlags.EnumeratePortabilityKHR;
		}

		Instance instance = new();
		VkInstanceCreateInfo localInstanceCreateInfo = instanceCreateInfo;
		VkResult res = vkCreateInstance(&localInstanceCreateInfo, info.AllocationCallbacks, &instance.VkInstance);
		if(res != VkResult.Success) {
			return Result.FromException<Instance>(new InstanceException(InstanceError.FailedCreateInstance, new VkException(res)));
		}

		instance.InstanceApi = GetApi(instance.VkInstance);

		if(info.UseDebugMessenger) {
			res = Detail.CreateDebugUtilsMessenger(instance,
				info.DebugCallback,
				info.DebugMessageSeverity,
				info.DebugMessageType,
				info.DebugUserDataPointer,
				&instance.DebugMessenger,
				info.AllocationCallbacks);
			if(res != VkResult.Success) {
				return Result.FromException<Instance>(new InstanceException(InstanceError.FailedCreateDebugMessenger, new VkException(res)));
			}
		}

		instance._headless = info.HeadlessContext;
		instance._properties2ExtEnabled = properties2ExtEnabled;
		instance.AllocationCallbacks = info.AllocationCallbacks;
		instance.InstanceVersion = instanceVersion;
		instance.ApiVersion = apiVersion;

		return Result.FromValue(instance);
	}

	private InstanceInfo _info = new();

	public InstanceBuilder() {
	}

	private unsafe struct InstanceInfo {
		public string? AppName;
		public string? EngineName;
		public VkVersion AppVersion;
		public VkVersion EngineVersion;
		public VkVersion MinimumInstanceVersion;
		public VkVersion RequiredApiVersion;

		public List<VkUtf8String> Layers = [];
		public List<VkUtf8String> Extensions = [];
		public VkInstanceCreateFlags flags;
		public List<nint> pNextElements = [];

		public delegate* unmanaged<VkDebugUtilsMessageSeverityFlagsEXT, VkDebugUtilsMessageTypeFlagsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, uint> DebugCallback = &DebugUtility.DefaultDebugCallback;
		public VkDebugUtilsMessageSeverityFlagsEXT DebugMessageSeverity =
			VkDebugUtilsMessageSeverityFlagsEXT.Warning | VkDebugUtilsMessageSeverityFlagsEXT.Error;
		public VkDebugUtilsMessageTypeFlagsEXT DebugMessageType =
			VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;

		public void* DebugUserDataPointer;

		public List<VkValidationCheckEXT> DisabledValidationChecks = [];
		public List<VkValidationFeatureEnableEXT> EnabledValidationFeatures = [];
		public List<VkValidationFeatureDisableEXT> DisabledValidationFeatures = [];

		public VkAllocationCallbacks* AllocationCallbacks;

		public bool RequestValidationLayers;
		public bool EnableValidationLayers;
		public bool UseDebugMessenger;
		public bool HeadlessContext;

		public InstanceInfo() {
		}
	}
}

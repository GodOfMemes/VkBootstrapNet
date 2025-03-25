using System.Runtime.InteropServices;
using DotNext;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

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
		_info.AppVersion = version;
		return ref this;
	}
	public ref InstanceBuilder SetAppVersion(uint major, uint minor, uint patch = 0) {
		_info.AppVersion = Vk.MAKE_API_VERSION(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder SetEngineVersion(uint version) {
		_info.EngineVersion = version;
		return ref this;
	}
	public ref InstanceBuilder SetEngineVersion(uint major, uint minor, uint patch = 0) {
		_info.EngineVersion = Vk.MAKE_API_VERSION(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder RequireApiVersion(uint version) {
		_info.RequiredApiVersion = version;
		return ref this;
	}
	public ref InstanceBuilder RequireApiVersion(uint major, uint minor, uint patch = 0) {
		_info.RequiredApiVersion = Vk.MAKE_API_VERSION(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder SetMinimumInstanceVersion(uint version) {
		_info.MinimumInstanceVersion = version;
		return ref this;
	}
	public ref InstanceBuilder SetMinimumInstanceVersion(uint major, uint minor, uint patch = 0) {
		_info.MinimumInstanceVersion = Vk.MAKE_API_VERSION(0, major, minor, patch);
		return ref this;
	}

	public ref InstanceBuilder EnableLayer(string layerName) {
		_info.Layers.Add(layerName);
		return ref this;
	}
	public ref InstanceBuilder EnableExtension(string extensionName) {
		_info.Extensions.Add(extensionName);
		return ref this;
	}
	public ref InstanceBuilder EnableExtensions(IEnumerable<string> extensions) {
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
	public ref InstanceBuilder SetDebugCallback(delegate* unmanaged[Cdecl]<VkDebugUtilsMessageSeverityFlagBitsEXT, VkDebugUtilsMessageTypeFlagBitsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, int> callback) {
		_info.UseDebugMessenger = true;
		_info.DebugCallback = callback;
		return ref this;
	}
	public ref InstanceBuilder SetDebugCallbackUserDataPointer(void* userDataPointer) {
		_info.DebugUserDataPointer = userDataPointer;
		return ref this;

	}
	public ref InstanceBuilder SetDebugMessengerSeverity(VkDebugUtilsMessageSeverityFlagBitsEXT severity) {
		_info.DebugMessageSeverity = severity;
		return ref this;
	}
	public ref InstanceBuilder AddDebugMessengerSeverity(VkDebugUtilsMessageSeverityFlagBitsEXT severity) {
		_info.DebugMessageSeverity |= severity;
		return ref this;
	}
	public ref InstanceBuilder SetDebugMessengerType(VkDebugUtilsMessageTypeFlagBitsEXT type) {
		_info.DebugMessageType = type;
		return ref this;
	}
	public ref InstanceBuilder AddDebugMessengerType(VkDebugUtilsMessageTypeFlagBitsEXT type) {
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

		uint instanceVersion = Vk.VK_API_VERSION_1_0;

		if(_info.MinimumInstanceVersion > Vk.VK_API_VERSION_1_0 || _info.RequiredApiVersion > Vk.VK_API_VERSION_1_0) {
			VkResult res = Vk.EnumerateInstanceVersion(&instanceVersion);
			if(res != VkResult.Success && _info.RequiredApiVersion > 0) {
				return Result.FromException<Instance>(new InstanceException(InstanceError.VulkanVersionUnavailable));
			}
		}

		uint apiVersion = instanceVersion < Vk.VK_API_VERSION_1_1 ? instanceVersion : _info.RequiredApiVersion;

		using NativeString appName = (NativeString)(_info.AppName ?? "");
		using NativeString engineName = (NativeString)(_info.EngineName ?? "");

		VkApplicationInfo appInfo = new() {
			pApplicationName = (byte*)appName.Address,
			applicationVersion = _info.AppVersion,
			pEngineName = (byte*)engineName.Address,
			engineVersion = _info.EngineVersion,
			apiVersion = apiVersion
		};

		List<string> extensions = _info.Extensions.ToList();
		List<string> layers = _info.Layers.ToList();

		if(_info.DebugCallback != null && _info.UseDebugMessenger && system.DebugUtilsAvailable) {
			extensions.Add(Vk.ExtDebugUtilsExtensionName);
		}

		bool properties2ExtEnabled = apiVersion < Vk.VK_API_VERSION_1_1 && Detail.CheckExtensionSupported(system.AvailableExtensions, Vk.KhrPortabilityEnumerationExtensionName);
		if(properties2ExtEnabled) {
			extensions.Add(Vk.KhrGetPhysicalDeviceProperties2ExtensionName);
		}

		bool portabilityEnumerationSupport = Detail.CheckExtensionSupported(system.AvailableExtensions, Vk.KhrPortabilityEnumerationExtensionName);
		if(portabilityEnumerationSupport) {
			extensions.Add(Vk.KhrPortabilityEnumerationExtensionName);
		}

		if(!_info.HeadlessContext) {
			bool CheckAddWindowExt(string name) {
				if(!Detail.CheckExtensionSupported(system.AvailableExtensions, name)) {
					return false;
				}
				extensions.Add(name);
				return true;
			}
			bool khrSurfaceAdded = CheckAddWindowExt("VK_KHR_surface");
			bool addedWindowExts = false;
			//TODO: Detect android & direct2display
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				addedWindowExts = CheckAddWindowExt("VK_KHR_win32_surface");
			} else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
				// Make sure all three calls to check_add_window_ext, don't allow short circuiting
				addedWindowExts = CheckAddWindowExt("VK_KHR_xcb_surface");
				addedWindowExts = CheckAddWindowExt("VK_KHR_xlib_surface") || addedWindowExts;
				addedWindowExts = CheckAddWindowExt("VK_KHR_wayland_surface") || addedWindowExts;
			} else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				addedWindowExts = CheckAddWindowExt("VK_EXT_metal_surface");
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
	private readonly Result<Instance> BuildRestOfIt(uint instanceVersion, uint apiVersion, VkApplicationInfo appInfo, List<string> extensions, List<string> layers, bool properties2ExtEnabled, bool portabilityEnumerationSupport, List<nint> pNextChain, List<GCHandle> miscHandles) {
		VkInstanceCreateInfo instanceCreateInfo = new();
		InstanceInfo info = _info;
		Detail.SetupPNextChain(&instanceCreateInfo, pNextChain);
		instanceCreateInfo.flags = info.flags;

		GCHandle pApplicationInfo = GCHandle.Alloc(appInfo, GCHandleType.Pinned);
		miscHandles.Add(pApplicationInfo);
		instanceCreateInfo.pApplicationInfo = (VkApplicationInfo*)pApplicationInfo.AddrOfPinnedObject();

		instanceCreateInfo.enabledExtensionCount = (uint)extensions.Count;
		using NativeStringArray ppEnabledExtensionNames = NativeStringArray.Create(extensions.ToArray());
		instanceCreateInfo.ppEnabledExtensionNames = (byte**)ppEnabledExtensionNames.Address;

		instanceCreateInfo.enabledLayerCount = (uint)layers.Count;
		using NativeStringArray ppEnabledLayerNames = NativeStringArray.Create(layers.ToArray());
		instanceCreateInfo.ppEnabledLayerNames = (byte**)ppEnabledLayerNames.Address;

		if(portabilityEnumerationSupport) {
			instanceCreateInfo.flags |= VkInstanceCreateFlagBits.InstanceCreateEnumeratePortabilityBitKhr;
		}

		Instance instance = new();
		VkInstanceCreateInfo localInstanceCreateInfo = instanceCreateInfo;
		VkResult res = Vk.CreateInstance(&localInstanceCreateInfo, info.AllocationCallbacks, &instance.VkInstance);
		if(res != VkResult.Success) {
			return Result.FromException<Instance>(new InstanceException(InstanceError.FailedCreateInstance, new VkException(res)));
		}

		VKLoader.SetInstance(instance.VkInstance);

		if(info.UseDebugMessenger) {
			res = Detail.CreateDebugUtilsMessenger(instance.VkInstance,
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
		public uint AppVersion;
		public uint EngineVersion;
		public uint MinimumInstanceVersion;
		public uint RequiredApiVersion;

		public List<string> Layers = [];
		public List<string> Extensions = [];
		public VkInstanceCreateFlagBits flags;
		public List<nint> pNextElements = [];

		public delegate* unmanaged[Cdecl]<VkDebugUtilsMessageSeverityFlagBitsEXT, VkDebugUtilsMessageTypeFlagBitsEXT, VkDebugUtilsMessengerCallbackDataEXT*, void*, int> DebugCallback = &DebugUtility.DefaultDebugCallback;
		public VkDebugUtilsMessageSeverityFlagBitsEXT DebugMessageSeverity =
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;
		public VkDebugUtilsMessageTypeFlagBitsEXT DebugMessageType =
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt | VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt | VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt;

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

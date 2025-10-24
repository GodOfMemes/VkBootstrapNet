using System.Diagnostics;
using DotNext;
using Vortice.Vulkan;

namespace VkBootstrapNet;

public unsafe ref struct SwapchainBuilder {
	public const int SingleBuffering = 1;
	public const int DoubleBuffering = 2;
	public const int TripleBuffering = 3;

	private SwapchainInfo _info = new();

	public SwapchainBuilder(in Device device) {
		_info.PhysicalDevice = device.PhysicalDevice;
		_info.Device = device;
		_info.Surface = device.Surface;
		_info.InstanceVersion = device.InstanceVersion;
		var present = device.GetQueueIndex(QueueType.Present);
		var graphics = device.GetQueueIndex(QueueType.Graphics);
		Debug.Assert(present.IsSuccessful && graphics.IsSuccessful, "Graphics and Present queue indexes must be valid");
		_info.GraphicsQueueIndex = graphics.Value;
		_info.PresentQueueIndex = present.Value;
	}
	public SwapchainBuilder(in Device device, in VkSurfaceKHR surface) {
		_info.PhysicalDevice = device.PhysicalDevice;
		_info.Device = device;
		_info.Surface = surface;
		_info.InstanceVersion = device.InstanceVersion;
		Device tempDevice = device;
		tempDevice.Surface = surface;
		var present = tempDevice.GetQueueIndex(QueueType.Present);
		var graphics = tempDevice.GetQueueIndex(QueueType.Graphics);
		Debug.Assert(present.IsSuccessful && graphics.IsSuccessful, "Graphics and Present queue indexes must be valid");
		_info.GraphicsQueueIndex = graphics.Value;
		_info.PresentQueueIndex = present.Value;
	}
	
	public unsafe SwapchainBuilder(PhysicalDevice physicalDevice, in Device device, in VkSurfaceKHR surface, uint graphicsQueueIndex, uint presentQueueIndex) {
		_info.PhysicalDevice = physicalDevice;
		_info.Device = device;
		_info.Surface = surface;
		_info.GraphicsQueueIndex = graphicsQueueIndex;
		_info.PresentQueueIndex = presentQueueIndex;
		if(graphicsQueueIndex == uint.MaxValue || presentQueueIndex == uint.MaxValue)
		{
			VkInstanceApi api = device.Instance;
			var queueFamilies = Detail.GetVectorNoError<VkQueueFamilyProperties>((p1, p2) => api.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, (uint*)p1, (VkQueueFamilyProperties*)p2));
			if(graphicsQueueIndex == uint.MaxValue) {
				_info.GraphicsQueueIndex = Detail.GetFirstQueueIndex(queueFamilies, VkQueueFlags.Graphics);
			}
			if(presentQueueIndex == uint.MaxValue) {
				_info.PresentQueueIndex = Detail.GetPresentQueueIndex(device.Instance, physicalDevice, surface, queueFamilies);
			}
		}
	}

	public Result<Swapchain> Build() {
		if(_info.Surface.IsNull) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.SurfaceHandleNotProvided));
		}

		var desiredFormats = _info.DesiredFormats;
		if(desiredFormats.Count == 0) {
			AddDesiredFormats(desiredFormats);
		}
		var desiredPresentModes = _info.DesiredPresentModes;
		if(desiredPresentModes.Count == 0) {
			AddDesiredPresentModes(desiredPresentModes);
		}

		var surfaceSupportRet = Detail.QuerySurfaceSupportDetails(_info.Device.Instance,_info.PhysicalDevice, _info.Surface);
		if(!surfaceSupportRet.IsSuccessful) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.FailedQuerySurfaceSupportDetails, surfaceSupportRet.Error!.InnerException));
		}
		var surfaceSupport = surfaceSupportRet.Value;

		uint imageCount;
		if(_info.RequiredMinImageCount >= 1) {
			if(_info.RequiredMinImageCount < surfaceSupport.Capabilities.minImageCount) {
				return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.RequiredMinImageCountTooLow));
			}

			imageCount = _info.RequiredMinImageCount;
		} else if(_info.MinImageCount == 0) {
			imageCount = surfaceSupport.Capabilities.minImageCount + 1;
		} else {
			imageCount = _info.MinImageCount;
			if(imageCount < surfaceSupport.Capabilities.minImageCount) {
				imageCount = surfaceSupport.Capabilities.minImageCount;
			}
		}
		if(surfaceSupport.Capabilities.maxImageCount > 0 && imageCount > surfaceSupport.Capabilities.maxImageCount) {
			imageCount = surfaceSupport.Capabilities.maxImageCount;
		}

		VkSurfaceFormatKHR surfaceFormat = Detail.FindBestSurfaceFormat(surfaceSupport.Formats, desiredFormats.ToArray());

		VkExtent2D extent = Detail.FindExtent(surfaceSupport.Capabilities, _info.DesiredWidth, _info.DesiredHeight);

		uint imageArrayLayers = _info.ArrayLayerCount;
		if(surfaceSupport.Capabilities.maxImageArrayLayers < _info.ArrayLayerCount) {
			imageArrayLayers = surfaceSupport.Capabilities.maxImageArrayLayers;
		}
		if(_info.ArrayLayerCount == 0) {
			imageArrayLayers = 1;
		}

		var queueFamilyIndices = stackalloc uint[] { _info.GraphicsQueueIndex, _info.PresentQueueIndex };

		VkPresentModeKHR presentMode = Detail.FindPresentMode(surfaceSupport.PresentModes, desiredPresentModes.ToArray());

		bool IsUnextendedPresentMode(VkPresentModeKHR presentMode) {
			return presentMode is VkPresentModeKHR.Immediate
				or VkPresentModeKHR.Mailbox
				or VkPresentModeKHR.Fifo
				or VkPresentModeKHR.FifoRelaxed;
		}

		if(IsUnextendedPresentMode(presentMode) && (_info.ImageUsageFlags & surfaceSupport.Capabilities.supportedUsageFlags) != _info.ImageUsageFlags) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.RequiredUsageNotSupported));
		}

		VkSurfaceTransformFlagsKHR preTransform = _info.PreTransform;
		if(_info.PreTransform == 0) {
			preTransform = surfaceSupport.Capabilities.currentTransform;
		}

		VkSwapchainCreateInfoKHR swapchainCreateInfo = new();
		SwapchainInfo info = _info;
		Detail.SetupPNextChain(&swapchainCreateInfo, _info.PNextChain);
		swapchainCreateInfo.flags = info.CreateFlags;
		swapchainCreateInfo.surface = info.Surface;
		swapchainCreateInfo.minImageCount = imageCount;
		swapchainCreateInfo.imageFormat = surfaceFormat.format;
		swapchainCreateInfo.imageColorSpace = surfaceFormat.colorSpace;
		swapchainCreateInfo.imageExtent = extent;
		swapchainCreateInfo.imageArrayLayers = imageArrayLayers;
		swapchainCreateInfo.imageUsage = info.ImageUsageFlags;

		if(info.GraphicsQueueIndex != info.PresentQueueIndex) {
			swapchainCreateInfo.imageSharingMode = VkSharingMode.Concurrent;
			swapchainCreateInfo.queueFamilyIndexCount = 2;
			swapchainCreateInfo.pQueueFamilyIndices = queueFamilyIndices;
		} else {
			swapchainCreateInfo.imageSharingMode = VkSharingMode.Exclusive;
		}

		swapchainCreateInfo.preTransform = preTransform;
		swapchainCreateInfo.compositeAlpha = info.CompositeAlpha;
		swapchainCreateInfo.presentMode = presentMode;
		swapchainCreateInfo.clipped = info.Clipped;
		swapchainCreateInfo.oldSwapchain = info.OldSwapchain;

		Swapchain swapchain = new();
		var res = CreateSwapchain(info.Device, ref swapchainCreateInfo, info, ref swapchain.VkSwapchain);

		if(res != VkResult.Success) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.FailedCreateSwapchain, new VkException(res)));
		}
		swapchain.Device = info.Device;
		swapchain.ImageFormat = surfaceFormat.format;
		swapchain.ColorSpace = surfaceFormat.colorSpace;
		swapchain.ImageUsageFlags = info.ImageUsageFlags;
		swapchain.Extent = extent;
		var images = swapchain.GetImages();
		if(!images.IsSuccessful) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.FailedGetSwapchainImages, images.Error));
		}
		swapchain.RequestedMinImageCount = imageCount;
		swapchain.PresentMode = presentMode;
		swapchain.ImageCount = (uint)images.Value.Length;
		swapchain.InstanceVersion = info.InstanceVersion;
		swapchain.AllocationCallbacks = info.AllocationCallbacks;
		return swapchain;
	}

	private static VkResult CreateSwapchain(VkDeviceApi deviceApi, ref VkSwapchainCreateInfoKHR swapchainCreateInfo, SwapchainInfo info, ref VkSwapchainKHR swapchain) {
		fixed(VkSwapchainCreateInfoKHR* pSwapchainCreateInfo = &swapchainCreateInfo) {
			fixed(VkSwapchainKHR* pSwapchain = &swapchain) {
				return deviceApi.vkCreateSwapchainKHR(info.Device, pSwapchainCreateInfo, info.AllocationCallbacks, pSwapchain);
			}
		}
	}

	public ref SwapchainBuilder SetOldSwapchain(VkSwapchainKHR oldSwapchain) {
		_info.OldSwapchain = oldSwapchain;
		return ref this;
	}
	public ref SwapchainBuilder SetOldSwapchain(in Swapchain swapchain) {
		_info.OldSwapchain = swapchain;
		return ref this;
	}

	public ref SwapchainBuilder SetDesiredExtent(uint width, uint height) {
		_info.DesiredWidth = width;
		_info.DesiredHeight = height;
		return ref this;
	}

	public ref SwapchainBuilder SetDesiredFormat(VkSurfaceFormatKHR format) {
		_info.DesiredFormats.Insert(0, format);
		return ref this;
	}
	public ref SwapchainBuilder AddFallbackFormat(VkSurfaceFormatKHR format) {
		_info.DesiredFormats.Add(format);
		return ref this;
	}
	public ref SwapchainBuilder UseDefaultFormatSelection() {
		_info.DesiredFormats.Clear();
		AddDesiredFormats(_info.DesiredFormats);
		return ref this;
	}

	public ref SwapchainBuilder SetDesiredPresentMode(VkPresentModeKHR presentMode) {
		_info.DesiredPresentModes.Insert(0, presentMode);
		return ref this;
	}
	public ref SwapchainBuilder AddFallbackPresentMode(VkPresentModeKHR presentMode) {
		_info.DesiredPresentModes.Add(presentMode);
		return ref this;
	}
	public ref SwapchainBuilder UseDefaultPresentModeSelection() {
		_info.DesiredPresentModes.Clear();
		AddDesiredPresentModes(_info.DesiredPresentModes);
		return ref this;
	}

	public ref SwapchainBuilder SetImageUsageFlags(VkImageUsageFlags usageFlags) {
		_info.ImageUsageFlags = usageFlags;
		return ref this;
	}
	public ref SwapchainBuilder AddImageUsageFlags(VkImageUsageFlags usageFlags) {
		_info.ImageUsageFlags |= usageFlags;
		return ref this;
	}
	public ref SwapchainBuilder UseDefaultImageUseFlags() {
		_info.ImageUsageFlags = VkImageUsageFlags.ColorAttachment;
		return ref this;
	}

	public ref SwapchainBuilder SetImageArrayLayerCount(uint arrayLayerCount) {
		_info.ArrayLayerCount = arrayLayerCount;
		return ref this;
	}

	public ref SwapchainBuilder SetDesiredMinImageCount(uint minImageCount) {
		_info.MinImageCount = minImageCount;
		return ref this;
	}

	public ref SwapchainBuilder SetRequiredMinImageCount(uint requiredMinImageCount) {
		_info.RequiredMinImageCount = requiredMinImageCount;
		return ref this;
	}

	public ref SwapchainBuilder SetClipped(bool clipped = true) {
		_info.Clipped = clipped;
		return ref this;
	}

	public ref SwapchainBuilder SetCreateFlags(VkSwapchainCreateFlagsKHR createFlags) {
		_info.CreateFlags = createFlags;
		return ref this;
	}
	public ref SwapchainBuilder SetPreTransformFlags(VkSurfaceTransformFlagsKHR preTransformFlags) {
		_info.PreTransform = preTransformFlags;
		return ref this;
	}
	public ref SwapchainBuilder SetCompositeAlphaFlags(VkCompositeAlphaFlagsKHR compositeAlphaFlags) {
		_info.CompositeAlpha = compositeAlphaFlags;
		return ref this;
	}

	public unsafe ref SwapchainBuilder AddPNext<T>(T* structure) where T : unmanaged {
		_info.PNextChain.Add((nint)structure);
		return ref this;
	}

	private static void AddDesiredFormats(List<VkSurfaceFormatKHR> formats) {
		formats.Add(new VkSurfaceFormatKHR(VkFormat.B8G8R8A8Srgb, VkColorSpaceKHR.SrgbNonLinear));
		formats.Add(new VkSurfaceFormatKHR(VkFormat.R8G8B8A8Srgb, VkColorSpaceKHR.SrgbNonLinear));
	}
	private static void AddDesiredPresentModes(List<VkPresentModeKHR> presentModes) {
		presentModes.Add(VkPresentModeKHR.Mailbox);
		presentModes.Add(VkPresentModeKHR.Fifo);
	}

	private unsafe struct SwapchainInfo {
		public VkPhysicalDevice PhysicalDevice;
		public Device Device;
		public List<nint> PNextChain = [];
		public VkSwapchainCreateFlagsKHR CreateFlags;
		public VkSurfaceKHR Surface;
		public List<VkSurfaceFormatKHR> DesiredFormats = [];
		public VkVersion InstanceVersion = VkVersion.Version_1_0;
		public uint DesiredWidth;
		public uint DesiredHeight;
		public uint ArrayLayerCount;
		public uint MinImageCount;
		public uint RequiredMinImageCount;
		public VkImageUsageFlags ImageUsageFlags = VkImageUsageFlags.ColorAttachment;
		public uint GraphicsQueueIndex;
		public uint PresentQueueIndex;
		public VkSurfaceTransformFlagsKHR PreTransform;
		public VkCompositeAlphaFlagsKHR CompositeAlpha = VkCompositeAlphaFlagsKHR.Opaque;
		public List<VkPresentModeKHR> DesiredPresentModes = [];
		public bool Clipped = true;
		public VkSwapchainKHR OldSwapchain;
		public VkAllocationCallbacks* AllocationCallbacks;

		public SwapchainInfo() {
		}
	}
}
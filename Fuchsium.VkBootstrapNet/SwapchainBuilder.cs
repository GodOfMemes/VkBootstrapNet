using System.Diagnostics;
using DotNext;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

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
	public unsafe SwapchainBuilder(VkPhysicalDevice physicalDevice, in VkDevice device, in VkSurfaceKHR surface, uint graphicsQueueIndex, uint presentQueueIndex) {
		_info.PhysicalDevice = physicalDevice;
		_info.Device = device;
		_info.Surface = surface;
		_info.GraphicsQueueIndex = graphicsQueueIndex;
		_info.PresentQueueIndex = presentQueueIndex;
		if(graphicsQueueIndex == uint.MaxValue || presentQueueIndex == uint.MaxValue) {
			var queueFamilies = Detail.GetVectorNoError<VkQueueFamilyProperties>((p1, p2) => Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, (uint*)p1, (VkQueueFamilyProperties*)p2));
			if(graphicsQueueIndex == uint.MaxValue) {
				_info.GraphicsQueueIndex = Detail.GetFirstQueueIndex(queueFamilies, VkQueueFlagBits.QueueGraphicsBit);
			}
			if(presentQueueIndex == uint.MaxValue) {
				_info.PresentQueueIndex = Detail.GetPresentQueueIndex(physicalDevice, surface, queueFamilies);
			}
		}
	}

	public Result<Swapchain> Build() {
		if(_info.Surface == default) {
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

		var surfaceSupportRet = Detail.QuerySurfaceSupportDetails(_info.PhysicalDevice, _info.Surface);
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
			return presentMode is VkPresentModeKHR.PresentModeImmediateKhr
				or VkPresentModeKHR.PresentModeMailboxKhr
				or VkPresentModeKHR.PresentModeFifoKhr
				or VkPresentModeKHR.PresentModeFifoRelaxedKhr;
		}

		if(IsUnextendedPresentMode(presentMode) && (_info.ImageUsageFlags & surfaceSupport.Capabilities.supportedUsageFlags) != _info.ImageUsageFlags) {
			return Result.FromException<Swapchain>(new SwapchainException(SwapchainError.RequiredUsageNotSupported));
		}

		VkSurfaceTransformFlagBitsKHR preTransform = _info.PreTransform;
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
			swapchainCreateInfo.imageSharingMode = VkSharingMode.SharingModeConcurrent;
			swapchainCreateInfo.queueFamilyIndexCount = 2;
			swapchainCreateInfo.pQueueFamilyIndices = queueFamilyIndices;
		} else {
			swapchainCreateInfo.imageSharingMode = VkSharingMode.SharingModeExclusive;
		}

		swapchainCreateInfo.preTransform = preTransform;
		swapchainCreateInfo.compositeAlpha = info.CompositeAlpha;
		swapchainCreateInfo.presentMode = presentMode;
		swapchainCreateInfo.clipped = info.Clipped ? 1 : 0;
		swapchainCreateInfo.oldSwapchain = info.OldSwapchain;

		Swapchain swapchain = new();
		var res = CreateSwapchain(ref swapchainCreateInfo, info, ref swapchain.VkSwapchain);

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

	private static VkResult CreateSwapchain(ref VkSwapchainCreateInfoKHR swapchainCreateInfo, SwapchainInfo info, ref VkSwapchainKHR swapchain) {
		fixed(VkSwapchainCreateInfoKHR* pSwapchainCreateInfo = &swapchainCreateInfo) {
			fixed(VkSwapchainKHR* pSwapchain = &swapchain) {
				return Vk.CreateSwapchainKHR(info.Device, pSwapchainCreateInfo, info.AllocationCallbacks, pSwapchain);
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

	public ref SwapchainBuilder SetImageUsageFlags(VkImageUsageFlagBits usageFlags) {
		_info.ImageUsageFlags = usageFlags;
		return ref this;
	}
	public ref SwapchainBuilder AddImageUsageFlags(VkImageUsageFlagBits usageFlags) {
		_info.ImageUsageFlags |= usageFlags;
		return ref this;
	}
	public ref SwapchainBuilder UseDefaultImageUseFlags() {
		_info.ImageUsageFlags = VkImageUsageFlagBits.ImageUsageColorAttachmentBit;
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

	public ref SwapchainBuilder SetCreateFlags(VkSwapchainCreateFlagBitsKHR createFlags) {
		_info.CreateFlags = createFlags;
		return ref this;
	}
	public ref SwapchainBuilder SetPreTransformFlags(VkSurfaceTransformFlagBitsKHR preTransformFlags) {
		_info.PreTransform = preTransformFlags;
		return ref this;
	}
	public ref SwapchainBuilder SetCompositeAlphaFlags(VkCompositeAlphaFlagBitsKHR compositeAlphaFlags) {
		_info.CompositeAlpha = compositeAlphaFlags;
		return ref this;
	}

	public unsafe ref SwapchainBuilder AddPNext<T>(T* structure) where T : unmanaged {
		_info.PNextChain.Add((nint)structure);
		return ref this;
	}

	private static void AddDesiredFormats(List<VkSurfaceFormatKHR> formats) {
		formats.Add(new VkSurfaceFormatKHR(VkFormat.FormatB8g8r8a8Srgb, VkColorSpaceKHR.ColorspaceSrgbNonlinearKhr));
		formats.Add(new VkSurfaceFormatKHR(VkFormat.FormatR8g8b8a8Srgb, VkColorSpaceKHR.ColorspaceSrgbNonlinearKhr));
	}
	private static void AddDesiredPresentModes(List<VkPresentModeKHR> presentModes) {
		presentModes.Add(VkPresentModeKHR.PresentModeMailboxKhr);
		presentModes.Add(VkPresentModeKHR.PresentModeFifoKhr);
	}

	private unsafe struct SwapchainInfo {
		public VkPhysicalDevice PhysicalDevice;
		public VkDevice Device;
		public List<nint> PNextChain = [];
		public VkSwapchainCreateFlagBitsKHR CreateFlags;
		public VkSurfaceKHR Surface;
		public List<VkSurfaceFormatKHR> DesiredFormats = [];
		public uint InstanceVersion = Vk.VK_API_VERSION_1_0;
		public uint DesiredWidth;
		public uint DesiredHeight;
		public uint ArrayLayerCount;
		public uint MinImageCount;
		public uint RequiredMinImageCount;
		public VkImageUsageFlagBits ImageUsageFlags = VkImageUsageFlagBits.ImageUsageColorAttachmentBit;
		public uint GraphicsQueueIndex;
		public uint PresentQueueIndex;
		public VkSurfaceTransformFlagBitsKHR PreTransform;
		public VkCompositeAlphaFlagBitsKHR CompositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr;
		public List<VkPresentModeKHR> DesiredPresentModes = [];
		public bool Clipped = true;
		public VkSwapchainKHR OldSwapchain;
		public VkAllocationCallbacks* AllocationCallbacks;

		public SwapchainInfo() {
		}
	}
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public unsafe struct Swapchain : IDisposable {
	public VkDevice Device;
	public VkSwapchainKHR VkSwapchain;
	public uint ImageCount;
	public VkFormat ImageFormat;
	public VkColorSpaceKHR ColorSpace;
	public VkImageUsageFlagBits ImageUsageFlags;
	public VkExtent2D Extent;
	public uint RequestedMinImageCount;
	public VkPresentModeKHR PresentMode;
	public uint InstanceVersion = Vk.VK_API_VERSION_1_0;
	public VkAllocationCallbacks* AllocationCallbacks;

	public Result<VkImage[]> GetImages() {
		VkDevice device = Device;
		VkSwapchainKHR vkSwapchain = VkSwapchain;
		var swapchainImagesRet = Detail.GetVector(out VkImage[] images, (p1, p2) => Vk.GetSwapchainImagesKHR(device, vkSwapchain, (uint*)p1, (VkImage*)p2));
		if(swapchainImagesRet != VkResult.Success) {
			return Result.FromException<VkImage[]>(new SwapchainException(SwapchainError.FailedGetSwapchainImages, new VkException(swapchainImagesRet)));
		}
		return images;
	}

	public Result<VkImageView[]> GetImageViews() {
		return GetImageViews(null);
	}
	public Result<VkImageView[]> GetImageViews(void* pNext) {
		var swapchainImagesRet = GetImages();
		if(!swapchainImagesRet.IsSuccessful) {
			return Result.FromException<VkImageView[]>(swapchainImagesRet.Error);
		}
		var swapchainImages = swapchainImagesRet.Value;

		bool alreadyContainsImageViewUsage = false;
		while(pNext != null) {
			if(((VkBaseInStructure*)pNext)->sType == VkStructureType.StructureTypeImageViewCreateInfo) {
				alreadyContainsImageViewUsage = true;
				break;
			}
			pNext = ((VkBaseInStructure*)pNext)->pNext;
		}
		VkImageViewUsageCreateInfo desiredFlags = new() {
			pNext = pNext,
			usage = ImageUsageFlags
		};
		var views = new VkImageView[swapchainImages.Length];
		for(int i = 0; i < views.Length; i++) {
			VkImageViewCreateInfo createInfo = new();
			if(InstanceVersion >= Vk.VK_API_VERSION_1_1 && !alreadyContainsImageViewUsage) {
				createInfo.pNext = &desiredFlags;
			} else {
				createInfo.pNext = pNext;
			}

			createInfo.image = swapchainImages[i];
			createInfo.viewType = VkImageViewType.ImageViewType2d;
			createInfo.format = ImageFormat;
			createInfo.components.r = VkComponentSwizzle.ComponentSwizzleIdentity;
			createInfo.components.g = VkComponentSwizzle.ComponentSwizzleIdentity;
			createInfo.components.b = VkComponentSwizzle.ComponentSwizzleIdentity;
			createInfo.components.a = VkComponentSwizzle.ComponentSwizzleIdentity;
			createInfo.subresourceRange = new() {
				aspectMask = VkImageAspectFlagBits.ImageAspectColorBit,
				baseMipLevel = 0,
				levelCount = 1,
				baseArrayLayer = 0,
				layerCount = 1
			};
			VkResult res;
			fixed(VkImageView* pView = &views[i]) {
				res = Vk.CreateImageView(Device, &createInfo, AllocationCallbacks, pView);
			}
			if(res != VkResult.Success) {
				return Result.FromException<VkImageView[]>(new SwapchainException(SwapchainError.FailedCreateSwapchainImageViews, new VkException(res)));
			}
		}
		return views;
	}
	public readonly void DestroyImageViews(VkImageView[] imageViews) {
		foreach(var item in imageViews) {
			Vk.DestroyImageView(Device, item, AllocationCallbacks);
		}
	}

	public static implicit operator VkSwapchainKHR(Swapchain swapchain) {
		return swapchain.VkSwapchain;
	}

	public Swapchain() {
	}

	public readonly void Dispose() {
		Vk.DestroySwapchainKHR(Device, VkSwapchain, AllocationCallbacks);
	}
}
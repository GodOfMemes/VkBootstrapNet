using DotNext;
using Vortice.Vulkan;

namespace VkBootstrapNet;

public unsafe struct Device : IDisposable {
	public VkDevice VkDevice;
	public VkDeviceApi DeviceApi;
	public PhysicalDevice PhysicalDevice;
	public Instance Instance;
	public VkSurfaceKHR Surface;
	public VkQueueFamilyProperties[] QueueFamilies = [];
	public VkAllocationCallbacks* AllocationCallbacks;
	public VkVersion InstanceVersion = VkVersion.Version_1_0;

	public readonly Result<uint> GetQueueIndex(QueueType type) {
		uint index = uint.MaxValue;
		switch(type) {
			case QueueType.Present:
				index = Detail.GetPresentQueueIndex(Instance, PhysicalDevice, Surface, QueueFamilies);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.PresentUnavailable));
				break;
			case QueueType.Graphics:
				index = Detail.GetFirstQueueIndex(QueueFamilies, VkQueueFlags.Graphics);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.GraphicsUnavailable));
				break;
			case QueueType.Compute:
				index = Detail.GetSeperateQueueIndex(QueueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.ComputeUnavailable));
				break;
			case QueueType.Transfer:
				index = Detail.GetSeperateQueueIndex(QueueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.TransferUnavailable));
				break;
			default:
				return Result.FromException<uint>(new QueueException(QueueError.InvalidQueueFamilyIndex));
		}
		return index;
	}
	public readonly Result<uint> GetDedicatedQueueIndex(QueueType type) {
		uint index = uint.MaxValue;
		switch(type) {
			case QueueType.Compute:
				index = Detail.GetDedicatedQueueIndex(QueueFamilies, VkQueueFlags.Compute, VkQueueFlags.Transfer);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.ComputeUnavailable));
				break;
			case QueueType.Transfer:
				index = Detail.GetDedicatedQueueIndex(QueueFamilies, VkQueueFlags.Transfer, VkQueueFlags.Compute);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.TransferUnavailable));
				break;
			default:
				return Result.FromException<uint>(new QueueException(QueueError.InvalidQueueFamilyIndex));
		}
		return index;
	}

	public readonly Result<VkQueue> GetQueue(QueueType type) {
		var index = GetQueueIndex(type);
		if(!index.IsSuccessful) {
			return Result.FromException<VkQueue>(index.Error);
		}
		VkQueue outQueue;
		DeviceApi.vkGetDeviceQueue(VkDevice, (uint)index.Value, 0, &outQueue);
		return outQueue;
	}
	public readonly Result<VkQueue> GetDedicatedQueue(QueueType type) {
		var index = GetDedicatedQueueIndex(type);
		if(!index.IsSuccessful) {
			return Result.FromException<VkQueue>(index.Error);
		}
		VkQueue outQueue;
		DeviceApi.vkGetDeviceQueue(VkDevice, (uint)index.Value, 0, &outQueue);
		return outQueue;
	}

	public void Dispose() {
		DeviceApi.vkDestroyDevice(VkDevice, AllocationCallbacks);
	}

	public static implicit operator VkDevice(in Device device) {
		return device.VkDevice;
	}
  
  	public static implicit operator VkDeviceApi(in Device device) {
		return device.DeviceApi;
	}


	public Device() {
	}
}
using DotNext;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

public unsafe struct Device : IDisposable {
	public VkDevice VkDevice;
	public PhysicalDevice PhysicalDevice;
	public VkSurfaceKHR Surface;
	public VkQueueFamilyProperties[] QueueFamilies = [];
	public VkAllocationCallbacks* AllocationCallbacks;
	public uint InstanceVersion = Vk.VK_API_VERSION_1_0;

	public readonly Result<uint> GetQueueIndex(QueueType type) {
		uint index = uint.MaxValue;
		switch(type) {
			case QueueType.Present:
				index = Detail.GetPresentQueueIndex(PhysicalDevice, Surface, QueueFamilies);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.PresentUnavailable));
				break;
			case QueueType.Graphics:
				index = Detail.GetFirstQueueIndex(QueueFamilies, VkQueueFlagBits.QueueGraphicsBit);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.GraphicsUnavailable));
				break;
			case QueueType.Compute:
				index = Detail.GetSeperateQueueIndex(QueueFamilies, VkQueueFlagBits.QueueComputeBit, VkQueueFlagBits.QueueTransferBit);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.ComputeUnavailable));
				break;
			case QueueType.Transfer:
				index = Detail.GetSeperateQueueIndex(QueueFamilies, VkQueueFlagBits.QueueTransferBit, VkQueueFlagBits.QueueComputeBit);
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
				index = Detail.GetDedicatedQueueIndex(QueueFamilies, VkQueueFlagBits.QueueComputeBit, VkQueueFlagBits.QueueTransferBit);
				if(index == uint.MaxValue) return Result.FromException<uint>(new QueueException(QueueError.ComputeUnavailable));
				break;
			case QueueType.Transfer:
				index = Detail.GetDedicatedQueueIndex(QueueFamilies, VkQueueFlagBits.QueueTransferBit, VkQueueFlagBits.QueueComputeBit);
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
		Vk.GetDeviceQueue(VkDevice, (uint)index.Value, 0, &outQueue);
		return outQueue;
	}
	public readonly Result<VkQueue> GetDedicatedQueue(QueueType type) {
		var index = GetDedicatedQueueIndex(type);
		if(!index.IsSuccessful) {
			return Result.FromException<VkQueue>(index.Error);
		}
		VkQueue outQueue;
		Vk.GetDeviceQueue(VkDevice, (uint)index.Value, 0, &outQueue);
		return outQueue;
	}

	public void Dispose() {
		Vk.DestroyDevice(VkDevice, AllocationCallbacks);
	}

	public static implicit operator VkDevice(in Device device) {
		return device.VkDevice;
	}

	public Device() {
	}
}
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.Vulkan;

namespace Fuchsium.VkBootstrapNet;

internal struct GenericFeatureChain {
	public List<GenericFeaturesPNextNode> Nodes = [];

	public unsafe void Add<T>(in T features) where T : unmanaged {
		// The original code for this method used compile-time duck typing to identify Vulkan structures by their sType field.
		// Since C# can't do that (well, it can with an interface, but we couldn't modify OpenTK's structs to implement it)
		// we will settle for reading the first sizeof(VkStructureType) bytes of the parameter instead.
		VkStructureType structureType;
		fixed(T* pFeatures = &features) {
			structureType = Unsafe.Read<VkStructureType>(pFeatures);
		}

		for(int i = 0; i < Nodes.Count; i++) {
			GenericFeaturesPNextNode node = Nodes[i];

			if(structureType == node.Type) {
				GenericFeaturesPNextNode.Create<T>(features, out GenericFeaturesPNextNode result2);
				node.Combine(result2);
				Nodes[i] = node;
				return;
			}
		}

		GenericFeaturesPNextNode.Create<T>(features, out GenericFeaturesPNextNode result);
		Nodes.Add(result);
	}

	public readonly bool MatchAll(in GenericFeatureChain extensionRequested) {
		// Should only be false if extension_supported was unable to be filled out, due to the
		// physical device not supporting vkGetPhysicalDeviceFeatures2 in any capacity.
		if(extensionRequested.Nodes.Count != Nodes.Count) {
			return false;
		}

		for(int i=0; i < extensionRequested.Nodes.Count && i < Nodes.Count; i++) {
			if(!GenericFeaturesPNextNode.Match(extensionRequested.Nodes[i], Nodes[i])) {
				return false;
			}
		}
		return true;
	}
	public readonly bool FindAndMatch(in GenericFeatureChain extensionRequested) {
		foreach(var requestedExtensionNode in extensionRequested.Nodes) {
			bool found = false;
			foreach(var supportedNode in Nodes) {
				if(supportedNode.Type == requestedExtensionNode.Type) {
					found = true;
					if(!GenericFeaturesPNextNode.Match(requestedExtensionNode, supportedNode)) {
						return false;
					}
					break;
				}
			}
			if(!found) {
				return false;
			}
		}
		return true;
	}

	public unsafe void ChainUp(Action<VkPhysicalDeviceFeatures2> callback) {
		// Using GCHandle to pin all of the nodes so we can work with their addresses.
		// Note that using GCHandle.Alloc on an unmanaged type will box it; I don't know if this will cause any issues but just keep that in mind in case something happens.

		Span<GCHandle> handles = stackalloc GCHandle[Nodes.Count];

		try {
			GenericFeaturesPNextNode* prev = null;
			for(int i = 0; i < Nodes.Count; i++) {
				handles[i] = GCHandle.Alloc(Nodes[i], GCHandleType.Pinned);

				if(prev != null) {
					prev->Next = handles[i].AddrOfPinnedObject();
				}
				prev = (GenericFeaturesPNextNode*)handles[i].AddrOfPinnedObject();
			}

			VkPhysicalDeviceFeatures2 pdf2 = new() {
				pNext = Nodes.Count > 0 ? (void*)handles[0].AddrOfPinnedObject() : null
			};
			callback(pdf2);
		} finally {
			for(int i=0; i<handles.Length; i++) {
				handles[i].Free();
			}
		}
	}

	public void Combine(in GenericFeatureChain right) {
		foreach(var rightNode in right.Nodes) {
			bool alreadyContained = false;
			foreach(var leftNode in Nodes) {
				if(leftNode.Type == rightNode.Type) {
					leftNode.Combine(rightNode);
					alreadyContained = true;
				}
			}
			if(!alreadyContained) {
				Nodes.Add(rightNode);
			}
		}
	}

	public readonly GenericFeatureChain Clone() {
		return new GenericFeatureChain() { Nodes = Nodes.ToList() };
	}

	public GenericFeatureChain() {
	}
}
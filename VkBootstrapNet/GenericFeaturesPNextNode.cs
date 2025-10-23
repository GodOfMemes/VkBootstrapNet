using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VkBootstrapNet;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
internal unsafe struct GenericFeaturesPNextNode {
	const int FieldsCapacity = 256;

	public VkStructureType Type;
	public IntPtr Next;
	public fixed uint Fields[FieldsCapacity];

	public void DisableFields() {
		for(int i=0; i<256; i++) {
			Fields[i] = 0;
		}
	}

	public static unsafe void Create<T>(in T features, out GenericFeaturesPNextNode result) where T : unmanaged {
		result = new GenericFeaturesPNextNode();
		
		fixed(T* pFeatures = &features) {
			fixed(void* pThis = &result) {
				Buffer.MemoryCopy(pFeatures, pThis, Unsafe.SizeOf<GenericFeaturesPNextNode>(), Unsafe.SizeOf<T>());
			}
		}
	}

	public static bool Match(in GenericFeaturesPNextNode requested, in GenericFeaturesPNextNode supported) {
		Debug.Assert(requested.Type == supported.Type, "Non-matching types in features nodes!");
		for(int i=0; i< FieldsCapacity; i++) {
			if(requested.Fields[i] != supported.Fields[i]) {
				return false;
			}
		}
		return true;
	}

	public void Combine(GenericFeaturesPNextNode right) {
		Debug.Assert(Type == right.Type, "Non-matching types in features nodes!");
		for(int i=0; i< FieldsCapacity; i++) {
			Fields[i] |= right.Fields[i];
		}
	}

	
}
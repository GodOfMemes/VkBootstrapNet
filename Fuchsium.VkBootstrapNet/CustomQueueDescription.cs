namespace Fuchsium.VkBootstrapNet;

public struct CustomQueueDescription {
	public int Index;
	public List<float> Priorities;

	public CustomQueueDescription(int index, List<float> priorities) {
		Index = index;
		Priorities = priorities;
	}
}
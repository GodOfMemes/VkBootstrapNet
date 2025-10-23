
namespace VkBootstrapNet;

public enum QueueError {
	PresentUnavailable,
    GraphicsUnavailable,
    ComputeUnavailable,
    TransferUnavailable,
    QueueIndexOutOfRange,
    InvalidQueueFamilyIndex
}

public sealed class QueueException : EnumException<QueueError> {
	public QueueException(QueueError error) : base(error) {
	}

	public QueueException(QueueError error, string message) : base(error, message) {
	}

	public QueueException(QueueError error, Exception? innerException) : base(error, innerException) {
	}

	public QueueException(QueueError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Queue error.";
}
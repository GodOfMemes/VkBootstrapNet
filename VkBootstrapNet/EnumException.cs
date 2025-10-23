namespace VkBootstrapNet;

public abstract class EnumException<T> : Exception {
	protected abstract string DefaultMessage { get; }

	protected EnumException(T error, string message, Exception? innerException) : base(message, innerException) {
		Error = error;
	}
	protected EnumException(T error, string message) : base(message) {
		Error = error;
	}
	protected EnumException(T error, Exception? innerException) : this(error, "", innerException) { }
	protected EnumException(T error) : this(error, (Exception?)null) { }

	public T Error { get; }

	public override string Message => (base.Message == "" ? DefaultMessage : base.Message) + $" (Error: {Error})";
}

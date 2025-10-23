
namespace VkBootstrapNet;

public enum SwapchainError {
	SurfaceHandleNotProvided,
    FailedQuerySurfaceSupportDetails,
    FailedCreateSwapchain,
    FailedGetSwapchainImages,
    FailedCreateSwapchainImageViews,
    RequiredMinImageCountTooLow,
    RequiredUsageNotSupported
}

public sealed class SwapchainException : EnumException<SwapchainError> {
	public SwapchainException(SwapchainError error) : base(error) {
	}

	public SwapchainException(SwapchainError error, string message) : base(error, message) {
	}

	public SwapchainException(SwapchainError error, Exception? innerException) : base(error, innerException) {
	}

	public SwapchainException(SwapchainError error, string message, Exception? innerException) : base(error, message, innerException) {
	}

	protected override string DefaultMessage => "Swapchain error.";
}
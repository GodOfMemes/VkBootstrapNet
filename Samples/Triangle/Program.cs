using VkBootstrapNet;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Triangle;

unsafe struct Init {
	public Window* Window;
	public Instance Instance;
	public VkSurfaceKHR Surface;
	public Device Device;
	public Swapchain Swapchain;
}

struct RenderData {
	public VkQueue GraphicsQueue;
	public VkQueue PresentQueue;

	public VkImage[] SwapchainImages = [];
	public VkImageView[] SwapchainImageViews = [];
	public VkFramebuffer[] Framebuffers = [];

	public VkRenderPass RenderPass;
	public VkPipelineLayout PipelineLayout;
	public VkPipeline GraphicsPipeline;

	public VkCommandPool CommandPool;
	public VkCommandBuffer[] CommandBuffers = [];

	public VkSemaphore[] AvailableSemaphores = [];
	public VkSemaphore[] FinishedSemaphore = [];
	public VkFence[] InFlightFences = [];
	public VkFence[] ImageInFlight = [];

	public int CurrentFrame;

	public RenderData() {
	}
}

internal unsafe class Program {
	const int MaxFramesInFlight = 2;

	static Window* CreateWindowGlfw(string windowName, bool resize = true) {
		GLFW.Init();
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
		if(!resize) GLFW.WindowHint(WindowHintBool.Resizable, false);

		return GLFW.CreateWindow(1024, 1024, windowName, null, null);
	}

	static void DestroyWindowGlfw(Window* window) {
		GLFW.DestroyWindow(window);
		GLFW.Terminate();
	}

	static VkSurfaceKHR CreateSurfaceGlfw(VkInstance instance, Window* window, VkAllocationCallbacks* allocator = null) {
		VkResult err = (VkResult)GLFW.CreateWindowSurface(new VkHandle((ulong)instance.Handle), window, allocator, out VkHandle surfaceHandle);
		if(err != VkResult.Success) {
			ErrorCode ret = GLFW.GetError(out string error);
			if(ret != ErrorCode.NoError) {
				Console.Write(ret + " ");
				if(!string.IsNullOrEmpty(error)) {
					Console.Write(error);
				}
				Console.WriteLine();
			}
		}
		VkSurfaceKHR surface = new VkSurfaceKHR(surfaceHandle.Handle);
		return surface;
	}

	static int DeviceInitialization(ref Init init) {
		init.Window = CreateWindowGlfw("Vulkan Triangle", true);

		InstanceBuilder instanceBuilder = new();
		var instanceRet = instanceBuilder
			.UseDefaultDebugMessenger()
			.RequestValidationLayers()
			.Build();
		if(!instanceRet.IsSuccessful) {
			Console.WriteLine(instanceRet.Error);
			return -1;
		}
		init.Instance = instanceRet.Value;

		init.Surface = CreateSurfaceGlfw(init.Instance, init.Window);

		PhysicalDeviceSelector physDeviceSelector = new(init.Instance);
		var physDeviceRet = physDeviceSelector.SetSurface(init.Surface).Select();
		if(!physDeviceRet.IsSuccessful) {
			Console.WriteLine(physDeviceRet.Error);
			return -1;
		}
		PhysicalDevice physicalDevice = physDeviceRet.Value;

		DeviceBuilder deviceBuilder = new(init.Instance,physicalDevice);
		var deviceRet = deviceBuilder.Build();
		if(!deviceRet.IsSuccessful) {
			Console.WriteLine(deviceRet.Error);
			return -1;
		}
		init.Device = deviceRet.Value;

		return 0;
	}

	static int CreateSwapchain(ref Init init) {
		SwapchainBuilder swapchainBuilder = new(init.Device);
		var swapRet = swapchainBuilder.SetOldSwapchain(init.Swapchain).Build();
		if(!swapRet.IsSuccessful) {
			Console.WriteLine(swapRet.Error);
			return -1;
		}
		if(init.Swapchain.VkSwapchain.IsNotNull) {
			init.Swapchain.Dispose();
		}
		init.Swapchain = swapRet.Value;
		return 0;
	}

	static int GetQueues(ref Init init, ref RenderData renderData) {
		var gq = init.Device.GetQueue(QueueType.Graphics);
		if(!gq.IsSuccessful) {
			Console.WriteLine(gq.Error);
			return -1;
		}
		renderData.GraphicsQueue = gq.Value;

		var pq = init.Device.GetQueue(QueueType.Graphics);
		if(!pq.IsSuccessful) {
			Console.WriteLine(pq.Error);
			return -1;
		}
		renderData.PresentQueue = pq.Value;
		return 0;
	}

	static int CreateRenderPass(ref Init init, ref RenderData renderData) {
		VkAttachmentDescription colorAttachment = new() {
			format = init.Swapchain.ImageFormat,
			samples = VkSampleCountFlags.Count1,
			loadOp = VkAttachmentLoadOp.Clear,
			storeOp = VkAttachmentStoreOp.Store,
			stencilLoadOp = VkAttachmentLoadOp.DontCare,
			stencilStoreOp = VkAttachmentStoreOp.DontCare,
			initialLayout = VkImageLayout.Undefined,
			finalLayout = VkImageLayout.PresentSrcKHR
		};

		VkAttachmentReference colorAttachmentRef = new() {
			attachment = 0,
			layout = VkImageLayout.ColorAttachmentOptimal
		};

		VkSubpassDescription subpass = new() {
			pipelineBindPoint = VkPipelineBindPoint.Graphics,
			colorAttachmentCount = 1,
			pColorAttachments = &colorAttachmentRef
		};

		VkSubpassDependency dependency = new() {
			srcSubpass = VK_SUBPASS_EXTERNAL,
			dstSubpass = 0,
			srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
			srcAccessMask = 0,
			dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
			dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite
		};

		VkRenderPassCreateInfo renderPassInfo = new() {
			attachmentCount = 1,
			pAttachments = &colorAttachment,
			subpassCount = 1,
			pSubpasses = &subpass,
			dependencyCount = 1,
			pDependencies = &dependency
		};

		fixed(VkRenderPass* pPass = &renderData.RenderPass) 
		{
			if(init.Device.DeviceApi.vkCreateRenderPass(init.Device, &renderPassInfo, null, pPass) != VkResult.Success) {
				Console.WriteLine("failed to create render pass");
				return -1;
			}
		}
		return 0;
	}

	static VkShaderModule CreateShaderModule(ref Init init, byte[] code) {
		fixed(byte* pCode = code) {
			VkShaderModuleCreateInfo createInfo = new() {
				codeSize = (nuint)code.Length,
				pCode = (uint*)pCode
			};

			VkShaderModule shaderModule;
			if(init.Device.DeviceApi.vkCreateShaderModule(init.Device, &createInfo, null, &shaderModule) != VkResult.Success) {
				return default;
			}

			return shaderModule;
		}
	}

	static int CreateGraphicsPipeline(ref Init init, ref RenderData data) {
		var vertCode = File.ReadAllBytes("shaders/triangle.vert.spv");
		var fragCode = File.ReadAllBytes("shaders/triangle.frag.spv");

		VkShaderModule vertModule = CreateShaderModule(ref init, vertCode);
		VkShaderModule fragModule = CreateShaderModule(ref init, fragCode);
		if(vertModule.IsNull || fragModule.IsNull) {
			Console.WriteLine("failed to create shader modules");
			return -1;
		}

		VkUtf8ReadOnlyString main = "main"u8;

		VkPipelineShaderStageCreateInfo vertStageInfo = new() {
			stage = VkShaderStageFlags.Vertex,
			module = vertModule,
			pName = main
		};

		VkPipelineShaderStageCreateInfo fragStageInfo = new() {
			stage = VkShaderStageFlags.Fragment,
			module = fragModule,
			pName = main
		};

		var shaderStages = stackalloc VkPipelineShaderStageCreateInfo[] { vertStageInfo, fragStageInfo };

		VkPipelineVertexInputStateCreateInfo vertexInputInfo = new() {
			vertexBindingDescriptionCount = 0,
			vertexAttributeDescriptionCount = 0
		};

		VkPipelineInputAssemblyStateCreateInfo inputAssembly = new() {
			topology = VkPrimitiveTopology.TriangleList,
			primitiveRestartEnable = false
		};

		VkViewport viewport = new() {
			width = (float)init.Swapchain.Extent.width,
			height = (float)init.Swapchain.Extent.height,
			minDepth = 0f,
			maxDepth = 1f
		};

		VkRect2D scissor = new(new VkOffset2D(), init.Swapchain.Extent);

		VkPipelineViewportStateCreateInfo viewportState = new() {
			viewportCount = 1,
			pViewports = &viewport,
			scissorCount = 1,
			pScissors = &scissor
		};

		VkPipelineRasterizationStateCreateInfo rasterizer = new() {
			depthClampEnable = false,
			rasterizerDiscardEnable = false,
			polygonMode = VkPolygonMode.Fill,
			lineWidth = 1f,
			cullMode = VkCullModeFlags.Back,
			frontFace = VkFrontFace.Clockwise,
			depthBiasEnable = false
		};

		VkPipelineMultisampleStateCreateInfo multisampling = new() {
			sampleShadingEnable = false,
			rasterizationSamples = VkSampleCountFlags.Count1
		};

		VkPipelineColorBlendAttachmentState colorBlendAttachment = new() {
			colorWriteMask =
				VkColorComponentFlags.R
				| VkColorComponentFlags.G
				| VkColorComponentFlags.B
				| VkColorComponentFlags.A,
			blendEnable = false
		};

		VkPipelineColorBlendStateCreateInfo colorBlending = new() {
			logicOpEnable = false,
			logicOp = VkLogicOp.Copy,
			attachmentCount = 1,
			pAttachments = &colorBlendAttachment,
		};
		colorBlending.blendConstants[0] = 0f;
		colorBlending.blendConstants[1] = 0f;
		colorBlending.blendConstants[2] = 0f;
		colorBlending.blendConstants[3] = 0f;

		VkPipelineLayoutCreateInfo pipelineLayoutInfo = new() {
			setLayoutCount = 0,
			pushConstantRangeCount = 0
		};

		fixed(VkPipelineLayout* pPipelineLayout = &data.PipelineLayout) {
			if(init.Device.DeviceApi.vkCreatePipelineLayout(init.Device, &pipelineLayoutInfo, null, pPipelineLayout) != VkResult.Success) {
				Console.WriteLine("failed to create pipeline layout");
				return -1;
			}
		}

		var dynamicStates = stackalloc VkDynamicState[] { VkDynamicState.Viewport, VkDynamicState.Scissor };

		VkPipelineDynamicStateCreateInfo dynamicInfo = new() {
			dynamicStateCount = 2,
			pDynamicStates = dynamicStates
		};

		VkGraphicsPipelineCreateInfo pipelineInfo = new() {
			stageCount = 2,
			pStages = shaderStages,
			pVertexInputState = &vertexInputInfo,
			pInputAssemblyState = &inputAssembly,
			pViewportState = &viewportState,
			pRasterizationState = &rasterizer,
			pMultisampleState = &multisampling,
			pColorBlendState = &colorBlending,
			pDynamicState = &dynamicInfo,
			layout = data.PipelineLayout,
			renderPass = data.RenderPass,
			subpass = 0,
			basePipelineHandle = default
		};

		fixed(VkPipeline* pPipeline = &data.GraphicsPipeline) {
			if(init.Device.DeviceApi.vkCreateGraphicsPipelines(init.Device, default, 1, &pipelineInfo, null, pPipeline) != VkResult.Success) {
				Console.WriteLine("failed to create pipeline");
				return -1;
			}
		}

		init.Device.DeviceApi.vkDestroyShaderModule(init.Device, vertModule, null);
		init.Device.DeviceApi.vkDestroyShaderModule(init.Device, fragModule, null);
		return 0;
	}

	static int CreateFramebuffers(ref Init init, ref RenderData data) {
		data.SwapchainImages = init.Swapchain.GetImages().Value;
		data.SwapchainImageViews = init.Swapchain.GetImageViews().Value;

		data.Framebuffers = new VkFramebuffer[data.SwapchainImageViews.Length];

		for(int i=0; i<data.Framebuffers.Length; i++) {
			VkImageView attachment = data.SwapchainImageViews[i];

			VkFramebufferCreateInfo framebufferInfo = new() {
				renderPass = data.RenderPass,
				attachmentCount = 1,
				pAttachments = &attachment,
				width = init.Swapchain.Extent.width,
				height = init.Swapchain.Extent.height,
				layers = 1,
			};

			fixed(VkFramebuffer* pFramebuffer = &data.Framebuffers[i]) {
				if(init.Device.DeviceApi.vkCreateFramebuffer(init.Device, &framebufferInfo, null, pFramebuffer) != VkResult.Success) {
					return -1;
				}
			}
		}
		return 0;
	}

	static int CreateCommandPool(ref Init init, ref RenderData data) {
		VkCommandPoolCreateInfo poolInfo = new() {
			queueFamilyIndex = init.Device.GetQueueIndex(QueueType.Graphics).Value
		};

		fixed(VkCommandPool* pCommandPool = &data.CommandPool) {
			if(init.Device.DeviceApi.vkCreateCommandPool(init.Device, &poolInfo, null, pCommandPool) != VkResult.Success) {
				Console.WriteLine("failed to create command pool");
				return -1;
			}
		}
		return 0;
	}

	static int CreateCommandBuffers(ref Init init, ref RenderData data) {
		data.CommandBuffers = new VkCommandBuffer[data.Framebuffers.Length];

		VkCommandBufferAllocateInfo allocInfo = new() {
			commandPool = data.CommandPool,
			level = VkCommandBufferLevel.Primary,
			commandBufferCount = (uint)data.CommandBuffers.Length
		};

		fixed(VkCommandBuffer* pCommandBuffers = data.CommandBuffers) {
			if(init.Device.DeviceApi.vkAllocateCommandBuffers(init.Device, &allocInfo, pCommandBuffers) != VkResult.Success) {
				return -1;
			}
		}

		for(int i=0; i<data.CommandBuffers.Length; i++) {
			VkCommandBufferBeginInfo beginInfo = new();
			if(init.Device.DeviceApi.vkBeginCommandBuffer(data.CommandBuffers[i], &beginInfo) != VkResult.Success) {
				return -1;
			}

			VkRenderPassBeginInfo renderPassInfo = new() {
				renderPass = data.RenderPass,
				framebuffer = data.Framebuffers[i],
				renderArea = new(new VkOffset2D(), init.Swapchain.Extent)
			};
			VkClearValue clearValue = new();
			renderPassInfo.clearValueCount = 1;
			renderPassInfo.pClearValues = &clearValue;

			VkViewport viewport = new() {
				width = init.Swapchain.Extent.width,
				height = init.Swapchain.Extent.height,
				minDepth = 0f,
				maxDepth = 1f
			};

			VkRect2D scissor = new(new VkOffset2D(), init.Swapchain.Extent);

			init.Device.DeviceApi.vkCmdSetViewport(data.CommandBuffers[i], 0, 1, &viewport);
			init.Device.DeviceApi.vkCmdSetScissor(data.CommandBuffers[i], 0, 1, &scissor);

			init.Device.DeviceApi.vkCmdBeginRenderPass(data.CommandBuffers[i], &renderPassInfo, VkSubpassContents.Inline);

			init.Device.DeviceApi.vkCmdBindPipeline(data.CommandBuffers[i], VkPipelineBindPoint.Graphics, data.GraphicsPipeline);

			init.Device.DeviceApi.vkCmdDraw(data.CommandBuffers[i], 3, 1, 0, 0);

			init.Device.DeviceApi.vkCmdEndRenderPass(data.CommandBuffers[i]);

			if(init.Device.DeviceApi.vkEndCommandBuffer(data.CommandBuffers[i]) != VkResult.Success) {
				Console.WriteLine("failed to record command buffer");
				return -1;
			}
		}
		return 0;
	}

	static int CreateSyncObjects(ref Init init, ref RenderData data) {
		data.AvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
		data.FinishedSemaphore = new VkSemaphore[MaxFramesInFlight];
		data.InFlightFences = new VkFence[MaxFramesInFlight];
		data.ImageInFlight = new VkFence[init.Swapchain.ImageCount];

		VkSemaphoreCreateInfo semaphoreInfo = new();

		VkFenceCreateInfo fenceInfo = new() {
			flags = VkFenceCreateFlags.Signaled
		};

		for(int i=0; i<MaxFramesInFlight; i++) {
			fixed(VkSemaphore* pAvailableSemaphore = &data.AvailableSemaphores[i])
			fixed(VkSemaphore* pFinishedSemaphore = &data.FinishedSemaphore[i])
			fixed(VkFence* pFence = &data.InFlightFences[i]) {
				if(init.Device.DeviceApi.vkCreateSemaphore(init.Device, &semaphoreInfo, null, pAvailableSemaphore) != VkResult.Success
					|| init.Device.DeviceApi.vkCreateSemaphore(init.Device, &semaphoreInfo, null, pFinishedSemaphore) != VkResult.Success
					|| init.Device.DeviceApi.vkCreateFence(init.Device, &fenceInfo, null, pFence) != VkResult.Success) {
					Console.WriteLine("failed to create sync objects");
					return -1;
				}
			}
		}
		return 0;
	}

	static int RecreateSwapchain(ref Init init, ref RenderData data) {
		init.Device.DeviceApi.vkDeviceWaitIdle(init.Device);

		init.Device.DeviceApi.vkDestroyCommandPool(init.Device, data.CommandPool, null);

		foreach(var framebuffer in data.Framebuffers) {
			init.Device.DeviceApi.vkDestroyFramebuffer(init.Device, framebuffer, null);
		}

		init.Swapchain.DestroyImageViews(data.SwapchainImageViews);

		if(0 != CreateSwapchain(ref init)) return -1;
		if(0 != CreateFramebuffers(ref init, ref data)) return -1;
		if(0 != CreateCommandPool(ref init, ref data)) return -1;
		if(0 != CreateCommandBuffers(ref init, ref data)) return -1;

		return 0;
	}

	static int DrawFrame(ref Init init, ref RenderData data) {
		VkFence fence = data.InFlightFences[data.CurrentFrame];
		init.Device.DeviceApi.vkWaitForFences(init.Device, 1, &fence, true, ulong.MaxValue);

		uint imageIndex = 0;
		VkResult result = init.Device.DeviceApi.vkAcquireNextImageKHR(init.Device, init.Swapchain, ulong.MaxValue, data.AvailableSemaphores[data.CurrentFrame], default, &imageIndex);

		if(result == VkResult.ErrorOutOfDateKHR) {
			return RecreateSwapchain(ref init, ref data);
		} else if(result != VkResult.Success && result != VkResult.SuboptimalKHR) {
			Console.WriteLine("failed to acquire swapchain image. Error " + result);
			return -1;
		}

		if(data.ImageInFlight[imageIndex].IsNotNull) {
			VkFence fence2 = data.InFlightFences[data.CurrentFrame];
			init.Device.DeviceApi.vkWaitForFences(init.Device, 1, &fence2, true, ulong.MaxValue);
		}
		data.ImageInFlight[imageIndex] = data.InFlightFences[data.CurrentFrame];

		VkSubmitInfo submitInfo = new();

		VkSemaphore waitSemaphore = data.AvailableSemaphores[data.CurrentFrame];
		VkPipelineStageFlags waitStage = VkPipelineStageFlags.ColorAttachmentOutput;
		submitInfo.waitSemaphoreCount = 1;
		submitInfo.pWaitSemaphores = &waitSemaphore;
		submitInfo.pWaitDstStageMask = &waitStage;

		submitInfo.commandBufferCount = 1;
		VkCommandBuffer commandBuffer = data.CommandBuffers[imageIndex];
		submitInfo.pCommandBuffers = &commandBuffer;

		var signal_semaphores = stackalloc VkSemaphore[] { data.FinishedSemaphore[data.CurrentFrame] };
		submitInfo.signalSemaphoreCount = 1;
		submitInfo.pSignalSemaphores = signal_semaphores;

		VkFence inFlightFence = data.InFlightFences[data.CurrentFrame];
		init.Device.DeviceApi.vkResetFences(init.Device, 1, &inFlightFence);

		if(init.Device.DeviceApi.vkQueueSubmit(data.GraphicsQueue, 1, &submitInfo, inFlightFence) != VkResult.Success) {
			Console.WriteLine("failed to submit draw command buffer");
			return -1;
		}

		VkPresentInfoKHR presentInfo = new() {
			waitSemaphoreCount = 1,
			pWaitSemaphores = signal_semaphores
		};

		var swapChains = stackalloc VkSwapchainKHR[] { init.Swapchain };
		presentInfo.swapchainCount = 1;
		presentInfo.pSwapchains = swapChains;

		presentInfo.pImageIndices = &imageIndex;

		result = init.Device.DeviceApi.vkQueuePresentKHR(data.PresentQueue, &presentInfo);
		if(result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR) {
			return RecreateSwapchain(ref init, ref data);
		} else if(result != VkResult.Success) {
			Console.WriteLine("failed to present swapchain image");
			return -1;
		}

		data.CurrentFrame = (data.CurrentFrame + 1) % MaxFramesInFlight;
		return 0;
	}

	static void Cleanup(ref Init init, ref RenderData data) {
		for(int i=0; i<MaxFramesInFlight; i++) {
			init.Device.DeviceApi.vkDestroySemaphore(init.Device, data.FinishedSemaphore[i], null);
			init.Device.DeviceApi.vkDestroySemaphore(init.Device, data.AvailableSemaphores[i], null);
			init.Device.DeviceApi.vkDestroyFence(init.Device, data.InFlightFences[i], null);
		}

		init.Device.DeviceApi.vkDestroyCommandPool(init.Device, data.CommandPool, null);

		foreach(var framebuffer in data.Framebuffers) {
			init.Device.DeviceApi.vkDestroyFramebuffer(init.Device, framebuffer, null);
		}

		init.Device.DeviceApi.vkDestroyPipeline(init.Device, data.GraphicsPipeline, null);
		init.Device.DeviceApi.vkDestroyPipelineLayout(init.Device, data.PipelineLayout, null);
		init.Device.DeviceApi.vkDestroyRenderPass(init.Device, data.RenderPass, null);

		init.Swapchain.DestroyImageViews(data.SwapchainImageViews);

		init.Swapchain.Dispose();
		init.Device.Dispose();
		init.Instance.InstanceApi.vkDestroySurfaceKHR(init.Instance, init.Surface, null);
		init.Instance.Dispose();
		DestroyWindowGlfw(init.Window);
	}

	static int Main(string[] args) {
		Init init = new();
		RenderData renderData = new();

		if(0 != DeviceInitialization(ref init)) return -1;
		if(0 != CreateSwapchain(ref init)) return -1;
		if(0 != GetQueues(ref init, ref renderData)) return -1;
		if(0 != CreateRenderPass(ref init, ref renderData)) return -1;
		if(0 != CreateGraphicsPipeline(ref init, ref renderData)) return -1;
		if(0 != CreateFramebuffers(ref init, ref renderData)) return -1;
		if(0 != CreateCommandPool(ref init, ref renderData)) return -1;
		if(0 != CreateCommandBuffers(ref init, ref renderData)) return -1;
		if(0 != CreateSyncObjects(ref init, ref renderData)) return -1;

		while(!GLFW.WindowShouldClose(init.Window)) {
			GLFW.PollEvents();
			int res = DrawFrame(ref init, ref renderData);
			if(res != 0) {
				Console.WriteLine("failed to draw frame");
				return -1;
			}
		}
		init.Device.DeviceApi.vkDeviceWaitIdle(init.Device);

		Cleanup(ref init, ref renderData);
		return 0;
	}
}

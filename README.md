# Fuchsium.VkBootstrapNet

.NET port of the [VkBootstrap](https://github.com/charles-lunarg/vk-bootstrap) C++ library.

## Basic Usage

See [BasicUsage](./Samples/BasicUsage) to see 500 lines slimmed down to about 50.

```cs
using Fuchsium.VkBootstrapNet;

static void InitVulkan() {
    InstanceBuilder builder = new();
    var instRet = builder.SetAppName("Example Vulkan Application")
        .RequestValidationLayers()
        .UseDefaultDebugMessenger()
        .Build();
    if(!instRet.IsSuccessful) { /* report */ }
    var inst = instRet.Value;

    PhysicalDeviceSelector selector = new(vkb_inst);
    var physRet = selector.SetSurface(surface)
                        .SetMinimumVersion(1, 1)
                        .RequireDedicatedTransferQueue()
                        .Select();
    if(!physRet.IsSuccessful) { /* report */ }

    DeviceBuilder device_builder = new(physRet.Value);
    var devRet = device_builder.Build();
    if (!devRet.IsSuccessful) { /* report */ }
    Device device = devRet.Value;

    var graphicsQueueRet = vkb_device.GetQueue(QueueType.Graphics);
    if(!graphicsQueueRet)  { /* report */ }
    VkQueue graphicsQueue = graphicsQueueRet.Value;
}
```

## Differences

VkBootstrapNet has taken several creative liberties that differentiate it with the original. These include:
 - All API names have been re-written to fit in with the rest of the .NET ecosystem.
 - The [DotNext](https://github.com/dotnet/dotNext) framework is utilized for the result type instead of a custom implementation.
 - [OpenTK](https://github.com/opentk/opentk)'s Vulkan bindings are used instead of a custom loader. This means that types like `Device` do not contain a dispatch table.
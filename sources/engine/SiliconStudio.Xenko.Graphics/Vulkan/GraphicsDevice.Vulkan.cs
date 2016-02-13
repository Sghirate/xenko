﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

#if SILICONSTUDIO_XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Threading;
using SharpVulkan;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Xenko.Graphics
{
    public partial class GraphicsDevice
    {
        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D11;

        private bool simulateReset = false;

        private SharpVulkan.Device nativeDevice;
        //internal CommandQueue NativeCommandQueue;

        //internal CommandAllocator NativeCopyCommandAllocator;
        //internal GraphicsCommandList NativeCopyCommandList;

        //internal DescriptorAllocator ShaderResourceViewAllocator;
        //internal DescriptorAllocator DepthStencilViewAllocator;
        //internal DescriptorAllocator RenderTargetViewAllocator;

        //private SharpDX.Direct3D12.Resource nativeUploadBuffer;
        //private IntPtr nativeUploadBufferStart;
        //private int nativeUploadBufferOffset;

        internal int SrvHandleIncrementSize;
        internal int SamplerHandleIncrementSize;

        //private Fence nativeFence;
        //private int fenceValue = 1;
        //private AutoResetEvent fenceEvent = new AutoResetEvent(false);
        //internal List<SharpDX.Direct3D12.Pageable> TemporaryResources = new List<SharpDX.Direct3D12.Pageable>();

        /// <summary>
        ///     Gets the status of this device.
        /// </summary>
        /// <value>The graphics device status.</value>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                if (simulateReset)
                {
                    simulateReset = false;
                    return GraphicsDeviceStatus.Reset;
                }

                //var result = NativeDevice.DeviceRemovedReason;
                //if (result == SharpDX.DXGI.ResultCode.DeviceRemoved)
                //{
                //    return GraphicsDeviceStatus.Removed;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DeviceReset)
                //{
                //    return GraphicsDeviceStatus.Reset;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DeviceHung)
                //{
                //    return GraphicsDeviceStatus.Hung;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DriverInternalError)
                //{
                //    return GraphicsDeviceStatus.InternalError;
                //}

                //if (result == SharpDX.DXGI.ResultCode.InvalidCall)
                //{
                //    return GraphicsDeviceStatus.InvalidCall;
                //}

                //if (result.Code < 0)
                //{
                //    return GraphicsDeviceStatus.Reset;
                //}

                return GraphicsDeviceStatus.Normal;
            }
        }

        /// <summary>
        ///     Gets the native device.
        /// </summary>
        /// <value>The native device.</value>
        internal SharpVulkan.Device NativeDevice
        {
            get
            {
                return nativeDevice;
            }
        }

        /// <summary>
        ///     Marks context as active on the current thread.
        /// </summary>
        public void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;
        }

        /// <summary>
        /// Enables profiling.
        /// </summary>
        /// <param name="enabledFlag">if set to <c>true</c> [enabled flag].</param>
        public void EnableProfile(bool enabledFlag)
        {
        }

        /// <summary>
        ///     Unmarks context as active on the current thread.
        /// </summary>
        public void End()
        {
        }

        /// <summary>
        /// Executes a deferred command list.
        /// </summary>
        /// <param name="commandList">The deferred command list.</param>
        public void ExecuteCommandList(CommandList commandList)
        {
            //if (commandList == null) throw new ArgumentNullException("commandList");
            //
            //NativeDeviceContext.ExecuteCommandList(((CommandList)commandList).NativeCommandList, false);
            //commandList.Dispose();
        }

        public void SimulateReset()
        {
            simulateReset = true;
        }

        private void InitializeFactories()
        {
            
        }

        /// <summary>
        ///     Initializes the specified device.
        /// </summary>
        /// <param name="graphicsProfiles">The graphics profiles.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            //if (nativeDevice != null)
            //{
            //    // Destroy previous device
            //    ReleaseDevice();
            //}

            //// Profiling is supported through pix markers
            //IsProfilingSupported = true;

            //// Map GraphicsProfile to D3D11 FeatureLevel
            //SharpDX.Direct3D.FeatureLevel[] levels = graphicsProfiles.ToFeatureLevel();
            //if ((deviceCreationFlags & DeviceCreationFlags.Debug) != 0)
            //{
            //    SharpDX.Direct3D12.DebugInterface.Get().EnableDebugLayer();
            //}

            //// Create Device D3D12 with feature Level based on profile
            //foreach (var level in levels)
            //{
            //    try
            //    {
            //        nativeDevice = new SharpDX.Direct3D12.Device(Adapter.NativeAdapter, level);
            //        break;
            //    }
            //    catch (Exception)
            //    {
            //        continue;
            //    }
            //}
            //if (nativeDevice == null)
            //    throw new InvalidOperationException("Could not create D3D12 graphics device");

            //// Describe and create the command queue.
            //var queueDesc = new SharpDX.Direct3D12.CommandQueueDescription(SharpDX.Direct3D12.CommandListType.Direct);
            //NativeCommandQueue = nativeDevice.CreateCommandQueue(queueDesc);

            //SrvHandleIncrementSize = NativeDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            //SamplerHandleIncrementSize = NativeDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);

            //// Prepare descriptor allocators
            //ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            //DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.DepthStencilView);
            //RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView);

            //// Prepare copy command list (start it closed, so that every new use start with a Reset)
            //NativeCopyCommandAllocator = NativeDevice.CreateCommandAllocator(CommandListType.Direct);
            //NativeCopyCommandList = NativeDevice.CreateCommandList(CommandListType.Direct, NativeCopyCommandAllocator, null);
            //NativeCopyCommandList.Close();

            //// Fence for next frame and resource cleaning
            //nativeFence = NativeDevice.CreateFence(0, FenceFlags.None);
        }

        //internal IntPtr AllocateUploadBuffer(int size, out SharpDX.Direct3D12.Resource resource, out int offset)
        //{
        //    // TODO D3D12 thread safety, should we simply use locks?
        //    if (nativeUploadBuffer == null || nativeUploadBufferOffset + size > nativeUploadBuffer.Description.Width)
        //    {
        //        // Allocate new buffer
        //        // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
        //        // TODO D3D12 ResourceStates.CopySource not working?
        //        var bufferSize = Math.Max(4 * 1024*1024, size);
        //        nativeUploadBuffer = NativeDevice.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(bufferSize), ResourceStates.GenericRead);
        //        TemporaryResources.Add(nativeUploadBuffer);
        //        nativeUploadBufferStart = nativeUploadBuffer.Map(0);
        //        nativeUploadBufferOffset = 0;
        //    }

        //    // Bump allocate
        //    resource = nativeUploadBuffer;
        //    offset = nativeUploadBufferOffset;
        //    nativeUploadBufferOffset += size;
        //    return nativeUploadBufferStart + offset;
        //}

        internal void ReleaseTemporaryResources()
        {
            //// Wait for frame to be finished
            //int localFence = fenceValue;
            //NativeCommandQueue.Signal(this.nativeFence, localFence);
            //fenceValue++;

            //// Wait until the previous frame is finished.
            //if (nativeFence.CompletedValue < localFence)
            //{
            //    nativeFence.SetEventOnCompletion(localFence, fenceEvent.SafeWaitHandle.DangerousGetHandle());
            //    fenceEvent.WaitOne();
            //}

            //// Release previous frame resources
            //foreach (var resource in TemporaryResources)
            //{
            //    resource.Dispose();
            //}
            //nativeUploadBuffer = null;

            //TemporaryResources.Clear();
        }

        protected void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        private void ReleaseDevice()
        {
            //nativeDevice.Dispose();
        }

        internal void OnDestroyed()
        {
        }

        /// <summary>
        /// Allocate descriptor handles. For now a simple bump alloc, but at some point we will have to make a real allocator with free
        /// </summary>
        internal class DescriptorAllocator
        {
            //private const int DescriptorPerHeap = 256;

            //private GraphicsDevice device;
            //private DescriptorHeapType descriptorHeapType;
            //private DescriptorHeap currentHeap;
            //private CpuDescriptorHandle currentHandle;
            //private int remainingHandles;
            //private readonly int descriptorSize;

            //public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType)
            //{
            //    this.device = device;
            //    this.descriptorHeapType = descriptorHeapType;
            //    this.descriptorSize = device.NativeDevice.GetDescriptorHandleIncrementSize(descriptorHeapType);
            //}

            //public CpuDescriptorHandle Allocate(int count)
            //{
            //    if (currentHeap == null || remainingHandles < count)
            //    {
            //        currentHeap = device.NativeDevice.CreateDescriptorHeap(new DescriptorHeapDescription
            //        {
            //            Flags = DescriptorHeapFlags.None,
            //            Type = descriptorHeapType,
            //            DescriptorCount = DescriptorPerHeap,
            //            NodeMask = 1,
            //        });
            //        remainingHandles = DescriptorPerHeap;
            //        currentHandle = currentHeap.CPUDescriptorHandleForHeapStart;
            //    }

            //    var result = currentHandle;

            //    currentHandle.Ptr += descriptorSize;
            //    remainingHandles -= count;

            //    return result;
            //}
        }
    }
}
#endif

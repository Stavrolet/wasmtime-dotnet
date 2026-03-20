using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components;

/// <summary>
/// Represents a WebAssembly Component
/// </summary>
public class Component : IDisposable
{
    private class Handle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public Handle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            Native.wasmtime_component_delete(handle);
            return true;
        }
    }

    /// <summary>
    /// Creates a <see cref="Component"/> from the given bytes.
    /// </summary>
    /// <param name="engine">The engine to use for the component.</param>
    /// <param name="bytes">The bytes of the component.</param>
    /// <returns>Returns a new <see cref="Component"/>.</returns>
    public static Component FromBytes(Engine engine, ReadOnlySpan<byte> bytes)
    {
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                var error = Native.wasmtime_component_new(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
                
                return new Component(handle);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="Component"/> from the given path to the WebAssembly file.
    /// </summary>
    /// <param name="engine">The engine to use for the component.</param>
    /// <param name="path">The path to the WebAssembly file.</param>
    /// <returns>Returns a new <see cref="Component"/>.</returns>
    public static Component FromFile(Engine engine, string path)
    {
        return FromBytes(engine, File.ReadAllBytes(path));
    }
    
    /// <summary>
    /// Creates a <see cref="Component"/> from a stream.
    /// </summary>
    /// <param name="engine">The engine to use for the component.</param>
    /// <param name="stream">The stream of the component data.</param>
    /// <returns>Returns a new <see cref="Component"/>.</returns>
    public static Component FromStream(Engine engine, Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return FromBytes(engine, ms.ToArray());
    }
    
    /// <summary>
    /// Serializes the component to an array of bytes.
    /// </summary>
    /// <returns>Returns the serialized component as an array of bytes.</returns>
    public byte[] Serialize()
    {
        var error = Native.wasmtime_component_serialize(this.handle, out var array);
        if (error != IntPtr.Zero)
        {
            throw WasmtimeException.FromOwnedError(error);
        }

        using (array)
        {
            var len = (int)array.size;
            var bytes = new byte[len];
            
            unsafe
            {
                Marshal.Copy((IntPtr)array.data, bytes, 0, len);
            }
            
            return bytes;
        }
    }
    
    /// <summary>
    /// Deserializes a previously serialized component from a span of bytes.
    /// </summary>
    /// <param name="engine">The engine to use to deserialize the component.</param>
    /// <param name="bytes">The previously serialized component bytes.</param>
    /// <returns>Returns the <see cref="Component" /> that was previously serialized.</returns>
    /// <remarks>The passed bytes must come from a previous call to <see cref="Component.Serialize" />.</remarks>
    public static Component Deserialize(Engine engine, ReadOnlySpan<byte> bytes)
    {
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                var error = Native.wasmtime_component_deserialize(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }

                return new Component(handle);
            }
        }
    }
    
    /// <summary>
    /// Deserializes a previously serialized component from a file.
    /// </summary>
    /// <param name="engine">The engine to deserialize the component with.</param>
    /// <param name="path">The path to the previously serialized component.</param>
    /// <returns>Returns the <see cref="Component" /> that was previously serialized.</returns>
    /// <remarks>The file's contents must come from a previous call to <see cref="Component.Serialize" />.</remarks>
    public static Component DeserializeFile(Engine engine, string path)
    {
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        var error = Native.wasmtime_component_deserialize_file(engine.NativeHandle, path, out var handle);
        if (error != IntPtr.Zero)
        {
            throw WasmtimeException.FromOwnedError(error);
        }

        return new Component(handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        handle.Dispose();
        GC.SuppressFinalize(this);
    }

    internal Component(IntPtr handle)
    {
        this.handle = new Handle(handle);
    }

    private static class Native
    {
        [DllImport(Engine.LibraryName)]
        public static extern unsafe IntPtr wasmtime_component_new(Engine.Handle engine, byte* bytes, nuint size, out IntPtr handle);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_delete(IntPtr module);
        
        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_serialize(Handle component, out ByteArray bytes);
        
        [DllImport(Engine.LibraryName)]
        public static extern unsafe IntPtr wasmtime_component_deserialize(Engine.Handle engine, byte* bytes, nuint size, out IntPtr handle);
        
        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_deserialize_file(Engine.Handle component, [MarshalAs(Extensions.LPUTF8Str)] string path, out IntPtr handle);
    }

    private readonly Handle handle;
}
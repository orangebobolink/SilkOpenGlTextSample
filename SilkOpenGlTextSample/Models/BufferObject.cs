using Silk.NET.OpenGL;

namespace SilkOpenGlTextSample;

/// <summary>
///     A generic wrapper for an OpenGL buffer object (VBO, EBO, etc.).
///     This class simplifies buffer creation, binding, and data uploading using Silk.NET.
/// </summary>
/// <typeparam name="TDataType">The type of data stored in the buffer (e.g., float for vertex data).</typeparam>
public sealed class BufferObject<TDataType> : IDisposable
    where TDataType : unmanaged
{
    private readonly BufferTargetARB _bufferType;
    private readonly GL _gl;
    private readonly uint _handle;

    /// <summary>
    ///     Creates a new OpenGL buffer and uploads the given data.
    /// </summary>
    /// <param name="gl">The OpenGL context from Silk.NET.</param>
    /// <param name="data">The data to be stored in the buffer.</param>
    /// <param name="bufferType">
    ///     The type of buffer (e.g., ArrayBuffer for VBO, ElementArrayBuffer for
    ///     EBO).
    /// </param>
    public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
    {
        _gl = gl;
        _bufferType = bufferType;

        // Generate a buffer object and store its handle.
        _handle = _gl.GenBuffer();
        Bind();

        // Upload data to the buffer
        fixed (void* d = data)
        {
            _gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d,
                BufferUsageARB.StreamDraw);
        }
    }

    public unsafe BufferObject(GL gl, int lenght, BufferTargetARB bufferType)
    {
        _gl = gl;
        _bufferType = bufferType;

        // Generate a buffer object and store its handle.
        _handle = _gl.GenBuffer();
        Bind();

        // Upload data to the buffer

        _gl.BufferData(bufferType, (nuint)(lenght * sizeof(TDataType)), nint.Zero,
            BufferUsageARB.StaticDraw);
    }

    /// <summary>
    ///     Releases the buffer resources by deleting it from OpenGL.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Binds the buffer to the specified target, making it active for subsequent OpenGL operations.
    /// </summary>
    public void Bind()
        => _gl.BindBuffer(_bufferType, _handle);

    ~BufferObject()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _gl.DeleteBuffer(_handle);
    }
}
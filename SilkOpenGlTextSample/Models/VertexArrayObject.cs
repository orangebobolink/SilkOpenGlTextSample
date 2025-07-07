using Silk.NET.OpenGL;

namespace SilkOpenGlTextSample;

/// <summary>
///     Encapsulates an OpenGL Vertex Array Object (VAO), which manages vertex attributes and buffer
///     bindings.
///     This class simplifies VAO creation, binding, and vertex attribute setup.
/// </summary>
/// <typeparam name="TVertexType">The type of vertex data stored in the buffer.</typeparam>
/// <typeparam name="TIndexType">The type of index data stored in the buffer.</typeparam>
public sealed class VertexArrayObject<TVertexType, TIndexType> : IDisposable
    where TVertexType : unmanaged
    where TIndexType : unmanaged
{
    private readonly GL _gl;
    private readonly uint _handle;

    /// <summary>
    ///     Creates a Vertex Array Object (VAO) and binds the provided Vertex Buffer Object (VBO) and
    ///     Element Buffer Object (EBO).
    /// </summary>
    /// <param name="gl">The OpenGL context from Silk.NET.</param>
    /// <param name="vbo">The vertex buffer object containing vertex data (can be null).</param>
    /// <param name="ebo">The element buffer object containing index data (can be null).</param>
    public VertexArrayObject(GL gl, BufferObject<TVertexType>? vbo, BufferObject<TIndexType>? ebo)
    {
        _gl = gl;

        // Generate and bind the VAO
        _handle = _gl.GenVertexArray();
        Bind();

        // Bind VBO and EBO if provided
        vbo?.Bind();
        ebo?.Bind();
    }

    /// <summary>
    ///     Deletes the VAO, freeing OpenGL resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Configures a vertex attribute pointer, defining how vertex data is interpreted.
    /// </summary>
    /// <param name="index">The index of the vertex attribute location in the shader.</param>
    /// <param name="count">The number of components per vertex attribute (e.g., 3 for vec3).</param>
    /// <param name="type">The data type of the attribute (e.g., Float, Int).</param>
    /// <param name="vertexSize">The total size of a single vertex in bytes.</param>
    /// <param name="offSet">The byte offset from the beginning of the vertex to this attribute.</param>
    public unsafe void VertexAttributePointer(
        uint index,
        int count,
        VertexAttribPointerType type,
        uint vertexSize,
        int offSet)
    {
        _gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType),
            (void*)(offSet * sizeof(TVertexType)));

        _gl.EnableVertexAttribArray(index);
    }

    /// <summary>
    ///     Binds the VAO, making it the active Vertex Array Object.
    /// </summary>
    public void Bind() => _gl.BindVertexArray(_handle);

    ~VertexArrayObject()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _gl.Dispose();
    }
}
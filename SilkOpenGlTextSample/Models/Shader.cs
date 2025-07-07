using System.Numerics;
using Silk.NET.OpenGL;

namespace SilkOpenGlTextSample;

/// <summary>
///     A class that encapsulates OpenGL shader creation, compilation, linking, and usage.
///     This class supports vertex and fragment shaders and provides methods for setting uniform
///     variables.
/// </summary>
public sealed class Shader : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    public Shader(GL gl, string vertexShader, string fragmentShader)
    {
        _gl = gl;

        // Load and compile vertex and fragment shaders
        uint vertex = LoadShader(ShaderType.VertexShader, vertexShader);
        uint fragment = LoadShader(ShaderType.FragmentShader, fragmentShader);

        // Create a shader program and attach compiled shaders
        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);

        // Check if linking was successful
        _gl.GetProgram(_handle, GLEnum.LinkStatus, out int status);

        if (status == 0)
            throw new GlErrorException(
                $"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");

        // Cleanup: Detach and delete shaders after linking
        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    /// <summary>
    ///     Releases the shader program by deleting it from OpenGL.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Activates the shader program for rendering.
    /// </summary>
    public void Use()
        => _gl.UseProgram(_handle);

    public void SetMatrix4X4(string name, Matrix4x4 matrix)
    {
        int location = _gl.GetUniformLocation(_handle, name);

        if (location == -1)
            throw new GlErrorException($"Uniform '{name}' not found.");

        unsafe
        {
            _gl.UniformMatrix4(location, 1, false, (float*)&matrix);
        }
    }

    public void SetVector3(string name, Vector3 vector)
    {
        int location = _gl.GetUniformLocation(_handle, name);

        if (location == -1)
            throw new GlErrorException($"Uniform '{name}' not found.");

        _gl.Uniform3(location, vector.X, vector.Y, vector.Z);
    }

    private uint LoadShader(ShaderType type, string src)
    {
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, src);
        _gl.CompileShader(handle);

        string infoLog = _gl.GetShaderInfoLog(handle);

        if (!string.IsNullOrWhiteSpace(infoLog))
            throw new GlErrorException(
                $"Error compiling shader of type {type}, failed with error {infoLog}");

        return handle;
    }

    public int GetUniformLocation(string src)
        => _gl.GetUniformLocation(_handle, src);

    ~Shader()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _gl.DeleteProgram(_handle);
        _gl.Dispose();
    }
}
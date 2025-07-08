using System.Numerics;
using SharpFont;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SilkOpenGlTextSample;

public struct CharacterGl
{
    public uint TextureId { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int ShiftLeft { get; init; }
    public int ShiftTop { get; init; }
    public int Advance { get; init; }
}

public class OpenGl
{
    private static IWindow? _window;
    private static GL _gl;

    private VertexArrayObject<float, uint>? _vaoGrid;
    private BufferObject<float>? _vboGrid;

    private float[] _gridLines = [];

    private uint _vaoLabelId;
    private uint _vboLabelId;
    private uint _eboLabelId;

    private bool _leftMouseDown;
    private Vector2 _lastMousePosition;

    private float _yaw = 35;
    private float _roll;
    private float _pitch = 15;
    private float _zoom = 4;

    private Shader? _shader;

    private const int CharsetLength = 128;
    private readonly Dictionary<char, CharacterGl> _glCharacters = new();

    public OpenGl()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "1.3 - Textures";

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;

        _window.Run();

        _window.Dispose();
    }

    private void OnLoad()
    {
        var input = _window!.CreateInput();

        foreach (var t in input.Mice)
        {
            t.Scroll += OnMouseWheel;
            t.MouseDown += OnMouseDown;
            t.MouseUp += OnMouseUp;
            t.MouseMove += OnMouseMove;
        }

        _gl = _window.CreateOpenGL();

        FontTextureGeneration("Jost.ttf", 20);

        _gridLines = GridLineGeneration(1.0f, 10)
            .ToArray();

        const uint aPos = 0;
        const uint aColor = 1;
        const uint aTex = 2;

        _vboGrid = new BufferObject<float>(_gl, _gridLines, BufferTargetARB.ArrayBuffer);
        _vaoGrid = new VertexArrayObject<float, uint>(_gl, _vboGrid, null);

        _vaoGrid.VertexAttributePointer(aPos, 3, VertexAttribPointerType.Float, 6, 0);
        _vaoGrid.VertexAttributePointer(aColor, 3, VertexAttribPointerType.Float, 6, 3);

        // Text 
        _vboLabelId = _gl.GenBuffer();
        _vaoLabelId = _gl.GenVertexArray();
        _eboLabelId = _gl.GenBuffer();

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboLabelId);

        _gl.BufferData(BufferTargetARB.ArrayBuffer, 48 * sizeof(float), nint.Zero,
            BufferUsageARB.StreamDraw);

        _gl.BindVertexArray(_vaoLabelId);

        _gl.VertexAttribPointer(aPos, 3, VertexAttribPointerType.Float, false,
            8 * sizeof(float), 0);

        _gl.EnableVertexAttribArray(aPos);

        _gl.VertexAttribPointer(aColor, 3, VertexAttribPointerType.Float, false,
            8 * sizeof(float), 3 * sizeof(float));

        _gl.EnableVertexAttribArray(aColor);

        _gl.VertexAttribPointer(aTex, 2, VertexAttribPointerType.Float, false,
            8 * sizeof(float), 6 * sizeof(float));

        _gl.EnableVertexAttribArray(aTex);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _eboLabelId);

        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, 6 * sizeof(uint), nint.Zero,
            BufferUsageARB.StreamDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);

        var vert = LoadShaderSource("shader.vert");
        var frag = LoadShaderSource("shader.frag");

        _shader = new Shader(_gl, vert, frag);

        _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        _gl.ClearStencil(0);
        _gl.ClearDepth(1.0f);
        // _gl.DepthFunc(DepthFunction.Lequal);
        // _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void OnRender(double dt)
    {
        _gl.Viewport(_window!.Size);

        _gl.Clear(ClearBufferMask.ColorBufferBit
                  | ClearBufferMask.DepthBufferBit
                  | ClearBufferMask.StencilBufferBit);

        _shader!.Use();

        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,
            (float)800 / 600, 0.1f, 100f);

        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, _zoom), Vector3.Zero, Vector3.UnitY);

        var model = Matrix4x4.CreateRotationZ(MathHelper.ToRadians(_roll))
                    * Matrix4x4.CreateRotationX(MathHelper.ToRadians(_pitch))
                    * Matrix4x4.CreateRotationY(MathHelper.ToRadians(_yaw));

        _shader.SetMatrix4X4("model", model);
        _shader.SetMatrix4X4("view", view);
        _shader.SetMatrix4X4("projection", projection);

        _gl.LineWidth(1.0f);
        _gl.Uniform1(_shader.GetUniformLocation("isTextured"), 0);
        _vaoGrid?.Bind();
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_gridLines.Length / 6);
        _gl.Uniform1(_shader.GetUniformLocation("isTextured"), 1);

        RenderText("0", 1.05f, -0.1f, 0.95f, 0.6f, 0.6f, 0.6f, 0.02f);
        RenderText("pix", 1.05f, -0.1f, 0.0f, 0.6f, 0.6f, 0.6f, 0.02f);
        RenderText("0", -0.95f, -0.1f, 1.05f, 0.6f, 0.6f, 0.6f, 0.02f);
        RenderText("pix", 0.00f, -0.1f, 1.05f, 0.6f, 0.6f, 0.6f, 0.02f);
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        _gl.Viewport(newSize);
    }

    private void FontTextureGeneration(string fontPath, uint fontSize)
    {
        var library = new Library();
        var face = new Face(library, fontPath);
        face.SetPixelSizes(0, fontSize);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        for (byte i = 0; i < CharsetLength; i++)
        {
            var tmpChar = (char)i;
            face.LoadChar(tmpChar, LoadFlags.Render, LoadTarget.Normal);

            var texture = _gl.GenTexture();
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, texture);

            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.R8,
                (uint)face.Glyph.Bitmap.Width,
                (uint)face.Glyph.Bitmap.Rows, 0, PixelFormat.Red, PixelType.UnsignedByte,
                face.Glyph.Bitmap.Buffer);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR,
                (int)GLEnum.One);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG,
                (int)GLEnum.One);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB,
                (int)GLEnum.One);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA,
                (int)GLEnum.Red);

            var charGl = new CharacterGl
            {
                TextureId = texture,
                Width = face.Glyph.Bitmap.Width,
                Height = face.Glyph.Bitmap.Rows,
                ShiftLeft = face.Glyph.BitmapLeft,
                ShiftTop = face.Glyph.BitmapTop,
                Advance = face.Glyph.Advance.X.ToInt32()
            };

            _glCharacters.Add(tmpChar, charGl);
        }

        face.Dispose();
        library.Dispose();
    }

    private unsafe void RenderText(
        string text,
        float x,
        float y,
        float z,
        float cR,
        float cG,
        float cB,
        float scale)
    {
        _gl.BindVertexArray(_vaoLabelId);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.Uniform1(_shader!.GetUniformLocation("glyphTexture"), 0);
        _gl.Uniform1(_shader.GetUniformLocation("isTextured"), 1);

        foreach (var c in text)
        {
            var xpos = x + _glCharacters[c].ShiftLeft * scale;
            var ypos = y - (_glCharacters[c].Height - _glCharacters[c].ShiftTop) * scale;
            var w = _glCharacters[c].Width * scale;
            var h = _glCharacters[c].Height * scale;

            float[] charVertices =
            [
                xpos, ypos + h, z, cR, cG, cB, 0.0f, 0.0f,
                xpos, ypos, z, cR, cG, cB, 0.0f, 1.0f,
                xpos + w, ypos, z, cR, cG, cB, 1.0f, 1.0f,
                xpos + w, ypos + h, z, cR, cG, cB, 1.0f, 0.0f
            ];

            uint[] indices = [0, 1, 2, 2, 3, 0];

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboLabelId);


            fixed (void* ptr = charVertices)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(charVertices.Length * sizeof(float)),
                    ptr,
                    BufferUsageARB.StreamDraw);
            }


            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _eboLabelId);


            fixed (void* indicesPtr = indices)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                    (nuint)indices.Length * sizeof(uint),
                    indicesPtr,
                    BufferUsageARB.StreamDraw);
            }


            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _glCharacters[c].TextureId);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length,
                DrawElementsType.UnsignedInt, null);
            x += _glCharacters[c].Advance  * scale;
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private static string LoadShaderSource(string filePath)
    {
        try
        {
            var shaderSource = File.ReadAllText(filePath);
            return shaderSource;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Shader file not found: {filePath}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading shader: {ex.Message}");
            return string.Empty;
        }
    }

    private static List<float> GridLineGeneration(float fillPercent, int numGridlines)
    {
        List<float> gridVertices = [];

        var step = fillPercent * 2 / numGridlines;
        var stepLegend = step / 2;

        for (var i = 0; i <= numGridlines; i++)
        {
            var offset = -fillPercent + (i * step);
            // bottom lines X
            gridVertices.AddRange([-fillPercent, 0f, offset, 0.6f, 0.6f, 0.6f]);
            gridVertices.AddRange([fillPercent, 0f, offset, 0.6f, 0.6f, 0.6f]);
            // bottom lines Z
            gridVertices.AddRange([offset, 0f, -fillPercent, 0.6f, 0.6f, 0.6f]);
            gridVertices.AddRange([offset, 0f, fillPercent, 0.6f, 0.6f, 0.6f]);
        }

        //axis
        gridVertices.AddRange([fillPercent, 0f, -fillPercent, 0f, 1f, 0f]); // Y GREEN
        gridVertices.AddRange([fillPercent, fillPercent, -fillPercent, 0f, 1f, 0f]); // Y GREEN    

        gridVertices.AddRange([-fillPercent, 0f, -fillPercent, 1f, 0f, 0f]); // X RED
        gridVertices.AddRange([fillPercent, 0f, -fillPercent, 1f, 0f, 0f]); // X RED
        gridVertices.AddRange([-fillPercent, 0f, -fillPercent, 0f, 0f, 1f]); // Z BLUE
        gridVertices.AddRange([-fillPercent, 0f, fillPercent, 0f, 0f, 1f]); // Z BLUE
        //vertical corners
        gridVertices.AddRange([fillPercent, 0f, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, 0f, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, 0f, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        //top borders
        gridVertices.AddRange([fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([-fillPercent, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([fillPercent, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([fillPercent, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);
        gridVertices.AddRange([fillPercent, fillPercent, fillPercent, 0.6f, 0.6f, 0.6f]);
        //legend ??? texture ????
        gridVertices.AddRange([fillPercent - stepLegend, 0f, -fillPercent, 0.6f, 0.6f, 0.6f]);

        gridVertices.AddRange(
            [fillPercent - stepLegend, fillPercent, -fillPercent, 0.6f, 0.6f, 0.6f]);

        gridVertices.AddRange([fillPercent, 0f, -fillPercent + stepLegend, 0.6f, 0.6f, 0.6f]);

        gridVertices.AddRange(
            [fillPercent, fillPercent, -fillPercent + stepLegend, 0.6f, 0.6f, 0.6f]);

        return gridVertices;
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        _zoom = Math.Clamp(_zoom - (scrollWheel.Y * 0.1f), 0.5f, 10f);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button != MouseButton.Left) return;

        _leftMouseDown = true;
        _lastMousePosition = mouse.Position;
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _leftMouseDown = false;
        }
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (!_leftMouseDown) return;

        var delta = position - _lastMousePosition;
        _lastMousePosition = position;

        const float sensitivity = 0.5f;

        _yaw += delta.X * sensitivity;
        _pitch -= delta.Y
                  * sensitivity;

        _pitch = Math.Clamp(_pitch, -89.0f, 89.0f);
    }
}
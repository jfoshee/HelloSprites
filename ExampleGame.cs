using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using HelloSprites.Interop;

namespace HelloSprites;

public class ExampleGame : IGame
{
    private JSObject _shaderProgram;
    private JSObject _modelMatrixLocation;

    /// <inheritdoc/>
    public async Task Initialize(IShaderLoader shaderLoader)
    {
        // Load the shader program
        var shaderProgram = shaderLoader.LoadShaderProgram("vertex", "fragment");

        // Define quad vertices with positions and texture coordinates
        Span<float> vertices =
        [
            // First Triangle
            -0.5f,  0.5f,  0.0f, 1.0f, // Top-left
        0.5f,  0.5f,  1.0f, 1.0f, // Top-right
        -0.5f, -0.5f,  0.0f, 0.0f, // Bottom-left

        // Second Triangle
        0.5f,  0.5f,  1.0f, 1.0f, // Top-right
        0.5f, -0.5f,  1.0f, 0.0f, // Bottom-right
        -0.5f, -0.5f,  0.0f, 0.0f  // Bottom-left
        ];

        // Create and bind the position and texture buffer
        var positionBuffer = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, positionBuffer);
        GL.BufferData(GL.ARRAY_BUFFER, vertices, GL.STATIC_DRAW);

        // Get attribute locations
        var posLoc = GL.GetAttribLocation(shaderProgram, "aPosition");
        var texLoc = GL.GetAttribLocation(shaderProgram, "aTexCoord");

        // Enable position attribute
        GL.VertexAttribPointer(index: posLoc,
                               size: 2,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: 4 * sizeof(float),
                               offset: 0);
        GL.EnableVertexAttribArray(posLoc);

        // Enable texture coordinate attribute
        GL.VertexAttribPointer(index: texLoc,
                               size: 2,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: 4 * sizeof(float),
                               offset: 2 * sizeof(float));
        GL.EnableVertexAttribArray(texLoc);

        // Get uniform location for the model matrix
        var modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "uModelMatrix");

        // Load and bind texture
        var textureId = await LoadTexture("/checker.png");
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, textureId);
        GL.Uniform1i(GL.GetUniformLocation(shaderProgram, "uTexture"), 0);

        // Set clear color to cornflower blue
        GL.ClearColor(0.392f, 0.584f, 0.929f, 1.0f);

        // Store the shader program and matrix location for later use
        _shaderProgram = shaderProgram;
        _modelMatrixLocation = modelMatrixLocation;
    }

    private async Task<JSObject> LoadTexture(string url)
    {
        // Load image using JS interop
        var image = await Utility.LoadImageFromUrl(url);
        var tex = GL.CreateTexture();
        GL.BindTexture(GL.TEXTURE_2D, tex);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.LINEAR);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.NEAREST);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
        GL.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, image);
        return tex;
    }

    private Vector3 position;
    private Vector3 velocity = new(0.2f, 0.1f, 0.0f);

    /// <inheritdoc/>
    public void Render()
    {
        GL.Clear(GL.COLOR_BUFFER_BIT);

        // First quad: move it to the left
        var modelMatrix = Matrix4x4.CreateTranslation(position);
        GL.UniformMatrix4fv(_modelMatrixLocation, false, ref modelMatrix);
        GL.DrawArrays(GL.TRIANGLES, 0, 6);

        // Second quad: move it to the right
        modelMatrix = Matrix4x4.CreateTranslation(0.75f, 0.0f, 0.0f);
        GL.UniformMatrix4fv(_modelMatrixLocation, false, ref modelMatrix);
        GL.DrawArrays(GL.TRIANGLES, 0, 6);
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        // Update position from velocity
        position += velocity * (float)deltaTime.TotalSeconds;
        // Clamp position to screen bounds
        position.X = Math.Clamp(position.X, -1.0f, 1.0f);
        position.Y = Math.Clamp(position.Y, -1.0f, 1.0f);
        // Mirror velocity when hitting screen bounds
        if (position.X == -1.0f || position.X == 1.0f)
        {
            velocity.X *= -1.0f;
        }
        if (position.Y == -1.0f || position.Y == 1.0f)
        {
            velocity.Y *= -1.0f;
        }
    }


    #region Unused Interface Methods


    /// <inheritdoc/>
    public void FixedUpdate(TimeSpan deltaTime)
    {
    }

    /// <inheritdoc/>
    public void OnKeyPress(string key, bool pressed)
    {
    }

    /// <inheritdoc/>
    public void OnMouseClick(int button, bool pressed, float x, float y)
    {
        x = 2 * x - 1;
        y = 2 * y - 1;
        position = new Vector3(x, y, 0.0f);
    }

    /// <inheritdoc/>
    public void OnMouseMove(float x, float y)
    {
    }

    /// <inheritdoc/>
    public void OnTouchStart(float x, float y)
    {
    }

    /// <inheritdoc/>
    public void OnTouchMove(float x, float y)
    {
    }

    /// <inheritdoc/>
    public void OnTouchEnd()
    {
    }
    #endregion
}

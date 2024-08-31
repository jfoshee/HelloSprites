using System.Runtime.InteropServices.JavaScript;
using HelloSprites.Interop;

namespace HelloSprites;

public class ExampleGame : IGame
{
    private const int SpawnParticleCount = 100;
    private JSObject? _modelMatrixLocation;
    private readonly List<Particle> _particles = new(1_000);
    private readonly Random random = new();

    public ExampleGame()
    {
        // initialize particles to random positions and velocities and scales
        Vector3 center = Vector3.Zero;
        SpawnParticles(center);
    }

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
        _modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "uModelMatrix");

        // Load and bind texture
        var textureId = await LoadTexture("/splat.png");
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, textureId);
        GL.Uniform1i(GL.GetUniformLocation(shaderProgram, "uTexture"), 0);

        // Enable alpha blending for the textures which have an alpha channel
        GL.Enable(GL.BLEND);
        GL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);

        // Set clear color to cornflower blue
        GL.ClearColor(0.392f, 0.584f, 0.929f, 1.0f);
    }

    private static async Task<JSObject> LoadTexture(string url)
    {
        // Load image using JS interop
        var image = await Utility.LoadImageFromUrl(url);
        var texture = GL.CreateTexture();
        GL.BindTexture(GL.TEXTURE_2D, texture);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, GL.LINEAR_MIPMAP_LINEAR);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.LINEAR);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, GL.CLAMP_TO_EDGE);
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, GL.CLAMP_TO_EDGE);
        GL.TexImage2D(GL.TEXTURE_2D, 0, GL.RGBA, GL.RGBA, GL.UNSIGNED_BYTE, image);
        GL.GenerateMipmap(GL.TEXTURE_2D);
        return texture;
    }

    /// <inheritdoc/>
    public void Render()
    {
        if (_modelMatrixLocation is null)
            throw new InvalidOperationException("Model matrix location is not set");

        GL.Clear(GL.COLOR_BUFFER_BIT);
        foreach (var particle in _particles)
        {
            // Create the model matrix for each particle
            var modelMatrix = Matrix4x4.CreateScale(particle.Scale) *
                              Matrix4x4.CreateTranslation(particle.Position);

            // Send the model matrix to the shader
            GL.UniformMatrix4fv(_modelMatrixLocation, false, ref modelMatrix);

            // Draw the quad (assuming it's already bound and configured)
            GL.DrawArrays(GL.TRIANGLES, 0, 6);
        }
    }

    /// <inheritdoc/>
    public void Update(TimeSpan deltaTime)
    {
        // Remove dead particles
        _particles.RemoveAll(p => p.Dead);
    }

    /// <inheritdoc/>
    public void FixedUpdate(TimeSpan deltaTime)
    {
        foreach (var particle in _particles)
        {
            particle.Update(deltaTime);
        }
    }


    private void SpawnParticles(Vector3 center)
    {
        for (int i = 0; i < SpawnParticleCount; i++)
        {
            var position = center;
            var velocity = new Vector3(
                (float)random.NextDouble() * 2 - 1,
                (float)random.NextDouble() * 2 - 1,
                0);
            var scale = (float)random.NextDouble() * 0.1f + 0.1f;
            _particles.Add(new Particle(position, velocity, scale));
        }
    }

    /// <inheritdoc/>
    public void OnMouseClick(int button, bool pressed, float x, float y)
    {
        if (pressed)
        {
            // From normalized screen space to GL NDC
            x = 2 * x - 1;
            y = 2 * y - 1;
            SpawnParticles(new Vector3(x, y, 0));
        }
    }

    /// <inheritdoc/>
    public void OnTouchStart(float x, float y) => OnMouseClick(0, true, x, y);

    #region Unused Interface Methods
    /// <inheritdoc/>
    public void OnKeyPress(string key, bool pressed)
    {
    }

    /// <inheritdoc/>
    public void OnMouseMove(float x, float y)
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

using System.Runtime.InteropServices.JavaScript;
using HelloSprites.Interop;

namespace HelloSprites;

public class ExampleGame : IGame
{
    private const int SpawnParticleCount = 100;
    private readonly List<Particle> _particles = new(1_000);
    private readonly Random random = new();
    private JSObject? _shaderProgram;
    private JSObject? _instanceVBO;
    private JSObject? _positionBuffer;

    public ExampleGame()
    {
        // initialize particles to random positions and velocities and scales
        Vector3 center = Vector3.Zero;
        SpawnParticles(center);
    }

    public async Task Initialize(IShaderLoader shaderLoader)
    {
        // Load the shader program
        _shaderProgram = shaderLoader.LoadShaderProgram("vertex", "fragment");

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

        // Create and bind the position and texture buffer for the quad vertices
        _positionBuffer = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, _positionBuffer);
        GL.BufferData(GL.ARRAY_BUFFER, vertices, GL.STATIC_DRAW);

        // Get attribute locations for position and texture coordinates
        var posLoc = GL.GetAttribLocation(_shaderProgram, "aPosition");
        var texLoc = GL.GetAttribLocation(_shaderProgram, "aTexCoord");

        // Enable the position attribute
        GL.VertexAttribPointer(index: posLoc,
                               size: 2,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: 4 * sizeof(float),
                               offset: 0);
        GL.EnableVertexAttribArray(posLoc);

        // Enable the texture coordinate attribute
        GL.VertexAttribPointer(index: texLoc,
                               size: 2,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: 4 * sizeof(float),
                               offset: 2 * sizeof(float));
        GL.EnableVertexAttribArray(texLoc);

        // Create a buffer for instance data (position and scale)
        _instanceVBO = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, _instanceVBO);

        // Enable alpha blending for the textures which have an alpha channel
        GL.Enable(GL.BLEND);
        GL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);

        // Load and bind texture
        var textureId = await LoadTexture("/splat.png");
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, textureId);
        GL.Uniform1i(GL.GetUniformLocation(_shaderProgram, "uTexture"), 0);

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
        GL.Clear(GL.COLOR_BUFFER_BIT);

        if (_instanceVBO is null || _shaderProgram is null || _positionBuffer is null)
            return;

        int particleCount = _particles.Count;
        Span<float> instanceData = stackalloc float[particleCount * 4]; // vec3 for position + float for scale

        for (int i = 0; i < particleCount; i++)
        {
            instanceData[i * 4 + 0] = _particles[i].Position.X;
            instanceData[i * 4 + 1] = _particles[i].Position.Y;
            instanceData[i * 4 + 2] = _particles[i].Position.Z;
            instanceData[i * 4 + 3] = _particles[i].Scale;
        }

        // Update the instance VBO with the latest data
        GL.BindBuffer(GL.ARRAY_BUFFER, _instanceVBO);
        GL.BufferData(GL.ARRAY_BUFFER, instanceData, GL.STREAM_DRAW);

        // Set up the instance attributes (position and scale)
        int instancePosLoc = GL.GetAttribLocation(_shaderProgram, "aInstancePosition");
        GL.EnableVertexAttribArray(instancePosLoc);
        GL.VertexAttribPointer(instancePosLoc, 3, GL.FLOAT, false, 4 * sizeof(float), 0);
        GL.VertexAttribDivisor(instancePosLoc, 1);

        int instanceScaleLoc = GL.GetAttribLocation(_shaderProgram, "aInstanceScale");
        GL.EnableVertexAttribArray(instanceScaleLoc);
        GL.VertexAttribPointer(instanceScaleLoc, 1, GL.FLOAT, false, 4 * sizeof(float), 3 * sizeof(float));
        GL.VertexAttribDivisor(instanceScaleLoc, 1);

        // Bind the vertex buffer for the quad
        GL.BindBuffer(GL.ARRAY_BUFFER, _positionBuffer);

        // Draw all particles in a single call
        GL.DrawArraysInstanced(GL.TRIANGLES, 0, 6, particleCount);
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

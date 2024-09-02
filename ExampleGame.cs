using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using HelloSprites.Interop;

namespace HelloSprites;

public class ExampleGame : IGame
{
    private const int SpawnParticleCount = 100;
    private readonly List<Particle> _particles = new(1_000);
    private readonly Random _random = new();
    private bool _mouseDown;
    private IEnumerable<Vector2> _spawnPositions = [];
    private JSObject? _shaderProgram;
    private JSObject? _instanceVBO;
    private JSObject? _positionBuffer;

    public string OverlayText => $"Particles: {_particles.Count:N0}";

    public ExampleGame()
    {
        // Spawn some particles at the center of the screen so the user sees something
        SpawnParticles(Vector2.One / 2);
    }

    public async Task Initialize(IShaderLoader shaderLoader)
    {
        // Load the shader program
        _shaderProgram = shaderLoader.LoadShaderProgram("sprite-sheet-vertex", "fragment");

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

        // Create a buffer for instance data (translation and scale)
        _instanceVBO = GL.CreateBuffer();
        GL.BindBuffer(GL.ARRAY_BUFFER, _instanceVBO);

        // Set up the instance attributes (for each member of InstanceData)
        // Setting the divisor to 1 means this attribute is "per instance"
        var instanceDataSize = Marshal.SizeOf<InstanceData>();
        var translationOffset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.Translation)).ToInt32();
        var instancePosLoc = GL.GetAttribLocation(_shaderProgram, "aInstanceTranslation");
        GL.EnableVertexAttribArray(instancePosLoc);
        GL.VertexAttribDivisor(instancePosLoc, 1);
        GL.VertexAttribPointer(index: instancePosLoc,
                               size: Marshal.SizeOf<Vector2>() / sizeof(float), // 2
                               type: GL.FLOAT,
                               normalized: false,
                               stride: instanceDataSize,
                               offset: translationOffset);

        var scaleOffset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.Scale)).ToInt32();
        var instanceScaleLoc = GL.GetAttribLocation(_shaderProgram, "aInstanceScale");
        GL.EnableVertexAttribArray(instanceScaleLoc);
        GL.VertexAttribDivisor(instanceScaleLoc, 1);
        GL.VertexAttribPointer(index: instanceScaleLoc,
                               size: 1,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: instanceDataSize,
                               offset: scaleOffset);

        var spriteIndexOffset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.SpriteIndex)).ToInt32();
        var instanceSpriteIndexLoc = GL.GetAttribLocation(_shaderProgram, "aInstanceSpriteIndex");
        GL.EnableVertexAttribArray(instanceSpriteIndexLoc);
        GL.VertexAttribDivisor(instanceSpriteIndexLoc, 1);
        GL.VertexAttribPointer(index: instanceSpriteIndexLoc,
                               size: 1,
                               type: GL.FLOAT,
                               normalized: false,
                               stride: instanceDataSize,
                               offset: spriteIndexOffset);

        // Enable alpha blending for the textures which have an alpha channel
        GL.Enable(GL.BLEND);
        GL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);

        // Load and bind texture
        var textureId = await LoadTexture("/SpriteSheets/butterfly.png");
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
        GL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, GL.NEAREST); // pixel art
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
        Span<InstanceData> instanceData = stackalloc InstanceData[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            var spriteIndex = _particles[i].SpriteIndex;
            instanceData[i] = new InstanceData(_particles[i].Position.XY(), _particles[i].Scale, spriteIndex);
        }
        // Update the instance VBO with the latest data
        GL.BindBuffer(GL.ARRAY_BUFFER, _instanceVBO);
        GL.BufferData(GL.ARRAY_BUFFER, instanceData, GL.STREAM_DRAW);

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

        // Update particles using wall-clock time so that if the game
        // gets bogged down the particles will die in fewer frames
        foreach (var particle in _particles)
        {
            particle.Update(deltaTime);
        }

        // Spawn new particles based on input
        foreach (var position in _spawnPositions)
        {
            SpawnParticles(position);
        }
    }

    /// <inheritdoc/>
    public void FixedUpdate(TimeSpan deltaTime) { }

    private void SpawnParticles(Vector2 center)
    {
        for (int i = 0; i < SpawnParticleCount; i++)
        {
            var position = ToWorldSpace(center);
            var velocity = ToWorldSpace(new Vector2((float)_random.NextDouble(), (float)_random.NextDouble()));
            var scale = (float)_random.NextDouble() * 0.05f + 0.01f;
            _particles.Add(new Particle(position, velocity, scale));
        }
    }

    /// <inheritdoc/>
    public void OnMouseClick(int button, bool pressed, float x, float y)
    {
        _mouseDown = pressed;
        _spawnPositions = _mouseDown ? ([new Vector2(x, y)]) : ([]);
    }

    /// <inheritdoc/>
    public void OnMouseMove(float x, float y)
    {
        _spawnPositions = _mouseDown ? ([new Vector2(x, y)]) : ([]);
    }

    /// <inheritdoc/>
    public void OnTouchStart(IEnumerable<Vector2> touches) => _spawnPositions = touches;

    /// <inheritdoc/>
    public void OnTouchMove(IEnumerable<Vector2> touches) => _spawnPositions = touches;

    /// <inheritdoc/>
    public void OnTouchEnd(IEnumerable<Vector2> touches) => _spawnPositions = touches;

    /// <inheritdoc/>
    public void OnKeyPress(string key, bool pressed) { }

    private static Vector3 ToWorldSpace(Vector2 screenPosition)
    {
        var x = screenPosition.X * 2 - 1;
        var y = screenPosition.Y * 2 - 1;
        return new Vector3(x, y, 0);
    }
}

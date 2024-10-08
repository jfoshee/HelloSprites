using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using HelloSprites.Interop;

namespace HelloSprites;

public class ExampleGame : IGame
{
    private const int SpawnParticleCount = 25;
    private const float ScaleMin = 0.01f;
    private const float ScaleMax = 0.1f;
    private const float VelocityMin = -0.5f;
    private const float VelocityMax = 0.5f;
    private readonly List<Particle> _particles = new(1_000);
    private readonly Random _random = new();
    private bool _mouseDown;
    private IEnumerable<Vector2> _spawnPositions = [];
    private JSObject? _shaderProgram;
    private JSObject? _instanceVBO;
    private JSObject? _positionBuffer;
    private List<int[]> _frameSetIndices = [[0]];
    private int _fpsMin = 24;
    private int _fpsMax = 24;

    public string OverlayText => $"Particles: {_particles.Count:N0}";

    /// <inheritdoc/>
    public async Task LoadAssetsEssentialAsync(IShaderLoader shaderLoader)
    {
        // Load the shader program
        _shaderProgram = shaderLoader.LoadShaderProgram("sprite-sheet-vertex", "fragment");

        // string texturePath = "./SpriteSheets/magic-fx.png";
        // // Sprite Sheet parameters
        // int columnCount = 20;
        // int rowCount = 11;
        // float paddingRight = 0f;
        // float paddingBottom = 0f;

        // Load the low-res texture
        string texturePath = "./SpriteSheets/arrows-lores.png";
        // Sprite Sheet parameters (see SpriteSheets/arrows.json)
        int columnCount = 8;
        int rowCount = 15;
        float paddingRight = (512 - 480) / 512f;
        float paddingBottom = (1024 - 900) / 1024f;
        var blackArrow = Enumerable.Range(0, 59);
        var blueArrow = Enumerable.Range(59, 58);
        _frameSetIndices = [
            blackArrow.ToArray(),
            blackArrow.Reverse().ToArray(),
            blueArrow.ToArray(),
            blueArrow.Reverse().ToArray()
        ];
        _fpsMin = 24;
        _fpsMax = 120;

        // Load and bind texture
        var textureId = await LoadTexture(texturePath);
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, textureId);
        var textureUniformLoc = GL.GetUniformLocation(_shaderProgram, "uTexture");
        GL.Uniform1i(textureUniformLoc, 0);

        // Setup Sprite Sheet parameters as shader Uniforms
        var uSpriteSheetColumnCountLocation = GL.GetUniformLocation(_shaderProgram, "uSpriteSheetColumnCount");
        var uSpriteSheetRowCountLocation = GL.GetUniformLocation(_shaderProgram, "uSpriteSheetRowCount");
        var uPaddingRightLoc = GL.GetUniformLocation(_shaderProgram, "uPaddingRight");
        var uPaddingBottomLoc = GL.GetUniformLocation(_shaderProgram, "uPaddingBottom");
        GL.Uniform1f(uSpriteSheetColumnCountLocation, columnCount);
        GL.Uniform1f(uSpriteSheetRowCountLocation, rowCount);
        GL.Uniform1f(uPaddingRightLoc, paddingRight);
        GL.Uniform1f(uPaddingBottomLoc, paddingBottom);
    }

    /// <inheritdoc/>
    public void InitializeScene(IShaderLoader shaderLoader)
    {
        if (_shaderProgram is null)
            throw new InvalidOperationException("Shader program not initialized");

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
        var instanceDataSize = Marshal.SizeOf<InstanceData>();

        // Set up TransformRow0 (a vec2 attribute)
        var transformRow0Offset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.TransformRow0)).ToInt32();
        var instanceTransformRow0Loc = GL.GetAttribLocation(_shaderProgram, "aInstanceTransformRow0");
        GL.EnableVertexAttribArray(instanceTransformRow0Loc);
        GL.VertexAttribDivisor(instanceTransformRow0Loc, 1);
        GL.VertexAttribPointer(index: instanceTransformRow0Loc,
                               size: Marshal.SizeOf<Vector2>() / sizeof(float), // 2
                               type: GL.FLOAT,
                               normalized: false,
                               stride: instanceDataSize,
                               offset: transformRow0Offset);

        // Set up TransformRow1 (a vec2 attribute)
        var transformRow1Offset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.TransformRow1)).ToInt32();
        var instanceTransformRow1Loc = GL.GetAttribLocation(_shaderProgram, "aInstanceTransformRow1");
        GL.EnableVertexAttribArray(instanceTransformRow1Loc);
        GL.VertexAttribDivisor(instanceTransformRow1Loc, 1);
        GL.VertexAttribPointer(index: instanceTransformRow1Loc,
                               size: Marshal.SizeOf<Vector2>() / sizeof(float), // 2
                               type: GL.FLOAT,
                               normalized: false,
                               stride: instanceDataSize,
                               offset: transformRow1Offset);

        // Set up Translation (a vec2 attribute)
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

        // Set up SpriteIndex (a float attribute)
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

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

        // Spawn some particles at the center of the screen so the user sees something
        SpawnParticles(Vector2.One / 2);
    }

    /// <inheritdoc/>
    public async Task LoadAssetsExtendedAsync()
    {
        // Load the high-res texture
        string texturePath = "./SpriteSheets/arrows.png";
        var textureId = await LoadTexture(texturePath);
        GL.ActiveTexture(GL.TEXTURE0);
        GL.BindTexture(GL.TEXTURE_2D, textureId);
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
            var spriteIndex = _particles[i].FrameIndex;
            var transform = Matrix3x2.CreateRotation(_particles[i].Rotation) *
                            Matrix3x2.CreateScale(_particles[i].Scale) *
                            Matrix3x2.CreateTranslation(_particles[i].Position.XY());
            instanceData[i] = new InstanceData(transform, spriteIndex);
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
            // Random velocity
            var v_x = (float)_random.NextDouble() * (VelocityMax - VelocityMin) + VelocityMin;
            var v_y = (float)_random.NextDouble() * (VelocityMax - VelocityMin) + VelocityMin;
            var velocity = new Vector3(v_x, v_y, 0);
            // Random scale
            var scale_x = (float)_random.NextDouble() * (ScaleMax - ScaleMin) + ScaleMin;
            var scale_y = (float)_random.NextDouble() * (ScaleMax - ScaleMin) + ScaleMin;
            var scale = new Vector2(scale_x, scale_y);
            // Rotate in direction of velocity
            var rotation = MathF.PI / 2 - MathF.Atan2(velocity.Y, velocity.X);
            // Random sprite based on the frame sets
            var frameSet = _random.Next(_frameSetIndices.Count);
            int[] frameIndices = _frameSetIndices[frameSet];
            var initialFrame = _random.Next(frameIndices.Length);
            var fps = _random.Next(_fpsMin, _fpsMax + 1);
            var particle = new Particle(position, velocity, scale, rotation, frameIndices, initialFrame, fps);
            _particles.Add(particle);
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

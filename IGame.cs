namespace HelloSprites;

public interface IGame : IRenderer
{
    string? OverlayText { get; }

    /// <summary>
    /// Loads essential assets such as textures and sounds asynchronously.
    /// Called before InitializeScene.
    /// Use for loading smaller lower-fidelity assets for first render.
    /// </summary>
    Task LoadAssetsEssentialAsync(IShaderLoader shaderLoader);

    /// <summary>
    /// Creates initial resources for the game scene.
    /// </summary>
    void InitializeScene(IShaderLoader shaderLoader);

    /// <summary>
    /// Called after first Update and Render.
    /// Use for loading larger higher-fidelity assets.
    /// </summary>
    Task LoadAssetsExtendedAsync();

    /// <summary>
    /// Handles keyboard events such as key presses and releases.
    /// </summary>
    /// <param name="key">The name of the key that was pressed or released e.g. "ArrowUp".</param>
    /// <param name="pressed">Indicates whether the key was pressed (true) or released (false).</param>
    void OnKeyPress(string key, bool pressed);

    /// <summary>
    /// Handles mouse click events.
    /// </summary>
    /// <param name="button">The mouse button that was pressed or released.</param>
    /// <param name="pressed">Indicates whether the button was pressed (true) or released (false).</param>
    /// <param name="x">The normalized x-coordinate of the mouse pointer at the time of the click.</param>
    /// <param name="y">The normalized y-coordinate of the mouse pointer at the time of the click.</param>
    void OnMouseClick(int button, bool pressed, float x, float y);

    /// <summary>
    /// Handles mouse move events.
    /// Coordinates are normalized to the range [0, 1] 
    /// with the origin in the bottom-left.
    /// </summary>
    /// <param name="x">The normalized x-coordinate of the mouse pointer.</param>
    /// <param name="y">The normalized y-coordinate of the mouse pointer.</param>
    void OnMouseMove(float x, float y);

    /// <summary>
    /// Handles touch start event.
    /// Coordinates are normalized to the range [0, 1]
    /// </summary>
    void OnTouchStart(IEnumerable<Vector2> touches);

    /// <summary>
    /// Handles touch move event.
    /// Coordinates are normalized to the range [0, 1]
    /// </summary>
    void OnTouchMove(IEnumerable<Vector2> touches);

    /// <summary>
    /// Handles touch end event.
    /// </summary>
    void OnTouchEnd(IEnumerable<Vector2> touches);

    /// <summary>
    /// Updates the game scene based on the elapsed wall-clock time.
    /// </summary>
    /// <param name="deltaTime">The wall-clock time elapsed since the last update.</param>
    void Update(TimeSpan deltaTime);

    /// <summary>
    /// Updates the game scene at a fixed time interval.
    /// Typically used for physics updates.
    /// </summary>
    /// <param name="deltaTime">The fixed time interval for the update.</param>
    void FixedUpdate(TimeSpan deltaTime);
}

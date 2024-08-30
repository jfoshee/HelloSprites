using HelloSprites;
using HelloSprites.Interop;

// Print information about the GL context to demonstrate that WebGL is working
var version = GL.GetParameterString(GL.VERSION);
Console.WriteLine("GL Version: " + version);
var vendor = GL.GetParameterString(GL.VENDOR);
Console.WriteLine("GL Vendor: " + vendor);
var renderer = GL.GetParameterString(GL.RENDERER);
Console.WriteLine("GL Renderer: " + renderer);
var glslVersion = GL.GetParameterString(GL.SHADING_LANGUAGE_VERSION);
Console.WriteLine("GLSL Version: " + glslVersion);

// var image = await Utility.LoadImageFromUrl("/favicon.png");
// if (image is null)
// {
//     throw new InvalidOperationException("Failed to load image");
// }
// Console.WriteLine(image);
// var width = image.GetPropertyAsInt32("width");
// var height = image.GetPropertyAsInt32("height");
// Console.WriteLine($"Image width: {width}, height: {height}");

// Bootstrap our Game which handles input, updates, and rendering
var game = new ExampleGame();
using var gameController = new GameController(game);
await gameController.Start();

// Prevent the main method from exiting so that the game loop (Timer) can continue
while (true)
{
    await Task.Delay(10_000);
}

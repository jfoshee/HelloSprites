using System.Runtime.InteropServices.JavaScript;
using HelloSprites;

// Global namespace to make JS consumption easy

public static partial class RenderLoop
{
    [JSExport]
    public static void Render()
    {
        Singletons.RendererInstance?.Render();
        // NOTE: If you throw an exception here it will kill the render loop
    }
}

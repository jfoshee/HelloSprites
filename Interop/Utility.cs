using System.Runtime.InteropServices.JavaScript;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HelloSprites.Interop;

static partial class Utility
{
    /// <summary>
    /// This should not be called directly.
    /// It is used as part of the shim and called by GL.BufferData
    /// </summary>
    [JSImport("utility.glBufferData", "main.js")]
    public static partial void GlBufferData(int target,
                                            [JSMarshalAs<MemoryView>] Span<byte> data,
                                            int usage);

    [JSImport("utility.loadImageFromUrl", "main.js")]
    internal static partial Task<JSObject> LoadImageFromUrl(string url);

    [JSImport("utility.bytesToFloat32Array", "main.js")]
    internal static partial JSObject ToFloat32Array(
        [JSMarshalAs<MemoryView>]
        Span<byte> data);

    [JSImport("utility.doMatrix", "main.js")]
    internal static partial void DoMatrix(
        JSObject matLocation,
        JSObject matValue);
}

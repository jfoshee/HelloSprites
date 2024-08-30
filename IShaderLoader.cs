using System.Runtime.InteropServices.JavaScript;

namespace HelloSprites;

public interface IShaderLoader
{
    JSObject LoadShaderProgram(string vertexShaderName, string fragmentShaderName);
}

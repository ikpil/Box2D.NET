using System.IO;
using System.Runtime.CompilerServices;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples;

public class SampleAppContext
{
    //
    public readonly string Signature;
    public readonly Glfw glfw;
    public readonly Camera camera;
    public readonly Draw draw;

    //
    public GL gl;
    public unsafe WindowHandle* mainWindow;

#if NDEBUG
    public bool sampleDebug = false;
#else
    public bool sampleDebug = true;
#endif

    private static string CreateSignature(string member, string file, int line)
    {
        return $"{member}() {Path.GetFileName(file)}:{line}";
    }

    public static SampleAppContext Create([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        // for windows - https://learn.microsoft.com/ko-kr/cpp/windows/latest-supported-vc-redist
        var glfw = Glfw.GetApi();
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, glfw);
    }

    public static SampleAppContext CreateWithoutGLFW([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var sig = CreateSignature(member, file, line);
        return CreateFor(sig, null);
    }

    public static SampleAppContext CreateFor(string sig, Glfw glfw)
    {
        var context = new SampleAppContext(sig, glfw);
        return context;
    }

    private SampleAppContext(string signature, Glfw glfw)
    {
        Signature = signature;
        this.glfw = glfw;
        camera = new Camera();
        draw = new Draw();
    }
}
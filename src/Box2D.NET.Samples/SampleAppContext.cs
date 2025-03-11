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

    public static SampleAppContext Create([CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        return CreateFor(member, file, line);
    }

    public static SampleAppContext CreateFor(string member, string file, int line)
    {
        var sig = $"{member}() {Path.GetFileName(file)}:line";
        var context = new SampleAppContext(sig);
        return context;
    }

    private SampleAppContext(string signature)
    {
        // for windows - https://learn.microsoft.com/ko-kr/cpp/windows/latest-supported-vc-redist
        Signature = signature;
        glfw = Glfw.GetApi();
        camera = new Camera();
        draw = new Draw();
    }
}
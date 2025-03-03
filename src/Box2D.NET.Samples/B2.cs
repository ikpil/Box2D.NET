using Silk.NET.GLFW;

namespace Box2D.NET.Samples;

public class B2
{
    public static Draw g_draw;
    public static Camera g_camera;
    public static Glfw g_glfw;
    public static Shader g_shader;
    public static unsafe WindowHandle* g_mainWindow;
    
#if NDEBUG
    public const bool g_sampleDebug = false;
#else
    public const bool g_sampleDebug = true;
#endif

}
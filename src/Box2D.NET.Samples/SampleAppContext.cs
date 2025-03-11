using Silk.NET.GLFW;

namespace Box2D.NET.Samples;

public class SampleAppContext
{
    public Glfw g_glfw;
    public Camera g_camera;
    public Shader g_shader;
    public Draw g_draw;
    public unsafe WindowHandle* g_mainWindow;
}
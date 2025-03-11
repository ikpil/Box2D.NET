using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace Box2D.NET.Samples;

public class SampleAppContext
{
    public Glfw glfw;
    public GL gl;
    public Camera camera;
    public Draw draw;
    public unsafe WindowHandle* mainWindow;

#if NDEBUG
    public bool sampleDebug = false;
#else
    public bool sampleDebug = true;
#endif
}
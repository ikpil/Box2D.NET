using Silk.NET.GLFW;

namespace Box2D.NET.Samples.Helpers;

public static class GlfwHelpers
{
    public static unsafe InputAction GetKey(Keys key)
    {
        var state = B2.g_glfw.GetKey(B2.g_mainWindow, key);
        switch (state)
        {
            case 0: return InputAction.Release;
            case 1: return InputAction.Press;
            case 2: return InputAction.Repeat;
            default: return InputAction.Release;
        }
    }
 
}
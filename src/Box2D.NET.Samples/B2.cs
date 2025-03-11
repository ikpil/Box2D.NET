namespace Box2D.NET.Samples;

public class B2
{
    public static Draw g_draw;
    public static Shader g_shader;
    
#if NDEBUG
    public const bool g_sampleDebug = false;
#else
    public const bool g_sampleDebug = true;
#endif

}
// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Primitives;
using Box2D.NET.Samples.Samples;
using Silk.NET.GLFW;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;


namespace Box2D.NET.Samples;

public class SampleApp
{
    private unsafe WindowHandle* g_mainWindow;
    static int s_selection = 0;
    static Sample s_sample = null;
    static Settings s_settings;
    static bool s_rightMouseDown = false;
    static B2Vec2 s_clickPointWS = b2Vec2_zero;
    static float s_windowScale = 1.0f;
    static float s_framebufferScale = 1.0f;

    public unsafe int Run()
    {
       	// Install memory hooks
        b2SetAllocator( AllocFcn, FreeFcn );
        b2SetAssertFcn( AssertFcn );

        s_settings.Load();
        s_settings.workerCount = b2MinInt( 8, Environment.ProcessorCount / 2);

        SortSamples();

        B2.g_glfw.SetErrorCallback( glfwErrorCallback );

        B2.g_camera.m_width = s_settings.windowWidth;
        B2.g_camera.m_height = s_settings.windowHeight;

        if ( B2.g_glfw.Init())
        {
            Console.WriteLine( "Failed to initialize GLFW" );
            return -1;
        }

        string buffer = string.Empty;
        
    #if __APPLE__
        string glslVersion = "#version 150";
    #else
        string glslVersion = string.Empty;
    #endif

        B2.g_glfw.WindowHint( WindowHintInt.ContextVersionMajor, 3 );
        B2.g_glfw.WindowHint( WindowHintInt.ContextVersionMinor, 3 );
        B2.g_glfw.WindowHint( WindowHintBool.OpenGLForwardCompat, true);
        B2.g_glfw.WindowHint( WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        // MSAA
        B2.g_glfw.WindowHint( WindowHintInt.Samples, 4 );

        B2Version version = b2GetVersion();
        buffer += $"Box2D Version {version.major}.{version.minor}.{version.revision}";

        Monitor* primaryMonitor = B2.g_glfw.GetPrimaryMonitor();
        if (null != primaryMonitor)
        {
    #ifdef __APPLE__
            B2.g_glfw.GetMonitorContentScale( primaryMonitor, out s_framebufferScale, out s_framebufferScale );
    #else
            B2.g_glfw.GetMonitorContentScale( primaryMonitor, out s_windowScale, out s_windowScale );
    #endif
        }

        bool fullscreen = false;
        if ( fullscreen )
        {
            g_mainWindow = B2.g_glfw.CreateWindow( (int)(1920 * s_windowScale) , (int)(1080 * s_windowScale) , buffer, B2.g_glfw.GetPrimaryMonitor(), null);
        }
        else
        {
            g_mainWindow = B2.g_glfw.CreateWindow((int)(B2.g_camera.m_width * s_windowScale), (int)(B2.g_camera.m_height * s_windowScale), buffer, null, null);
        }

        if ( g_mainWindow == null)
        {
            Console.WriteLine("Failed to open GLFW g_mainWindow." );
            B2.g_glfw.Terminate();
            return -1;
        }

    #ifdef __APPLE__
        B2.g_glfw.GetWindowContentScale( g_mainWindow, &s_framebufferScale, &s_framebufferScale );
    #else
        B2.g_glfw.GetWindowContentScale( g_mainWindow, &s_windowScale, &s_windowScale );
    #endif

        B2.g_glfw.MakeContextCurrent( g_mainWindow );

        // Load OpenGL functions using glad
        if ( !gladLoadGL() )
        {
            fprintf( stderr, "Failed to initialize glad\n" );
            B2.g_glfw.Terminate();
            return -1;
        }

        Console.WriteLine( "GL %d.%d\n", GLVersion.major, GLVersion.minor );
        Console.WriteLine( "OpenGL %s, GLSL %s\n", glGetString( GL_VERSION ), glGetString( GL_SHADING_LANGUAGE_VERSION ) );

        B2.g_glfw.SetWindowSizeCallback( g_mainWindow, ResizeWindowCallback );
        B2.g_glfw.SetKeyCallback( g_mainWindow, KeyCallback );
        B2.g_glfw.SetCharCallback( g_mainWindow, CharCallback );
        B2.g_glfw.SetMouseButtonCallback( g_mainWindow, MouseButtonCallback );
        B2.g_glfw.SetCursorPosCallback( g_mainWindow, MouseMotionCallback );
        B2.g_glfw.SetScrollCallback( g_mainWindow, ScrollCallback );

        // todo put this in s_settings
        CreateUI( g_mainWindow, glslVersion );
        B2.g_draw.Create();

        s_settings.sampleIndex = b2ClampInt( s_settings.sampleIndex, 0, g_sampleCount - 1 );
        s_selection = s_settings.sampleIndex;

        glClearColor( 0.2f, 0.2f, 0.2f, 1.0f );

        float frameTime = 0.0;

        while ( !B2.g_glfw.WindowShouldClose( g_mainWindow ) )
        {
            double time1 = B2.g_glfw.GetTime();

            if ( GetKey(, GLFW_KEY_Z ) == InputAction.Press )
            {
                // Zoom out
                B2.g_camera.m_zoom = b2MinFloat( 1.005f * B2.g_camera.m_zoom, 100.0f );
            }
            else if ( GetKey(, GLFW_KEY_X ) == InputAction.Press )
            {
                // Zoom in
                B2.g_camera.m_zoom = b2MaxFloat( 0.995f * B2.g_camera.m_zoom, 0.5f );
            }

            B2.g_glfw.GetWindowSize( g_mainWindow, &B2.g_camera.m_width, &B2.g_camera.m_height );
            B2.g_camera.m_width = int( B2.g_camera.m_width / s_windowScale );
            B2.g_camera.m_height = int( B2.g_camera.m_height / s_windowScale );

            int bufferWidth, bufferHeight;
            B2.g_glfw.GetFramebufferSize( g_mainWindow, &bufferWidth, &bufferHeight );
            B2.g_shader.gl.Viewport( 0, 0, bufferWidth, bufferHeight );

            B2.g_shader.gl.Clear( GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT );

            //B2.g_draw.DrawBackground();

            double cursorPosX = 0, cursorPosY = 0;
            B2.g_glfw.GetCursorPos( g_mainWindow, &cursorPosX, &cursorPosY );
            ImGui_ImplGlfw_CursorPosCallback( g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale );
            ImGui_ImplOpenGL3_NewFrame();
            ImGui_ImplGlfw_NewFrame();
            ImGui_ImplGlfw_CursorPosCallback( g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale );

            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize.X = (float) B2.g_camera.m_width ;
            io.DisplaySize.Y = (float) B2.g_camera.m_height ;
            io.DisplayFramebufferScale.X = bufferWidth / (float) B2.g_camera.m_width ;
            io.DisplayFramebufferScale.Y = bufferHeight / (float) B2.g_camera.m_height ;

            ImGui.NewFrame();

            ImGui.SetNextWindowPos( new Vector2( 0.0f, 0.0f ) );
            ImGui.SetNextWindowSize( new Vector2( float( B2.g_camera.m_width ), float( B2.g_camera.m_height ) ) );
            ImGui.SetNextWindowBgAlpha( 0.0f );
            ImGui.Begin( "Overlay", nullptr,
                          ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
                              ImGuiWindowFlags.NoScrollbar );
            ImGui.End();

            if ( s_sample == nullptr )
            {
                // delayed creation because imgui doesn't create fonts until NewFrame() is called
                s_sample = g_sampleEntries[s_settings.sampleIndex].createFcn( s_settings );
            }

            if ( B2.g_draw.m_showUI )
            {
                const SampleEntry& entry = g_sampleEntries[s_settings.sampleIndex];
                snprintf( buffer, 128, "%s : %s", entry.category, entry.name );
                s_sample.DrawTitle( buffer );
            }

            s_sample.Step( s_settings );

            B2.g_draw.Flush();

            UpdateUI();

            // ImGui.ShowDemoWindow();

            if ( B2.g_draw.m_showUI )
            {
                snprintf( buffer, 128, "%.1f ms - step %d - camera (%g, %g, %g)", 1000.0f * frameTime, s_sample.m_stepCount,
                          B2.g_camera.m_center.x, B2.g_camera.m_center.y, B2.g_camera.m_zoom );
                // snprintf( buffer, 128, "%.1f ms", 1000.0f * frameTime );

                ImGui.Begin( "Overlay", nullptr,
                              ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
                                  ImGuiWindowFlags.NoScrollbar );
                ImGui.SetCursorPos( new Vector2( 5.0f, B2.g_camera.m_height - 20.0f ) );
                ImGui.TextColored( new Vector4( 153, 230, 153, 255 ), "%s", buffer );
                ImGui.End();
            }

            ImGui.Render();
            ImGui_ImplOpenGL3_RenderDrawData( ImGui.GetDrawData() );

            B2.g_glfw.SwapBuffers( g_mainWindow );

            // For the Tracy profiler
            //FrameMark;

            if ( s_selection != s_settings.sampleIndex )
            {
                B2.g_camera.ResetView();
                s_settings.sampleIndex = s_selection;

                // #todo restore all drawing settings that may have been overridden by a sample
                s_settings.subStepCount = 4;
                s_settings.drawJoints = true;
                s_settings.useCameraBounds = false;

                delete s_sample;
                s_sample = nullptr;
                s_sample = g_sampleEntries[s_settings.sampleIndex].createFcn( s_settings );
            }

            B2.g_glfw.PollEvents();

            // Limit frame rate to 60Hz
            double time2 = B2.g_glfw.GetTime();
            double targetTime = time1 + 1.0 / 60.0;
            while ( time2 < targetTime )
            {
                b2Yield();
                time2 = B2.g_glfw.GetTime();
            }

            frameTime = float( time2 - time1 );
        }

        delete s_sample;
        s_sample = nullptr;

        B2.g_draw.Destroy();

        DestroyUI();
        B2.g_glfw.Terminate();

        s_settings.Save();
 
    }

    public static bool IsPowerOfTwo( int x )
    {
        return ( x != 0 ) && ( ( x & ( x - 1 ) ) == 0 );
    }

    public static byte[] AllocFcn( uint size, int alignment )
    {
        // Allocation must be a multiple of alignment or risk a seg fault
        // https://en.cppreference.com/w/c/memory/aligned_alloc
        Debug.Assert( IsPowerOfTwo( alignment ) );
        size_t sizeAligned = ( ( size - 1 ) | ( alignment - 1 ) ) + 1;
        Debug.Assert( ( sizeAligned & ( alignment - 1 ) ) == 0 );

    #if defined( _WIN64 ) || defined( _WIN32 )
        void* ptr = _aligned_malloc( sizeAligned, alignment );
    #else
        void* ptr = aligned_alloc( alignment, sizeAligned );
    #endif
        Debug.Assert( ptr != nullptr );
        return ptr;
    }

    public static void FreeFcn( byte[] mem )
    {
    #if defined( _WIN64 ) || defined( _WIN32 )
        _aligned_free( mem );
    #else
        free( mem );
    #endif
    }

    public static int AssertFcn( string condition, string fileName, int lineNumber )
    {
        Console.WriteLine( "SAMPLE ASSERTION: %s, %s, line %d\n", condition, fileName, lineNumber );
        return 1;
    }

    public static void glfwErrorCallback( ErrorCode error, string description )
    {
        Console.WriteLine($"GLFW error occurred. Code: {error}. Description: {description}");
    }

    public static int CompareSamples( object a, object b )
    {
        SampleEntry* sa = (SampleEntry*)a;
        SampleEntry* sb = (SampleEntry*)b;

        int result = strcmp( sa.category, sb.category );
        if ( result == 0 )
        {
            result = strcmp( sa.name, sb.name );
        }

        return result;
    }

    private static void SortSamples()
    {
        qsort( g_sampleEntries, g_sampleCount, sizeof( SampleEntry ), CompareSamples );
    }

    private static void RestartSample()
    {
        delete s_sample;
        s_sample = nullptr;
        s_settings.restart = true;
        s_sample = g_sampleEntries[s_settings.sampleIndex].createFcn( s_settings );
        s_settings.restart = false;
    }

    private static void CreateUI( WindowHandle* window, string glslVersion )
    {
        IMGUI_CHECKVERSION();
        ImGui.CreateContext();

        bool success = ImGui_ImplGlfw_InitForOpenGL( window, false );
        if ( success == false )
        {
            Console.WriteLine( "ImGui_ImplGlfw_InitForOpenGL failed\n" );
            Debug.Assert( false );
        }

        success = ImGui_ImplOpenGL3_Init( glslVersion );
        if ( success == false )
        {
            Console.WriteLine( "ImGui_ImplOpenGL3_Init failed\n" );
            Debug.Assert( false );
        }

        string fontPath = "samples/data/droid_sans.ttf";
        FILE* file = fopen( fontPath, "rb" );

        if ( file != nullptr )
        {
            ImFontConfig fontConfig;
            fontConfig.RasterizerMultiply = s_windowScale * s_framebufferScale;
            B2.g_draw.m_smallFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 14.0f, &fontConfig );
            B2.g_draw.m_regularFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 18.0f, &fontConfig );
            B2.g_draw.m_mediumFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 40.0f, &fontConfig );
            B2.g_draw.m_largeFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 64.0f, &fontConfig );
        }
        else
        {
            Console.WriteLine( "\n\nERROR: the Box2D samples working directory must be the top level Box2D directory (same as README.md)\n\n" );
            exit( EXIT_FAILURE );
        }
    }

    public static void DestroyUI()
    {
        ImGui_ImplOpenGL3_Shutdown();
        ImGui_ImplGlfw_Shutdown();
        ImGui.DestroyContext();
    }

    public static void ResizeWindowCallback( WindowHandle*, int width, int height )
    {
        B2.g_camera.m_width = int( width / s_windowScale );
        B2.g_camera.m_height = int( height / s_windowScale );
        s_settings.windowWidth = int( width / s_windowScale );
        s_settings.windowHeight = int( height / s_windowScale );
    }

    static void KeyCallback( WindowHandle* window, int key, int scancode, int action, int mods )
    {
        ImGui_ImplGlfw_KeyCallback( window, key, scancode, action, mods );
        if ( ImGui.GetIO().WantCaptureKeyboard )
        {
            return;
        }

        if ( action == InputAction.Press )
        {
            switch ( key )
            {
                case GLFW_KEY_ESCAPE:
                    // Quit
                    B2.g_glfw.SetWindowShouldClose( g_mainWindow, GL_TRUE );
                    break;

                case GLFW_KEY_LEFT:
                    // Pan left
                    if ( mods == GLFW_MOD_CONTROL )
                    {
                        B2Vec2 newOrigin = { 2.0f, 0.0f };
                        s_sample.ShiftOrigin( newOrigin );
                    }
                    else
                    {
                        B2.g_camera.m_center.x -= 0.5f;
                    }
                    break;

                case GLFW_KEY_RIGHT:
                    // Pan right
                    if ( mods == GLFW_MOD_CONTROL )
                    {
                        B2Vec2 newOrigin = { -2.0f, 0.0f };
                        s_sample.ShiftOrigin( newOrigin );
                    }
                    else
                    {
                        B2.g_camera.m_center.x += 0.5f;
                    }
                    break;

                case GLFW_KEY_DOWN:
                    // Pan down
                    if ( mods == GLFW_MOD_CONTROL )
                    {
                        B2Vec2 newOrigin = { 0.0f, 2.0f };
                        s_sample.ShiftOrigin( newOrigin );
                    }
                    else
                    {
                        B2.g_camera.m_center.y -= 0.5f;
                    }
                    break;

                case GLFW_KEY_UP:
                    // Pan up
                    if ( mods == GLFW_MOD_CONTROL )
                    {
                        B2Vec2 newOrigin = { 0.0f, -2.0f };
                        s_sample.ShiftOrigin( newOrigin );
                    }
                    else
                    {
                        B2.g_camera.m_center.y += 0.5f;
                    }
                    break;

                case GLFW_KEY_HOME:
                    B2.g_camera.ResetView();
                    break;

                case GLFW_KEY_R:
                    RestartSample();
                    break;

                case GLFW_KEY_O:
                    s_settings.singleStep = true;
                    break;

                case GLFW_KEY_P:
                    s_settings.pause = !s_settings.pause;
                    break;

                case GLFW_KEY_LEFT_BRACKET:
                    // Switch to previous test
                    --s_selection;
                    if ( s_selection < 0 )
                    {
                        s_selection = g_sampleCount - 1;
                    }
                    break;

                case GLFW_KEY_RIGHT_BRACKET:
                    // Switch to next test
                    ++s_selection;
                    if ( s_selection == g_sampleCount )
                    {
                        s_selection = 0;
                    }
                    break;

                case GLFW_KEY_TAB:
                    B2.g_draw.m_showUI = !B2.g_draw.m_showUI;

                default:
                    if ( s_sample )
                    {
                        s_sample.Keyboard( key );
                    }
            }
        }
    }

    static void CharCallback( WindowHandle* window, unsigned int c )
    {
        ImGui_ImplGlfw_CharCallback( window, c );
    }

    static void MouseButtonCallback( WindowHandle* window, int button, int action, int mods )
    {
        ImGui_ImplGlfw_MouseButtonCallback( window, button, action, mods );

        if ( ImGui.GetIO().WantCaptureMouse )
        {
            return;
        }

        double xd, yd;
        B2.g_glfw.GetCursorPos( g_mainWindow, &xd, &yd );
        B2Vec2 ps = { float( xd ) / s_windowScale, float( yd ) / s_windowScale };

        // Use the mouse to move things around.
        if ( button == (int)MouseButton.Left )
        {
            B2Vec2 pw = B2.g_camera.ConvertScreenToWorld( ps );
            if ( action == InputAction.Press )
            {
                s_sample.MouseDown( pw, button, mods );
            }

            if ( action == GLFW_RELEASE )
            {
                s_sample.MouseUp( pw, button );
            }
        }
        else if ( button == GLFW_MOUSE_BUTTON_2 )
        {
            if ( action == InputAction.Press )
            {
                s_clickPointWS = B2.g_camera.ConvertScreenToWorld( ps );
                s_rightMouseDown = true;
            }

            if ( action == GLFW_RELEASE )
            {
                s_rightMouseDown = false;
            }
        }
    }

    static void MouseMotionCallback( WindowHandle* window, double xd, double yd )
    {
        B2Vec2 ps = { float( xd ) / s_windowScale, float( yd ) / s_windowScale };

        ImGui_ImplGlfw_CursorPosCallback( window, ps.x, ps.y );

        B2Vec2 pw = B2.g_camera.ConvertScreenToWorld( ps );
        s_sample.MouseMove( pw );

        if ( s_rightMouseDown )
        {
            B2Vec2 diff = b2Sub( pw, s_clickPointWS );
            B2.g_camera.m_center.x -= diff.x;
            B2.g_camera.m_center.y -= diff.y;
            s_clickPointWS = B2.g_camera.ConvertScreenToWorld( ps );
        }
    }

    static void ScrollCallback( WindowHandle* window, double dx, double dy )
    {
        ImGui_ImplGlfw_ScrollCallback( window, dx, dy );
        if ( ImGui.GetIO().WantCaptureMouse )
        {
            return;
        }

        if ( dy > 0 )
        {
            B2.g_camera.m_zoom /= 1.1f;
        }
        else
        {
            B2.g_camera.m_zoom *= 1.1f;
        }
    }

    static void UpdateUI()
    {
        int maxWorkers = Environment.ProcessorCount;

        float menuWidth = 180.0f;
        if ( B2.g_draw.m_showUI )
        {
            ImGui.SetNextWindowPos( { B2.g_camera.m_width - menuWidth - 10.0f, 10.0f } );
            ImGui.SetNextWindowSize( { menuWidth, B2.g_camera.m_height - 20.0f } );

            ImGui.Begin( "Tools", &B2.g_draw.m_showUI,
                          ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse );

            if ( ImGui.BeginTabBar( "ControlTabs", ImGuiTabBarFlags.None ) )
            {
                if ( ImGui.BeginTabItem( "Controls" ) )
                {
                    ImGui.PushItemWidth( 100.0f );
                    ImGui.SliderInt( "Sub-steps", &s_settings.subStepCount, 1, 50 );
                    ImGui.SliderFloat( "Hertz", &s_settings.hertz, 5.0f, 120.0f, "%.0f hz" );

                    if ( ImGui.SliderInt( "Workers", &s_settings.workerCount, 1, maxWorkers ) )
                    {
                        s_settings.workerCount = b2ClampInt( s_settings.workerCount, 1, maxWorkers );
                        RestartSample();
                    }
                    ImGui.PopItemWidth();

                    ImGui.Separator();

                    ImGui.Checkbox( "Sleep", &s_settings.enableSleep );
                    ImGui.Checkbox( "Warm Starting", &s_settings.enableWarmStarting );
                    ImGui.Checkbox( "Continuous", &s_settings.enableContinuous );

                    ImGui.Separator();

                    ImGui.Checkbox( "Shapes", &s_settings.drawShapes );
                    ImGui.Checkbox( "Joints", &s_settings.drawJoints );
                    ImGui.Checkbox( "Joint Extras", &s_settings.drawJointExtras );
                    ImGui.Checkbox( "AABBs", &s_settings.drawAABBs );
                    ImGui.Checkbox( "Contact Points", &s_settings.drawContactPoints );
                    ImGui.Checkbox( "Contact Normals", &s_settings.drawContactNormals );
                    ImGui.Checkbox( "Contact Impulses", &s_settings.drawContactImpulses );
                    ImGui.Checkbox( "Friction Impulses", &s_settings.drawFrictionImpulses );
                    ImGui.Checkbox( "Center of Masses", &s_settings.drawMass );
                    ImGui.Checkbox( "Body Names", &s_settings.drawBodyNames );
                    ImGui.Checkbox( "Graph Colors", &s_settings.drawGraphColors );
                    ImGui.Checkbox( "Counters", &s_settings.drawCounters );
                    ImGui.Checkbox( "Profile", &s_settings.drawProfile );

                    Vector2 button_sz = new Vector2( -1, 0 );
                    if ( ImGui.Button( "Pause (P)", button_sz ) )
                    {
                        s_settings.pause = !s_settings.pause;
                    }

                    if ( ImGui.Button( "Single Step (O)", button_sz ) )
                    {
                        s_settings.singleStep = !s_settings.singleStep;
                    }

                    if ( ImGui.Button( "Dump Mem Stats", button_sz ) )
                    {
                        b2World_DumpMemoryStats( s_sample.m_worldId );
                    }

                    if ( ImGui.Button( "Reset Profile", button_sz ) )
                    {
                        s_sample.ResetProfile();
                    }

                    if ( ImGui.Button( "Restart (R)", button_sz ) )
                    {
                        RestartSample();
                    }

                    if ( ImGui.Button( "Quit", button_sz ) )
                    {
                        B2.g_glfw.SetWindowShouldClose( g_mainWindow, GL_TRUE );
                    }

                    ImGui.EndTabItem();
                }

                ImGuiTreeNodeFlags leafNodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
                leafNodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                if ( ImGui.BeginTabItem( "Samples" ) )
                {
                    int categoryIndex = 0;
                    string category = g_sampleEntries[categoryIndex].category;
                    int i = 0;
                    while ( i < g_sampleCount )
                    {
                        bool categorySelected = strcmp( category, g_sampleEntries[s_settings.sampleIndex].category ) == 0;
                        ImGuiTreeNodeFlags nodeSelectionFlags = categorySelected ? ImGuiTreeNodeFlags.Selected : 0;
                        bool nodeOpen = ImGui.TreeNodeEx( category, nodeFlags | nodeSelectionFlags );

                        if ( nodeOpen )
                        {
                            while ( i < g_sampleCount && strcmp( category, g_sampleEntries[i].category ) == 0 )
                            {
                                ImGuiTreeNodeFlags selectionFlags = 0;
                                if ( s_settings.sampleIndex == i )
                                {
                                    selectionFlags = ImGuiTreeNodeFlags.Selected;
                                }
                                ImGui.TreeNodeEx( (void*)(intptr_t)i, leafNodeFlags | selectionFlags, "%s",
                                                   g_sampleEntries[i].name );
                                if ( ImGui.IsItemClicked() )
                                {
                                    s_selection = i;
                                }
                                ++i;
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            while ( i < g_sampleCount && strcmp( category, g_sampleEntries[i].category ) == 0 )
                            {
                                ++i;
                            }
                        }

                        if ( i < g_sampleCount )
                        {
                            category = g_sampleEntries[i].category;
                            categoryIndex = i;
                        }
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();

            s_sample.UpdateUI();
        }
    }
 
}

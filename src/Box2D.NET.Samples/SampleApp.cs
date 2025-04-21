// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Box2D.NET.Samples.Helpers;
using ImGuiNET;
using Box2D.NET.Samples.Samples;
using Serilog;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Timers;
using ErrorCode = Silk.NET.GLFW.ErrorCode;
using Monitor = Silk.NET.GLFW.Monitor;
using MouseButton = Silk.NET.GLFW.MouseButton;


namespace Box2D.NET.Samples;

public class SampleApp
{
    private static readonly ILogger Logger = Log.ForContext<SampleApp>();

    private IWindow _window;
    private IInputContext _input;
    private ImGuiController _imgui;
    private int s_selection = 0;
    private Sample s_sample = null;
    private SampleAppContext _ctx;
    private Settings s_settings;
    private bool s_rightMouseDown = false;
    private B2Vec2 s_clickPointWS = b2Vec2_zero;
    private float s_windowScale = 1.0f;
    private float s_framebufferScale = 1.0f;
    private float _frameTime = 0.0f;

    public SampleApp()
    {
        s_settings = new Settings();
        _ctx = SampleAppContext.Create();
    }

    public int Run(string[] args)
    {
        // Install memory hooks
        b2SetAllocator(AllocFcn, FreeFcn);
        b2SetAssertFcn(AssertFcn);

        s_settings.Load();
        s_settings.workerCount = b2MinInt(8, Environment.ProcessorCount / 2);

        SampleFactory.Shared.LoadSamples();
        SampleFactory.Shared.SortSamples();

        Window.PrioritizeGlfw();


        _ctx.glfw.SetErrorCallback(glfwErrorCallback);

        _ctx.camera.m_width = s_settings.windowWidth;
        _ctx.camera.m_height = s_settings.windowHeight;

        var options = WindowOptions.Default;
        options.ShouldSwapAutomatically = false;
        if (!_ctx.glfw.Init())
        {
            Logger.Information("Failed to initialize GLFW");
            return -1;
        }

        _ctx.glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _ctx.glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _ctx.glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        _ctx.glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        // MSAA
        //_ctx.glfw.WindowHint(WindowHintInt.Samples, 4);
        options.Samples = 4;

        B2Version version = b2GetVersion();
        options.Title = $"Box2D.NET Version {version.major}.{version.minor}.{version.revision}";

        unsafe
        {
            Monitor* primaryMonitor = _ctx.glfw.GetPrimaryMonitor();
            if (null != primaryMonitor)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _ctx.glfw.GetMonitorContentScale(primaryMonitor, out s_framebufferScale, out s_framebufferScale);
                }
                else
                {
                    _ctx.glfw.GetMonitorContentScale(primaryMonitor, out s_windowScale, out s_windowScale);
                }
            }
        }


        bool fullscreen = false;
        if (fullscreen)
        {
            options.Size = new Vector2D<int>((int)(1920 * s_windowScale), (int)(1080 * s_windowScale));
            //_ctx.g_mainWindow = _ctx.g_glfw.CreateWindow((int)(1920 * s_windowScale), (int)(1080 * s_windowScale), buffer, _ctx.g_glfw.GetPrimaryMonitor(), null);
        }
        else
        {
            options.Size = new Vector2D<int>((int)(_ctx.camera.m_width * s_windowScale), (int)(_ctx.camera.m_height * s_windowScale));
            //_ctx.g_mainWindow = _ctx.g_glfw.CreateWindow((int)(_ctx.g_camera.m_width * s_windowScale), (int)(_ctx.g_camera.m_height * s_windowScale), buffer, null, null);
        }

        _window = Window.Create(options);
        _window.Closing += OnWindowClosingSafe;
        _window.Load += OnWindowLoadSafe;
        _window.Resize += OnWindowResize;
        _window.FramebufferResize += OnWindowFrameBufferResize;
        _window.Update += OnWindowUpdateSafe;
        _window.Render += OnWindowRenderSafe;
        _window.Run();

        _ctx.glfw.Terminate();
        s_settings.Save();

        return 0;
    }

    private void OnWindowClosingSafe()
    {
        try
        {
            OnWindowClosing();
        }
        catch (Exception e)
        {
            Logger.Error(e, "");
        }
    }

    private void OnWindowClosing()
    {
        s_sample?.Dispose();
        s_sample = null;
        _ctx.draw.Destroy();
        DestroyUI();
    }

    private void OnWindowResize(Vector2D<int> resize)
    {
        var width = resize.X;
        var height = resize.Y;
        s_settings.windowWidth = (int)(width / s_windowScale);
        s_settings.windowHeight = (int)(height / s_windowScale);

        _ctx.camera.m_width = (int)(width / s_windowScale);
        _ctx.camera.m_height = (int)(height / s_windowScale);
    }

    private void OnWindowFrameBufferResize(Vector2D<int> resize)
    {
        _ctx.gl.Viewport(0, 0, (uint)resize.X, (uint)resize.Y);
    }

    private void OnWindowLoadSafe()
    {
        try
        {
            OnWindowLoad();
        }
        catch (Exception e)
        {
            Logger.Error(e, "");
        }
    }

    private void OnWindowLoad()
    {
        string glslVersion = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            glslVersion = "#version 150";

        unsafe
        {
            _ctx.mainWindow = (WindowHandle*)_window.Handle;
            if (_ctx.mainWindow == null)
            {
                Logger.Information("Failed to open GLFW _ctx.g_mainWindow.");
                return;
            }

            // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            // {
            //     _ctx.g_glfw.GetWindowContentScale(_ctx.g_mainWindow, out s_framebufferScale, out s_framebufferScale);
            // }
            // else
            // {
            //     _ctx.g_glfw.GetWindowContentScale(_ctx.g_mainWindow, out s_windowScale, out s_windowScale);
            // }

            _ctx.glfw.MakeContextCurrent(_ctx.mainWindow);
        }

        _input = _window.CreateInput();
        // Load OpenGL functions using glad
        _ctx.gl = _window.CreateOpenGL();
        if (null == _ctx.gl)
        {
            Logger.Information("Failed to initialize glad");
            return;
        }

        {
            // Span<float> temp1 = stackalloc float[2];
            // _ctx.gl.GetFloat(GLEnum.AliasedLineWidthRange, temp1);
            //
            // Span<float> temp2 = stackalloc float[2];
            // _ctx.gl.GetFloat(GLEnum.SmoothLineWidthRange, temp2);

            string glVersionString = _ctx.gl.GetStringS(GLEnum.Version);
            string glslVersionString = _ctx.gl.GetStringS(GLEnum.ShadingLanguageVersion);
            Logger.Information($"OpenGL {glVersionString}, GLSL {glslVersionString}");
            // Logger.Information($"OpenGL aliased line width range : [{temp1[0]}, {temp1[1]}]");
            // Logger.Information($"OpenGL smooth line width range : [{temp2[0]}, {temp2[1]}]");
        }

        unsafe
        {
            // _ctx.glfw.SetWindowSizeCallback(_ctx.mainWindow, ResizeWindowCallback);
            // _ctx.glfw.SetFramebufferSizeCallback(_ctx.mainWindow, ResizeFramebufferCallback);
            _ctx.glfw.SetKeyCallback(_ctx.mainWindow, KeyCallback);
            _ctx.glfw.SetCharCallback(_ctx.mainWindow, CharCallback);
            _ctx.glfw.SetMouseButtonCallback(_ctx.mainWindow, MouseButtonCallback);
            _ctx.glfw.SetCursorPosCallback(_ctx.mainWindow, MouseMotionCallback);
            _ctx.glfw.SetScrollCallback(_ctx.mainWindow, ScrollCallback);
        }

        _ctx.draw.Create(_ctx);

        s_settings.sampleIndex = b2ClampInt(s_settings.sampleIndex, 0, SampleFactory.Shared.SampleCount - 1);
        s_selection = s_settings.sampleIndex;

        // todo put this in s_settings
        CreateUI(glslVersion);

        _ctx.gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
    }

    private void OnWindowUpdateSafe(double dt)
    {
        try
        {
            OnWindowUpdate(dt);
        }
        catch (Exception e)
        {
            Logger.Error(e, "");
        }
    }
    
    private void OnWindowUpdate(double dt)
    {
        unsafe
        {
            if (_ctx.glfw.WindowShouldClose(_ctx.mainWindow))
                return;
        }

        double time1 = _ctx.glfw.GetTime();

        if (GlfwHelpers.GetKey(_ctx, Keys.Z) == InputAction.Press)
        {
            // Zoom out
            _ctx.camera.m_zoom = b2MinFloat(1.005f * _ctx.camera.m_zoom, 100.0f);
        }
        else if (GlfwHelpers.GetKey(_ctx, Keys.X) == InputAction.Press)
        {
            // Zoom in
            _ctx.camera.m_zoom = b2MaxFloat(0.995f * _ctx.camera.m_zoom, 0.5f);
        }

        int bufferWidth = 0;
        int bufferHeight = 0;
        double cursorPosX = 0.0d;
        double cursorPosY = 0.0d;
        unsafe
        {
            _ctx.glfw.GetFramebufferSize(_ctx.mainWindow, out bufferWidth, out bufferHeight);

            // _ctx.draw.DrawBackground();

            _ctx.glfw.GetCursorPos(_ctx.mainWindow, out cursorPosX, out cursorPosY);
        }

        // For the Tracy profiler
        //FrameMark;

        if (s_selection != s_settings.sampleIndex)
        {
            _ctx.camera.ResetView();
            s_settings.sampleIndex = s_selection;

            // #todo restore all drawing settings that may have been overridden by a sample
            s_settings.subStepCount = 4;
            s_settings.drawJoints = true;
            s_settings.useCameraBounds = false;

            s_sample?.Dispose();
            s_sample = null;
            s_sample = SampleFactory.Shared.Create(s_settings.sampleIndex, _ctx, s_settings);
        }

        if (s_sample == null)
        {
            // delayed creation because imgui doesn't create fonts until NewFrame() is called
            s_sample = SampleFactory.Shared.Create(s_settings.sampleIndex, _ctx, s_settings);
        }

        s_sample.Step(s_settings);

        _ctx.glfw.PollEvents();

        // Limit frame rate to 60Hz
        double time2 = _ctx.glfw.GetTime();
        double targetTime = time1 + 1.0 / 60.0;
        while (time2 < targetTime)
        {
            b2Yield();
            time2 = _ctx.glfw.GetTime();
        }

        _frameTime = (float)(time2 - time1);

        // ImGui_ImplGlfw_CursorPosCallback(_ctx.g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale);
        // ImGui_ImplOpenGL3_NewFrame();
        // ImGui_ImplGlfw_NewFrame();
        // ImGui_ImplGlfw_CursorPosCallback(_ctx.g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale);
        if (null != _imgui)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_ctx.camera.m_width, _ctx.camera.m_height);
            io.DisplayFramebufferScale = new Vector2(bufferWidth / (float)_ctx.camera.m_width, bufferHeight / (float)_ctx.camera.m_height);
            io.DeltaTime = (float)dt;
            _imgui.Update((float)dt);
        }
    }

    private void OnWindowRenderSafe(double dt)
    {
        try
        {
            OnWindowRender(dt);
        }
        catch (Exception e)
        {
            Logger.Error(e, "");
        }
    }
    
    private void OnWindowRender(double dt)
    {
        _ctx.gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        ImGui.SetNextWindowPos(new Vector2(0.0f, 0.0f));
        ImGui.SetNextWindowSize(new Vector2(_ctx.camera.m_width, _ctx.camera.m_height));
        ImGui.SetNextWindowBgAlpha(0.0f);
        ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);
        ImGui.End();

        if (_ctx.draw.m_showUI)
        {
            var title = SampleFactory.Shared.GetTitle(s_settings.sampleIndex);
            s_sample.DrawTitle(title);
        }

        s_sample.Draw(s_settings);
        _ctx.draw.Flush();

        UpdateUI();

        //ImGui.ShowDemoWindow();

        if (_ctx.draw.m_showUI)
        {
            string buffer = $"{1000.0f * _frameTime:0.0} ms - step {s_sample.m_stepCount} - " +
                            $"camera ({_ctx.camera.m_center.X:G}, {_ctx.camera.m_center.Y:G}, {_ctx.camera.m_zoom:G})";
            // snprintf(buffer, 128, "%.1f ms - step %d - camera (%g, %g, %g)", 1000.0f * _frameTime, s_sample.m_stepCount,
            //     _ctx.g_camera.m_center.x, _ctx.g_camera.m_center.y, _ctx.g_camera.m_zoom);
            // snprintf( buffer, 128, "%.1f ms", 1000.0f * frameTime );

            ImGui.Begin("Overlay",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoScrollbar);
            ImGui.SetCursorPos(new Vector2(5.0f, _ctx.camera.m_height - 20.0f));
            ImGui.TextColored(new Vector4(153, 230, 153, 255), buffer);
            ImGui.End();
        }

        _imgui.Render();
        //ImGui_ImplOpenGL3_RenderDrawData(ImGui.GetDrawData());
        unsafe
        {
            _ctx.glfw.SwapBuffers(_ctx.mainWindow);
        }
    }


    public bool IsPowerOfTwo(int x)
    {
        return (x != 0) && ((x & (x - 1)) == 0);
    }

    public byte[] AllocFcn(uint size, int alignment)
    {
        // Allocation must be a multiple of alignment or risk a seg fault
        // https://en.cppreference.com/w/c/memory/aligned_alloc
        Debug.Assert(IsPowerOfTwo(alignment));
        long sizeAligned = ((size - 1) | (uint)(alignment - 1)) + 1;
        Debug.Assert((sizeAligned & (alignment - 1)) == 0);

// #if defined( _WIN64 ) || defined( _WIN32 )
//         void* ptr = _aligned_malloc( sizeAligned, alignment );
// #else
//         void* ptr = aligned_alloc(alignment, sizeAligned);
// #endif
//         Debug.Assert(ptr != nullptr);
//         return ptr;
        return null;
    }

    private void FreeFcn(byte[] mem)
    {
// #if defined( _WIN64 ) || defined( _WIN32 )
//         _aligned_free( mem );
// #else
//         free(mem);
// #endif
    }

    private int AssertFcn(string condition, string fileName, int lineNumber)
    {
        Logger.Information("SAMPLE ASSERTION: %s, %s, line %d\n", condition, fileName, lineNumber);
        return 1;
    }

    private void glfwErrorCallback(ErrorCode error, string description)
    {
        Logger.Information($"GLFW error occurred. Code: {error}. Description: {description}");
    }

    private void RestartSample()
    {
        s_sample?.Dispose();
        s_sample = null;
        s_settings.restart = true;

        s_sample = SampleFactory.Shared.Create(s_settings.sampleIndex, _ctx, s_settings);
        s_settings.restart = false;
    }

    private void CreateUI(string glslVersion)
    {
        //IMGUI_CHECKVERSION();
        //ImGui.CreateContext();
        //
        // bool success = ImGui_ImplGlfw_InitForOpenGL(window, false);
        // if (success == false)
        // {
        //     Logger.Information("ImGui_ImplGlfw_InitForOpenGL failed\n");
        //     Debug.Assert(false);
        // }
        //
        // success = ImGui_ImplOpenGL3_Init(glslVersion);
        // if (success == false)
        // {
        //     Logger.Information("ImGui_ImplOpenGL3_Init failed\n");
        //     Debug.Assert(false);
        // }
        //

        var fontPath = Path.Combine("data", "droid_sans.ttf");
        if (!File.Exists(fontPath))
        {
            Logger.Information("ERROR: the Box2D samples working directory must be the top level Box2D directory (same as README.md)");
            //exit(EXIT_FAILURE);
            return;
        }


        // for windows : Microsoft Visual C++ Redistributable Package
        // link - https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist
        var imGuiFontConfig = new ImGuiFontConfig(fontPath, 15, null);
        _imgui = new ImGuiController(_ctx.gl, _window, _input, imGuiFontConfig);

        //ImGui.GetStyle().ScaleAllSizes(2);
        //imGuiIo.FontGlobalScale = 2.0f;

        // var imGuiIo = ImGui.GetIO();
        // var s = new ImFontConfig();
        // ImFontConfigPtr fontConfig = new ImFontConfigPtr(&s);
        // fontConfig.RasterizerMultiply = s_windowScale * s_framebufferScale;
        // _ctx.draw.m_smallFont = imGuiIo.Fonts.AddFontFromFileTTF(fontPath, 14.0f, fontConfig, IntPtr.Zero);
        // _ctx.draw.m_regularFont = imGuiIo.Fonts.AddFontFromFileTTF(fontPath, 18.0f, fontConfig, IntPtr.Zero);
        // _ctx.draw.m_mediumFont = imGuiIo.Fonts.AddFontFromFileTTF(fontPath, 40.0f, fontConfig, IntPtr.Zero);
        // _ctx.draw.m_largeFont = imGuiIo.Fonts.AddFontFromFileTTF(fontPath, 64.0f, fontConfig, IntPtr.Zero);
    }

    public void DestroyUI()
    {
        // ImGui_ImplOpenGL3_Shutdown();
        // ImGui_ImplGlfw_Shutdown();
        // ImGui.DestroyContext();
        _imgui.Dispose();
        _imgui = null;
    }

    private unsafe void KeyCallback(WindowHandle* window, Keys key, int scancode, InputAction action, KeyModifiers mods)
    {
        //ImGui_ImplGlfw_KeyCallback(window, key, scancode, action, mods);
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            return;
        }

        if (action == InputAction.Press)
        {
            switch (key)
            {
                case Keys.Escape:
                    // Quit
                    _ctx.glfw.SetWindowShouldClose(_ctx.mainWindow, true);
                    break;

                case Keys.Left:
                    // Pan left
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(2.0f, 0.0f);
                        s_sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _ctx.camera.m_center.X -= 0.5f;
                    }

                    break;

                case Keys.Right:
                    // Pan right
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(-2.0f, 0.0f);
                        s_sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _ctx.camera.m_center.X += 0.5f;
                    }

                    break;

                case Keys.Down:
                    // Pan down
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(0.0f, 2.0f);
                        s_sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _ctx.camera.m_center.Y -= 0.5f;
                    }

                    break;

                case Keys.Up:
                    // Pan up
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(0.0f, -2.0f);
                        s_sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _ctx.camera.m_center.Y += 0.5f;
                    }

                    break;

                case Keys.Home:
                    _ctx.camera.ResetView();
                    break;

                case Keys.R:
                    RestartSample();
                    break;

                case Keys.O:
                    s_settings.singleStep = true;
                    break;

                case Keys.P:
                    s_settings.pause = !s_settings.pause;
                    break;

                case Keys.LeftBracket:
                    // Switch to previous test
                    --s_selection;
                    if (s_selection < 0)
                    {
                        s_selection = SampleFactory.Shared.SampleCount - 1;
                    }

                    break;

                case Keys.RightBracket:
                    // Switch to next test
                    ++s_selection;
                    if (s_selection == SampleFactory.Shared.SampleCount)
                    {
                        s_selection = 0;
                    }

                    break;

                case Keys.Tab:
                    _ctx.draw.m_showUI = !_ctx.draw.m_showUI;
                    break;

                default:
                    if (null != s_sample)
                    {
                        s_sample.Keyboard(key);
                    }

                    break;
            }
        }
    }

    private unsafe void CharCallback(WindowHandle* window, uint c)
    {
        //ImGui_ImplGlfw_CharCallback(window, c);
    }

    private unsafe void MouseButtonCallback(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        //ImGui_ImplGlfw_MouseButtonCallback(window, button, action, mods);
        // var io = ImGui.GetIO();
        // io.AddMouseButtonEvent((float)xd, (float)yd);

        if (ImGui.GetIO().WantCaptureMouse)
        {
            return;
        }

        double xd, yd;
        _ctx.glfw.GetCursorPos(_ctx.mainWindow, out xd, out yd);
        B2Vec2 ps = new B2Vec2((float)(xd / s_windowScale), (float)(yd / s_windowScale));

        // Use the mouse to move things around.
        if (button == (int)MouseButton.Left)
        {
            B2Vec2 pw = _ctx.camera.ConvertScreenToWorld(ps);
            if (action == InputAction.Press)
            {
                s_sample.MouseDown(pw, button, mods);
            }

            if (action == InputAction.Release)
            {
                s_sample.MouseUp(pw, button);
            }
        }
        else if (button == MouseButton.Right)
        {
            if (action == InputAction.Press)
            {
                s_clickPointWS = _ctx.camera.ConvertScreenToWorld(ps);
                s_rightMouseDown = true;
            }

            if (action == InputAction.Release)
            {
                s_rightMouseDown = false;
            }
        }
    }

    private unsafe void MouseMotionCallback(WindowHandle* window, double xd, double yd)
    {
        B2Vec2 ps = new B2Vec2((float)(xd / s_windowScale), (float)(yd / s_windowScale));

        //ImGui_ImplGlfw_CursorPosCallback(window, ps.x, ps.y);

        B2Vec2 pw = _ctx.camera.ConvertScreenToWorld(ps);
        s_sample?.MouseMove(pw);

        if (s_rightMouseDown)
        {
            B2Vec2 diff = b2Sub(pw, s_clickPointWS);
            _ctx.camera.m_center.X -= diff.X;
            _ctx.camera.m_center.Y -= diff.Y;
            s_clickPointWS = _ctx.camera.ConvertScreenToWorld(ps);
        }
    }

    private unsafe void ScrollCallback(WindowHandle* window, double dx, double dy)
    {
        var io = ImGui.GetIO();
        io.AddMouseWheelEvent((float)dx, (float)dy);

        if (io.WantCaptureMouse)
        {
            return;
        }

        if (dy > 0)
        {
            _ctx.camera.m_zoom /= 1.1f;
        }
        else
        {
            _ctx.camera.m_zoom *= 1.1f;
        }
    }

    private void UpdateUI()
    {
        int maxWorkers = (int)(Environment.ProcessorCount * 1.5f);

        float menuWidth = 180.0f;
        if (_ctx.draw.m_showUI)
        {
            ImGui.SetNextWindowPos(new Vector2(_ctx.camera.m_width - menuWidth - 10.0f, 10.0f));
            ImGui.SetNextWindowSize(new Vector2(menuWidth, _ctx.camera.m_height - 20.0f));

            ImGui.Begin("Tools", ref _ctx.draw.m_showUI, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            if (ImGui.BeginTabBar("ControlTabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Controls"))
                {
                    ImGui.PushItemWidth(100.0f);
                    ImGui.SliderInt("Sub-steps", ref s_settings.subStepCount, 1, 50);
                    ImGui.SliderFloat("Hertz", ref s_settings.hertz, 5.0f, 120.0f, "%.0f hz");

                    if (ImGui.SliderInt("Workers", ref s_settings.workerCount, 1, maxWorkers))
                    {
                        s_settings.workerCount = b2ClampInt(s_settings.workerCount, 1, maxWorkers);
                        RestartSample();
                    }

                    ImGui.PopItemWidth();

                    ImGui.Separator();

                    ImGui.Checkbox("Sleep", ref s_settings.enableSleep);
                    ImGui.Checkbox("Warm Starting", ref s_settings.enableWarmStarting);
                    ImGui.Checkbox("Continuous", ref s_settings.enableContinuous);

                    ImGui.Separator();

                    ImGui.Checkbox( "Shapes", ref s_settings.drawShapes );
                    ImGui.Checkbox( "Joints", ref s_settings.drawJoints );
                    ImGui.Checkbox( "Joint Extras", ref s_settings.drawJointExtras );
                    ImGui.Checkbox( "Bounds", ref s_settings.drawBounds );
                    ImGui.Checkbox( "Contact Points", ref s_settings.drawContactPoints );
                    ImGui.Checkbox( "Contact Normals", ref s_settings.drawContactNormals );
                    ImGui.Checkbox( "Contact Impulses", ref s_settings.drawContactImpulses );
                    ImGui.Checkbox( "Contact Features", ref s_settings.drawContactFeatures );
                    ImGui.Checkbox( "Friction Impulses", ref s_settings.drawFrictionImpulses );
                    ImGui.Checkbox( "Mass", ref s_settings.drawMass );
                    ImGui.Checkbox( "Body Names", ref s_settings.drawBodyNames );
                    ImGui.Checkbox( "Graph Colors", ref s_settings.drawGraphColors );
                    ImGui.Checkbox( "Islands", ref s_settings.drawIslands );
                    ImGui.Checkbox( "Counters", ref s_settings.drawCounters );
                    ImGui.Checkbox( "Profile", ref s_settings.drawProfile );

                    Vector2 button_sz = new Vector2(-1, 0);
                    if (ImGui.Button("Pause (P)", button_sz))
                    {
                        s_settings.pause = !s_settings.pause;
                    }

                    if (ImGui.Button("Single Step (O)", button_sz))
                    {
                        s_settings.singleStep = !s_settings.singleStep;
                    }

                    if (ImGui.Button("Dump Mem Stats", button_sz))
                    {
                        b2World_DumpMemoryStats(s_sample.m_worldId);
                    }

                    if (ImGui.Button("Reset Profile", button_sz))
                    {
                        s_sample.ResetProfile();
                    }

                    if (ImGui.Button("Restart (R)", button_sz))
                    {
                        RestartSample();
                    }

                    if (ImGui.Button("Quit", button_sz))
                    {
                        unsafe
                        {
                            _ctx.glfw.SetWindowShouldClose(_ctx.mainWindow, true);
                        }
                    }

                    ImGui.EndTabItem();
                }

                ImGuiTreeNodeFlags leafNodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
                leafNodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                if (ImGui.BeginTabItem("Samples"))
                {
                    int categoryIndex = 0;
                    string category = SampleFactory.Shared.GetCategory(categoryIndex);
                    int i = 0;
                    while (i < SampleFactory.Shared.SampleCount)
                    {
                        bool categorySelected = category == SampleFactory.Shared.GetCategory(s_settings.sampleIndex);
                        ImGuiTreeNodeFlags nodeSelectionFlags = categorySelected ? ImGuiTreeNodeFlags.Selected : 0;
                        bool nodeOpen = ImGui.TreeNodeEx(category, nodeFlags | nodeSelectionFlags);

                        if (nodeOpen)
                        {
                            while (i < SampleFactory.Shared.SampleCount && category == SampleFactory.Shared.GetCategory(i))
                            {
                                ImGuiTreeNodeFlags selectionFlags = 0;
                                if (s_settings.sampleIndex == i)
                                {
                                    selectionFlags = ImGuiTreeNodeFlags.Selected;
                                }

                                ImGui.TreeNodeEx(SampleFactory.Shared.GetName(i), leafNodeFlags | selectionFlags);
                                if (ImGui.IsItemClicked())
                                {
                                    s_selection = i;
                                }

                                ++i;
                            }

                            ImGui.TreePop();
                        }
                        else
                        {
                            while (i < SampleFactory.Shared.SampleCount && category == SampleFactory.Shared.GetCategory(i))
                            {
                                ++i;
                            }
                        }

                        if (i < SampleFactory.Shared.SampleCount)
                        {
                            category = SampleFactory.Shared.GetCategory(i);
                            categoryIndex = i;
                        }
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();

            s_sample.UpdateGui();
        }
    }
}
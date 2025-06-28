// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
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
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
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
    private SampleContext _context;
    private bool s_rightMouseDown = false;
    private B2Vec2 s_clickPointWS = b2Vec2_zero;
    private float s_framebufferScale = 1.0f;
    private float _frameTime = 0.0f;

    public SampleApp()
    {
        _context = SampleContext.Create();
    }

    public int Run(string[] args)
    {
        // Install memory hooks
        b2SetAllocator(AllocFcn, FreeFcn);
        b2SetAssertFcn(AssertFcn);

        _context.settings.Load();
        _context.settings.workerCount = b2MinInt(8, Environment.ProcessorCount / 2);

        _context.camera.m_width = _context.settings.windowWidth;
        _context.camera.m_height = _context.settings.windowHeight;

        SampleFactory.Shared.LoadSamples();
        SampleFactory.Shared.SortSamples();

        Window.PrioritizeGlfw();

        _context.glfw.SetErrorCallback(glfwErrorCallback);

        var options = WindowOptions.Default;
        options.ShouldSwapAutomatically = false;
        if (!_context.glfw.Init())
        {
            Logger.Information("Failed to initialize GLFW");
            return -1;
        }

        _context.glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _context.glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _context.glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        _context.glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        // MSAA
        _context.glfw.WindowHint(WindowHintInt.Samples, 4);
        options.Samples = 4;

        B2Version version = b2GetVersion();
        options.Title = $"Box2D.NET Version {version.major}.{version.minor}.{version.revision}";

        unsafe
        {
            Monitor* primaryMonitor = _context.glfw.GetPrimaryMonitor();
            if (null != primaryMonitor)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _context.glfw.GetMonitorContentScale(primaryMonitor, out s_framebufferScale, out s_framebufferScale);
                }
                else
                {
                    float uiScale = 1.0f;
                    _context.glfw.GetMonitorContentScale(primaryMonitor, out uiScale, out uiScale);
                    _context.settings.uiScale = uiScale;
                }
            }
        }


        bool fullscreen = false;
        if (fullscreen)
        {
            options.Size = new Vector2D<int>(1920, 1080);
            //_context.g_mainWindow = _context.g_glfw.CreateWindow((int)(1920 ), (int)(1080 ), buffer, _ctx.g_glfw.GetPrimaryMonitor(), null);
        }
        else
        {
            options.Size = new Vector2D<int>((int)(_context.camera.m_width), (int)(_context.camera.m_height));
            //_context.g_mainWindow = _ctx.g_glfw.CreateWindow((int)(_ctx.g_camera.m_width * s_windowScale), (int)(_ctx.g_camera.m_height * s_windowScale), buffer, null, null);
        }

        _window = Window.Create(options);
        _window.Closing += OnWindowClosingSafe;
        _window.Load += OnWindowLoadSafe;
        _window.Resize += OnWindowResize;
        _window.FramebufferResize += OnWindowFrameBufferResize;
        _window.Update += OnWindowUpdateSafe;
        _window.Render += OnWindowRenderSafe;
        _window.Run();

        _context.glfw.Terminate();
        _context.settings.Save();

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
        _context.draw.Destroy();
        DestroyUI();
    }

    private void OnWindowResize(Vector2D<int> resize)
    {
        var width = resize.X;
        var height = resize.Y;
        _context.settings.windowWidth = width;
        _context.settings.windowHeight = height;

        _context.camera.m_width = width;
        _context.camera.m_height = height;
    }

    private void OnWindowFrameBufferResize(Vector2D<int> resize)
    {
        _context.gl.Viewport(0, 0, (uint)resize.X, (uint)resize.Y);
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
            _context.window = (WindowHandle*)_window.Handle;
            if (_context.window == null)
            {
                Logger.Information("Failed to open GLFW _ctx.g_mainWindow.");
                return;
            }

            _context.glfw.MakeContextCurrent(_context.window);
        }

        _input = _window.CreateInput();
        // Load OpenGL functions using glad
        _context.gl = _window.CreateOpenGL();
        if (null == _context.gl)
        {
            Logger.Information("Failed to initialize glad");
            return;
        }

        {
            string glVersionString = _context.gl.GetStringS(GLEnum.Version);
            string glslVersionString = _context.gl.GetStringS(GLEnum.ShadingLanguageVersion);
            Logger.Information($"OpenGL {glVersionString}, GLSL {glslVersionString}");
        }

        unsafe
        {
            // _ctx.glfw.SetWindowSizeCallback(_ctx.mainWindow, ResizeWindowCallback);
            // _ctx.glfw.SetFramebufferSizeCallback(_ctx.mainWindow, ResizeFramebufferCallback);
            _context.glfw.SetKeyCallback(_context.window, KeyCallback);
            _context.glfw.SetCharCallback(_context.window, CharCallback);
            _context.glfw.SetMouseButtonCallback(_context.window, MouseButtonCallback);
            _context.glfw.SetCursorPosCallback(_context.window, MouseMotionCallback);
            _context.glfw.SetScrollCallback(_context.window, ScrollCallback);
        }

        _context.draw.Create(_context);

        _context.settings.sampleIndex = b2ClampInt(_context.settings.sampleIndex, 0, SampleFactory.Shared.SampleCount - 1);
        s_selection = _context.settings.sampleIndex;

        // todo put this in _context.settings
        CreateUI(glslVersion);

        _context.gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
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
            if (_context.glfw.WindowShouldClose(_context.window))
                return;
        }

        double time1 = _context.glfw.GetTime();

        if (GlfwHelpers.GetKey(_context, Keys.Z) == InputAction.Press)
        {
            // Zoom out
            _context.camera.m_zoom = b2MinFloat(1.005f * _context.camera.m_zoom, 100.0f);
        }
        else if (GlfwHelpers.GetKey(_context, Keys.X) == InputAction.Press)
        {
            // Zoom in
            _context.camera.m_zoom = b2MaxFloat(0.995f * _context.camera.m_zoom, 0.5f);
        }

        int bufferWidth = 0;
        int bufferHeight = 0;
        double cursorPosX = 0.0d;
        double cursorPosY = 0.0d;
        unsafe
        {
            _context.glfw.GetFramebufferSize(_context.window, out bufferWidth, out bufferHeight);

            // _ctx.draw.DrawBackground();

            _context.glfw.GetCursorPos(_context.window, out cursorPosX, out cursorPosY);
        }

        // For the Tracy profiler
        //FrameMark;

        if (s_selection != _context.settings.sampleIndex)
        {
            _context.camera.ResetView();
            _context.settings.sampleIndex = s_selection;

            // #todo restore all drawing settings that may have been overridden by a sample
            _context.settings.subStepCount = 4;
            _context.settings.drawJoints = true;
            _context.settings.useCameraBounds = true;

            s_sample?.Dispose();
            s_sample = null;
            s_sample = SampleFactory.Shared.Create(_context.settings.sampleIndex, _context);
        }

        if (s_sample == null)
        {
            // delayed creation because imgui doesn't create fonts until NewFrame() is called
            s_sample = SampleFactory.Shared.Create(_context.settings.sampleIndex, _context);
        }

        s_sample.Step();

        _context.glfw.PollEvents();

        // Limit frame rate to 60Hz
        double time2 = _context.glfw.GetTime();
        double targetTime = time1 + 1.0 / 60.0;
        while (time2 < targetTime)
        {
            b2Yield();
            time2 = _context.glfw.GetTime();
        }

        _frameTime = (float)(time2 - time1);

        // ImGui_ImplGlfw_CursorPosCallback(_ctx.g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale);
        // ImGui_ImplOpenGL3_NewFrame();
        // ImGui_ImplGlfw_NewFrame();
        // ImGui_ImplGlfw_CursorPosCallback(_ctx.g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale);
        if (null != _imgui)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_context.camera.m_width, _context.camera.m_height);
            io.DisplayFramebufferScale = new Vector2(bufferWidth / (float)_context.camera.m_width, bufferHeight / (float)_context.camera.m_height);
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
        _context.gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        ImGui.SetNextWindowPos(new Vector2(0.0f, 0.0f));
        ImGui.SetNextWindowSize(new Vector2(_context.camera.m_width, _context.camera.m_height));
        ImGui.SetNextWindowBgAlpha(0.0f);
        ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);
        ImGui.End();

        if (_context.draw.m_showUI)
        {
            var title = SampleFactory.Shared.GetTitle(_context.settings.sampleIndex);
            s_sample.DrawTitle(title);
        }

        s_sample.Draw(_context.settings);
        _context.draw.Flush();

        UpdateUI();

        //ImGui.ShowDemoWindow();

        if (_context.draw.m_showUI)
        {
            string buffer = $"{1000.0f * _frameTime:0.0} ms - step {s_sample.m_stepCount} - " +
                            $"camera ({_context.camera.m_center.X:G}, {_context.camera.m_center.Y:G}, {_context.camera.m_zoom:G})";
            // snprintf(buffer, 128, "%.1f ms - step %d - camera (%g, %g, %g)", 1000.0f * _frameTime, s_sample.m_stepCount,
            //     _ctx.g_camera.m_center.x, _ctx.g_camera.m_center.y, _ctx.g_camera.m_zoom);
            // snprintf( buffer, 128, "%.1f ms", 1000.0f * frameTime );

            ImGui.Begin("Overlay",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoScrollbar);
            ImGui.SetCursorPos(new Vector2(5.0f, _context.camera.m_height - 20.0f));
            ImGui.TextColored(new Vector4(153, 230, 153, 255), buffer);
            ImGui.End();
        }

        _imgui.Render();
        //ImGui_ImplOpenGL3_RenderDrawData(ImGui.GetDrawData());
        unsafe
        {
            _context.glfw.SwapBuffers(_context.window);
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
        B2_ASSERT(IsPowerOfTwo(alignment));
        long sizeAligned = ((size - 1) | (uint)(alignment - 1)) + 1;
        B2_ASSERT((sizeAligned & (alignment - 1)) == 0);

// #if defined( _MSC_VER ) || defined( __MINGW32__ ) || defined( __MINGW64__ )
//         void* ptr = _aligned_malloc( sizeAligned, alignment );
// #else
//         void* ptr = aligned_alloc(alignment, sizeAligned);
// #endif
//         B2_ASSERT(ptr != nullptr);
//         return ptr;
        return null;
    }

    private void FreeFcn(byte[] mem)
    {
// #if defined( _MSC_VER ) || defined( __MINGW32__ ) || defined( __MINGW64__ )
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
        _context.settings.restart = true;

        s_sample = SampleFactory.Shared.Create(_context.settings.sampleIndex, _context);
        _context.settings.restart = false;
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
        //     B2_ASSERT(false);
        // }
        //
        // success = ImGui_ImplOpenGL3_Init(glslVersion);
        // if (success == false)
        // {
        //     Logger.Information("ImGui_ImplOpenGL3_Init failed\n");
        //     B2_ASSERT(false);
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
        _imgui = new ImGuiController(_context.gl, _window, _input, imGuiFontConfig);

        ImGui.GetFontSize();
        ImGui.GetStyle().ScaleAllSizes(_context.settings.uiScale);

        unsafe
        {
            // ImFontConfigPtr fontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
            // fontConfig.RasterizerMultiply = _context.settings.uiScale * s_framebufferScale;
            //
            // float regularSize = MathF.Floor(13.0f * _context.settings.uiScale);
            // float mediumSize = MathF.Floor(40.0f * _context.settings.uiScale);
            // float largeSize = MathF.Floor(64.0f * _context.settings.uiScale);
            //
            // var io = ImGui.GetIO();
            //_context.draw.m_regularFont = io.Fonts.AddFontFromFileTTF(fontPath, regularSize, fontConfig);
            // _context.draw.m_mediumFont = io.Fonts.AddFontFromFileTTF(fontPath, mediumSize, fontConfig);
            // _context.draw.m_largeFont = io.Fonts.AddFontFromFileTTF(fontPath, largeSize, fontConfig);

            //io.FontDefault = _context.draw.m_regularFont;
        }
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
                    _context.glfw.SetWindowShouldClose(_context.window, true);
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
                        _context.camera.m_center.X -= 0.5f;
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
                        _context.camera.m_center.X += 0.5f;
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
                        _context.camera.m_center.Y -= 0.5f;
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
                        _context.camera.m_center.Y += 0.5f;
                    }

                    break;

                case Keys.Home:
                    _context.camera.ResetView();
                    break;

                case Keys.R:
                    RestartSample();
                    break;

                case Keys.O:
                    _context.settings.singleStep = true;
                    break;

                case Keys.P:
                    _context.settings.pause = !_context.settings.pause;
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
                    _context.draw.m_showUI = !_context.draw.m_showUI;
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

    private unsafe void MouseButtonCallback(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers modifiers)
    {
        if (ImGui.GetIO().WantCaptureMouse)
        {
            return;
        }

        double xd, yd;
        _context.glfw.GetCursorPos(_context.window, out xd, out yd);
        B2Vec2 ps = new B2Vec2((float)(xd), (float)(yd));

        // Use the mouse to move things around.
        if (button == (int)MouseButton.Left)
        {
            B2Vec2 pw = _context.camera.ConvertScreenToWorld(ps);
            if (action == InputAction.Press)
            {
                s_sample.MouseDown(pw, button, modifiers);
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
                s_clickPointWS = _context.camera.ConvertScreenToWorld(ps);
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
        B2Vec2 ps = new B2Vec2((float)(xd), (float)(yd));

        //ImGui_ImplGlfw_CursorPosCallback(window, ps.x, ps.y);

        B2Vec2 pw = _context.camera.ConvertScreenToWorld(ps);
        s_sample?.MouseMove(pw);

        if (s_rightMouseDown)
        {
            B2Vec2 diff = b2Sub(pw, s_clickPointWS);
            _context.camera.m_center.X -= diff.X;
            _context.camera.m_center.Y -= diff.Y;
            s_clickPointWS = _context.camera.ConvertScreenToWorld(ps);
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
            _context.camera.m_zoom /= 1.1f;
        }
        else
        {
            _context.camera.m_zoom *= 1.1f;
        }
    }

    private void UpdateUI()
    {
        int maxWorkers = (int)(Environment.ProcessorCount * 1.5f);

        float fontSize = ImGui.GetFontSize();
        float menuWidth = 13.0f * fontSize;
        if (_context.draw.m_showUI)
        {
            ImGui.SetNextWindowPos(new Vector2(_context.camera.m_width - menuWidth - 0.5f * fontSize, 0.5f * fontSize));
            ImGui.SetNextWindowSize(new Vector2(menuWidth, _context.camera.m_height - fontSize));

            ImGui.Begin("Tools", ref _context.draw.m_showUI, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            if (ImGui.BeginTabBar("ControlTabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Controls"))
                {
                    ImGui.PushItemWidth(100.0f);
                    ImGui.SliderInt("Sub-steps", ref _context.settings.subStepCount, 1, 32);
                    ImGui.SliderFloat("Hertz", ref _context.settings.hertz, 5.0f, 240.0f, "%.0f hz");

                    if (ImGui.SliderInt("Workers", ref _context.settings.workerCount, 1, maxWorkers))
                    {
                        _context.settings.workerCount = b2ClampInt(_context.settings.workerCount, 1, maxWorkers);
                        RestartSample();
                    }

                    ImGui.PopItemWidth();

                    ImGui.Separator();

                    ImGui.Checkbox("Sleep", ref _context.settings.enableSleep);
                    ImGui.Checkbox("Warm Starting", ref _context.settings.enableWarmStarting);
                    ImGui.Checkbox("Continuous", ref _context.settings.enableContinuous);

                    ImGui.Separator();

                    ImGui.Checkbox("Shapes", ref _context.settings.drawShapes);
                    ImGui.Checkbox("Joints", ref _context.settings.drawJoints);
                    ImGui.Checkbox("Joint Extras", ref _context.settings.drawJointExtras);
                    ImGui.Checkbox("Bounds", ref _context.settings.drawBounds);
                    ImGui.Checkbox("Contact Points", ref _context.settings.drawContactPoints);
                    ImGui.Checkbox("Contact Normals", ref _context.settings.drawContactNormals);
                    ImGui.Checkbox("Contact Impulses", ref _context.settings.drawContactImpulses);
                    ImGui.Checkbox("Contact Features", ref _context.settings.drawContactFeatures);
                    ImGui.Checkbox("Friction Impulses", ref _context.settings.drawFrictionImpulses);
                    ImGui.Checkbox("Mass", ref _context.settings.drawMass);
                    ImGui.Checkbox("Body Names", ref _context.settings.drawBodyNames);
                    ImGui.Checkbox("Graph Colors", ref _context.settings.drawGraphColors);
                    ImGui.Checkbox("Islands", ref _context.settings.drawIslands);
                    ImGui.Checkbox("Counters", ref _context.settings.drawCounters);
                    ImGui.Checkbox("Profile", ref _context.settings.drawProfile);

                    Vector2 button_sz = new Vector2(-1, 0);
                    if (ImGui.Button("Pause (P)", button_sz))
                    {
                        _context.settings.pause = !_context.settings.pause;
                    }

                    if (ImGui.Button("Single Step (O)", button_sz))
                    {
                        _context.settings.singleStep = !_context.settings.singleStep;
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
                            _context.glfw.SetWindowShouldClose(_context.window, true);
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
                        bool categorySelected = category == SampleFactory.Shared.GetCategory(_context.settings.sampleIndex);
                        ImGuiTreeNodeFlags nodeSelectionFlags = categorySelected ? ImGuiTreeNodeFlags.Selected : 0;
                        bool nodeOpen = ImGui.TreeNodeEx(category, nodeFlags | nodeSelectionFlags);

                        if (nodeOpen)
                        {
                            while (i < SampleFactory.Shared.SampleCount && category == SampleFactory.Shared.GetCategory(i))
                            {
                                ImGuiTreeNodeFlags selectionFlags = 0;
                                if (_context.settings.sampleIndex == i)
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
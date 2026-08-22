// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime;
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
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Timers;
using static Box2D.NET.Samples.Graphics.Draws;
using static Box2D.NET.Samples.Graphics.Cameras;
using static Box2D.NET.Samples.Samples.Sample;
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
    private SampleContext _context;
    private bool s_rightMouseDown = false;
    private B2Vec2 s_clickPointWS = b2Vec2_zero;
    private float s_framebufferScale = 1.0f;
    private float _frameTime = 0.0f;
    private double _frameStartTime = 0.0;
    private byte[] _fontData;
    private GCHandle _fontDataHandle;

    public SampleApp()
    {
        _context = SampleContext.Create();
    }

    public int Run(string[] args)
    {
        // Install memory hooks
        b2SetAllocator(AllocFcn, FreeFcn);
        b2SetAssertFcn(AssertFcn);

        _context.Load();
        _context.workerCount = b2MinInt(8, Environment.ProcessorCount / 2);

        SampleFactory.Shared.LoadSamples();
        SampleFactory.Shared.SortSamples();

        var currentCulture = CultureInfo.CurrentCulture;
        string bitness = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

        var workingDirectory = Directory.GetCurrentDirectory();
        Logger.Information($"Working directory - {workingDirectory}");
        Logger.Information($"OS Version - {Environment.OSVersion} {bitness}");
        Logger.Information($"{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}");
        Logger.Information($"{RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}");
        Logger.Information($"Dotnet - {Environment.Version.ToString()} culture({currentCulture.Name})");
        Logger.Information($"Processor Count : {Environment.ProcessorCount}");

        Logger.Information($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
        Logger.Information($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");
        Logger.Information("");

        Logger.Information($"ImGui.Net - version({ImGui.GetVersion()})");

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
        options.Title = $"Box2D.NET Version {version.major}.{version.minor}.{version.revision}, {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}";

        unsafe
        {
            Monitor* primaryMonitor = _context.glfw.GetPrimaryMonitor();
            if (null != primaryMonitor)
            {
                float contentScale = 1.0f;
                _context.glfw.GetMonitorContentScale(primaryMonitor, out contentScale, out contentScale);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _context.uiScale = 1.0f;
                    s_framebufferScale = contentScale;
                }
                else
                {
                    _context.uiScale = contentScale;
                    s_framebufferScale = 1.0f;
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
            options.Size = new Vector2D<int>((int)(_context.camera.width), (int)(_context.camera.height));
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
        Settings.Save(_context);

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
        _context.sample?.Dispose();
        DestroyDraw(_context.draw);
        DestroyUI();
    }

    private void OnWindowResize(Vector2D<int> resize)
    {
        var width = resize.X;
        var height = resize.Y;

        _context.camera.width = width;
        _context.camera.height = height;
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

        // todo put this in _context.settings
        CreateUI(glslVersion);
        _context.draw = CreateDraw(_context);

        _context.sampleIndex = b2ClampInt(_context.sampleIndex, 0, SampleFactory.Shared.SampleCount - 1);

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

        _frameStartTime = _context.glfw.GetTime();

        if (GlfwHelpers.GetKey(_context, Keys.Z) == InputAction.Press)
        {
            // Zoom out
            _context.camera.zoom = b2MinFloat(1.005f * _context.camera.zoom, 100.0f);
        }
        else if (GlfwHelpers.GetKey(_context, Keys.X) == InputAction.Press)
        {
            // Zoom in
            _context.camera.zoom = b2MaxFloat(0.995f * _context.camera.zoom, 0.5f);
        }

        int bufferWidth = 0;
        int bufferHeight = 0;
        unsafe
        {
            _context.glfw.GetFramebufferSize(_context.window, out bufferWidth, out bufferHeight);

            // _ctx.draw.DrawBackground();
        }

        if (null != _imgui)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_context.camera.width, _context.camera.height);

            // These can be zero if the window is minimized
            if (_context.camera.width > 0.0f && _context.camera.height > 0.0f)
            {
                // Framebuffer/window ratio: 1 on Windows/Linux, 2 on a Retina display. Drives
                // both UI magnification and font rasterizer density.
                io.DisplayFramebufferScale = new Vector2(bufferWidth / (float)_context.camera.width,
                    bufferHeight / (float)_context.camera.height);
            }

            io.DeltaTime = (float)dt;
            _imgui.Update((float)dt);
        }

        if (_context.sample == null)
        {
            // delayed creation because imgui doesn't create fonts until NewFrame() is called
            _context.sample = SampleFactory.Shared.Create(_context.sampleIndex, _context);
        }

        _context.sample.ResetText();
        _context.sample.Step();
        _context.sample.Draw();
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

        FlushDraw(_context.draw, _context.camera);

        DrawUI(_context, _frameTime);

        //ImGui.ShowDemoWindow();


        _imgui.Render();
        //ImGui_ImplOpenGL3_RenderDrawData(ImGui.GetDrawData());
        unsafe
        {
            _context.glfw.SwapBuffers(_context.window);
        }

        // For the Tracy profiler
        //FrameMark;

        // Silk.NET.Windowing polls events for the run loop.

        // Limit frame rate to 60Hz
        double time2 = _context.glfw.GetTime();
        double targetTime = _frameStartTime + 1.0 / 60.0;
        while (time2 < targetTime)
        {
            b2Yield();
            time2 = _context.glfw.GetTime();
        }

        _frameTime = (float)(time2 - _frameStartTime);
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

    private void FreeFcn(byte[] mem, uint size)
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

    private static void ApplyUIStyle()
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        // Metrics: containers round at 4px, controls at 3px - one deliberate
        // system instead of the stock mix. Padding gives rows room to breathe.
        style.WindowPadding = new Vector2(10.0f, 10.0f);
        style.FramePadding = new Vector2(8.0f, 4.0f);
        style.CellPadding = new Vector2(6.0f, 4.0f);
        style.ItemSpacing = new Vector2(8.0f, 7.0f);
        style.ItemInnerSpacing = new Vector2(7.0f, 4.0f);
        style.IndentSpacing = 18.0f;
        style.ScrollbarSize = 12.0f;
        style.GrabMinSize = 10.0f;

        style.WindowBorderSize = 1.0f;
        style.FrameBorderSize = 0.0f;
        style.PopupBorderSize = 1.0f;
        style.TabBorderSize = 0.0f;
        style.SeparatorTextBorderSize = 1.0f;

        style.WindowRounding = 4.0f;
        style.ChildRounding = 4.0f;
        style.PopupRounding = 4.0f;
        style.FrameRounding = 3.0f;
        style.GrabRounding = 3.0f;
        style.ScrollbarRounding = 3.0f;
        style.TabRounding = 3.0f;

        style.WindowTitleAlign = new Vector2(0.0f, 0.5f);

        // Palette: neutral charcoal surfaces, one steel-blue accent at three
        // brightnesses. Replaces stock ImGui's saturated cornflower blue.
        Vector4 accent = new Vector4(0.28f, 0.48f, 0.66f, 1.00f);
        Vector4 accentHi = new Vector4(0.38f, 0.60f, 0.80f, 1.00f);
        Vector4 accentLo = new Vector4(0.22f, 0.36f, 0.50f, 1.00f);

        style.Colors[(int)ImGuiCol.Text] = new Vector4(0.90f, 0.91f, 0.93f, 1.00f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.49f, 0.51f, 0.55f, 1.00f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.110f, 0.115f, 0.125f, 0.97f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.100f, 0.105f, 0.115f, 0.98f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.00f, 0.00f, 0.00f, 0.45f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.18f, 0.19f, 0.21f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.24f, 0.26f, 0.29f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.29f, 0.32f, 0.36f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.090f, 0.095f, 0.105f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.14f, 0.16f, 0.19f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.090f, 0.095f, 0.105f, 0.75f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.13f, 0.14f, 0.16f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.06f, 0.06f, 0.07f, 0.55f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.28f, 0.30f, 0.33f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.36f, 0.39f, 0.43f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = accent;
        style.Colors[(int)ImGuiCol.CheckMark] = accentHi;
        style.Colors[(int)ImGuiCol.SliderGrab] = accent;
        style.Colors[(int)ImGuiCol.SliderGrabActive] = accentHi;
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.22f, 0.24f, 0.27f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = accentLo;
        style.Colors[(int)ImGuiCol.ButtonActive] = accent;
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.19f, 0.21f, 0.24f, 1.00f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = accentLo;
        style.Colors[(int)ImGuiCol.HeaderActive] = accent;
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(1.00f, 1.00f, 1.00f, 0.09f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = accentLo;
        style.Colors[(int)ImGuiCol.SeparatorActive] = accent;
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = accentLo;
        style.Colors[(int)ImGuiCol.ResizeGripActive] = accent;
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.15f, 0.16f, 0.18f, 1.00f);
        style.Colors[(int)ImGuiCol.TabHovered] = accentLo;
        style.Colors[(int)ImGuiCol.TabActive] = accent;
        style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.12f, 0.13f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = accentLo;
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(accent.X, accent.Y, accent.Z, 0.40f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = accentHi;
        style.Colors[(int)ImGuiCol.NavHighlight] = accentHi;
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.70f, 0.72f, 0.75f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = accentHi;
        style.Colors[(int)ImGuiCol.PlotHistogram] = accent;
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = accentHi;
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

        if (_context.uiScale != 1.0f || s_framebufferScale != 1.0f)
        {
            // ImGui.NET 1.90 does not expose AddFontDefaultVector, so use the existing font as an embedded resource.
            using Stream stream = typeof(SampleApp).Assembly.GetManifestResourceStream("Box2D.NET.Samples.Fonts.droid_sans.ttf")!;
            _fontData = new byte[stream.Length];
            stream.ReadExactly(_fontData);
            _fontDataHandle = GCHandle.Alloc(_fontData, GCHandleType.Pinned);
        }

        // for windows : Microsoft Visual C++ Redistributable Package
        // link - https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist
        _imgui = new ImGuiController(_context.gl, _window, _input, () =>
        {
            ApplyUIStyle();

            ImGuiStylePtr style = ImGui.GetStyle();
            style.ScaleAllSizes(_context.uiScale);

            ImGuiIOPtr io = ImGui.GetIO();
            if (_context.uiScale == 1.0f && s_framebufferScale == 1.0f)
            {
                io.Fonts.AddFontDefault();
            }
            else
            {
                unsafe
                {
                    ImFontConfigPtr fontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
                    fontConfig.FontDataOwnedByAtlas = false;

                    float regularSize = MathF.Floor(13.0f * _context.uiScale * s_framebufferScale);
                    io.Fonts.AddFontFromMemoryTTF(_fontDataHandle.AddrOfPinnedObject(), _fontData.Length, regularSize, fontConfig);
                    io.FontGlobalScale = 1.0f / s_framebufferScale;
                    ImGuiNative.ImFontConfig_destroy(fontConfig.NativePtr);
                }
            }
        });
    }

    public void DestroyUI()
    {
        var tmp = _imgui;
        _imgui = null;
        tmp.Dispose();
        if (_fontDataHandle.IsAllocated)
        {
            _fontDataHandle.Free();
        }
        _fontData = null;
    }

    private unsafe void KeyCallback(WindowHandle* window, Keys key, int scancode, InputAction action, KeyModifiers mods)
    {
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
        bool down = action != InputAction.Release;
        ImGuiKey imguiKey = ToImGuiKey(key);
        if (imguiKey != ImGuiKey.None)
        {
            io.AddKeyEvent(imguiKey, down);
        }
        // The custom GLFW callback replaces the input backend's callback, so feed both the physical
        // modifier keys and their aggregate state into ImGui before handling sample shortcuts.
        io.AddKeyEvent(ImGuiKey.ModCtrl, (mods & KeyModifiers.Control) != 0);
        io.AddKeyEvent(ImGuiKey.ModShift, (mods & KeyModifiers.Shift) != 0);
        io.AddKeyEvent(ImGuiKey.ModAlt, (mods & KeyModifiers.Alt) != 0);
        io.AddKeyEvent(ImGuiKey.ModSuper, (mods & KeyModifiers.Super) != 0);
        if (io.WantCaptureKeyboard)
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
                    _context.camera.center.X -= 0.5f;
                    break;

                case Keys.Right:
                    // Pan right
                    _context.camera.center.X += 0.5f;
                    break;

                case Keys.Down:
                    _context.camera.center.Y -= 0.5f;
                    break;

                case Keys.Up:
                    _context.camera.center.Y += 0.5f;
                    break;

                case Keys.Home:
                    ResetView(_context.camera);
                    break;

                case Keys.R:
                    SelectSample(_context, _context.sampleIndex, true);
                    break;

                case Keys.O:
                    if (mods == KeyModifiers.Control)
                    {
                        _context.showUI = true;
                        _context.openSamplePicker = true;
                    }
                    else
                    {
                        _context.singleStep = true;
                    }
                    break;

                case Keys.P:
                    _context.pause = !_context.pause;
                    break;

                case Keys.LeftBracket:
                    // Switch to previous test
                    {
                        int selection = _context.sampleIndex - 1;
                        if (selection < 0)
                        {
                            selection = SampleFactory.Shared.SampleCount - 1;
                        }

                        SelectSample(_context, selection, false);
                    }

                    break;

                case Keys.RightBracket:
                    // Switch to next test
                    {
                        int selection = _context.sampleIndex + 1;
                        if (selection == SampleFactory.Shared.SampleCount)
                        {
                            selection = 0;
                        }

                        SelectSample(_context, selection, false);
                    }

                    break;

                case Keys.Tab:
                    _context.showUI = !_context.showUI;
                    break;

                case Keys.M:
                    _context.showMetrics = !_context.showMetrics;
                    break;

                default:
                    if (_context.sample != null)
                    {
                        _context.sample.Keyboard(key);
                    }

                    break;
            }
        }
    }

    private unsafe void CharCallback(WindowHandle* window, uint c)
    {
        if (null == _imgui)
        {
            return;
        }

        // Text input must always reach ImGui. The previous callback discarded every character,
        // leaving InputText widgets visible and focusable but impossible to edit.
        ImGui.GetIO().AddInputCharacter(c);
    }

    private static ImGuiKey ToImGuiKey(Keys key)
    {
        return key switch
        {
            Keys.Tab => ImGuiKey.Tab,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.Home => ImGuiKey.Home,
            Keys.End => ImGuiKey.End,
            Keys.Delete => ImGuiKey.Delete,
            Keys.Backspace => ImGuiKey.Backspace,
            Keys.Enter => ImGuiKey.Enter,
            Keys.KeypadEnter => ImGuiKey.KeypadEnter,
            Keys.Escape => ImGuiKey.Escape,
            Keys.A => ImGuiKey.A,
            Keys.C => ImGuiKey.C,
            Keys.L => ImGuiKey.L,
            Keys.V => ImGuiKey.V,
            Keys.X => ImGuiKey.X,
            Keys.Y => ImGuiKey.Y,
            Keys.Z => ImGuiKey.Z,
            Keys.ShiftLeft => ImGuiKey.LeftShift,
            Keys.ShiftRight => ImGuiKey.RightShift,
            Keys.ControlLeft => ImGuiKey.LeftCtrl,
            Keys.ControlRight => ImGuiKey.RightCtrl,
            Keys.AltLeft => ImGuiKey.LeftAlt,
            Keys.AltRight => ImGuiKey.RightAlt,
            Keys.SuperLeft => ImGuiKey.LeftSuper,
            Keys.SuperRight => ImGuiKey.RightSuper,
            _ => ImGuiKey.None,
        };
    }

    private unsafe void MouseButtonCallback(WindowHandle* window, MouseButton button, InputAction action, KeyModifiers modifiers)
    {
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
        if (io.WantCaptureMouse)
        {
            return;
        }

        // Silk.NET may dispatch mouse events before the first update creates the delayed sample.
        if (_context.sample == null)
        {
            return;
        }

        double xd, yd;
        _context.glfw.GetCursorPos(_context.window, out xd, out yd);
        B2Vec2 ps = new B2Vec2((float)(xd), (float)(yd));

        // Use the mouse to move things around.
        if (button == (int)MouseButton.Left)
        {
            B2Vec2 pw = ConvertScreenToWorld(_context.camera, ps);
            if (action == InputAction.Press)
            {
                _context.sample.MouseDown(pw, button, modifiers);
            }

            if (action == InputAction.Release)
            {
                _context.sample.MouseUp(pw, button);
            }
        }
        else if (button == MouseButton.Right)
        {
            if (action == InputAction.Press)
            {
                s_clickPointWS = ConvertScreenToWorld(_context.camera, ps);
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
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
        if (io.WantCaptureMouse)
        {
            return;
        }

        // Silk.NET may dispatch mouse events before the first update creates the delayed sample.
        if (_context.sample == null)
        {
            return;
        }

        B2Vec2 ps = new B2Vec2((float)(xd), (float)(yd));

        //ImGui_ImplGlfw_CursorPosCallback(window, ps.x, ps.y);

        B2Vec2 pw = ConvertScreenToWorld(_context.camera, ps);
        _context.sample.MouseMove(pw);

        if (s_rightMouseDown)
        {
            B2Vec2 diff = b2Sub(pw, s_clickPointWS);
            _context.camera.center.X -= diff.X;
            _context.camera.center.Y -= diff.Y;
            s_clickPointWS = ConvertScreenToWorld(_context.camera, ps);
        }
    }

    private unsafe void ScrollCallback(WindowHandle* window, double dx, double dy)
    {
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
        if (io.WantCaptureMouse)
        {
            io.AddMouseWheelEvent((float)dx, (float)dy);
            return;
        }

        _context.glfw.GetCursorPos(_context.window, out double xd, out double yd);
        B2Vec2 ps = new B2Vec2((float)xd, (float)yd);
        B2Vec2 pw1 = ConvertScreenToWorld(_context.camera, ps);

        if (dy > 0)
        {
            _context.camera.zoom /= 1.1f;
        }
        else
        {
            _context.camera.zoom *= 1.1f;
        }

        B2Vec2 pw2 = ConvertScreenToWorld(_context.camera, ps);
        _context.camera.center -= pw2 - pw1;
    }

}

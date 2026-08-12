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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _context.glfw.GetMonitorContentScale(primaryMonitor, out s_framebufferScale, out s_framebufferScale);
                }
                else
                {
                    float uiScale = 1.0f;
                    _context.glfw.GetMonitorContentScale(primaryMonitor, out uiScale, out uiScale);
                    _context.uiScale = uiScale;
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

        _context.draw = CreateDraw(_context);

        _context.sampleIndex = b2ClampInt(_context.sampleIndex, 0, SampleFactory.Shared.SampleCount - 1);

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
            _context.camera.zoom = b2MinFloat(1.005f * _context.camera.zoom, 100.0f);
        }
        else if (GlfwHelpers.GetKey(_context, Keys.X) == InputAction.Press)
        {
            // Zoom in
            _context.camera.zoom = b2MaxFloat(0.995f * _context.camera.zoom, 0.5f);
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

        if (_context.sample == null)
        {
            // delayed creation because imgui doesn't create fonts until NewFrame() is called
            _context.sample = SampleFactory.Shared.Create(_context.sampleIndex, _context);
        }

        _context.sample.Step();

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
            io.DisplaySize = new Vector2(_context.camera.width, _context.camera.height);
            io.DisplayFramebufferScale = new Vector2(bufferWidth / (float)_context.camera.width, bufferHeight / (float)_context.camera.height);
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
        ImGui.SetNextWindowSize(new Vector2(_context.camera.width, _context.camera.height));
        ImGui.SetNextWindowBgAlpha(0.0f);
        ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar);
        ImGui.End();

        _context.sample.ResetText();

        var title = SampleFactory.Shared.GetTitle(_context.sampleIndex);
        _context.sample.DrawColoredTextLine(B2HexColor.b2_colorYellow, title);

        string buffer = $"{1000.0f * _frameTime:0.0} ms - step {_context.sample.m_stepCount} - " +
                        $"camera ({_context.camera.center.X:G}, {_context.camera.center.Y:G}, {_context.camera.zoom:G})";
        DrawScreenString(_context.draw, 5.0f, _context.camera.height - 18.0f, B2HexColor.b2_colorSeaGreen, buffer);

        _context.sample.Draw();
        FlushDraw(_context.draw, _context.camera);

        UpdateSampleUI(_context);

        //ImGui.ShowDemoWindow();


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
        ImGui.GetStyle().ScaleAllSizes(_context.uiScale);

        unsafe
        {
            // ImFontConfigPtr fontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
            // This brightens the font, improving readability when it is small.
            // fontConfig.RasterizerMultiply = _context.uiScale * s_framebufferScale;
            //
            // float regularSize = MathF.Floor(13.0f * _context.uiScale);
            // float mediumSize = MathF.Floor(40.0f * _context.uiScale);
            // float largeSize = MathF.Floor(64.0f * _context.uiScale);
            //
            // var io = ImGui.GetIO();
            //_context.regularFont = io.Fonts.AddFontFromFileTTF(fontPath, regularSize);
            //_context.regularFont = io.Fonts.AddFontFromFileTTF(fontPath, regularSize, fontConfig);
            // _context.mediumFont = io.Fonts.AddFontFromFileTTF(fontPath, mediumSize, fontConfig);
            // _context.largeFont = io.Fonts.AddFontFromFileTTF(fontPath, largeSize, fontConfig);

            //io.FontDefault = _context.regularFont;
        }
    }

    public void DestroyUI()
    {
        var tmp = _imgui;
        _imgui = null;
        tmp.Dispose();
    }

    private unsafe void KeyCallback(WindowHandle* window, Keys key, int scancode, InputAction action, KeyModifiers mods)
    {
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
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
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(2.0f, 0.0f);
                        _context.sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _context.camera.center.X -= 0.5f;
                    }

                    break;

                case Keys.Right:
                    // Pan right
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(-2.0f, 0.0f);
                        _context.sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _context.camera.center.X += 0.5f;
                    }

                    break;

                case Keys.Down:
                    // Pan down
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(0.0f, 2.0f);
                        _context.sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _context.camera.center.Y -= 0.5f;
                    }

                    break;

                case Keys.Up:
                    // Pan up
                    if (0 != ((uint)mods & (uint)KeyModifiers.Control))
                    {
                        B2Vec2 newOrigin = new B2Vec2(0.0f, -2.0f);
                        _context.sample.ShiftOrigin(newOrigin);
                    }
                    else
                    {
                        _context.camera.center.Y += 0.5f;
                    }

                    break;

                case Keys.Home:
                    ResetView(_context.camera);
                    break;

                case Keys.R:
                    SelectSample(_context, _context.sampleIndex, true);
                    break;

                case Keys.O:
                    _context.singleStep = true;
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
        //ImGui_ImplGlfw_CharCallback(window, c);
        if (null == _imgui)
        {
            return;
        }

        var io = ImGui.GetIO();
        if (io.WantCaptureKeyboard)
        {
            return;
        }
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

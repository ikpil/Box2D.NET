// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using Box2D.NET.Primitives;
using Box2D.NET.Samples.Helpers;
using Serilog;
using static Box2D.NET.core;


#define _CRTDBG_MAP_ALLOC
#define IMGUI_DISABLE_OBSOLETE_FUNCTIONS 1

using Box2D.NET.Samples;
using Box2D.NET.Samples.Primitives;
using ImGuiNET;

public class Program
{

    private static void InitializeLogger()
    {
        var format = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} [{ThreadName}:{ThreadId}]{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .WriteTo.Async(c => c.LogMessageBroker(outputTemplate: format))
            .WriteTo.Async(c => c.Console(outputTemplate: format))
            .WriteTo.Async(c => c.File(
                "logs/log.log",
                rollingInterval: RollingInterval.Hour,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: null,
                outputTemplate: format)
            )
            .CreateLogger();
    }

    private static void InitializeWorkingDirectory()
    {
        var path = DirectoryUtils.SearchFile("LICENSE");
        if (!string.IsNullOrEmpty(path))
        {
            var workingDirectory = Path.GetDirectoryName(path) ?? string.Empty;
            workingDirectory = Path.GetFullPath(workingDirectory);
            Directory.SetCurrentDirectory(workingDirectory);
        }
    }

#if defined( _WIN32 ) && 0
#include <crtdbg.h>

static int MyAllocHook( int allocType, void* userData, size_t size, int blockType, long requestNumber,
						const unsigned char* filename, int lineNumber )
{
	// This hook can help find leaks
	if ( size == 143 )
	{
		size += 0;
	}

	return 1;
}
#endif

GLFWwindow* g_mainWindow = nullptr;
static int s_selection = 0;
static Sample* s_sample = nullptr;
static Settings s_settings;
static bool s_rightMouseDown = false;
static b2Vec2 s_clickPointWS = b2Vec2_zero;
static float s_windowScale = 1.0f;
static float s_framebufferScale = 1.0f;

inline bool IsPowerOfTwo( int x )
{
	return ( x != 0 ) && ( ( x & ( x - 1 ) ) == 0 );
}

void* AllocFcn( uint size, int alignment )
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

void FreeFcn( void* mem )
{
#if defined( _WIN64 ) || defined( _WIN32 )
	_aligned_free( mem );
#else
	free( mem );
#endif
}

int AssertFcn( string condition, string fileName, int lineNumber )
{
	printf( "SAMPLE ASSERTION: %s, %s, line %d\n", condition, fileName, lineNumber );
	return 1;
}

void glfwErrorCallback( int error, string description )
{
	fprintf( stderr, "GLFW error occurred. Code: %d. Description: %s\n", error, description );
}

static int CompareSamples( const void* a, const void* b )
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

static void SortSamples()
{
	qsort( g_sampleEntries, g_sampleCount, sizeof( SampleEntry ), CompareSamples );
}

static void RestartSample()
{
	delete s_sample;
	s_sample = nullptr;
	s_settings.restart = true;
	s_sample = g_sampleEntries[s_settings.sampleIndex].createFcn( s_settings );
	s_settings.restart = false;
}

static void CreateUI( GLFWwindow* window, string glslVersion )
{
	IMGUI_CHECKVERSION();
	ImGui.CreateContext();

	bool success = ImGui_ImplGlfw_InitForOpenGL( window, false );
	if ( success == false )
	{
		printf( "ImGui_ImplGlfw_InitForOpenGL failed\n" );
		Debug.Assert( false );
	}

	success = ImGui_ImplOpenGL3_Init( glslVersion );
	if ( success == false )
	{
		printf( "ImGui_ImplOpenGL3_Init failed\n" );
		Debug.Assert( false );
	}

	string fontPath = "samples/data/droid_sans.ttf";
	FILE* file = fopen( fontPath, "rb" );

	if ( file != nullptr )
	{
		ImFontConfig fontConfig;
		fontConfig.RasterizerMultiply = s_windowScale * s_framebufferScale;
		Draw.g_draw.m_smallFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 14.0f, &fontConfig );
		Draw.g_draw.m_regularFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 18.0f, &fontConfig );
		Draw.g_draw.m_mediumFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 40.0f, &fontConfig );
		Draw.g_draw.m_largeFont = ImGui.GetIO().Fonts.AddFontFromFileTTF( fontPath, 64.0f, &fontConfig );
	}
	else
	{
		printf( "\n\nERROR: the Box2D samples working directory must be the top level Box2D directory (same as README.md)\n\n" );
		exit( EXIT_FAILURE );
	}
}

static void DestroyUI()
{
	ImGui_ImplOpenGL3_Shutdown();
	ImGui_ImplGlfw_Shutdown();
	ImGui.DestroyContext();
}

static void ResizeWindowCallback( GLFWwindow*, int width, int height )
{
	Draw.g_camera.m_width = int( width / s_windowScale );
	Draw.g_camera.m_height = int( height / s_windowScale );
	s_settings.windowWidth = int( width / s_windowScale );
	s_settings.windowHeight = int( height / s_windowScale );
}

static void KeyCallback( GLFWwindow* window, int key, int scancode, int action, int mods )
{
	ImGui_ImplGlfw_KeyCallback( window, key, scancode, action, mods );
	if ( ImGui.GetIO().WantCaptureKeyboard )
	{
		return;
	}

	if ( action == GLFW_PRESS )
	{
		switch ( key )
		{
			case GLFW_KEY_ESCAPE:
				// Quit
				glfwSetWindowShouldClose( g_mainWindow, GL_TRUE );
				break;

			case GLFW_KEY_LEFT:
				// Pan left
				if ( mods == GLFW_MOD_CONTROL )
				{
					b2Vec2 newOrigin = { 2.0f, 0.0f };
					s_sample.ShiftOrigin( newOrigin );
				}
				else
				{
					Draw.g_camera.m_center.x -= 0.5f;
				}
				break;

			case GLFW_KEY_RIGHT:
				// Pan right
				if ( mods == GLFW_MOD_CONTROL )
				{
					b2Vec2 newOrigin = { -2.0f, 0.0f };
					s_sample.ShiftOrigin( newOrigin );
				}
				else
				{
					Draw.g_camera.m_center.x += 0.5f;
				}
				break;

			case GLFW_KEY_DOWN:
				// Pan down
				if ( mods == GLFW_MOD_CONTROL )
				{
					b2Vec2 newOrigin = { 0.0f, 2.0f };
					s_sample.ShiftOrigin( newOrigin );
				}
				else
				{
					Draw.g_camera.m_center.y -= 0.5f;
				}
				break;

			case GLFW_KEY_UP:
				// Pan up
				if ( mods == GLFW_MOD_CONTROL )
				{
					b2Vec2 newOrigin = { 0.0f, -2.0f };
					s_sample.ShiftOrigin( newOrigin );
				}
				else
				{
					Draw.g_camera.m_center.y += 0.5f;
				}
				break;

			case GLFW_KEY_HOME:
				Draw.g_camera.ResetView();
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
				Draw.g_draw.m_showUI = !Draw.g_draw.m_showUI;

			default:
				if ( s_sample )
				{
					s_sample.Keyboard( key );
				}
		}
	}
}

static void CharCallback( GLFWwindow* window, unsigned int c )
{
	ImGui_ImplGlfw_CharCallback( window, c );
}

static void MouseButtonCallback( GLFWwindow* window, int button, int action, int mods )
{
	ImGui_ImplGlfw_MouseButtonCallback( window, button, action, mods );

	if ( ImGui.GetIO().WantCaptureMouse )
	{
		return;
	}

	double xd, yd;
	glfwGetCursorPos( g_mainWindow, &xd, &yd );
	b2Vec2 ps = { float( xd ) / s_windowScale, float( yd ) / s_windowScale };

	// Use the mouse to move things around.
	if ( button == (int)MouseButton.Left )
	{
		b2Vec2 pw = Draw.g_camera.ConvertScreenToWorld( ps );
		if ( action == GLFW_PRESS )
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
		if ( action == GLFW_PRESS )
		{
			s_clickPointWS = Draw.g_camera.ConvertScreenToWorld( ps );
			s_rightMouseDown = true;
		}

		if ( action == GLFW_RELEASE )
		{
			s_rightMouseDown = false;
		}
	}
}

static void MouseMotionCallback( GLFWwindow* window, double xd, double yd )
{
	b2Vec2 ps = { float( xd ) / s_windowScale, float( yd ) / s_windowScale };

	ImGui_ImplGlfw_CursorPosCallback( window, ps.x, ps.y );

	b2Vec2 pw = Draw.g_camera.ConvertScreenToWorld( ps );
	s_sample.MouseMove( pw );

	if ( s_rightMouseDown )
	{
		b2Vec2 diff = b2Sub( pw, s_clickPointWS );
		Draw.g_camera.m_center.x -= diff.x;
		Draw.g_camera.m_center.y -= diff.y;
		s_clickPointWS = Draw.g_camera.ConvertScreenToWorld( ps );
	}
}

static void ScrollCallback( GLFWwindow* window, double dx, double dy )
{
	ImGui_ImplGlfw_ScrollCallback( window, dx, dy );
	if ( ImGui.GetIO().WantCaptureMouse )
	{
		return;
	}

	if ( dy > 0 )
	{
		Draw.g_camera.m_zoom /= 1.1f;
	}
	else
	{
		Draw.g_camera.m_zoom *= 1.1f;
	}
}

static void UpdateUI()
{
	int maxWorkers = enki::GetNumHardwareThreads();

	float menuWidth = 180.0f;
	if ( Draw.g_draw.m_showUI )
	{
		ImGui.SetNextWindowPos( { Draw.g_camera.m_width - menuWidth - 10.0f, 10.0f } );
		ImGui.SetNextWindowSize( { menuWidth, Draw.g_camera.m_height - 20.0f } );

		ImGui.Begin( "Tools", &Draw.g_draw.m_showUI,
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
					glfwSetWindowShouldClose( g_mainWindow, GL_TRUE );
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

public static void Main(string[] args)
{
    Thread.CurrentThread.Name ??= "main";
    InitializeWorkingDirectory();
    InitializeLogger();
        
    Log.Logger.Information($"Hello World! - {Directory.GetCurrentDirectory()}");
    Thread.Sleep(1);
    
#if defined( _WIN32 )
	// Enable memory-leak reports
	_CrtSetReportMode( _CRT_WARN, _CRTDBG_MODE_DEBUG | _CRTDBG_MODE_FILE );
	_CrtSetReportFile( _CRT_WARN, _CRTDBG_FILE_STDOUT );
	//_CrtSetAllocHook(MyAllocHook);

	// How to break at the leaking allocation, in the watch window enter this variable
	// and set it to the allocation number in {}. Do this at the first line in main.
	// {,,ucrtbased.dll}_crtBreakAlloc = <allocation number>
#endif

	// Install memory hooks
	b2SetAllocator( AllocFcn, FreeFcn );
	b2SetAssertFcn( AssertFcn );

	char buffer[128];

	s_settings.Load();
	s_settings.workerCount = b2MinInt( 8, (int)enki::GetNumHardwareThreads() / 2 );

	SortSamples();

	glfwSetErrorCallback( glfwErrorCallback );

	Draw.g_camera.m_width = s_settings.windowWidth;
	Draw.g_camera.m_height = s_settings.windowHeight;

	if ( glfwInit() == 0 )
	{
		fprintf( stderr, "Failed to initialize GLFW\n" );
		return -1;
	}

#if __APPLE__
	string glslVersion = "#version 150";
#else
	string glslVersion = nullptr;
#endif

	glfwWindowHint( GLFW_CONTEXT_VERSION_MAJOR, 3 );
	glfwWindowHint( GLFW_CONTEXT_VERSION_MINOR, 3 );
	glfwWindowHint( GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE );
	glfwWindowHint( GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE );

	// MSAA
	glfwWindowHint( GLFW_SAMPLES, 4 );

	b2Version version = b2GetVersion();
	snprintf( buffer, 128, "Box2D Version %d.%d.%d", version.major, version.minor, version.revision );

	if ( GLFWmonitor* primaryMonitor = glfwGetPrimaryMonitor() )
	{
#ifdef __APPLE__
		glfwGetMonitorContentScale( primaryMonitor, &s_framebufferScale, &s_framebufferScale );
#else
		glfwGetMonitorContentScale( primaryMonitor, &s_windowScale, &s_windowScale );
#endif
	}

	bool fullscreen = false;
	if ( fullscreen )
	{
		g_mainWindow = glfwCreateWindow( int( 1920 * s_windowScale ), int( 1080 * s_windowScale ), buffer,
										 glfwGetPrimaryMonitor(), nullptr );
	}
	else
	{
		g_mainWindow = glfwCreateWindow( int( Draw.g_camera.m_width * s_windowScale ), int( Draw.g_camera.m_height * s_windowScale ),
										 buffer, nullptr, nullptr );
	}

	if ( g_mainWindow == nullptr )
	{
		fprintf( stderr, "Failed to open GLFW g_mainWindow.\n" );
		glfwTerminate();
		return -1;
	}

#ifdef __APPLE__
	glfwGetWindowContentScale( g_mainWindow, &s_framebufferScale, &s_framebufferScale );
#else
	glfwGetWindowContentScale( g_mainWindow, &s_windowScale, &s_windowScale );
#endif

	glfwMakeContextCurrent( g_mainWindow );

	// Load OpenGL functions using glad
	if ( !gladLoadGL() )
	{
		fprintf( stderr, "Failed to initialize glad\n" );
		glfwTerminate();
		return -1;
	}

	printf( "GL %d.%d\n", GLVersion.major, GLVersion.minor );
	printf( "OpenGL %s, GLSL %s\n", glGetString( GL_VERSION ), glGetString( GL_SHADING_LANGUAGE_VERSION ) );

	glfwSetWindowSizeCallback( g_mainWindow, ResizeWindowCallback );
	glfwSetKeyCallback( g_mainWindow, KeyCallback );
	glfwSetCharCallback( g_mainWindow, CharCallback );
	glfwSetMouseButtonCallback( g_mainWindow, MouseButtonCallback );
	glfwSetCursorPosCallback( g_mainWindow, MouseMotionCallback );
	glfwSetScrollCallback( g_mainWindow, ScrollCallback );

	// todo put this in s_settings
	CreateUI( g_mainWindow, glslVersion );
	Draw.g_draw.Create();

	s_settings.sampleIndex = b2ClampInt( s_settings.sampleIndex, 0, g_sampleCount - 1 );
	s_selection = s_settings.sampleIndex;

	glClearColor( 0.2f, 0.2f, 0.2f, 1.0f );

	float frameTime = 0.0;

	while ( !glfwWindowShouldClose( g_mainWindow ) )
	{
		double time1 = glfwGetTime();

		if ( glfwGetKey( g_mainWindow, GLFW_KEY_Z ) == GLFW_PRESS )
		{
			// Zoom out
			Draw.g_camera.m_zoom = b2MinFloat( 1.005f * Draw.g_camera.m_zoom, 100.0f );
		}
		else if ( glfwGetKey( g_mainWindow, GLFW_KEY_X ) == GLFW_PRESS )
		{
			// Zoom in
			Draw.g_camera.m_zoom = b2MaxFloat( 0.995f * Draw.g_camera.m_zoom, 0.5f );
		}

		glfwGetWindowSize( g_mainWindow, &Draw.g_camera.m_width, &Draw.g_camera.m_height );
		Draw.g_camera.m_width = int( Draw.g_camera.m_width / s_windowScale );
		Draw.g_camera.m_height = int( Draw.g_camera.m_height / s_windowScale );

		int bufferWidth, bufferHeight;
		glfwGetFramebufferSize( g_mainWindow, &bufferWidth, &bufferHeight );
		glViewport( 0, 0, bufferWidth, bufferHeight );

		glClear( GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT );

		//Draw.g_draw.DrawBackground();

		double cursorPosX = 0, cursorPosY = 0;
		glfwGetCursorPos( g_mainWindow, &cursorPosX, &cursorPosY );
		ImGui_ImplGlfw_CursorPosCallback( g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale );
		ImGui_ImplOpenGL3_NewFrame();
		ImGui_ImplGlfw_NewFrame();
		ImGui_ImplGlfw_CursorPosCallback( g_mainWindow, cursorPosX / s_windowScale, cursorPosY / s_windowScale );

		ImGuiIO& io = ImGui.GetIO();
		io.DisplaySize.x = float( Draw.g_camera.m_width );
		io.DisplaySize.y = float( Draw.g_camera.m_height );
		io.DisplayFramebufferScale.x = bufferWidth / float( Draw.g_camera.m_width );
		io.DisplayFramebufferScale.y = bufferHeight / float( Draw.g_camera.m_height );

		ImGui.NewFrame();

		ImGui.SetNextWindowPos( new Vector2( 0.0f, 0.0f ) );
		ImGui.SetNextWindowSize( new Vector2( float( Draw.g_camera.m_width ), float( Draw.g_camera.m_height ) ) );
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

		if ( Draw.g_draw.m_showUI )
		{
			const SampleEntry& entry = g_sampleEntries[s_settings.sampleIndex];
			snprintf( buffer, 128, "%s : %s", entry.category, entry.name );
			s_sample.DrawTitle( buffer );
		}

		s_sample.Step( s_settings );

		Draw.g_draw.Flush();

		UpdateUI();

		// ImGui.ShowDemoWindow();

		if ( Draw.g_draw.m_showUI )
		{
			snprintf( buffer, 128, "%.1f ms - step %d - camera (%g, %g, %g)", 1000.0f * frameTime, s_sample.m_stepCount,
					  Draw.g_camera.m_center.x, Draw.g_camera.m_center.y, Draw.g_camera.m_zoom );
			// snprintf( buffer, 128, "%.1f ms", 1000.0f * frameTime );

			ImGui.Begin( "Overlay", nullptr,
						  ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize |
							  ImGuiWindowFlags.NoScrollbar );
			ImGui.SetCursorPos( new Vector2( 5.0f, Draw.g_camera.m_height - 20.0f ) );
			ImGui.TextColored( new Vector4( 153, 230, 153, 255 ), "%s", buffer );
			ImGui.End();
		}

		ImGui.Render();
		ImGui_ImplOpenGL3_RenderDrawData( ImGui.GetDrawData() );

		glfwSwapBuffers( g_mainWindow );

		// For the Tracy profiler
		FrameMark;

		if ( s_selection != s_settings.sampleIndex )
		{
			Draw.g_camera.ResetView();
			s_settings.sampleIndex = s_selection;

			// #todo restore all drawing settings that may have been overridden by a sample
			s_settings.subStepCount = 4;
			s_settings.drawJoints = true;
			s_settings.useCameraBounds = false;

			delete s_sample;
			s_sample = nullptr;
			s_sample = g_sampleEntries[s_settings.sampleIndex].createFcn( s_settings );
		}

		glfwPollEvents();

		// Limit frame rate to 60Hz
		double time2 = glfwGetTime();
		double targetTime = time1 + 1.0 / 60.0;
		while ( time2 < targetTime )
		{
			b2Yield();
			time2 = glfwGetTime();
		}

		frameTime = float( time2 - time1 );
	}

	delete s_sample;
	s_sample = nullptr;

	Draw.g_draw.Destroy();

	DestroyUI();
	glfwTerminate();

	s_settings.Save();

#if defined( _WIN32 )
	_CrtDumpMemoryLeaks();
#endif

	return 0;
}
}
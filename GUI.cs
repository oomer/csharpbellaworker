using System.Drawing;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using System.Numerics;

namespace oomerbellaworkercli;
public class GUI {
    ImGuiController? _imguiController;
    IInputContext? _inputContext;
    private static IWindow? _silkWindow;
    public static GL? _openglContext;
    private static uint _glTextureID;
    private readonly object _synclock = new object();
    private BellaPart _bella;
    public Anim _anim;
    public Networking _networking;
    public void Start() {
        _anim = new Anim();
        _bella = new BellaPart();
        _bella.BellaInit();
        _networking = new Networking( "localhost", 8799, _anim, _bella );
        _networking.CommandChannelAsync();
        WindowStart();
    }
    public async Task WindowStart() {
        // Create a Silk.NET window as usual
        WindowOptions options = WindowOptions.Default with {
            Size = new Silk.NET.Maths.Vector2D<int>( 400, 300 ),
            Title = "Bella Render Worker"
        };
        _silkWindow = Window.Create( options );
        
        // loading function
        _silkWindow.Load += OnWindowLoad;

        // render function
        _silkWindow.Render += OnWindowRender; 
        _silkWindow.Update += OnWindowUpdate; 

        // closing function
        _silkWindow.Closing += OnWindowClose;
        _silkWindow.Run();
        _silkWindow.Dispose();
    }
    private void OnWindowClose() {
        _imguiController?.Dispose();
        _inputContext?.Dispose();
        _openglContext?.Dispose();
        _bella.BellaClose();
    }
    private void OnWindowLoad() {
        _imguiController = new ImGuiController(
            _openglContext = _silkWindow.CreateOpenGL(), 
            _silkWindow, 
            _inputContext = _silkWindow?.CreateInput()
        );

        // Capture all input devices
        for ( int i = 0; i < _inputContext?.Keyboards.Count; i++ ) {
            _inputContext.Keyboards[ i ].KeyDown += KeyDown;
        }
        // OpenGL is a state machine
        _openglContext.TexParameterI( GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat );
        _openglContext.TexParameterI( GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat );
        _openglContext.TexParameterI( GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest );
        _openglContext.TexParameterI( GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest );
        // Bind texture
        //_openglContext.BindTexture( TextureTarget.Texture2D, 0 );
    }
    private unsafe void OnWindowRender( double deltaTime ) {
        _imguiController?.Update( (float) deltaTime );
        _openglContext?.ClearColor( Color.FromArgb(255, 
                                    (int)(.45f * 255), 
                                    (int) (.55f * 255), 
                                    (int) (.60f * 255) ) );
        _openglContext?.Clear( ClearBufferMask.ColorBufferBit );
        ImGuiNET.ImGui.SetCursorPos( new System.Numerics.Vector2( 0, 0 ) );
        ImGuiNET.ImGui.ShowStyleEditor();

        ImGuiNET.ImGui.PushStyleVar( ImGuiNET.ImGuiStyleVar.WindowBorderSize,0 );
        ImGuiNET.ImGui.PushStyleVar( ImGuiNET.ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2( 0,0 ) );
        ImGuiNET.ImGui.Begin(   "Bella", 
                                ImGuiNET.ImGuiWindowFlags.NoCollapse | 
                                ImGuiNET.ImGuiWindowFlags.NoMove | 
                                ImGuiNET.ImGuiWindowFlags.NoTitleBar );  
        ImGuiNET.ImGui.SetCursorPos( new System.Numerics.Vector2( 0, 0 ) );

        if (_bella._engine.rendering()) {
            // Draw bella viewport
            float rezratio = (float)_bella._width/(float)_bella._height;
            int newheight = (int)(( 400*(float)_bella._height ) / (float)_bella._width);
            ImGuiNET.ImGui.Image(   (nint)_glTextureID, 
                                    new System.Numerics.Vector2( 
                                        400, 
                                        newheight ) );
        } else {
            ImGuiNET.ImGui.Text( "***IDLE***" );
        }

        ImGuiNET.ImGui.ProgressBar((float)_bella._progress, new Vector2(400,20));
        ImGuiNET.ImGui.Text( "Rendering Frame: " );
        
        ImGuiNET.ImGui.End();
        _imguiController?.Render();
    }
    private void OnWindowUpdate( double deltaTime ) {
        if ( _bella._bellaWindowUpdate == true && _bella._engine.rendering() ) {
            lock( _synclock ) {
                unsafe {
                    IntPtr pixelsAddr = _bella._skBitmap.GetPixels();
                    // Cast IntPtr addres to a pointer
                    // this also works: byte* ptr = (byte*)pixelsAddr;
                    byte* ptr = (byte*) pixelsAddr.ToPointer(); {
                        _openglContext?.TexImage2D( TextureTarget.Texture2D,
                        0, 
                        InternalFormat.Rgba, 
                        (uint)_bella._skBitmap.Width, 
                        (uint)_bella._skBitmap.Height, 
                        0, 
                        PixelFormat.Rgba, 
                        PixelType.UnsignedByte, 
                        ptr );
                    }
                }
            }
            _bella._bellaWindowUpdate = false;
        }
    }
    private void KeyDown( IKeyboard keyboard, Key key, int keyCode ) {
        if ( key == Key.Escape ) { _silkWindow?.Close(); }
    }
}

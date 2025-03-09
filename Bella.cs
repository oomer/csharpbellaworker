using bella;
using libBella = bella.bella;
using SkiaSharp;

namespace oomerbellaworkercli;
public class BellaPart : IBella {
	private EngineObserver? _engineObserver;
	public bella.Engine? _engine;
    private readonly object _synclock = new object();
    public SKBitmap _skBitmap;
    public bool _bellaWindowUpdate = false;
    public bella.Node _camXform;
    public bella.Node _camLens;

    public float _progress;

    public int _width;
    public int _height;
    public void BellaInit() {
        _engine = new bella.Engine();
        _engineObserver = new EngineObserver(this);
        _engine.subscribe(_engineObserver);
        _engine.scene().loadDefs();
        _engine.stop();
        //_progress = new bella.Progress();

        bella.Node bellaGlobal = _engine.scene().createNode("global","global");
        bella.Node bellaState = _engine.scene().createNode("state","__state__");
        bella.Node bellaSettings = _engine.scene().createNode("settings","__settings__");
        bella.Node bellaWorld = _engine.scene().createNode("xform","world");
        bella.Node bellaCamera = _engine.scene().createNode("camera","__camera__");
        bella.Node bellaCamxform = _engine.scene().createNode("xform","__cameraxform__");
        bella.Node bellaBeautyPass = _engine.scene().createNode("beautyPass","__beautyPass__");
        bella.Node bellaSensor = _engine.scene().createNode("sensor","__sensor__");
        bella.Node bellaLens = _engine.scene().createNode("thinLens","__thinLens__");

        //bellaGlobal.input("state").appendElement();
        var foo = bellaGlobal.createArray("states", AttrType.AttrType_Node);
        Console.WriteLine("global is array:"+foo.isArray());
        bella.Input myfoo = foo.appendElement();
        myfoo.setNode(bellaState);

        // this works 
        //bella.Input foo2 = bellaSettings.createInput("iprScale", AttrType.AttrType_Int);
        //foo2.setInt(50);
        bool ok = bellaCamxform.parentTo( bellaWorld );
        ok = bellaCamera.parentTo( bellaCamxform );
        bellaState["world"] = bellaWorld;
        bellaState["settings"] = bellaSettings;
        bellaSettings["beautyPass"] = bellaBeautyPass;
        bellaSettings["camera"] = bellaCamera;
        bellaCamera["lens"] = bellaLens;
        bellaCamera["sensor"] = bellaSensor;
        bellaCamera["resolution"] = new bella.Vec2(200,200);
        bellaSensor["size"] = new bella.Vec2(24,24);
    }

    public void BellaLoadExisting( string uuid) {
        if ( System.IO.Directory.Exists( uuid + "/tmp.bsz")) {
            _engine.loadScene( uuid+"/tmp.bsz");
        }
    }
    public void ShowImage( string pass, bella.Image img ) {
        Console.WriteLine("showImage");
        lock( _synclock ) {
            unsafe {
                IntPtr pixelsAddr = _skBitmap.GetPixels();
                var w = (int)img.width();
                var h = (int)img.height();
                byte* destptr = (byte*)pixelsAddr.ToPointer();
                byte* srcptr = (byte*)img.rgba8().ToPointer();
                for ( int row = 0; row < h; row++ ) {
                    for ( int col = 0; col < w; col++ ) {
                        *destptr++ = *srcptr++; //red Using ++ on pointer advances to next pointer
                        *destptr++ = *srcptr++; //green
                        *destptr++ = *srcptr++; //blue
                        *destptr++ = *srcptr++; //alpha
                    }
                }
            }
        }
        _bellaWindowUpdate = true;
    }
    public void SaveImage( string pass ) {
        Console.WriteLine("save image");
    }
    public void onSceneLoaded( bella.Scene scene ) {
        Console.WriteLine("HASH: "+scene.hash());
        _camLens = _engine.scene().camera()["lens"].asNode();

        bella.Vec2 reducedres = scene.camera()["resolution"].asVec2();
        reducedres.x /= 4;
        reducedres.y /= 4;
        scene.camera()["resolution"] = reducedres;

        bella.Vec2u effres = scene.resolution(scene.camera(),true,true);
        _engine.enableInteractiveMode();
        _engine.scene().beautyPass()["targetNoise"]=10;
        _camXform = _engine.scene().cameraPath().parent();
        //_camLens = _engine.scene().camera()["lens"].asNode();
        
        // Half rez to match IPR size
        Console.WriteLine("REZ: "+effres.x+" "+effres.y);

        _width = (int)effres.x;
        _height= (int)effres.y;
        //_bella._skBitmap = new SKBitmap( (int)width / 2, (int)height/2 );
        double iprScale = _engine.scene().settings()["iprScale"].asReal()/100.0;
        _skBitmap = new SKBitmap( (int)effres.x, (int)effres.y);
        Console.WriteLine("ON SCENE LOADED");
    }
    public void onError( string pass, string msg) {
        Console.WriteLine("ERROR: "+msg);
    }
    public void onProgress( string pass, bella.Progress progress) {
        _progress = (float)progress.progress();
        Console.WriteLine(_progress.ToString());
    }

    public void BellaRotate(){
        var delta = new Vec2(10, 0);
        libBella.orbitCamera(_engine?.scene().cameraPath(), delta);
    }

    public void BellaClose() {
        _engine.Dispose();
        _engineObserver.Dispose();
    }
}

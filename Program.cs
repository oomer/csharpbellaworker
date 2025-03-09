namespace oomerbellaworkercli; 
public class EngineObserver : bella.EngineObserver {
    private IBella _looseCouple;
    public EngineObserver( IBella MyApp ) {
        _looseCouple = MyApp;
        Console.WriteLine( "Engine observed..." ); 
    }
    public override void onImage( string renderpass, bella.Image image ) {
        _looseCouple.ShowImage( renderpass, image ); 
    }
    public override void onStopped( string renderpass ) {
        _looseCouple.SaveImage( renderpass ); 
    }
    public override void onSceneLoaded( bella.Scene scene ) {
        _looseCouple.onSceneLoaded( scene ); 
    }
    public override void onError(string pass, string msg) {
        _looseCouple.onError( pass, msg  ); 
    }
    public override void onProgress(string pass, bella.Progress progress) {
        _looseCouple.onProgress( pass, progress  ); 
    }

}
public interface IBella {
    void ShowImage( string pass, bella.Image img );
    void SaveImage( string pass );
    void onSceneLoaded( bella.Scene scene );
    void onError( string pass, string msg );
    void onProgress( string pass, bella.Progress progress );
}
class Program {

    static void Main(string[] args) {
        //var cli =  new CLI();
        //cli.Start();
        var cli =  new GUI();
        cli.Start();
    }

}


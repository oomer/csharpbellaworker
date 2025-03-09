using System.Numerics;

namespace oomerbellaworkercli;
public class CLI {
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
        // Wait to make sure UUID is assigned or read
        Thread.Sleep(100);
        // [ TODO add skip ] Every time worker started, the last .bsz scene is loaded
        // When getBsz action runs, the server passes file hash that worker can compare
        // if hash matches then this pre-load made sense
        // if not then is could have been skipped, especially for very large scenes.
        //_bella.BellaLoadExisting( _networking._uuid );
        while ( true ) {
            Console.WriteLine("cli");
            Thread.Sleep(5000);
        }
    }
}
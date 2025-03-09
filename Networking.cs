// Asynchronous handling of communications between server and clients

using NetMQ;
using NetMQ.Sockets;
using System.Security.Cryptography;

namespace oomerbellaworkercli;
public class Networking {
    private readonly object synclock = new object();
    private Anim _anim;
    public string _serverAddr = "localhost";
    public uint _serverPort = 8799;
    public BellaPart _bella;
    private bella.Mat4[] _camAnim = new bella.Mat4[2];
    private double[] _focusDistAnim = new double[2];
    private bella.Node _camXform;
    private bella.Input _camLens;

    string receiveString;
    bool standby = true;
    bool rendering = true;
    string command = "requestWork";

    string _workerMode = "requestWork";
    public string _uuid;
    public byte[] _uuidByteArray;
    public Networking( string address_, uint port_, Anim anim_, BellaPart bella_) {

        // Generate unique id, store/retrieve 
        Guid uuid;
        if  ( System.IO.File.Exists(".oomerbellaworker")) {
            if ( Guid.TryParse( File.ReadAllText(".oomerbellaworker") , out uuid ) ) { 
                _uuid = uuid.ToString();
            } else {
                Console.WriteLine("FAILED to parse uuid from .oomerbellaworker:");
                Environment.Exit(1);
            }
        } else {
            uuid = Guid.NewGuid();
            _uuid = uuid.ToString();
            File.WriteAllText(".oomerbellaworker",_uuid);
        }
        _uuidByteArray = uuid.ToByteArray();
        _bella = bella_;
        _bella.BellaInit();
        _serverAddr = address_;
        _serverPort = port_;
        _anim  = anim_;
    }

    // Long running background connection to server
    // Dealer sockets extend simple Request sockets with additional message frames that include uuid
    public async Task CommandChannelAsync( ) {
        // Run asynchronously
        Console.WriteLine(Environment.MachineName);
        await Task.Run( async () => { 
            try {
                // Dealer sockets are request sockets with identity
                using ( var worker = new DealerSocket()) {

                    int renderFrame = 0;
                    // hereon in uuid is passed to Router socket automatically
                    worker.Options.Identity = _uuidByteArray;
                    worker.Connect( "tcp://"+_serverAddr+":"+_serverPort );

                    while( true ) {
                        if ( _workerMode == "getBsz" && standby == false) {
                            worker.SendFrame( "getBsz" );
                            string receiveDelim = worker.ReceiveFrameString(); //zeromq messaging protocol
                            string receiveString = worker.ReceiveFrameString();
                            if ( receiveString == "standby" ) {
                                standby = true;
                            } else {
                                byte[] receiveBsz = worker.ReceiveFrameBytes(); 
                                System.IO.File.WriteAllBytes( _uuid+"/tmp.bsz", receiveBsz ); 
                                Console.WriteLine("Received bsz "+receiveString);
                                _bella._engine.loadScene(_uuid+"/tmp.bsz");
                                _workerMode = "getFragment";
                            }
                        } else if ( _workerMode == "getFragment" && standby == false) {
                            worker.SendFrame("getFragment");
                            string receiveDelim = worker.ReceiveFrameString(); //zeromq messaging protocol
                            receiveString = worker.ReceiveFrameString();
                            Console.WriteLine(_bella._camXform.GetType());
                            if ( receiveString == "standby") { standby = true;
                            } else {
                                renderFrame = int.Parse(receiveString);
                                receiveString = worker.ReceiveFrameString();
                                Console.WriteLine(_bella._camXform.GetType());
                                Console.WriteLine(_bella._camXform.name()+".steps[0].xform="+receiveString);
                                _bella._engine.scene().parse(_bella._camXform.name()+".steps[0].xform="+receiveString+";");
                                receiveString = worker.ReceiveFrameString();
                                Console.WriteLine(_bella._camLens.name()+".steps[0].FocusDist="+receiveString);
                                _bella._engine.scene().parse(_bella._camLens.name()+".steps[0].FocusDist="+receiveString+";");
                            }
                            _workerMode = "render";

                        } else if ( _workerMode == "render"  && standby == false) {
                            bool ok = _bella._engine.start();
                            if ( ok ) {
                                rendering = true;
                                _workerMode = "engineWait";
                            } else {
                                Console.WriteLine("Bella Render failed to start");
                                Environment.Exit(1);
                            }
                        } else if ( _workerMode == "engineWait"  && rendering == true) {
                            if ( _bella._engine.rendering() ) {
                            } else {
                                bool ok = _bella._engine.writeImage(_bella._engine.scene().beautyPass().name(),_uuid+"/tmp.png", 8,"sRGB");
                                if ( ok ) {
                                    rendering = false;
                                    standby = false;
                                    _workerMode = "sendImage";
                                } else {
                                    Console.WriteLine("FAILED to write image");
                                    _workerMode = "failed";
                                }
                            }
                        } else if ( _workerMode == "sendImage"  && standby == false) {

                            // Create a synchronous request socket to pass images to server
                            try {
                                using ( var worker2 = new RequestSocket() ) {
                                    worker2.Connect( "tcp://"+_serverAddr+":8800");

                                    worker2.SendMoreFrame(renderFrame.ToString());
                                    byte[] byteFile = System.IO.File.ReadAllBytes(_uuid+"/tmp.png");
                                    worker2.SendFrame(byteFile);
                                    receiveString = worker2.ReceiveFrameString();
                                    Console.WriteLine("sent  png "+receiveString+" "+renderFrame);
                                    standby = true;
                                    _workerMode = "requestWork";
                                    Console.WriteLine(_workerMode+" "+standby);
                                }
                            } catch ( NetMQException ex ) {
                                Console.WriteLine("Image Channel NetMQ Exception: " + ex.Message);       
                                Environment.Exit(1);
                            } catch ( Exception ex) {
                                Console.WriteLine("Image Channel Exception: " + ex.Message);       
                                Environment.Exit(1);
                            }

                        } else if (standby == true) {
                            // Standby loop
                            Console.WriteLine("Request Work");
                            worker.SendFrame("requestWork");
                            standby = false;
                            string receiverDelim = worker.ReceiveFrameString(); 
                            string receiveString2 = worker.ReceiveFrameString(); 
                            if ( receiveString2 == "standby") { 
                                standby = true;
                            } else {
                                Console.WriteLine("using UUID"+ _uuid);
                                if ( System.IO.Directory.Exists( _uuid )) {
                                    // Router will return sha256sum of current tmp.bsz
                                    // This avoids redownload if hashes match
                                    string sha256sum = "0";
                                    if ( System.IO.File.Exists( _uuid+"/tmp.bsz")) {
                                            using (FileStream stream = File.OpenRead( _uuid+"/tmp.bsz"))
                                            {
                                                Console.WriteLine("sha256");
                                                SHA256Managed sha = new SHA256Managed();
                                                byte[] checksum = sha.ComputeHash(stream);
                                                sha256sum = BitConverter.ToString(checksum).Replace("-", String.Empty);
                                            }
                                    } 
                                    if (receiveString2 == sha256sum) {
                                        _bella._engine.loadScene(_uuid+"/tmp.bsz");
                                        //_bella.BellaLoadExisting( _uuid );
                                        _workerMode = "getFragment";
                                        Console.WriteLine("tmp.bsz is hash identical "+receiveString2+" "+sha256sum);
                                        standby = false;
                                    } else {
                                        // Server copy of .bsz has changed
                                        _workerMode = "getBsz";
                                        Console.WriteLine("tmp.bsz is hash identical "+receiveString2+" "+sha256sum);
                                        Console.WriteLine("tmp.bsz is hash different");
                                        standby = false;
                                    }
                                } else {
                                    System.IO.Directory.CreateDirectory( _uuid );
                                    _workerMode = "getBsz";
                                    standby = false;
                                }
                            }
                        }
                        if (standby) {
                            Console.WriteLine("Standby Mode");
                            System.Threading.Thread.Sleep(5000);
                        }
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            } catch ( NetMQException ex ) {
                Console.WriteLine("NetMQ Exception: " + ex.Message);       
                Environment.Exit(1);
            } catch ( Exception ex) {
                Console.WriteLine("Exception: " + ex.Message);       
                Console.WriteLine( ex.StackTrace );
                Environment.Exit(1);
            }
        });
    }

}








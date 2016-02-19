using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Net.Sockets;

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    class GestureDetector : IDisposable
    {
        string gestureDatabase = @"Database\WheelChair.gbd";
        string wheelChairGestName = "WheelChair";
        VisualGestureBuilderFrameSource vgbSource = null;//handles tying to tracking id
        VisualGestureBuilderFrameReader vgbReader = null;//handles incoming gesture events
        TcpClient clientSocket;
        DateTime gestureTimer;


        public GestureDetector(KinectSensor kinectSensor, GestureResultView resultView)
        {
            gestureTimer = DateTime.Now;
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }
            if (resultView == null)
            {
                throw new ArgumentNullException("gestresultview");
            }
            this.GestureResultView = resultView;
            vgbSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            vgbSource.TrackingIdLost += VgbSource_TrackingIdLost;
            vgbReader = this.vgbSource.OpenReader();
            if (vgbReader != null)
            {
                vgbReader.IsPaused = true;
                vgbReader.FrameArrived += VgbReader_FrameArrived;
            }

            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(gestureDatabase))
            {
                foreach (Gesture gesture in database.AvailableGestures)
                {
                    if (gesture.Name.Equals(this.wheelChairGestName))
                    {
                        this.vgbSource.AddGesture(gesture);
                    }
                }
            }
        }



        public GestureResultView GestureResultView { get; private set; }


        private void VgbSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            this.GestureResultView.UpdateGestureResult(false, false, 0.0f);
        }

        public ulong TrackingId
        {
            get
            {
                return this.vgbSource.TrackingId;
            }

            set
            {
                if (this.vgbSource.TrackingId != value)
                {
                    this.vgbSource.TrackingId = value;
                }
            }
        }
        public bool IsPaused
        {
            get
            {
                return this.vgbReader.IsPaused;
            }

            set
            {
                if (this.vgbReader.IsPaused != value)
                {
                    this.vgbReader.IsPaused = value;
                }
            }
        }

        //on gesture detection
        private void VgbReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    if (discreteResults != null)
                    {
                        foreach (Gesture gesture in this.vgbSource.Gestures)
                        {
                            if (gesture.Name.Equals(this.wheelChairGestName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);
                                var prevDetect = GestureResultView.Detected;
                                this.GestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);
                                if(prevDetect!= GestureResultView.Detected&& result.Detected)
                                {
                                    var currentTime = DateTime.Now;
                                    var elapsedTime = (gestureTimer - currentTime).TotalSeconds * -1;
                                    if(elapsedTime > 2)
                                    {
                                        gestureTimer = DateTime.Now;
                                        sendToPi(gesture.Name);

                                    }
                                }
                                    //update view                                
                            }

                        }
                    }
                }
            }

        }

        private void sendToPi(string gestureName)
        {
            switch (gestureName)
            {
                case "WheelChair":
                    clientSocket = new TcpClient();

                    try
                    {
                        clientSocket.Connect("192.168.1.163", 8888);
                    }
                    catch
                    {
                        Console.WriteLine("Couldnt connect");
                    }

                    NetworkStream serverStream = clientSocket.GetStream();
                    byte[] outStream = Encoding.ASCII.GetBytes(gestureName+" Detection");
                    serverStream.Write(outStream, 0, outStream.Length);
                    serverStream.Flush();
                    serverStream.Close();
                    clientSocket.Close();
                    break;
                default:
                    break;
            }

        }


        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

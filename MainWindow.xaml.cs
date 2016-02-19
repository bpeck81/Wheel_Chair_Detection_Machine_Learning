using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        KinectSensor kinectSensor;
        Body[] bodies;
        BodyFrameReader bodyFrameReader;
        BodyConstruct bodyBuilder;
        List<GestureDetector> gestureDetectorList;
        


        public MainWindow()
        {
            kinectSensor = KinectSensor.GetDefault();
            kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            kinectSensor.Open();
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
            bodyBuilder = new BodyConstruct(kinectSensor);
            gestureDetectorList = new List<GestureDetector>();

            InitializeComponent();
            this.DataContext = this;
            this.kinectBodyViewbox.DataContext = this.bodyBuilder;

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                GestureResultView result = new GestureResultView(i, false, false, 0f);
                GestureDetector detector = new GestureDetector(kinectSensor, result);
                result.PropertyChanged += Result_PropertyChanged;
                this.gestureDetectorList.Add(detector);
            }


        }

        private void Result_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        { //event called each frame
            var dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        //initializes body data
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                bodyBuilder.UpdateBodyFrame(this.bodies); //updates display with bod ydata

                if (this.bodies != null)
                {
                    for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
                    {
                        //make sure gesture detectors are updated
                        Body body = this.bodies[i];
                        var trackingId = body.TrackingId;
                        if (trackingId != this.gestureDetectorList[i].TrackingId)
                        {
                            this.gestureDetectorList[i].TrackingId = trackingId;
                            this.gestureDetectorList[i].IsPaused = trackingId == 0;
                        }

                    }
                }

            }

        }

        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
        }
    }
}

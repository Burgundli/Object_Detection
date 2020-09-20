using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Drawing;

namespace Object_Detection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private const int  DepthToByte = 8000 / 256;        // - constant for conversion from depth distance in meters to 256 byte format 
        private KinectSensor kinectSensor = null;           // - variable reporesenting the active sensor 
        private DepthFrameReader depthFrameReader = null;   // - variable representing the depth frame reader 
        private FrameDescription frameDescription = null;   // - description of the data contained in the depth frame 
        private WriteableBitmap depthbitmap = null;         // - bitmap for displaying image 
        private byte[] depthpixels = null;                  // - intermediate storage for frame datta conveted to color pointer 
        private string KinectStatus = null;                 // - current status of the kinect sensor 
        public MainWindow()
        {
            // Set up of  the Sensor and reader 
            kinectSensor = KinectSensor.GetDefault();
            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            // Create and event handler for the arrived frames 
            depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;
            frameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            // Allocate memory for the arrived and converted frames then store them to bitmap  
            depthpixels = new byte[frameDescription.Width * frameDescription.Height];
            depthbitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            kinectSensor.IsAvailableChanged += Sensor_IsAvaibleChanged;
            kinectSensor.Open();
            StatusText = kinectSensor.IsAvailable ? "Running" : "Turned off";
            DataContext = this;   //  - window object is used as the view model for default binding source of objects || WIKI : Data context is a concept that allows elements to inherit information from their parent elements about the data source that is used for binding, as well as other characteristics of the binding, such as the path. 
            

            InitializeComponent();
        }
        public ImageSource ImageSource
        {
            get
            {
                return depthbitmap; 
            }
        }
        public string StatusText
        {
            get
            {
                return KinectStatus; 
            }
            set
            {
                if (KinectStatus != value)
                {
                    KinectStatus = value;

                    // notify any bound elements that the text has changed using the property changed event handler for Main Window
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }

        }


        private void Sensor_IsAvaibleChanged(object sender, IsAvailableChangedEventArgs e)
        {

            StatusText = kinectSensor.IsAvailable ? "Running" : "Turned off";

           
        }

        private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool IsFrameProcessed = false;
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    using (KinectBuffer kinectBuffer = depthFrame.LockImageBuffer())
                    {
                        if (((frameDescription.Width*frameDescription.Height) == (kinectBuffer.Size / frameDescription.BytesPerPixel)) && (frameDescription.Width == depthbitmap.PixelWidth) && (frameDescription.Height == depthbitmap.PixelHeight))
                        {
                            ushort maxDepthValue = ushort.MaxValue;
                            ProcessDepthFrameData(kinectBuffer.UnderlyingBuffer, kinectBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepthValue);
                            IsFrameProcessed = true;
                        }
                    }


                }
            }
            if (IsFrameProcessed)
            {
                RenderPixels(); 
            }

               
        }
        private unsafe void ProcessDepthFrameData (IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            ushort*  framedata = (ushort*)depthFrameData;

            for (int i = 0; i < (int)(depthFrameDataSize / frameDescription.BytesPerPixel); ++i)
            {
                ushort depth = framedata[i];
                depthpixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / DepthToByte) : 0);

            }


        }
        private void RenderPixels()

        {
            MyLabel2.Content = depthpixels.Length.ToString();
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), depthpixels, depthbitmap.PixelWidth,0);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (depthFrameReader != null)
            {
                depthFrameReader.Dispose();
                depthFrameReader = null; 
            }
            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null; 
            }
           
        }

        private void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            var Gray8DepthBmp = new FormatConvertedBitmap(depthbitmap, PixelFormats.Gray8, null, 0d);              // - create a new formated bitmap for saving the image to the file 
            var encoder = new BmpBitmapEncoder();                                                                  // - create an encoder for converting to a bmp file  
            encoder.Frames.Add(BitmapFrame.Create(Gray8DepthBmp));                                                 // - adds a frame with the speciefied format to the encoder 

            using (var fileStream = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.Create))
            {
                encoder.Save(fileStream);                                                                           // - save the file to the defined path from the encoder 
            }   

        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
          // - opens a specific file, decode it using bitmap decoder and copies it to an byte array of pixel values, then draws it to an image 
            byte[] ArrOfPxl = new byte[512*424];
            Stream imageStreamSource = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.Open, FileAccess.Read, FileShare.Read);
            BmpBitmapDecoder decoder = new BmpBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0]; 
            LoadCapture.Source = bitmapSource;
            bitmapSource.CopyPixels(ArrOfPxl, 512, 0);
            MyLabel.Content = ArrOfPxl.Length.ToString();  
           
        }
    }
}

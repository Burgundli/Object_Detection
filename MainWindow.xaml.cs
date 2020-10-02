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
using Emgu.Util;
using Emgu.CV.Structure;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV.Cuda;
using Emgu.CV.Reflection;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Runtime;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using Emgu.CV.Features2D;
using System.Windows.Media.Media3D;

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
                            ushort maxDepthValue = 1000;
                            ushort minDepthValue = 200;
                            ProcessDepthFrameData(kinectBuffer.UnderlyingBuffer, kinectBuffer.Size, minDepthValue, maxDepthValue);
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
            int PixelCount = 0;  
             // show olny pixels within the requried range and count them  
            for (int i = 0; i < (int)(depthFrameDataSize / frameDescription.BytesPerPixel); ++i)
            {
                ushort depth = framedata[i];
                if (depth >= minDepth && depth <= maxDepth && depth != 0)
                {
                    depthpixels[i] = (byte)(256 - (depth / DepthToByte));
                    PixelCount++;
                }
                else
                {
                    depthpixels[i] = 0;
                }
              
            }

            MyLabel2.Content = PixelCount.ToString();
        }
        private unsafe  void RenderPixels()

        {
   
           
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), depthpixels, depthbitmap.PixelWidth,0);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // after the window is closed dispose the framereade and close the kinect sensor 

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

            using (var fileStream = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.OpenOrCreate))
            {
                encoder.Save(fileStream);                                                                           // - save the file to the defined path from the encoder 
            }   

        }
        // converts Image EmguCV class to bitmapsource 
        public static class BitmapSourceConvert
        {
            [DllImport("gdi32")]
            private static extern int DeleteObject(IntPtr o);

            public static BitmapSource ToBitmapSource(Image<Gray,byte> image)
            {
                using (System.Drawing.Bitmap source = image.ToBitmap()) 
                {
                    IntPtr ptr = source.GetHbitmap();

                    BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()) ;

                    DeleteObject(ptr);
                    return bs;
                }
            }
        }
        private unsafe  void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            byte[] ArrOfPxl = new byte[512 * 424];
            int PixelCount = 0;
            BitmapSource BluredBitmap;
            Region[] regions = new Region[4]; 

            for (int k = 0; k < 4; k++)
            {
                regions[k] = new Region();
                regions[k].RegionIndex = k + 1; 
            }

            
            // - read image from file calculate centroid based on moments 
            Mat mat = CvInvoke.Imread("C:/Users/CPT Danko/Pictures/capture.png",ImreadModes.AnyColor);             
            Moments moments = CvInvoke.Moments(mat, false);
            System.Drawing.Point WeightedCentroid = new System.Drawing.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
            Centroid.Content = WeightedCentroid.X.ToString() + "  " + WeightedCentroid.Y.ToString();
            // - Canny edge recognition based on image contours with Gaussian bluring 
             var image = new Image<Gray, byte>("C:/Users/CPT Danko/Pictures/capture.png");
             CvInvoke.GaussianBlur(image, image, new System.Drawing.Size(5, 5), 0);
             var CannyImage = new UMat();
             CvInvoke.Canny(image, CannyImage, 10, 200);
             VectorOfVectorOfPoint ImageContours = new VectorOfVectorOfPoint();
             CvInvoke.FindContours(CannyImage, ImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint AppContour = new VectorOfPoint(2);

            for (int k= 0;k < ImageContours.Size; k++)
            {
                VectorOfPoint contour = ImageContours[k];
                

               if (CvInvoke.ContourArea(contour) > CvInvoke.ContourArea(AppContour))
                {
                    AppContour = contour; 
                }
               
            }
            RotatedRect rotatedRect = CvInvoke.MinAreaRect(AppContour);
            CvInvoke.Polylines(image, Array.ConvertAll(rotatedRect.GetVertices(), Point.Round), true, new MCvScalar(255, 0, 0), 2);
            System.Drawing.PointF direction, pointOnLine;
            System.Drawing.PointF[] line = new System.Drawing.PointF[AppContour.ToArray().Length];
            for (int i = 0;i<AppContour.ToArray().Length; i++)
            {
                line[i].X = (float)AppContour.ToArray()[i].X;
                line[i].Y = (float)AppContour.ToArray()[i].Y; 

            }
            
            CvInvoke.FitLine(line, out direction , out pointOnLine ,Emgu.CV.CvEnum.DistType.L2, 0, 0.1, 0.1);
            int left = (int)((-pointOnLine.X * direction.Y / direction.X) + pointOnLine.Y);
            int right = (int)(((512 - pointOnLine.X) * direction.Y / direction.X) + pointOnLine.Y);
            CvInvoke.Line(image,WeightedCentroid, new Point(0,left),new MCvScalar(255,0,0),2);
            CvInvoke.Line(image, new Point(511, right), WeightedCentroid, new MCvScalar(255, 0, 0), 2);


            float angle = rotatedRect.Angle;
            RectAngle.Content = angle.ToString();
            
             //var imageContours = new Image<Gray, byte>(image.Width, image.Height, new Gray(0));
             //CvInvoke.DrawContours(CannyImage, ImageContours, -1, new MCvScalar(255, 0, 0));
             CvInvoke.Circle(image, WeightedCentroid, 2,new MCvScalar(0,255,255));
             BluredBitmap = BitmapSourceConvert.ToBitmapSource(image);
            





            // - opens a specific file, decode it using bitmap decoder and copies it to an byte array of pixel values, then draws it to an image
            //- had to clear the filestream somehow 
            using (System.IO.Stream imageStreamSource = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BmpBitmapDecoder decoder = new BmpBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bitmapSource = decoder.Frames[0];
                bitmapSource.CopyPixels(ArrOfPxl, 512, 0);
                
            }
            


            //draw a black dot a the centre of a shape 
            WriteableBitmap CentroidBitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            CentroidBitmap.WritePixels(new Int32Rect(0, 0, CentroidBitmap.PixelWidth, CentroidBitmap.PixelHeight), ArrOfPxl, CentroidBitmap.PixelWidth, 0);
            LoadCapture.Source = BluredBitmap;


            // count pixels in the loaded image
            PixelCount = ArrOfPxl.Count(n => n != 0);
            //MyLabel.Content = PixelCount.ToString();
            regions[1].RegionPixelCoun = ArrOfPxl.Where(s => s);
            MyLabel.Content = regions[1].RegionPixelCoun.ToString();


        }
    }
}

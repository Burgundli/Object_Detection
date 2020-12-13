﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using static System.Math;
using Point = System.Drawing.Point;

namespace Object_Detection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private const int DepthToByte = 8000 / 256;        // - constant for conversion from depth distance in meters to 256 byte format 
        private KinectSensor kinectSensor = null;           // - variable reporesenting the active sensor 
        public DepthFrameReader depthFrameReader = null;   // - variable representing the depth frame reader 
        private FrameDescription frameDescription = null;   // - description of the data contained in the depth frame 
        public WriteableBitmap depthbitmap = null;         // - bitmap for displaying image 
        private byte[] depthpixels = null;                  // - intermediate storage for frame datta conveted to color pointer 
        private string KinectStatus = null;                 // - current status of the kinect sensor 
        public Object FrameObj = new Object();
        public string ObjectProperty = "";
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
                        if (((frameDescription.Width * frameDescription.Height) == (kinectBuffer.Size / frameDescription.BytesPerPixel)) && (frameDescription.Width == depthbitmap.PixelWidth) && (frameDescription.Height == depthbitmap.PixelHeight))
                        {
                            ushort maxDepthValue = Convert.ToUInt16(TreshSlide.Value);
                            ushort minDepthValue = 50;
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
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            ushort* framedata = (ushort*)depthFrameData;
            int PixelCount = 0;
            // show olny pixels within the requried range and count them  
            for (int i = 0; i < (int)(depthFrameDataSize / frameDescription.BytesPerPixel); ++i)
            {
                ushort depth = framedata[i];
                if (depth >= minDepth && depth <= maxDepth && depth != 0 && ((i / frameDescription.Width) > 120) && (i / frameDescription.Width < 424 - 100) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) > 250) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) < 512 - 120))
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
        private unsafe void RenderPixels()

        {
            FrameObj.Clear();
            Image<Gray, byte> Frame = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            Frame.Bytes = depthpixels;
            Image<Gray, byte> FilteredFrame = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            CvInvoke.MedianBlur(Frame, Frame, 7); 
            CvInvoke.BilateralFilter(Frame, FilteredFrame, 3, 140, 140);
            UMat FrameCannyImage = new UMat();
            CvInvoke.Canny(FilteredFrame, FrameCannyImage, 10, 200);
            VectorOfVectorOfPoint FrameImageContours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(FrameCannyImage, FrameImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint FrameAppContour = new VectorOfPoint(2);
            BitmapSource FrameBitmap;

            for (int k = 0; k < FrameImageContours.Size; k++)
            {
                VectorOfPoint contour = FrameImageContours[k];


                if (CvInvoke.ContourArea(contour) > CvInvoke.ContourArea(FrameAppContour))
                {
                    FrameAppContour = contour;
                }

            }
            RotatedRect FrameRotatedRect = CvInvoke.MinAreaRect(FrameAppContour);
            Image<Bgr, byte> ColoredImage = new Image<Bgr, byte>(frameDescription.Width, frameDescription.Height);
            System.Drawing.PointF[] FrameInitCenters = new System.Drawing.PointF[4];

            Moments moments = CvInvoke.Moments(FrameAppContour);
            
            FrameInitCenters[0].X = (int)(moments.M10 / moments.M00) + 1;
            FrameInitCenters[0].Y = (int)(moments.M01 / moments.M00) - 1;
            //ColoredImage[(int)FrameInitCenters[0].X, (int)FrameInitCenters[0].Y] = new Bgr(8, 159, 246);
            FrameInitCenters[1].X = (int)(moments.M10 / moments.M00) + 1;
            FrameInitCenters[1].Y = (int)(moments.M01 / moments.M00) + 1;
            //ColoredImage[(int)FrameInitCenters[0].X, (int)FrameInitCenters[0].Y] = new Bgr(8, 159, 246);
            FrameInitCenters[2].X = (int)(moments.M10 / moments.M00) - 1;
            FrameInitCenters[2].Y = (int)(moments.M01 / moments.M00) - 1;

            FrameInitCenters[3].X = (int)(moments.M10 / moments.M00) - 1;
            FrameInitCenters[3].Y = (int)(moments.M01 / moments.M00) + 1;

            byte[] NonZeroPixels = depthpixels.Where(s => s != 0).ToArray();
            List<System.Drawing.PointF> NonZPxlPoint = new List<System.Drawing.PointF>();

            for (int y = 0; y < (frameDescription.Height); y++)
            {
                for (int x = 0; x < (frameDescription.Width); x++)
                {
                    if (depthpixels[y * frameDescription.Width + x] != 0)
                    {
                        NonZPxlPoint.Add(new System.Drawing.PointF(x, y));
                    }
                }
            }

            Point3D[] CustomLabels = new Point3D[NonZeroPixels.Length];


            double[] distances = new double[4];

            for (int k = 0; k < NonZeroPixels.Length; k++)
            {


                distances[0] = Sqrt(Pow(Abs(NonZPxlPoint[k].X - FrameInitCenters[0].X), 2) + Pow(Abs(NonZPxlPoint[k].Y - FrameInitCenters[0].Y), 2));
                distances[1] = Sqrt(Pow(Abs(NonZPxlPoint[k].X - FrameInitCenters[1].X), 2) + Pow(Abs(NonZPxlPoint[k].Y - FrameInitCenters[1].Y), 2));
                distances[2] = Sqrt(Pow(Abs(NonZPxlPoint[k].X - FrameInitCenters[2].X), 2) + Pow(Abs(NonZPxlPoint[k].Y - FrameInitCenters[2].Y), 2));
                distances[3] = Sqrt(Pow(Abs(NonZPxlPoint[k].X - FrameInitCenters[3].X), 2) + Pow(Abs(NonZPxlPoint[k].Y - FrameInitCenters[3].Y), 2));



                CustomLabels[k].X = NonZPxlPoint[k].X;
                CustomLabels[k].Y = NonZPxlPoint[k].Y;
                CustomLabels[k].Z = Array.IndexOf(distances, distances.Max());

            }

            
            

            for (int k = 0; k < NonZeroPixels.Length - 1; k++)

            {
                switch (CustomLabels[k].Z)
                {


                    case 0:
                        FrameObj.Region1PixelCnt++;
                        ColoredImage[(int)CustomLabels[k].Y, (int)CustomLabels[k].X] = new Bgr(255, 0, 0);
                        break;
                    case 1:

                        FrameObj.Region2PixelCnt++;
                        ColoredImage[(int)CustomLabels[k].Y, (int)CustomLabels[k].X] = new Bgr(0, 255, 0);
                        break;
                    case 2:

                        FrameObj.Region3PixelCnt++;
                        ColoredImage[(int)CustomLabels[k].Y, (int)CustomLabels[k].X] = new Bgr(0, 0,255);
                        break;
                    case 3:

                        FrameObj.Region4PixelCnt++;
                        ColoredImage[(int)CustomLabels[k].Y, (int)CustomLabels[k].X] = new Bgr(180, 27, 100);
                        break;
                }
                FrameObj.PixelCount++;
            }

            R1.Content = FrameObj.Region1PixelCnt;
            R2.Content = FrameObj.Region2PixelCnt;
            R3.Content = FrameObj.Region3PixelCnt;
            R4.Content = FrameObj.Region4PixelCnt;
            MyLabel.Content = FrameObj.PixelCount;
            CvInvoke.Polylines(FilteredFrame, Array.ConvertAll(FrameRotatedRect.GetVertices(), Point.Round), true, new MCvScalar(205, 0, 255), 2);
            FrameBitmap = BitmapSourceConvert.ToBitmapSource(ColoredImage);
            LoadCapture.Source = FrameBitmap;
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), depthpixels, depthbitmap.PixelWidth, 0);
            FrameObj.CalculateTolerances();
            Precision.Content = ClassifyObject(FrameObj);

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

            ClassLabel classLabel = new ClassLabel();
            classLabel.Show();




        }
        // converts Image EmguCV class to bitmapsource 
        public static class BitmapSourceConvert
        {
            [DllImport("gdi32")]
            private static extern int DeleteObject(IntPtr o);

            public static BitmapSource ToBitmapSource(Image<Bgr, byte> image)
            {
                using (System.Drawing.Bitmap source = image.ToBitmap())
                {
                    IntPtr ptr = source.GetHbitmap();

                    BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    DeleteObject(ptr);
                    return bs;
                }
            }
        }
        private unsafe void LoadBtn_Click(object sender, RoutedEventArgs e)
        {


        }
        private string ClassifyObject(Object ClassificatedObj)
        {
            string Class = "";

            try
            {
                if (File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt").Count() != 0)
                {
                    for (int line = 0; line < File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt").Count(); line++)
                    {
                        double[,] Ratios = GetObjectRanges(File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt")[line].Split('*'));

                        if (File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt")[line] != "")
                        {

                            //Int32 TotalPixels = Int32.Parse(File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt")[line].Split('*')[6]);

                            if (
                                Ratios[0, 0] > ClassificatedObj.ratio1 && ClassificatedObj.ratio1 > Ratios[1, 0]
                                 && Ratios[0, 1] > ClassificatedObj.ratio2 && ClassificatedObj.ratio2 > Ratios[1, 1]
                                  && Ratios[0, 2] > ClassificatedObj.ratio3 && ClassificatedObj.ratio3 > Ratios[1, 2]
                                   && Ratios[0, 3] > ClassificatedObj.ratio4 && ClassificatedObj.ratio4 > Ratios[1, 3]
                                  )
                            {



                                Class = File.ReadAllLines("C:/Users/CPT Danko/Pictures/ObjectValues.txt")[line].Split('*')[0];
                                break;




                            }
                            else
                            {
                                Class = "Not Identified";
                            }
                        }


                    }
                }
            }
            catch (FileNotFoundException e)
            {
                using (var file = File.Create("C:/Users/CPT Danko/Pictures/ObjectValues.txt"))
                {

                }
            }
            return Class;

        }
        private double[,] GetObjectRanges(string[] ObjectLine)
        {
            double[,] rationRange = new double[2, 4];
            if (ObjectLine.Length != 1)

            {
                
                rationRange[0, 0] = Max(Convert.ToDouble((ObjectLine[2]).Split('-')[0]), Convert.ToDouble((ObjectLine[2]).Split('-')[1]));       // Upper tolerance value R1-R2
                rationRange[1, 0] = Min(Convert.ToDouble((ObjectLine[2]).Split('-')[0]), Convert.ToDouble((ObjectLine[2]).Split('-')[1]));      // Lower tolerance value R1- R2

                rationRange[0, 1] = Max(Convert.ToDouble((ObjectLine[3]).Split('-')[0]), Convert.ToDouble((ObjectLine[3]).Split('-')[1]));       // Upper tolerance value R2-R3
                rationRange[1, 1] = Min(Convert.ToDouble((ObjectLine[3]).Split('-')[0]), Convert.ToDouble((ObjectLine[3]).Split('-')[1]));       // Lower tolerance value R2- R3

                rationRange[0, 2] = Max(Convert.ToDouble((ObjectLine[4]).Split('-')[0]), Convert.ToDouble((ObjectLine[4]).Split('-')[1]));      // Upper tolerance value R1-R4
                rationRange[1, 2] = Min(Convert.ToDouble((ObjectLine[4]).Split('-')[0]), Convert.ToDouble((ObjectLine[4]).Split('-')[1]));      // Lower tolerance value R1- R4

                rationRange[0, 3] = Max(Convert.ToDouble((ObjectLine[5]).Split('-')[0]), Convert.ToDouble((ObjectLine[5]).Split('-')[1]));      // Upper tolerance value R2-R3
                rationRange[1, 3] = Min(Convert.ToDouble((ObjectLine[5]).Split('-')[0]), Convert.ToDouble((ObjectLine[5]).Split('-')[1]));      // Lower tolerance value R2- R3
            
            }

            return rationRange;

        }



    }

}

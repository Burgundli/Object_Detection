using Emgu.CV;
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
                        if (((frameDescription.Width * frameDescription.Height) == (kinectBuffer.Size / frameDescription.BytesPerPixel)) && (frameDescription.Width == depthbitmap.PixelWidth) && (frameDescription.Height == depthbitmap.PixelHeight))
                        {
                            ushort maxDepthValue = 1600;
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
                if (depth >= minDepth && depth <= maxDepth && depth != 0 && ((i / frameDescription.Width) > 100) && (i / frameDescription.Width < 253) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) > 100) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) < 412))
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
            /* Image<Gray,byte> image = new Image<Gray, byte>(512,424);
             image.Bytes = depthpixels; 
             CvInvoke.GaussianBlur(image, image, new System.Drawing.Size(5, 5), 0);
             depthpixels = image.Bytes;
            */
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), depthpixels, depthbitmap.PixelWidth, 0);

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
            byte[] ArrOfPxl = new byte[frameDescription.Width * frameDescription.Height];
            int PixelCount = 0;
            BitmapSource BluredBitmap;
            Object newObj = new Object();

            // - opens a specific file, decode it using bitmap decoder and copies it to an byte array of pixel values, then draws it to an image
            //- had to clear the filestream somehow 
            using (System.IO.Stream imageStreamSource = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BmpBitmapDecoder decoder = new BmpBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bitmapSource = decoder.Frames[0];
                bitmapSource.CopyPixels(ArrOfPxl, frameDescription.Width, 0);

            }



            // - read image from file calculate centroid based on moments

            Mat mat = CvInvoke.Imread("C:/Users/CPT Danko/Pictures/capture.png", ImreadModes.AnyColor);
            Moments moments = CvInvoke.Moments(mat, false);
            System.Drawing.Point WeightedCentroid = new System.Drawing.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
            Centroid.Content = WeightedCentroid.X.ToString() + "  " + WeightedCentroid.Y.ToString();

            // - Canny edge recognition based on image contours with Gaussian bluring 

            var image = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            image.Bytes = ArrOfPxl;
            CvInvoke.GaussianBlur(image, image, new System.Drawing.Size(5, 5), 0);
            var CannyImage = new UMat();
            CvInvoke.Canny(image, CannyImage, 10, 200);
            VectorOfVectorOfPoint ImageContours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(CannyImage, ImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint AppContour = new VectorOfPoint(2);
            VectorOfPoint AppContour2 = new VectorOfPoint(2);

            byte[,,] ImageArray = new byte[image.Width, image.Height, image.NumberOfChannels];
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {

                    ImageArray[x, y, 0] = depthpixels[y * image.Width + x];

                }
            }



            Image<Gray, byte> CapturedImage = new Image<Gray, byte>(ImageArray);
            var CannyImage2 = new UMat();
            CvInvoke.Canny(CapturedImage, CannyImage2, 10, 200);
            VectorOfVectorOfPoint ImageContours2 = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(CannyImage2, ImageContours2, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            for (int k = 0; k < ImageContours.Size; k++)
            {
                VectorOfPoint contour = ImageContours[k];


                if (CvInvoke.ContourArea(contour) > CvInvoke.ContourArea(AppContour))
                {
                    AppContour = contour;
                }

            }

            for (int k = 0; k < ImageContours2.Size; k++)
            {
                VectorOfPoint contour = ImageContours2[k];


                if (CvInvoke.ContourArea(contour) > CvInvoke.ContourArea(AppContour))
                {
                    AppContour2 = contour;
                }

            }

            double precision = CvInvoke.MatchShapes(AppContour, AppContour2, ContoursMatchType.I1);

            Precision.Content = precision;



            RotatedRect rotatedRect = CvInvoke.MinAreaRect(AppContour);














            List<System.Drawing.PointF> Input = new List<System.Drawing.PointF>();


            double[] min, max;
            Point[] minP, maxP;
            mat.MinMax(out min, out max, out minP, out maxP);


            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {




                    if (ArrOfPxl[y * image.Width + x] != 0)

                        Input.Add(new System.Drawing.PointF(x, y));



                }

            }
            //Distance.Content = max[0]; 
            System.Drawing.PointF[] newInput = Input.ToArray();


            float[,] floatInput = new float[newInput.Length, 2];
            for (int k = 0; k < newInput.Length; k++)
            {
                floatInput[k, 0] = newInput[k].X;
                floatInput[k, 1] = newInput[k].Y;
            }
            Matrix<float> FinalInput = new Matrix<float>(floatInput);

            Matrix<int> labels = new Matrix<int>(FinalInput.Size.Height, 1);

            System.Drawing.PointF[] initCenters = new System.Drawing.PointF[4];
            initCenters = rotatedRect.GetVertices();

            Point3D[] CustomLabels = new Point3D[newInput.Length];


            double[] distances = new double[4];

            for (int k = 0; k < newInput.Length; k++)
            {




                distances[0] = Sqrt(Pow(Abs(newInput[k].X - initCenters[0].X), 2) + Pow(Abs(newInput[k].Y - initCenters[0].Y), 2));
                distances[1] = Sqrt(Pow(Abs(newInput[k].X - initCenters[1].X), 2) + Pow(Abs(newInput[k].Y - initCenters[1].Y), 2));
                distances[2] = Sqrt(Pow(Abs(newInput[k].X - initCenters[2].X), 2) + Pow(Abs(newInput[k].Y - initCenters[2].Y), 2));
                distances[3] = Sqrt(Pow(Abs(newInput[k].X - initCenters[3].X), 2) + Pow(Abs(newInput[k].Y - initCenters[3].Y), 2));



                CustomLabels[k].X = newInput[k].X;
                CustomLabels[k].Y = newInput[k].Y;
                CustomLabels[k].Z = Array.IndexOf(distances, distances.Max());



            }


            Distance.Content = initCenters[1];




            //double compactness = CvInvoke.Kmeans(FinalInput, 4, labels, new MCvTermCriteria(200,0.5), 100,KMeansInitType.RandomCenters);
            Image<Bgr, byte> OutputImage = new Image<Bgr, byte>(mat.Cols, mat.Rows);


            for (int k = 0; k < FinalInput.Size.Height; k++)

            {
                switch (CustomLabels[k].Z)
                {


                    case 0:
                        OutputImage[(int)FinalInput[k, 1], (int)FinalInput[k, 0]] = new Bgr(255, 0, 0);
                        newObj.Region1PixelCnt++;
                        break;
                    case 1:
                        OutputImage[(int)FinalInput[k, 1], (int)FinalInput[k, 0]] = new Bgr(0, 255, 0);
                        newObj.Region2PixelCnt++;
                        break;
                    case 2:
                        OutputImage[(int)FinalInput[k, 1], (int)FinalInput[k, 0]] = new Bgr(0, 0, 255);
                        newObj.Region3PixelCnt++;
                        break;
                    case 3:
                        OutputImage[(int)FinalInput[k, 1], (int)FinalInput[k, 0]] = new Bgr(51, 255, 246);
                        newObj.Region4PixelCnt++;
                        break;
                }
                newObj.PixelCount++;
            }


            float[] region = new float[labels.Width];

            //labels.CopyTo(region); 


            float angle = rotatedRect.Angle;


            //CvInvoke.Polylines(OutputImage, Array.ConvertAll(rotatedRect.GetVertices(), Point.Round), true, new MCvScalar(255, 0, 0), 2);


            //var imageContours = new Image<Gray, byte>(image.Width, image.Height, new Gray(0));
            //CvInvoke.DrawContours(CannyImage, ImageContours, -1, new MCvScalar(255, 0, 0));

            BluredBitmap = BitmapSourceConvert.ToBitmapSource(OutputImage);








            //draw a black dot a the centre of a shape 
            WriteableBitmap CentroidBitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            CentroidBitmap.WritePixels(new Int32Rect(0, 0, CentroidBitmap.PixelWidth, CentroidBitmap.PixelHeight), ArrOfPxl, CentroidBitmap.PixelWidth, 0);
            LoadCapture.Source = BluredBitmap;


            // count pixels in the loaded image

            PixelCount = ArrOfPxl.Count(n => n != 0);

            //MyLabel.Content = PixelCount.ToString();

            MyLabel.Content = newObj.PixelCount;
            R1.Content = newObj.Region1PixelCnt;
            R2.Content = newObj.Region2PixelCnt;
            R3.Content = newObj.Region3PixelCnt;
            R4.Content = newObj.Region4PixelCnt;
        }




    }

}

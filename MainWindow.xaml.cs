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
using System.Numerics;
using Emgu.CV.ML;
using System.Windows.Markup.Localizer;

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
                            ushort maxDepthValue = 1500;
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
                if (depth >= minDepth && depth <= maxDepth && depth != 0 && ((i/512) > 100) && (i / 512 < 253) && ((i-((i/512)*512))>100) && ((i - ((i / 512) * 512)) < 412))
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

            public static BitmapSource ToBitmapSource(Image<Bgr,byte> image)
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
            Object newObj = new Object(); 

            
            // - read image from file calculate centroid based on moments 
            Mat mat = CvInvoke.Imread("C:/Users/CPT Danko/Pictures/capture.png",ImreadModes.AnyColor);             
            Moments moments = CvInvoke.Moments(mat, false);
            System.Drawing.Point WeightedCentroid = new System.Drawing.Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
            Centroid.Content = WeightedCentroid.X.ToString() + "  " + WeightedCentroid.Y.ToString();
            // - Canny edge recognition based on image contours with Gaussian bluring 
             var image = new Image<Gray, byte>("C:/Users/CPT Danko/Pictures/capture.png");
            // CvInvoke.GaussianBlur(mat, mat, new System.Drawing.Size(5, 5), 0);
             var CannyImage = new UMat();
             CvInvoke.Canny(image, CannyImage, 10, 200);
             VectorOfVectorOfPoint ImageContours = new VectorOfVectorOfPoint();
             CvInvoke.FindContours(CannyImage, ImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint AppContour = new VectorOfPoint(2);
            VectorOfPoint AppContour2 = new VectorOfPoint(2);

            byte[,,] ImageArray = new byte[512, 424, 1];
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 424; y++)
                {

                    ImageArray[x, y, 0] = depthpixels[y * 512 + x];

                }
            }



            Image<Gray, byte> CapturedImage = new Image<Gray, byte>(ImageArray);
            var CannyImage2 = new UMat();
            CvInvoke.Canny(CapturedImage, CannyImage2, 10, 200);
            VectorOfVectorOfPoint ImageContours2 = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(CannyImage2, ImageContours2, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            for (int k= 0;k < ImageContours.Size; k++)
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

            double precision = CvInvoke.MatchShapes(AppContour,AppContour2,ContoursMatchType.I1);

            Precision.Content = precision; 



            RotatedRect rotatedRect = CvInvoke.MinAreaRect(AppContour);
            //CvInvoke.Polylines(image, Array.ConvertAll(rotatedRect.GetVertices(), Point.Round), true, new MCvScalar(255, 0, 0), 2);
            System.Drawing.PointF direction, pointOnLine;
            System.Drawing.PointF[] line = rotatedRect.GetVertices();

            /*
             * for (int i = 0;i<AppContour.ToArray().Length; i++)
            {
                line[i].X = (float)AppContour.ToArray()[i].X;
                line[i].Y = (float)AppContour.ToArray()[i].Y; 

            }
            */

           
            CvInvoke.FitLine(line, out direction , out pointOnLine ,Emgu.CV.CvEnum.DistType.L2, 0, 0.01, 0.01);
            int left = (int)((- pointOnLine.X * direction.Y / direction.X) + pointOnLine.Y);
            int right = (int)(((image.Width - pointOnLine.X) * direction.Y / direction.X) + pointOnLine.Y);


            int normLeft = (int)((-pointOnLine.Y * direction.Y / direction.X) + pointOnLine.X);
            int normRight = (int)(((image.Width - pointOnLine.Y) * direction.Y / direction.X) + pointOnLine.X);

            //CvInvoke.Line(image, new Point(image.Width -1, right), new Point(0, left), new MCvScalar(255,0,0),1);
           // CvInvoke.Line(image, new Point( normRight, image.Width - 1), new Point(normLeft, 0), new MCvScalar(255, 0, 0), 1);
           
            
            LineIterator HorzLine = new LineIterator(mat, new Point(normRight, image.Width - 1), new Point(normLeft, 0));

            LineIterator VerctLine = new LineIterator(mat, new Point(image.Width - 1, right), new Point(0, left));



            // - opens a specific file, decode it using bitmap decoder and copies it to an byte array of pixel values, then draws it to an image
            //- had to clear the filestream somehow 
            using (System.IO.Stream imageStreamSource = new FileStream("C:/Users/CPT Danko/Pictures/capture.png", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BmpBitmapDecoder decoder = new BmpBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource bitmapSource = decoder.Frames[0];
                bitmapSource.CopyPixels(ArrOfPxl, 512, 0);

            }



            Point[] buffHorz = new Point[HorzLine.Count];
            Point[] buffVertic = new Point[VerctLine.Count];
            int lenght = ArrOfPxl.Count(n => n != 0);

            Point4D[] regionProp = new Point4D[512*424];
            int counter = 0;
            Matrix<float> trainsample = new Matrix<float>(512*424, 2);
            Matrix<float> trainClasses = new Matrix<float>(512*424, 1);

            for (int i = 0; i < rotatedRect.Center.Y; i++)
            {
              
              
                
                for (int x = 0; x < 512; x++)
                {
                    int[] value = new int[1];
                    Marshal.Copy(mat.DataPointer + (i * mat.Cols + x) * mat.ElementSize, value, 0, 1);

                    
                   
                    
                    if (x < rotatedRect.Center.X && value[0] !=0)
                    {
                        regionProp[i*512+x].Z = 1;
                        regionProp[i * 512 + x].W = value[0] * regionProp[i * 512 + x].Z;
                        regionProp[i * 512 + x].X = x;
                        regionProp[i * 512 + x].X = i;
                       // image[i, x] = new Bgr(0, 0, 255);
                    }
                    else if (x > rotatedRect.Center.X && value[0] != 0)
                    {
                        regionProp[i * 512 + x].Z = 2;
                        regionProp[i * 512 + x].W = value[0] * regionProp[i * 512 + x].Z;
                        regionProp[i * 512 + x].X = x;
                        regionProp[i * 512 + x].X = i;
                       // image[i, x] = new Bgr(120, 120, 120);
                    }
                    regionProp[i * 512 + x].X = 0;
                }
                 
            }
            counter = 0; 
            for (int l = 0; l <512 ; l++)
            {

                
                for (int x = (int)rotatedRect.Center.Y; x < 424; x++)
                {
                    int[] value = new int[1];
                    Marshal.Copy(mat.DataPointer + (x * mat.Cols + l) * mat.ElementSize, value, 0, 1);

                    


                    if (l< rotatedRect.Center.X && value[0] != 0)
                    {
                        regionProp[x*512+l].W = value[0] * regionProp[x * 512 + l].Z;
                        regionProp[x * 512 + l  ].Z = 4;
                        regionProp[x * 512 + l].X = l;
                        regionProp[x * 512 + l].Y = x;
                       // image[x, l] = new Bgr(255, 0, 0); 

                    }
                    else if (l < rotatedRect.Center.X && value[0] != 0)
                    {
                        regionProp[x * 512 + l].W = value[0] * regionProp[x * 512 + l].Z;
                        regionProp[x * 512 + l].Z = 3;
                        regionProp[x * 512 + l].X = l;
                        regionProp[x * 512 + l].Y = x;
                       // image[x, l] = new Bgr(0, 255, 0);


                    }
                    regionProp[x * 512 + l].X = 0;
                }

               
            }


           
            for (int i = 0;i<ArrOfPxl.Length;i++) 
            {
                    trainsample[i, 0] = (float)regionProp[i].W;
                    trainClasses[i, 0] = (float)regionProp[i].Z;

            }



           // Matrix<byte> InputArrayMat = new Matrix<byte>(512, 424);
            List <System.Drawing.PointF> Input = new List<System.Drawing.PointF>(); 
           // mat.CopyTo(InputArrayMat);
            double[] cnt;
            double[] min, max;
            Point[] minP, maxP;
            mat.MinMax( out min,out max,out minP,out maxP); 
            //CvInvoke.Resize(mat, mat,new System.Drawing.Size (1025,768),0,0,Inter.Linear); 
            
            for (int y=0; y<mat.Size.Height; y++)
            {
                for (int x = 0; x < mat.Size.Width; x++)
                {
                    
                    int[] value = new int[1];
                    Marshal.Copy(mat.DataPointer + (y * mat.Cols + x) * mat.ElementSize, value, 0, 1);
                    
                    if (value[0] != 0)

                        Input.Add(new System.Drawing.PointF(x,y));
                     
                    

                }
                
            }
            Distance.Content = max[0]; 
            System.Drawing.PointF[] newInput = Input.ToArray();
            float[,] floatInput = new float[newInput.Length,2]; 
            for (int k=0; k < newInput.Length; k++)
            {
                floatInput[k,0] = newInput[k].X;
                floatInput[k,1] = newInput[k].Y;
            }
            Matrix<float> FinalInput = new Matrix<float>(floatInput);

            Matrix<int> labels = new Matrix<int>(FinalInput.Size.Height,1);
            
           

            double compactness = CvInvoke.Kmeans(FinalInput, 4, labels, new MCvTermCriteria(200,0.5), 100,0);
            Image<Bgr, byte> OutputImage = new Image<Bgr,byte>(mat.Cols,mat.Rows);


            for (int k = 0; k < FinalInput.Size.Height; k++)

            {
                switch (labels[k, 0]) {


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
            




            //var imageContours = new Image<Gray, byte>(image.Width, image.Height, new Gray(0));
            //CvInvoke.DrawContours(CannyImage, ImageContours, -1, new MCvScalar(255, 0, 0));
            CvInvoke.Circle(image,new Point( (int)pointOnLine.X, (int)pointOnLine.Y), 2,new MCvScalar(0,255,255));
             BluredBitmap = BitmapSourceConvert.ToBitmapSource(OutputImage);
            







            //draw a black dot a the centre of a shape 
            WriteableBitmap CentroidBitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            CentroidBitmap.WritePixels(new Int32Rect(0, 0, CentroidBitmap.PixelWidth, CentroidBitmap.PixelHeight), ArrOfPxl, CentroidBitmap.PixelWidth, 0);
            LoadCapture.Source = BluredBitmap;


            // count pixels in the loaded image
            PixelCount = ArrOfPxl.Count(n => n != 0);
            //MyLabel.Content = PixelCount.ToString();
            
            MyLabel.Content =PixelCount;
            R1.Content = newObj.Region1PixelCnt;
            R2.Content = newObj.Region2PixelCnt;
            R3.Content = newObj.Region3PixelCnt;
            R4.Content = newObj.Region4PixelCnt;
        }
             

    }
    
}

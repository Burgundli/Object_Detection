﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
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
using static System.Math;

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
        public string ObjectProperty = "";
        List<ImageDataset> Datasets = new List<ImageDataset>();
        Object FrameObj = new Object();
        KNearest KNN = new KNearest();
        List<ImageDataset> TrainD = new List<ImageDataset>();
        List<ImageDataset> TestD = new List<ImageDataset>();
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
                if (depthFrame != null && !Application.Current.Windows.OfType<Window>().Any(w => w.Name.Equals("ClassLabelWin")))
                {
                    using (KinectBuffer kinectBuffer = depthFrame.LockImageBuffer())
                    {
                        if (((frameDescription.Width * frameDescription.Height) == (kinectBuffer.Size / frameDescription.BytesPerPixel)) && (frameDescription.Width == depthbitmap.PixelWidth) && (frameDescription.Height == depthbitmap.PixelHeight))
                        {
                            ushort maxDepthValue = 1039;
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
                if (depth >= minDepth && depth <= maxDepth && depth != 0 && ((i / frameDescription.Width) > 136) && (i / frameDescription.Width < 424 - 136) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) > 186) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) < 512 - 186))
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

            /*
            Image<Gray, byte> Frame = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            Frame.Bytes = depthpixels;
            System.Drawing.PointF[] centers, NonZeroPxlPoint;
            byte[] NonZeroPxl ; 
            


            Image<Gray, byte> FilteredFrame = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            CvInvoke.BilateralFilter(Frame, FilteredFrame, 9, 140, 140);

            (NonZeroPxl,centers) =  ProceessImage(FilteredFrame);

             
            var LabeledArray = FrameObj.Calculate_Kmeans(centers, NonZeroPxl.ToArray());
            FrameObj.CalculateRegions(LabeledArray);

            CvInvoke.Polylines(FilteredFrame, Array.ConvertAll(centers, Point.Round), true, new MCvScalar(205, 0, 255), 2);
            BitmapSource FrameBitmap = BitmapSourceConvert.ToBitmapSource(FilteredFrame);
            LoadCapture.Source = FrameBitmap;



            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), depthpixels, depthbitmap.PixelWidth, 0);

            FrameObj.CalculateRegions(LabeledArray);

            Precision.Content = ClassifyObject(FrameObj);
 
            */
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
        private unsafe void GetTrainData_Click(object sender, RoutedEventArgs e)
        {

            var images = Directory.GetFiles("C:/Users/CPT Danko/Desktop/images");
            double oneperc = 100.00 / images.Length;

            if (File.Exists("C:/Users/CPT Danko/Pictures/ObjectValues.txt"))
            {
                File.Delete("C:/Users/CPT Danko/Pictures/ObjectValues.txt");

            }


            for (var image = 0; image < images.Length; image++)
            {
                var SelectImg = new Image<Gray, byte>(images[image]);
                var ImgName = Path.GetFileName(images[image]);
                var label = int.Parse(ImgName.Substring(ImgName.IndexOf("_") - 2, 2));
                var index = Datasets.FindIndex(x => x.Class == label);

                System.Drawing.PointF[] centers;
                byte[] NoZeroPxls;


                (NoZeroPxls, centers) = ProceessImage(SelectImg);

                var LabeledArray = FrameObj.Calculate_Kmeans(centers, NoZeroPxls);
                var Regions = FrameObj.CalculateRegions(LabeledArray);

                Image<Bgr, byte> colored = new Image<Bgr, byte>(512, 424);

                /*
                if (image == 5)
                {
                    foreach (var pixel in LabeledArray)
                    {
                        switch (pixel.Z)
                        {
                            case 0:
                                colored[(int)(pixel.Y), (int)(pixel.X)] = new Bgr(0, 255, 0);
                                break;
                            case 1:
                                colored[(int)(pixel.Y), (int)(pixel.X)] = new Bgr(0, 0, 255);
                                break;
                            case 2:
                                colored[(int)(pixel.Y), (int)(pixel.X)] = new Bgr(255, 0, 0);
                                break;
                            case 3:
                                colored[(int)(pixel.Y), (int)(pixel.X)] = new Bgr(0, 247, 255);
                                break;


                        }

                    }
                     
                    //CvInvoke.Polylines(SelectImg, Array.ConvertAll(centers, System.Drawing.Point.Round), true, new MCvScalar(205, 0, 255), 2);
                    BitmapSource FrameBitmap = BitmapSourceConvert.ToBitmapSource(colored);
                    LoadCapture.Source = FrameBitmap;

                }
                */

                if (index > -1)
                {
                    Datasets[index].Images.Add(SelectImg);
                    Datasets[index].RegionsValues.Add(Regions.ToArray());
                }
                else
                {
                    ImageDataset img = new ImageDataset();
                    img.Images = new List<Image<Gray, byte>>();
                    img.RegionsValues = new List<float[]>();
                    img.Images.Add(SelectImg);
                    img.Class = label;
                    img.RegionsValues.Add(Regions.ToArray());
                    Datasets.Add(img);

                }



                string ImageTrainData = label + "*" + "*" + ImgName;
                foreach (var region in Regions)
                {
                    ImageTrainData += region.ToString() + "*";


                }

                Progress.Value += oneperc;


                File.AppendAllText("C:/Users/CPT Danko/Pictures/ObjectValues.txt", ImageTrainData + Environment.NewLine);



            }








        }
        private void Train_KNN()
        {


            Matrix<float> MatTrainData =null;
            Matrix<int> MatClass = null;
           (MatTrainData, MatClass) = FrameObj.DataToMatrix(TrainD);



            KNN.DefaultK = 10;
            KNN.IsClassifier = true;
            KNN.Train(MatTrainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, MatClass);

            

            

        }
        private void DataToMatrix()
        {


        }
        private void SplitData()
        {


            (TrainD,TestD) = FrameObj.TestTrainSplit(Datasets);
                     

        }
        private void Test_KNN()
        {
            Matrix<float> MData = null;
            Matrix<int> MClass = null;
            List<int> Predictions = new List<int>();
            List<int> ActualClasses = new List<int>(); 


            (MData, MClass) = FrameObj.DataToMatrix(TestD);

            for (int data = 0;data<MData.Rows; data++)
            {


                var prediction = KNN.Predict(MData.GetRow(data));
                Predictions.Add((int)prediction);
                ActualClasses.Add(MClass[data, 0]); 
                

                
                
            }

            var ConfMatrix = Object.ComputeConfusionMatrix(ActualClasses.ToArray(), Predictions.ToArray());
            var metrics = Object.CalculateMetrics(ConfMatrix, ActualClasses.ToArray(), Predictions.ToArray());
            string results = $"Test Samples {ActualClasses.Count} \n   Accuraccy = {metrics[0] * 100} % \n " +
                $"Precission = {metrics[1] * 100} % \n  Recall = {metrics[2] * 100} %";
            Precision.Content = results; 

        }

        private (byte[] ProcessedPixels, System.Drawing.PointF[] centroids) ProceessImage(Image<Gray, byte> FilteredImage)
        {


            UMat FrameCannyImage = new UMat();
            CvInvoke.Canny(FilteredImage, FrameCannyImage, 10, 200);
            VectorOfVectorOfPoint FrameImageContours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(FrameCannyImage, FrameImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint FrameAppContour = new VectorOfPoint(2);

            for (int k = 0; k < FrameImageContours.Size; k++)
            {
                VectorOfPoint contour = FrameImageContours[k];


                if (CvInvoke.ContourArea(contour) > CvInvoke.ContourArea(FrameAppContour))
                {
                    FrameAppContour = contour;
                }

            }


            RotatedRect FrameRotatedRect = CvInvoke.MinAreaRect(FrameAppContour);
            System.Drawing.PointF[] FrameInitCenters = FrameRotatedRect.GetVertices();

            byte[] NonZeroPixels = FilteredImage.Bytes;


            return (NonZeroPixels, FrameInitCenters);
        }

        private void Test_Accuracy_Click(object sender, RoutedEventArgs e)
        {

            SplitData();
            Train_KNN(); 
            Test_KNN(); 


        }
    }

}

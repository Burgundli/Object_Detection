using Emgu.CV;
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
using Numpy; 

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
        private bool IsDataLoaded, IsTrained = false;
        private List<int> RawData = new List<int>();
        int frameCount = 0;
        List<Bgr> Colors = new List<Bgr>();




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

            RawData.Clear();
            Random rnd = new Random();
            for (int k = 0; k < 15; k++)
            {
               

                Bgr colorB = new Bgr((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
                Colors.Add(colorB);

            }
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
                            ushort maxDepthValue = 1499;
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
            RawData.Clear();
            // show olny pixels within the requried range and count them  
            for (int i = 0; i < (int)(depthFrameDataSize / frameDescription.BytesPerPixel); ++i)
            {
                ushort depth = framedata[i];

                

                if (depth >= minDepth && depth <= maxDepth && depth != 0 && ((i / frameDescription.Width) > 50) && (i / frameDescription.Width < 424 - 50) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) > 50) && ((i - ((i / frameDescription.Width) * frameDescription.Width)) < 512 - 50))
                {
                    depthpixels[i] = (byte)(256 - (depth / DepthToByte));
                    RawData.Add((int)depth);
                    PixelCount++;

                }
                else
                {
                    depthpixels[i] = 0;
                    RawData.Add(0);
                }

            }

            MyLabel2.Content = PixelCount.ToString();
        }
        private unsafe void RenderPixels()

        {
            /*
            Image<Gray, byte> image = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            image.Bytes = depthpixels;

            if (frameCount == 2 && IsDataLoaded == true && IsTrained == true )
            {
                Image<Bgr, byte> ColoredImage = new Image<Bgr, byte>(512, 424);
                Image<Bgr, byte> NewImage = new Image<Bgr, byte>(512, 424);
                
                var RawDataArr = RawData.ToArray();
                int[] RawDataCount = new int[15];

                List<Image<Gray, byte>> ImageList = new List<Image<Gray, byte>>(); 
                for (int k = 0; k < 15; k++)
                {
                    ImageList.Add(new Image<Gray, byte>(512, 424)); 
                }
                
                for (int i = 0; i < RawDataArr.Length; i++)
                {
                    
                    var mapped = MapValue(0, 1500, 0, 15, RawDataArr[i]); 

                    ColoredImage[(i / 512), (i - ((i / 512) * 512))] = Colors[(int)mapped];
                    RawDataCount[(int)mapped]++;
                    ImageList[(int)mapped][(i / 512), (i - ((i / 512) * 512))] = (int)mapped == 0 ?new Gray(0):new Gray(255); 

                    
                }
                
                int help = 0; 
                foreach(var gray in ImageList)
                {
                    CvInvoke.MedianBlur(gray, gray, 5);
                    gray.Save("C:/Users/CPT Danko/Desktop/Test/" + help + ".png");
                    help++; 
                }
                
                
                CvInvoke.MedianBlur(ColoredImage, ColoredImage, 5);
                List<float> predictedClassed = new List<float>(); 

                foreach(var IMG in ImageList)
                {
                    CvInvoke.MedianBlur(IMG, IMG, 5);
                    System.Drawing.PointF[] cntrs;
                    byte[] NonZeroPixel;
                    List<float[]> newRegion = new List<float[]>();

                    System.Drawing.Point[] contour; 

                    (NonZeroPixel, cntrs,contour) = ProceessImage(IMG);

                    VectorOfPoint vector = new VectorOfPoint(contour);

                    var Points = FrameObj.Calculate_Kmeans(cntrs, NonZeroPixel);
                    var Regions = FrameObj.CalculateRegions(Points);
                    newRegion.Add(Regions.ToArray());


                    Matrix<float> matrix = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));


                    var prediction = KNN.Predict(matrix);
                    DetectedClass.Content = prediction;
                    predictedClassed.Add(prediction);
                    VectorOfPoint DataVector = new VectorOfPoint(Datasets[(int)prediction-1].ImageContours[0]); 

                    if (prediction == 4 
                        //&& CvInvoke.MatchShapes(vector,DataVector,ContoursMatchType.I1) < 6
                        )
                    {
                        var rotedRect = CvInvoke.MinAreaRect(vector);
                        CvInvoke.Polylines(ColoredImage, Array.ConvertAll(rotedRect.GetVertices(), System.Drawing.Point.Round), true, new MCvScalar(255, 255, 255), 2);


                    }

                }

               

                var CannyImage = ColoredImage.Canny(100, 200);

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                
               

                CvInvoke.FindContours(CannyImage, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                

                List<double> matcheShapes = new List<double>(); 
                
               
                
                BitmapSource bitmapSource = BitmapSourceConvert.ToBitmapSource(ColoredImage);
                LoadCapture.Source = bitmapSource;
                
                


                //Image<Gray, byte> Filteredimage = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
                //CvInvoke.MedianBlur(image, image, 3);
                //CvInvoke.BilateralFilter(image, Filteredimage, 9, 75, 75);


                /*





                            if (KNN != null && IsDataLoaded == true && IsTrained == true && depthpixels.Where(x => x > 0).Count() != 0)
                            {


                                System.Drawing.PointF[] cntrs;
                                byte[] NonZeroPixel;
                                List<float[]> newRegion = new List<float[]>();
                                VectorOfPoint vector = new VectorOfPoint(); 


                                (NonZeroPixel, cntrs) = ProceessImage(Filteredimage);
                                var Points = FrameObj.Calculate_Kmeans(cntrs, NonZeroPixel);
                                var Regions = FrameObj.CalculateRegions(Points);

                                newRegion.Add(Regions.ToArray());


                                Matrix<float> matrix = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));


                                var prediction = KNN.Predict(matrix);
                                Prediction.Content = prediction;

                            }

                            
                frameCount = 0;
            }
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), image.Bytes, depthbitmap.PixelWidth, 0);
            frameCount++;
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

            var images = Directory.GetFiles("C:/Users/CPT Danko/Desktop/images_1");
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
                List<Image<Gray, byte>> NewImage = new List<Image<Gray, byte>>();
                System.Drawing.Point[] contour;  

                (NoZeroPxls, centers,contour) = ProceessImage(SelectImg);

                var LabeledArray = FrameObj.Calculate_Kmeans(centers, NoZeroPxls);
                var Regions = FrameObj.CalculateRegions(LabeledArray);

                Image<Bgr, byte> colored = new Image<Bgr, byte>(512, 424);


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

                    CvInvoke.Polylines(colored, Array.ConvertAll(centers, System.Drawing.Point.Round), true, new MCvScalar(100, 50, 30), 2);
                    //BitmapSource FrameBitmap = BitmapSourceConvert.ToBitmapSource(colored);
                    //LoadCapture.Source = FrameBitmap;

                }


                if (index > -1)
                {
                    Datasets[index].Images.Add(SelectImg);
                    Datasets[index].RegionsValues.Add(Regions.ToArray());
                    Datasets[index].ImageContours.Add(contour);
                    Datasets[index].NonZeroPixels.Add(NoZeroPxls.Length); 
                }
                else
                {
                    ImageDataset img = new ImageDataset();
                    img.Images = new List<Image<Gray, byte>>();
                    img.RegionsValues = new List<float[]>();
                    img.ImageContours = new List<System.Drawing.Point[]>();
                    img.NonZeroPixels = new List<int>(); 
                    img.Images.Add(SelectImg);
                    img.Class = label;
                    img.RegionsValues.Add(Regions.ToArray());
                    img.ImageContours.Add(contour); 
                    img.NonZeroPixels.Add(NoZeroPxls.Length); 
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



            IsDataLoaded = true;
            frameCount = 0; 



        }
        private void Train_KNN()
        {


            Matrix<float> MatTrainData = null;
            Matrix<int> MatClass = null;
            (MatTrainData, MatClass) = FrameObj.DataToMatrix(TrainD);



            KNN.DefaultK = 10;
            KNN.IsClassifier = true;
            KNN.Train(MatTrainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, MatClass);





        }
        private void Train_KNN_Full()
        {

            Matrix<float> MatTrainData = null;
            Matrix<int> MatClass = null;
            (MatTrainData, MatClass) = FrameObj.DataToMatrix(Datasets);



            KNN.DefaultK = 10;
            KNN.IsClassifier = true;
           
            KNN.Train(MatTrainData, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, MatClass);

        }
        private void SplitData()
        {


            (TrainD, TestD) = FrameObj.TestTrainSplit(Datasets);


        }
        private void Test_KNN()
        {
            Matrix<float> MData = null;
            Matrix<int> MClass = null;
            List<int> Predictions = new List<int>();
            List<int> ActualClasses = new List<int>();


            (MData, MClass) = FrameObj.DataToMatrix(TestD);

            for (int data = 0; data < MData.Rows; data++)
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

        private (byte[] ProcessedPixels, System.Drawing.PointF[] centroids, System.Drawing.Point[]) ProceessImage(Image<Gray, byte> FilteredImage)
        {

            UMat FrameCannyImage = new UMat();
            CvInvoke.Canny(FilteredImage, FrameCannyImage, 100,200);
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

            System.Drawing.Point[] contours = FrameAppContour.ToArray(); 
            byte[] NonZeroPixels = FilteredImage.Bytes;
            NonZeroPixels = NonZeroPixels.Where(x => x != 0).ToArray(); 

            return (NonZeroPixels, FrameInitCenters,FrameAppContour.ToArray());
        }

        private void Test_Accuracy_Click(object sender, RoutedEventArgs e)
        {

            SplitData();
            Train_KNN();
            Test_KNN();


        }

        private void Start_Detection_Click(object sender, RoutedEventArgs e)
        {
            Train_KNN_Full();
            IsTrained = true;
            frameCount = 0; 
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {


            Image<Gray, byte> image = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            image.Bytes = depthpixels;

            Image<Bgr, byte> ColoredImage = new Image<Bgr, byte>(512, 424);


            var ImageList = SegmentImage(15); 

           
        
            List<System.Drawing.Rectangle> Boxes = Sliding_Window(ImageList, new System.Drawing.Size(128, 140));
            int[] intersectionCount = new int[Boxes.Count]; 

            for (int c = 0; c < Boxes.Count; c++)
            {
                for(int k = 0; k < Boxes.Count; k++)
                {
                    if (Boxes[c].IntersectsWith(Boxes[k]))
                    {
                        intersectionCount[c]++; 
                    }

                }
                

            }

            var maxindex = intersectionCount.ToList().IndexOf(intersectionCount.Max()); 
            CvInvoke.Rectangle(image, Boxes[maxindex], new MCvScalar(255, 255, 255));
            CvInvoke.MedianBlur(ColoredImage, ColoredImage, 3);

            /*
            foreach (var box in Boxes)
            {

                CvInvoke.Rectangle(image, box, new MCvScalar(255, 255, 255));
                CvInvoke.MedianBlur(ColoredImage, ColoredImage, 3);
            }
            */

            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), image.Bytes, depthbitmap.PixelWidth, 0);
            BitmapSource bitmapSource = BitmapSourceConvert.ToBitmapSource(ColoredImage);
            LoadCapture.Source = bitmapSource;


        }

        private List<Image<Gray, byte>> SegmentImage(int RegionCount)
        {

            var RawDataArr = RawData.ToArray();
            int[] RawDataCount = new int[RegionCount];

            List<Image<Gray, byte>> ImageList = new List<Image<Gray, byte>>();

            for (int k = 0; k < RegionCount; k++)
            {
                ImageList.Add(new Image<Gray, byte>(512, 424));
            }

            for (int i = 0; i < RawDataArr.Length; i++)
            {

                var mapped = MapValue(0, 1500, 0, RegionCount, RawDataArr[i]);

                //ColoredImage[(i / 512), (i - ((i / 512) * 512))] = Colors[(int)mapped];
                RawDataCount[(int)mapped]++;
                ImageList[(int)mapped][(i / 512), (i - ((i / 512) * 512))] = (int)mapped == 0 ? new Gray(0) : new Gray(255);


            }

            return ImageList; 

        }
        private List<System.Drawing.Rectangle> Sliding_Window(List<Image<Gray, byte>> InputImage, System.Drawing.Size patchSize)

        {

            List<System.Drawing.Rectangle> Outrectangles = new List<System.Drawing.Rectangle>();
            Image<Gray, byte> ImagePatch = new Image<Gray, byte>(patchSize);
            Image<Gray, byte> OutputImage = new Image<Gray, byte>(512, 424); 
            System.Drawing.Rectangle subrect = new System.Drawing.Rectangle(0, 0, patchSize.Width, patchSize.Height);
            var help = 0; 

            foreach (var IMG in InputImage)
            {

                CvInvoke.MedianBlur(IMG, IMG, 5); 

                for (int h = 0; h <= (IMG.Height - patchSize.Height); h += 35)
                {

                    for (int w = 0; w <= (IMG.Width - patchSize.Width); w += 32)
                    {

                        subrect.X = w;
                        subrect.Y = h;
                        ImagePatch = IMG.GetSubRect(subrect).Copy();
                       
                           
                      

                        help++;
                        
                        if (ImagePatch.Bytes.Where(x => x != 0).Count() != 0) {

                            List<float[]> newRegion = new List<float[]>();
                            byte[] Pixels;
                            System.Drawing.PointF[] centers;
                            System.Drawing.Point[] contour;

                            //ImagePatch.Save("C:/Users/CPT Danko/Desktop/Test/" + help + ".png");
                            (Pixels, centers, contour) = ProceessImage(ImagePatch);

                            var Kmeans = FrameObj.Calculate_Kmeans(centers, Pixels);
                            var Regions = FrameObj.CalculateRegions(Kmeans);

                            newRegion.Add(Regions.ToArray());

                            Matrix<float> matrixk = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));

                            var prediction = KNN.Predict(matrixk);


                            if (prediction == 4)
                            {

                                Outrectangles.Add(subrect);

                            }
                        }

                    }



                }
            }

            
            return Outrectangles;



        }
        private void Sliding_Window_Scan(List<Image<Gray, byte>> InputImage, System.Drawing.Size patchSize)

        {

            List<System.Drawing.Rectangle> Outrectangles = new List<System.Drawing.Rectangle>();
            Image<Gray, byte> ImagePatch = new Image<Gray, byte>(patchSize);
            Image<Gray, byte> OutputImage = new Image<Gray, byte>(512, 424);
            System.Drawing.Rectangle subrect = new System.Drawing.Rectangle(0, 0, patchSize.Width, patchSize.Height);
            var help = 0;

            foreach (var IMG in InputImage)
            {

                CvInvoke.MedianBlur(IMG, IMG, 5);

                for (int h = 0; h <= (IMG.Height - patchSize.Height); h += 35)
                {

                    for (int w = 0; w <= (IMG.Width - patchSize.Width); w += 32)
                    {

                        subrect.X = w;
                        subrect.Y = h;
                        ImagePatch = IMG.GetSubRect(subrect).Copy();




                        help++;
                        var Count = ImagePatch.Bytes.Where(x => x != 0).Count();

                        if (Count > 100)
                        {

                            ImagePatch.Save("C:/Users/CPT Danko/Desktop/images_1/NegativeOne07_" + help+ ".png");
                           
                        }

                    }



                }
            }


            



        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            
            List<float[]> newRegion = new List<float[]>();
            byte[] Pixels;
            System.Drawing.PointF[] centers;
            System.Drawing.Point[] contour;

            Image<Gray,byte> IMG = new Image<Gray, byte>("C:/Users/CPT Danko/Desktop/Test/"+TexBox.Text+".png"); 
            (Pixels, centers, contour) = ProceessImage(IMG);
            var Kmeans = FrameObj.Calculate_Kmeans(centers, Pixels);
            var Regions = FrameObj.CalculateRegions(Kmeans);

            newRegion.Add(Regions.ToArray());

            Matrix<float> matrixk = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));

            var prediction = KNN.Predict(matrixk);
            DetectedClass.Content = prediction; 
        }

        private void Scan_Enviroment_Click(object sender, RoutedEventArgs e)
        {

            var Segmented = SegmentImage(15);
            Sliding_Window_Scan(Segmented, new System.Drawing.Size(128, 140)); 


        }

        public double MapValue(double a0, double a1, double b0, double b1, double a)
        {
            return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
        }
        public List<System.Drawing.Rectangle> NonMaxSuppression(List<System.Drawing.Rectangle> Boxes, float overlapTresh)
        {

            if(Boxes.Count == 0)
            {

            }

            List<float> X1_cor = new List<float>();
            List<float> Y1_cor = new List<float>();
            List<float> X2_cor = new List<float>();
            List<float> Y2_cor = new List<float>();
            List<float> Area = new List<float>();
            List<float> vs = new List<float>();
            List<float> pick = new List<float>(); 
            List<System.Drawing.Rectangle> PickedBoxes = new List<System.Drawing.Rectangle>();
            List<long> OldIndexes = new List<long>();
            foreach (var box in Boxes)
            {

                X1_cor.Add(box.X);
                X2_cor.Add(box.X+box.Width);
                Y1_cor.Add(box.Y);
                Y2_cor.Add(box.Y+box.Height);
                Area.Add(((box.X+box.Width) - box.X + 1) * ((box.Y+box.Height) - box.Y + 1));

            }


            
            
            var nw =  np.argsort(np.array(Y2_cor.ToArray()));
            var indxs = nw.GetData<Int64>().ToList();
            var sss = nw.dtype; 
            while (indxs.Count > 0 && OldIndexes.Count != indxs.Count)
            {

                var last = indxs.Count - 1;
                var i = indxs[last];
                pick.Add(i);
                List<Int64> supress = new List<Int64>(); 

                for(int k =0;k<last;k++)
                {

                    var j = indxs[k];

                    var xx1 = Math.Max(X1_cor[(int)i], X1_cor[(int)j]);
                    var yy1 = Math.Max(Y1_cor[(int)i], Y1_cor[(int)j]);
                    var xx2 = Math.Min(X2_cor[(int)i], X2_cor[(int)j]);
                    var yy2 = Math.Min(Y2_cor[(int)i], Y2_cor[(int)j]);

                    var w = Math.Max(0, xx2 - xx1 + 1);
                    var h = Math.Max(0, yy2 - yy1 + 1);

                    var overlap = (w * h) / Area[(int)j];


                    if (overlap > overlapTresh) supress.Add((int)k);
                    
                    
                }
                OldIndexes = indxs; 
                indxs = indxs.Except(supress).ToList(); 

            }
            
            foreach(var index in pick)
            {

                PickedBoxes.Add(Boxes[(int)index]); 

            }
            
            return PickedBoxes;
        }
    }
    
}

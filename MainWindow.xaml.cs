using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Kinect;
using Numpy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private List<ImageDataset> Datasets = new List<ImageDataset>();
        Object FrameObj = new Object();
        KNearest KNN = new KNearest();
        List<ImageDataset> TrainD = new List<ImageDataset>();
        List<ImageDataset> TestD = new List<ImageDataset>();
        private bool IsDataLoaded, IsTrained = false;
        private List<int> RawData = new List<int>();
        int frameCount = 0;
        List<Bgr> Colors = new List<Bgr>();
        private int help = 0;
        BackgroundWorker bckgroundworker1 = new BackgroundWorker();
        BackgroundWorker Rendering = new BackgroundWorker();
        private string[] images = null;
        bool TaskCompleted = false;
        private System.Drawing.Rectangle BoundingBox = new System.Drawing.Rectangle();
        private System.Collections.IList clsses = null;
        private Object OB = new Object();
        ConfMat confMat = new ConfMat();
        int counter = 0;
        Stopwatch abc = new Stopwatch();
        int distance = 0;

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
            DataContext = this; 
            
            //  - window object is used as the view model for default binding source of objects || WIKI : Data context is a concept that allows elements to inherit information from their parent elements about the data source that is used for binding, as well as other characteristics of the binding, such as the path. 
          
            bckgroundworker1.WorkerSupportsCancellation = true;
            bckgroundworker1.WorkerReportsProgress = true;
            bckgroundworker1.ProgressChanged += ProgressChanged;
            bckgroundworker1.DoWork += DoWork;

            Rendering.WorkerSupportsCancellation = true;
            Rendering.WorkerReportsProgress = true;
            Rendering.ProgressChanged += Rendering_ProgressChanged;
            Rendering.DoWork += Rendering_DoWork;
            // not required for this question, but is a helpful event to handle
            bckgroundworker1.RunWorkerCompleted += Bckgroundworker1_RunWorkerCompleted;
            Rendering.RunWorkerCompleted += Rendering_RunWorkerCompleted;
            
            images = Directory.GetFiles("C:/Users/CPT Danko/Desktop/images_1");

            RawData.Clear();
            Random rnd = new Random();
            for (int k = 0; k < 15; k++)
            {


                Bgr colorB = new Bgr((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
                Colors.Add(colorB);

            }
            InitializeComponent();
        }

        private void Rendering_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TaskCompleted = true;
        }

        private void Rendering_DoWork(object sender, DoWorkEventArgs e)
        {
            
            Image<Gray, byte> image = new Image<Gray, byte>(frameDescription.Width, frameDescription.Height);
            image.Bytes = depthpixels;
            List<Image<Gray, byte>> ImageList = new List<Image<Gray, byte>>();
            Image<Bgr, byte> BGR = new Image<Bgr, byte>(512, 424);




            (ImageList, BGR) = SegmentImage(15);




            List<System.Drawing.Rectangle> Boxes = Sliding_Window(ImageList, new System.Drawing.Size(128, 140));
            


            int[] intersectionCount = new int[Boxes.Count];

            for (int c = 0; c < Boxes.Count; c++)
            {
                for (int k = 0; k < Boxes.Count; k++)
                {
                    if (Boxes[c].IntersectsWith(Boxes[k]))
                    {
                        intersectionCount[c]++;
                        CvInvoke.Rectangle(BGR, Boxes[c], new MCvScalar(255, 255, 255));
                    }

                }


            }


            var maxindex = intersectionCount.Select((x, i) => new { Index = i, Value = x }).Where(x => x.Value == intersectionCount.Max()).Select(x => x.Index).ToList();



            List<float> X1 = new List<float>();
            List<float> X2 = new List<float>();
            List<float> Y1 = new List<float>();
            List<float> Y2 = new List<float>();


            foreach (var box in maxindex)
            {
                X1.Add(Boxes[box].X);
                X2.Add(Boxes[box].X + Boxes[box].Width);
                Y1.Add(Boxes[box].Y);
                Y2.Add(Boxes[box].Y + Boxes[box].Height);
               // CvInvoke.Rectangle(BGR, Boxes[box], new MCvScalar(255, 255, 255));
            }

            if (X1.Count != 0)
            {

                System.Drawing.Rectangle finalrectangle = new System.Drawing.Rectangle(new System.Drawing.Point((int)X1.Min(), (int)Y1.Min()), new System.Drawing.Size((int)(X2.Max() - X1.Max()), (int)(Y2.Max() - Y1.Max())));
                BoundingBox = finalrectangle;
            }

          

            LoadCapture.Dispatcher.Invoke(() =>
            {
                 BitmapSource FrameBitmap = BitmapSourceConvert.ToBitmapSourceBgr(BGR);
                 FrameBitmap.Freeze();
                 LoadCapture.Source = FrameBitmap;
            });

            depthbitmap.Dispatcher.Invoke(() =>
            {
                depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), image.Bytes, depthbitmap.PixelWidth, 0);
            });

           
            
          

           
            
                if (abc.IsRunning && distance < 20)
                {
                    abc.Stop();
                    var time = abc.ElapsedMilliseconds;
                    File.AppendAllText("C:/Users/CPT Danko/Desktop/SpeedValues.txt", time + " ms  " + distance + " meranie" + Environment.NewLine);
                    distance++;
                    
                }

           


        }

        private void Rendering_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void Bckgroundworker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Load_Status.Content = "Completed";
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {


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

                (NoZeroPxls, centers, contour) = ProceessImage(SelectImg);

                var LabeledArray = FrameObj.Calculate_Kmeans(centers, NoZeroPxls, SelectImg.Size);
                var Regions = FrameObj.CalculateRegions(LabeledArray);

                Image<Bgr, byte> colored = new Image<Bgr, byte>(512, 424);




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
                    Class_Select.Dispatcher.Invoke(() =>
                    {
                        Class_Select.Items.Add(label);
                    }
                    );
                    IndexList.Dispatcher.Invoke(() =>
                    {
                        IndexList.Items.Add(label);
                    });


                }





                bckgroundworker1.ReportProgress(image);






            }



            IsDataLoaded = true;
            frameCount = 0;
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            Progress.Value = e.ProgressPercentage;
            var perc = ((decimal)(e.ProgressPercentage)) / ((decimal)(images.Length));
            Percentual.Content = (int)(Math.Round(perc, 2) * 100) + "%";


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

            if (!Rendering.IsBusy && !abc.IsRunning && IsDataLoaded && IsTrained) abc = Stopwatch.StartNew();
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
            
            if (!Rendering.IsBusy && IsDataLoaded && IsTrained)
            {
                Rendering.RunWorkerAsync();

                clsses = Class_Select.SelectedItems;

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

            Image<Gray, byte> FinalImage = new Image<Gray, byte>(512, 424);
            FinalImage.Bytes = depthpixels;
            CvInvoke.Rectangle(FinalImage, BoundingBox, new MCvScalar(255, 255, 255));
            depthbitmap.WritePixels(new Int32Rect(0, 0, depthbitmap.PixelWidth, depthbitmap.PixelHeight), FinalImage.Bytes, depthbitmap.PixelWidth, 0);




        }
        private void Processing()
        {




        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // after the window is closed dispose the framereade and close the kinect sensor 
            confMat.Close();
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
            bckgroundworker1.CancelAsync();
            Rendering.Dispose();
            Rendering.CancelAsync();
            
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

            public static BitmapSource ToBitmapSource(Image<Gray, byte> image)
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
            public static BitmapSource ToBitmapSourceBgr(Image<Bgr, byte> image)
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
            Progress.Maximum = images.Length;
            if (!bckgroundworker1.IsBusy) bckgroundworker1.RunWorkerAsync();
            Load_Status.Content = "Loading";

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
            

            
            var ConfMatrix = OB.ComputeConfusionMatrix(ActualClasses.ToArray(), Predictions.ToArray(), Datasets.Count);
            var metrics = OB.CalculateMetrics(ConfMatrix, ActualClasses.ToArray(), Predictions.ToArray());
            string results = $"Test Samples {ActualClasses.Count} \n   Accuraccy = {metrics[0] * 100} % \n " +
                $"Precission = {metrics[1] * 100} % \n  Recall = {metrics[2] * 100} %";
            Precision.Content = results;

            DataTable dataTable = new DataTable();
            var collum = OB.CM_Total.GetLength(0);
            var row = OB.CM_Total.GetLength(1);
            dataTable.Columns.Add(new DataColumn(""));

            for (var c = 0; c < collum; c++)
            {
                dataTable.Columns.Add(new DataColumn("Class" + c.ToString()));


            }

            for (var r = 0; r < row; r++)
            {
                var newRow = dataTable.NewRow();
                newRow[0] = "Class" + r;
                for (var c = 0; c < collum; c++)
                {

                    newRow[c + 1] = OB.CM_Total[r, c];
                }
                dataTable.Rows.Add(newRow);

            }


            confMat.CMat.ItemsSource = dataTable.DefaultView;

            foreach (var col in confMat.CMat.Columns)
            {
                col.Width = 100;


            }

            confMat.CMat.RowHeight = confMat.CMat.Height / (ConfMatrix.GetLength(1) + 2);
            
        }
        private void Load_Data()
        {
            var images = Directory.GetFiles("C:/Users/CPT Danko/Desktop/images_1");


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

                (NoZeroPxls, centers, contour) = ProceessImage(SelectImg);

                var LabeledArray = FrameObj.Calculate_Kmeans(centers, NoZeroPxls, SelectImg.Size);
                var Regions = FrameObj.CalculateRegions(LabeledArray);

                Image<Bgr, byte> colored = new Image<Bgr, byte>(512, 424);


                if (label == 6)
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

                    CvInvoke.Polylines(colored, Array.ConvertAll(centers, System.Drawing.Point.Round), true, new MCvScalar(255, 255, 255), 1);
                    //BitmapSource FrameBitmap = BitmapSourceConvert.ToBitmapSource(colored);
                    //LoadCapture.Source = FrameBitmap;
                    //colored.Save("C:/Users/CPT Danko/Desktop/Fotky_bakalárka/06.png"); 
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




                File.AppendAllText("C:/Users/CPT Danko/Pictures/ObjectValues.txt", ImageTrainData + Environment.NewLine);



            }



            IsDataLoaded = true;
            frameCount = 0;


        }

        private (byte[] ProcessedPixels, System.Drawing.PointF[] centroids, System.Drawing.Point[]) ProceessImage(Image<Gray, byte> FilteredImage)
        {

            UMat FrameCannyImage = new UMat();
            CvInvoke.Canny(FilteredImage, FrameCannyImage, 100, 200);
            VectorOfVectorOfPoint FrameImageContours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(FrameCannyImage, FrameImageContours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            VectorOfPoint FrameAppContour = new VectorOfPoint(2);
            Image<Gray, byte> image = new Image<Gray, byte>(512, 424);

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


            return (NonZeroPixels, FrameInitCenters, FrameAppContour.ToArray());
        }

        private void Test_Accuracy_Click(object sender, RoutedEventArgs e)
        {

            var Scanned_files = Directory.GetFiles("C:/Users/CPT Danko/Desktop/ObjectDScan");
            foreach (var file in Scanned_files)
            {
                
                var image = new Image<Gray, byte>(file);
                var name = Path.GetFileName(file);
                int label = int.Parse(name.Substring(name.IndexOf("_") - 1, 1));
                var index = TestD.FindIndex(x => x.Class == label);

                System.Drawing.PointF[] centers;
                byte[] NoZeroPxls;
                List<Image<Gray, byte>> NewImage = new List<Image<Gray, byte>>();
                System.Drawing.Point[] contour;

                (NoZeroPxls, centers, contour) = ProceessImage(image);

                var LabeledArray = FrameObj.Calculate_Kmeans(centers, NoZeroPxls, image.Size);
                var Regions = FrameObj.CalculateRegions(LabeledArray);

                Image<Bgr, byte> colored = new Image<Bgr, byte>(512, 424);




                if (index > -1)
                {
                    TestD[index].Images.Add(image);
                    TestD[index].RegionsValues.Add(Regions.ToArray());
                    TestD[index].ImageContours.Add(contour);
                    TestD[index].NonZeroPixels.Add(NoZeroPxls.Length);




                }
                else
                {
                    ImageDataset img = new ImageDataset();
                    img.Images = new List<Image<Gray, byte>>();
                    img.RegionsValues = new List<float[]>();
                    img.ImageContours = new List<System.Drawing.Point[]>();
                    img.NonZeroPixels = new List<int>();
                    img.Images.Add(image);
                    img.Class = label;
                    img.RegionsValues.Add(Regions.ToArray());
                    img.ImageContours.Add(contour);
                    img.NonZeroPixels.Add(NoZeroPxls.Length);
                    TestD.Add(img);

                }

                
            }
            ;
            
            Train_KNN_Full();
            Test_KNN();
            
            
            if (confMat.IsActive == false) confMat.Show();
            counter++;
            DetectedClass.Content = counter;
            
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





        }

        private (List<Image<Gray, byte>>, Image<Bgr, byte>) SegmentImage(int RegionCount)
        {

            var RawDataArr = RawData.ToArray();
            int[] RawDataCount = new int[RegionCount];

            List<Image<Gray, byte>> ImageList = new List<Image<Gray, byte>>();
            Image<Bgr, byte> ColoredImage = new Image<Bgr, byte>(512, 424);
            for (int k = 0; k < RegionCount; k++)
            {
                ImageList.Add(new Image<Gray, byte>(512, 424));
            }

            for (int i = 0; i < RawDataArr.Length; i++)
            {

                var mapped = MapValue(0, 1500, 0, RegionCount, RawDataArr[i]);

                ColoredImage[(i / 512), (i - ((i / 512) * 512))] = Colors[(int)mapped];
                RawDataCount[(int)mapped]++;
                ImageList[(int)mapped][(i / 512), (i - ((i / 512) * 512))] = (int)mapped == 0 ? new Gray(0) : new Gray(255);


            }

            return (ImageList, ColoredImage);

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

                        if (ImagePatch.Bytes.Where(x => x != 0).Count() != 0)
                        {

                            List<float[]> newRegion = new List<float[]>();
                            byte[] Pixels;
                            System.Drawing.PointF[] centers;
                            System.Drawing.Point[] contour;
                            VectorOfVectorOfPoint dasd = new VectorOfVectorOfPoint();


                            //ImagePatch.Save("C:/Users/CPT Danko/Desktop/Test/" + help + ".png");
                            (Pixels, centers, contour) = ProceessImage(ImagePatch);


                            var Kmeans = FrameObj.Calculate_Kmeans(centers, Pixels, ImagePatch.Size);
                            var Regions = FrameObj.CalculateRegions(Kmeans);

                            newRegion.Add(Regions.ToArray());

                            Matrix<float> matrixk = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));

                            var prediction = KNN.Predict(matrixk);

                           

                           

                            if (clsses.Contains((int)prediction))
                            {

                                Outrectangles.Add(subrect);

                            }
                        }

                    }



                }
            }


            return Outrectangles;



        }
        private void Sliding_Window_Scan(List<Image<Gray, byte>> InputImage, System.Drawing.Size patchSize, string path)

        {

            
            Image<Gray, byte> ImagePatch = new Image<Gray, byte>(patchSize);
            Image<Gray, byte> OutputImage = new Image<Gray, byte>(512, 424);
            System.Drawing.Rectangle subrect = new System.Drawing.Rectangle(0, 0, patchSize.Width, patchSize.Height);
            var newHelp = 0;

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





                        var Count = ImagePatch.Bytes.Where(x => x != 0).Count();

                        if (Count > 100)
                        {

                            ImagePatch.Save(path + newHelp + ".png");
                            
                            BitmapSource bitmapSource = BitmapSourceConvert.ToBitmapSource(ImagePatch);
                            Actual_Picker.Items.Add(new System.Windows.Controls.Image() { Source = bitmapSource, Height = 50, Width = 50 });
                            newHelp++;
                            
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

            Image<Gray, byte> IMG = new Image<Gray, byte>("C:/Users/CPT Danko/Desktop/Test/" + TexBox.Text + ".png");
            (Pixels, centers, contour) = ProceessImage(IMG);
            var Kmeans = FrameObj.Calculate_Kmeans(centers, Pixels, IMG.Size);
            var Regions = FrameObj.CalculateRegions(Kmeans);

            newRegion.Add(Regions.ToArray());

            Matrix<float> matrixk = new Matrix<float>(Object.To2D<float>(newRegion.ToArray()));

            var prediction = KNN.Predict(matrixk);
            DetectedClass.Content = prediction;
        }

        private void Scan_Enviroment_Click(object sender, RoutedEventArgs e)
        {
            
            List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();
            Image<Bgr, byte> Colored = new Image<Bgr, byte>(512, 424);
            List<int> indexes = new List<int>();



            (images, Colored) = SegmentImage(15);


            for (int c = 0; c < 3; c++)

            {
                Sliding_Window_Scan(images, new System.Drawing.Size(128, 140), "C:/Users/CPT Danko/Desktop/images_1/NegativeOne07_");
            }
            help = 0;


        }

        public double MapValue(double a0, double a1, double b0, double b1, double a)
        {
            return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
        }

        private void Separate_data_Click(object sender, RoutedEventArgs e)
        {
            TestD.Clear();
            var Scanned_files = Directory.GetFiles("C:/Users/CPT Danko/Desktop/ObjectDScan");
            var pickerValues = Actual_Picker.SelectedItems;
            int helpC = 0;
            List<int> indexes = new List<int>();
            int anotherHelp = 0;
            object[] indx = new object[pickerValues.Count];
            pickerValues.CopyTo(indx, 0);

            for (int file = 0; file < Scanned_files.Length; file++)
            {
                bool contain = false;
                var filename = Path.GetFileName(Scanned_files[file]);
            
                
                Image<Gray, byte> image = new Image<Gray, byte>(512,424);
                string name = "";

                foreach (var value in pickerValues)
                {
                    if (filename.IndexOf("_") == -1)
                    {
                        contain = Int32.Parse(filename.Substring(0, filename.Length - 4)) == Actual_Picker.Items.IndexOf(value) ? true : false;
                        if (contain) break;
                    }
                    else
                    {
                        break;
                    }
                   
                }



                if (contain)
                {
                    File.Move(Scanned_files[file], "C:/Users/CPT Danko/Desktop/ObjectDScan/" + IndexList.SelectedItem.ToString() + "_" + helpC + ".png");
                    helpC++;
                }
                    
                   
               
               
                

                
            

            }

            Scanned_files = Directory.GetFiles("C:/Users/CPT Danko/Desktop/ObjectDScan");
           var newScanned =  Scanned_files.Where(x=>x.Contains("_") == false).OrderBy(x => new string(x.Where(char.IsLetter).ToArray())).ThenBy(x =>
            {
                int number;
                if (int.TryParse(new string(x.Where(char.IsDigit).ToArray()), out number))
                    return number;
                return -1;
            }).ToList();

            foreach (var fl in newScanned)
            {
                var filename = Path.GetFileName(fl);
                if (!File.Exists("C:/Users/CPT Danko/Desktop/ObjectDScan/" + anotherHelp + ".png"))
                {
                    File.Move(fl, "C:/Users/CPT Danko/Desktop/ObjectDScan/" + anotherHelp + ".png");
                    
                }
                anotherHelp++;
            }

            foreach (var index in indx) Actual_Picker.Items.Remove(index);
        }

        private void Scan_Data_Click(object sender, RoutedEventArgs e)
        {
           
            Image<Gray, byte> imageSC = new Image<Gray, byte>(512, 424);
            imageSC.Bytes = depthpixels;
            List<Image<Gray, byte>> imagesSC = new List<Image<Gray, byte>>();
            var files = Directory.GetFiles("C:/Users/CPT Danko/Desktop/ObjectDScan");
            foreach (var file in files) File.Delete(file);
            Image<Gray, byte> imageKK = new Image<Gray, byte>(512,424);
            Image<Bgr, byte> IMAGEBGR = new Image<Bgr, byte>(52,424);
            (imagesSC,IMAGEBGR) = SegmentImage(15);
            Sliding_Window_Scan(imagesSC, new System.Drawing.Size(128, 140), "C:/Users/CPT Danko/Desktop/ObjectDScan/");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Actual_Picker.SelectAll();
        }

        public List<System.Drawing.Rectangle> NonMaxSuppression(List<System.Drawing.Rectangle> Boxes, float overlapTresh)
        {

            if (Boxes.Count == 0)
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
                X2_cor.Add(box.X + box.Width);
                Y1_cor.Add(box.Y);
                Y2_cor.Add(box.Y + box.Height);
                Area.Add(((box.X + box.Width) - box.X + 1) * ((box.Y + box.Height) - box.Y + 1));

            }




            var nw = np.argsort(np.array(Y2_cor.ToArray()));
            var indxs = nw.GetData<Int64>().ToList();
            var sss = nw.dtype;
            while (indxs.Count > 0 && OldIndexes.Count != indxs.Count)
            {

                var last = indxs.Count - 1;
                var i = indxs[last];
                pick.Add(i);
                List<Int64> supress = new List<Int64>();

                for (int k = 0; k < last; k++)
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

            foreach (var index in pick)
            {

                PickedBoxes.Add(Boxes[(int)index]);

            }

            return PickedBoxes;
        }
    }

}

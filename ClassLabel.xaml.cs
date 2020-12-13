using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect; 

namespace Object_Detection
{
    /// <summary>
    /// Interaction logic for ClassLabel.xaml
    /// </summary>

    public partial class ClassLabel : Window
    {

        Object FrmObject = ((MainWindow)Application.Current.MainWindow).FrameObj;
        
        string path = "C:/Users/CPT Danko/Pictures/capture" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".png";
        int captures = 0;
        private bool IsButtonPressed = false; 


        public ClassLabel()
        {

            ((MainWindow)Application.Current.MainWindow).depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;
            InitializeComponent();

        }

        private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            if (captures < 3 && IsButtonPressed == true)
            {
                if (!File.Exists("C:/Users/CPT Danko/Pictures/ObjectValues.txt"))
                {
                    using (var file = File.Create("C:/Users/CPT Danko/Pictures/ObjectValues.txt"))
                    {

                    }

                }

                ((MainWindow)Application.Current.MainWindow).ObjectProperty = ClassBox.Text + "*" + path + "*" + FrmObject.Up_tolerance_R1_R2 + "-" + FrmObject.Dwn_tolerance_R1_R2 + "*"
                                                                                                                                                                                 + FrmObject.Up_tolerance_R3_R4 + "-" + FrmObject.Dwn_tolerance_R3_R4 + "*"
                                                                                                                                                                                 + FrmObject.Up_tolerance_R1_R4 + "-" + FrmObject.Dwn_tolerance_R1_R4 + "*"
                                                                                                                                                                                 + FrmObject.Up_tolerance_R2_R3 + "-" + FrmObject.Dwn_tolerance_R2_R3 + "*"
                                                                                                                                                                                 + FrmObject.PixelCount;
                File.AppendAllText("C:/Users/CPT Danko/Pictures/ObjectValues.txt", ((MainWindow)Application.Current.MainWindow).ObjectProperty + Environment.NewLine);
                captures++;
            }
            else if  (captures == 3) 
            {
                this.Close();

            }
        }


        

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var Gray8DepthBmp = new FormatConvertedBitmap(((MainWindow)Application.Current.MainWindow).depthbitmap, PixelFormats.Gray8, null, 0d);    // - create a new formated bitmap for saving the image to the file 
            var encoder = new BmpBitmapEncoder();                                                                                                     // - create an encoder for converting to a bmp file  

            encoder.Frames.Add(BitmapFrame.Create(Gray8DepthBmp));                                                                                    // - adds a frame with the speciefied format to the encoder 

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(fileStream);                                                                                                            // - save the file to the defined path from the encoder 
            }



            IsButtonPressed = true; 
        }


    }
}

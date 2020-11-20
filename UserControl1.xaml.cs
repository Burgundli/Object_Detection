using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Object_Detection
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {

            

        }

        public void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            var Gray8DepthBmp = new FormatConvertedBitmap(mainWindow.depthbitmap, PixelFormats.Gray8, null, 0d);    // - create a new formated bitmap for saving the image to the file 
            var encoder = new BmpBitmapEncoder();                                                                  // - create an encoder for converting to a bmp file  
            encoder.Frames.Add(BitmapFrame.Create(Gray8DepthBmp));                                                 // - adds a frame with the speciefied format to the encoder 

            using (var fileStream = new FileStream("C:/Users/CPT Danko/Pictures/capture" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".png", FileMode.Create))
            {
                encoder.Save(fileStream);                                                                           // - save the file to the defined path from the encoder 
            }
            if (!File.Exists("C:/Users/CPT Danko/Pictures/ObjectValues.txt"))
            {
                File.Create("C:/Users/CPT Danko/Pictures/ObjectValues.txt");
            }

            string ObjectPropery = InputText.Text + "   " + mainWindow.FrameObj.PixelCount + "   " + mainWindow.FrameObj.Up_tolerance_R1_R2 + mainWindow.FrameObj.Dwn_tolerance_R1_R2 + "     "
                                                                                                   + mainWindow.FrameObj.Up_tolerance_R3_R4 + mainWindow.FrameObj.Dwn_tolerance_R3_R4 + "     "
                                                                                                   + mainWindow.FrameObj.Up_tolerance_R1_R4 + mainWindow.FrameObj.Dwn_tolerance_R1_R4 + "     "
                                                                                                   + mainWindow.FrameObj.Up_tolerance_R2_R3 + mainWindow.FrameObj.Dwn_tolerance_R2_R3 + "     ";
            File.AppendAllText("C:/Users/CPT Danko/Pictures/ObjectValues.txt", ObjectPropery);

            var parent = this.Parent as Window;
            parent.Close(); 
        }
    }

}

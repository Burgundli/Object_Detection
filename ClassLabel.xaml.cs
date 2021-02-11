using Microsoft.Kinect;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Object_Detection

{
    /// <summary>
    /// Interaction logic for ClassLabel.xaml
    /// </summary>

    public partial class ClassLabel : Window
    {

        private string label = null;
        private string index = null;
        private bool NameInserted = false;
        private int ImageCount = 0;


        public ClassLabel()
        {
            InitializeComponent();
            MainWindow main = (MainWindow)(Application.Current.MainWindow);

            main.depthFrameReader.FrameArrived += DepthFrameReader_FrameArrived;
        }



        private void DepthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            if (NameInserted == true && ImageCount < 6)
            {
                string path = "C:/Users/CPT Danko/Desktop/images/" + label + index + "_" + ImageCount.ToString() + ".png";


                var Gray8DepthBmp = new FormatConvertedBitmap(((MainWindow)Application.Current.MainWindow).depthbitmap, PixelFormats.Gray8, null, 0d);    // - create a new formated bitmap for saving the image to the file 
                var encoder = new BmpBitmapEncoder();                                                                                                     // - create an encoder for converting to a bmp file  

                encoder.Frames.Add(BitmapFrame.Create(Gray8DepthBmp));                                                                                    // - adds a frame with the speciefied format to the encoder 


                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fileStream);                                                                                                            // - save the file to the defined path from the encoder 
                }
                ImageCount++;
            }
            else if (ImageCount > 5)
            {
                this.Close();
            }


        }

        private void Button_Click(object sender, RoutedEventArgs e)

        {

            label = ClassBox.Text;
            index = Index.Text;

            NameInserted = true;





        }
    }


}

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Generic;

namespace Object_Detection
{
    class ImageDataset
    {
        public List<System.Drawing.Point[]> ImageContours = new List<System.Drawing.Point[]>();
        public List<float[]> RegionsValues { get; set; }
        public int Class { get; set; }
        public List<Image<Gray, byte>> Images { get; set; }

        public List<int> NonZeroPixels { get; set; }

    }
}

using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;

namespace Object_Detection
{
    class ImageDataset
    {
        public List<int[]> RegionsValues { get; set; }
        public int Class { get; set; }
        public List<Image<Gray, byte>> Images { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Object_Detection
{
    class Region
    {
        private int PixelCount;
        private int Quadrant;
        private List<byte> Pixels = new List<byte>();

        public int RegionPixelCoun
        {
            get
            {
                return PixelCount;

            }
            set
            {

                PixelCount = value;

            }

        }
        public int RegionIndex
        {
            get
            {
                return RegionIndex;
            }

            set
            {
                Quadrant = value;
            }
        }
        public List<byte> RegionPixels
        {
            set
            {
                Pixels = value;
            }
            get
            {
                return Pixels;
            }

        }
    }
}

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
        private int Width;
        private int Height; 
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

        public int RectHeight
        {

            get
            {
                return Height; 

            }
            set
            {
                Height = value; 

            }

        }

        public int RectWidth
        {
            get
            {
                return Width; 
            }
            set
            {
                Width = value; 
            }
        }

       
    }
}

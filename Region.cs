using static System.Math;
namespace Object_Detection
{
    public class Object
    {
        private int R1 = 0;
        private int R2 = 0;
        private int R3 = 0;
        private int R4 = 0;
        private int PxlCnt = 0;

        public double ratio1 = 0;
        public double ratio2 = 0;
        public double ratio3 = 0;
        public double ratio4 = 0;
        public double Up_tolerance_R1_R2 = 0;
        public double Up_tolerance_R3_R4 = 0;
        public double Up_tolerance_R1_R4 = 0;
        public double Up_tolerance_R2_R3 = 0;
        public double Dwn_tolerance_R1_R2 = 0;
        public double Dwn_tolerance_R3_R4 = 0;
        public double Dwn_tolerance_R1_R4 = 0;
        public double Dwn_tolerance_R2_R3 = 0;
        private const int tolerance = 20; 
        public void Clear()
        {
            R1 = 0;
            R2 = 0;
            R3 = 0;
            R4 = 0;
            PxlCnt = 0;
            Up_tolerance_R1_R2 = 0;
            Up_tolerance_R3_R4 = 0;
            Up_tolerance_R1_R4 = 0;
            Up_tolerance_R2_R3 = 0;
            Dwn_tolerance_R1_R2 = 0;
            Dwn_tolerance_R3_R4 = 0;
            Dwn_tolerance_R1_R4 = 0;
            Dwn_tolerance_R2_R3 = 0;

        }
        public void CalculateTolerances()
        {
            if (R1 < tolerance)
            {
                R1 = tolerance+1;
            }
            else if (R2 < tolerance)
            {
                R2 = tolerance + 1;
            }
            else if (R3 < tolerance)
            {
                R3 = tolerance + 1;
            }
            else if (R4 < tolerance)
            {
                R4 = tolerance + 1;
            }

            ratio1 = (double)(R1) / (R2);
            ratio2 = (double)(R3) / (R4);
            ratio3 = (double)(R1) / (R4);
            ratio4 = (double)(R2) / (R3); 

            Up_tolerance_R1_R2 = Round((double)(R1 + tolerance) / (R2 - tolerance), 3);
            Up_tolerance_R3_R4 = Round((double)(R3 + tolerance) / (R4 - tolerance), 3);
            Up_tolerance_R1_R4 = Round((double)(R1 + tolerance) / (R4 - tolerance), 3);
            Up_tolerance_R2_R3 = Round((double)(R2 + tolerance) / (R3 - tolerance), 3);
            Dwn_tolerance_R1_R2 = Round((double)(R1 - tolerance) / (R2 + tolerance), 3);
            Dwn_tolerance_R3_R4 = Round((double)(R3 - tolerance) / (R4 + tolerance), 3);
            Dwn_tolerance_R1_R4 = Round((double)(R1 - tolerance) / (R4 + tolerance), 3);
            Dwn_tolerance_R2_R3 = Round((double)(R2 - tolerance) / (R3 + tolerance), 3);

        }
        public int Region1PixelCnt
        {
            get
            {
                return R1;

            }
            set
            {

                R1 = value;

            }

        }
        public int Region2PixelCnt
        {
            get
            {
                return R2;

            }
            set
            {

                R2 = value;

            }

        }
        public int Region3PixelCnt
        {
            get
            {
                return R3;

            }
            set
            {

                R3 = value;

            }

        }
        public int Region4PixelCnt
        {
            get
            {
                return R4;

            }
            set
            {

                R4 = value;

            }

        }
        public int PixelCount
        {
            get
            {
                return PxlCnt;

            }
            set
            {

                PxlCnt = value;

            }

        }
    }
}

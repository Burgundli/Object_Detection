﻿namespace Object_Detection
{
    public class Object
    {
        private int R1 = 0;
        private int R2 = 0;
        private int R3 = 0;
        private int R4 = 0;
        private int PxlCnt = 0;

        public int Up_tolerance_R1_R2 = 0;
        public int Up_tolerance_R3_R4 = 0;
        public int Up_tolerance_R1_R4 = 0;
        public int Up_tolerance_R2_R3 = 0;
        public int Dwn_tolerance_R1_R2 = 0;
        public int Dwn_tolerance_R3_R4 = 0;
        public int Dwn_tolerance_R1_R4 = 0;
        public int Dwn_tolerance_R2_R3 = 0;
        public void Clear()
        {
            R1 = 0;
            R2 = 0;
            R3 = 0;
            R4 = 0;
            PxlCnt = 0;

        }
        public void CalculateTolerances()
        {
            Up_tolerance_R1_R2 = (R1 + 20) / (R2 - 20);
            Up_tolerance_R3_R4 = (R3 + 20) / (R4 - 20);
            Up_tolerance_R1_R4 = (R1 + 20) / (R4 - 20);
            Up_tolerance_R2_R3 = (R2 + 20) / (R3 - 20);
            Dwn_tolerance_R1_R2 = (R1 - 20) / (R2 + 20);
            Dwn_tolerance_R3_R4 = (R3 - 20) / (R4 + 20);
            Dwn_tolerance_R1_R4 = (R1 - 20) / (R4 + 20);
            Dwn_tolerance_R2_R3 = (R2 - 20) / (R3 + 20);
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

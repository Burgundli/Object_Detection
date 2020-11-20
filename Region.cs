namespace Object_Detection
{
    class Object
    {
        private int R1 = 0;
        private int R2 = 0;
        private int R3 = 0;
        private int R4 = 0;
        private int PxlCnt = 0;


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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Media3D;
using static System.Math;

namespace Object_Detection
{
    public class Object
    {


        public Point3D[] Calculate_Kmeans(PointF[] centers, byte[] NonZeroPixel)

        {

            List<Point3D> LabeledPixels = new List<Point3D>();

            for (var pixel = 0; pixel < NonZeroPixel.Length; pixel++)

            {
                double[] distances = new double[centers.Length];

                if (NonZeroPixel[pixel] != 0) {

                    for (int k = 0; k < centers.Length; k++)
                    {

                        distances[k] = Sqrt(Pow(Abs((pixel - (pixel / 512) * 512) - centers[k].X), 2) + Pow(Abs((pixel / 512) - centers[k].Y), 2));

                    }

                    Point3D point3D = new Point3D();
                    point3D.X = pixel - ((pixel / 512) * 512);
                    point3D.Y = pixel / 512;
                    point3D.Z = Array.IndexOf(distances, distances.Max());


                    LabeledPixels.Add(point3D);
                }


            }



            return LabeledPixels.ToArray();



        }
        public List<int> CalculateRegions(Point3D[] labeledArray)
        {
            List<int> Regions = new List<int>();

            var maxIndex = labeledArray.Max(x => x.Z);

            for (int index = 0; index <= maxIndex; index++)
            {

                Regions.Add(labeledArray.Where(y => y.Z == index).Count());

            }


            return Regions;


        }


    }
}


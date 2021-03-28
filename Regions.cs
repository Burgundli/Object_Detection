using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Media3D;
using static System.Math;

namespace Object_Detection
{
    class Object
    {


        public Point3D[] Calculate_Kmeans(PointF[] centers, byte[] NonZeroPixel, Size imageSize)

        {

            List<Point3D> LabeledPixels = new List<Point3D>();

            for (var pixel = 0; pixel < NonZeroPixel.Length; pixel++)

            {
                double[] distances = new double[centers.Length];

                if (NonZeroPixel[pixel] != 0)
                {

                    for (int k = 0; k < centers.Length; k++)
                    {

                        distances[k] = Sqrt(Pow(Abs((pixel - (pixel / imageSize.Width) * imageSize.Width) - centers[k].X), 2) + Pow(Abs((pixel / imageSize.Width) - centers[k].Y), 2));

                    }

                    Point3D point3D = new Point3D();
                    point3D.X = pixel - ((pixel / imageSize.Width) * imageSize.Width);
                    point3D.Y = pixel / imageSize.Width;
                    point3D.Z = Array.IndexOf(distances, distances.Max());


                    LabeledPixels.Add(point3D);
                }


            }



            return LabeledPixels.ToArray();



        }
        public List<float> CalculateRegions(Point3D[] labeledArray)
        {
            List<float> Regions = new List<float>();

            var maxIndex = 4;

            for (int index = 0; index < maxIndex; index++)
            {

                Regions.Add(labeledArray.Where(y => y.Z == index).Count());

            }


            return Regions;


        }
        public static T[,] To2D<T>(T[][] source)
        {

            int FirstDim = source.Length;
            int SecondDim = source.GroupBy(row => row.Length).Single().Key;

            var result = new T[FirstDim, SecondDim];

            for (int i = 0; i < FirstDim; ++i)
            {

                for (int j = 0; j < SecondDim; ++j)
                {
                    result[i, j] = source[i][j];

                }
            }

            return result;


        }
        public (List<ImageDataset>, List<ImageDataset>) TestTrainSplit(List<ImageDataset> data, float split = 0.8f)
        {
            try
            {
                if (data.Count < 1)
                {
                    throw new Exception("Data is not found.");
                }

                int numTrainSamples = (int)Math.Floor(data[0].Images.Count * split);
                int numTestSamples = data[0].Images.Count - numTrainSamples;

                if (numTrainSamples == 0 || numTestSamples == 0)
                {
                    throw new Exception("Insufficient training or testing data.");
                }

                List<ImageDataset> TestData = (from d in data
                                               select new ImageDataset
                                               {
                                                   RegionsValues = d.RegionsValues.Take(numTestSamples).ToList(),
                                                   Class = d.Class
                                               }).ToList();

                List<ImageDataset> TrainData = (from d in data
                                                select new ImageDataset
                                                {
                                                    RegionsValues = d.RegionsValues.Skip(numTestSamples)
                                                    .Take(numTrainSamples).ToList(),
                                                    Class = d.Class
                                                }).ToList();
                return (TrainData, TestData);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


        }

        public (Matrix<float> Data, Matrix<int> Class) DataToMatrix(List<ImageDataset> datasets)
        {

            List<float[]> TrainD = new List<float[]>();
            List<int> LabelD = new List<int>();


            foreach (var dataset in datasets)
            {

                foreach (var image in dataset.RegionsValues)
                {

                    TrainD.Add(image);
                    LabelD.Add(dataset.Class);

                }


            }

            Matrix<float> ReturnD = new Matrix<float>(To2D<float>(TrainD.ToArray()));
            Matrix<int> ReturnC = new Matrix<int>(LabelD.ToArray());

            return (ReturnD, ReturnC);

        }
        public static int[,] ComputeConfusionMatrix(int[] actual, int[] predicted, int NoClasses)
        {
           
                if (actual.Length != predicted.Length)
                {
                    throw new Exception("Vectors lengths not matched");
                }

                
                int[,] CM = new int[NoClasses, NoClasses];
                for (int i = 0; i < actual.Length; i++)
                {
                    int r = predicted[i] - 1;
                    int c = actual[i] - 1;
                    CM[r, c]++;
                }
                return CM;
          
        }
        public static double[] CalculateMetrics(int[,] CM, int[] actual, int[] predicted)
        {
            try
            {
                double[] metrics = new double[3];
                int samples = actual.Length;
                int classes = (int)CM.GetLongLength(0);
                var diagonal = GetDiagonal(CM);
                var diagnolSum = diagonal.Sum();

                int[] ColTotal = GetSumCols(CM);
                int[] RowTotal = GetSumRows(CM);

                // Accuracy
                var accuracy = diagnolSum / (double)samples;

                // predicion
                var precision = new double[classes];
                for (int i = 0; i < classes; i++)
                {
                    precision[i] = diagonal[i] == 0 ? 0 : (double)diagonal[i] / ColTotal[i];
                }

                // Recall
                var recall = new double[classes];
                for (int i = 0; i < classes; i++)
                {
                    recall[i] = diagonal[i] == 0 ? 0 : (double)diagonal[i] / RowTotal[i];
                }

                metrics[0] = accuracy;
                metrics[1] = precision.Average();
                metrics[2] = recall.Average();

                return metrics;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static int[] GetDiagonal(int[,] matrix)
        {
            return Enumerable.Range(0, matrix.GetLength(0)).Select(i => matrix[i, i]).ToArray();
        }
        public static int[] GetSumCols(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int[] colSum = new int[cols];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    colSum[col] += matrix[row, col];
                }
            }
            return colSum;
        }

        public static int[] GetSumRows(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int[] rowSum = new int[cols];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    rowSum[row] += matrix[row, col];
                }
            }
            return rowSum;
        }
    }
}


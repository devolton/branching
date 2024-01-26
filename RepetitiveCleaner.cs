using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using AForge.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


namespace RepetitiveFileCleaner
{
    public class RepetitiveCleaner
    {
        private string[] _imageFileResolutions = { ".jpg", ".png", ".webp", ".jpeg", "bmp" };
        private readonly string _folderPath;
        private HashSet<string> _fileContentHashSet;
        private Stopwatch _stopwatch;
        public RepetitiveCleaner(string folderPath)
        {
            _folderPath = folderPath;
            _fileContentHashSet = new HashSet<string>();
            _stopwatch = new Stopwatch();
        }
        public void SoftOperation()
        {

            int deleteFileCount = 0;
            var filePathArray = Directory.GetFiles(_folderPath);
            foreach (var filePath in filePathArray)
            {
                var oneFileContent = File.ReadAllText(filePath);
                var isUniqueFile = _fileContentHashSet.Add(oneFileContent);
                if (!isUniqueFile)
                {
                    var fileInfo = new FileInfo(filePath);
                    _stopwatch.Start();
                    fileInfo.Delete();
                    _stopwatch.Stop();
                    deleteFileCount++;
                }
            }
            _fileContentHashSet.Clear();
            Console.WriteLine("Deleted file count: " + deleteFileCount);
            Console.WriteLine("Time for delete: " + _stopwatch.Elapsed.Nanoseconds + " nanoseconds!");
            _stopwatch.Restart();

        }

        public void SolidOperation()
        {
            List<FileInfo> fileInfoList = new List<FileInfo>();
            var filePathArray = Directory.GetFiles(_folderPath);
            for (int i = 0; i < filePathArray.Length; i++)
            {
                var firstFile = File.ReadAllText(filePathArray[i]);
                for (int j = i + 1; j < filePathArray.Length; j++)
                {
                    var secondFile = File.ReadAllText(filePathArray[j]);
                    double percent;

                    if (IsTwoImageFile(filePathArray[i], filePathArray[j]))
                    {
                        percent = ComparisionImage(filePathArray[i], filePathArray[j]);
                        if (percent >= 85)
                        {
                            var fileInfo = new FileInfo(filePathArray[j]);
                            fileInfoList.Add(fileInfo);
                        }
                    }
                    else if (IsOneImageFile(filePathArray[i], filePathArray[j]))
                        continue;
                    else
                    {
                        if (filePathArray[i].EndsWith(".txt") && filePathArray[j].EndsWith(".txt"))
                        {
                            firstFile=firstFile.Trim();
                            secondFile=secondFile.Trim();

                        }
                        var levenshtainIndex = LevenshtainDistance(firstFile, secondFile);
                        var longerFileLength = (firstFile.Length >= secondFile.Length) ? firstFile.Length : secondFile.Length;
                        percent = (double)levenshtainIndex / (double)longerFileLength * 100;
                        if (percent <= 15)
                        {
                            var fileInfo = new FileInfo(filePathArray[j]);
                            fileInfoList.Add(fileInfo);
                        }
                    }


                }
            }


            if (fileInfoList.Count > 0)
            {
                _stopwatch.Start();
                foreach (var oneFileInfo in fileInfoList)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    oneFileInfo.Delete();
                }
                _stopwatch.Stop();
            }

            Console.WriteLine("Deleted file count:" + fileInfoList.Count);
            Console.WriteLine("Time for delete: " + ((fileInfoList.Count > 0) ? _stopwatch.Elapsed.Nanoseconds : 0) + " nanoseconds!");
            _stopwatch.Restart();
            fileInfoList.Clear();

        }

        private double ComparisionImage(string firstFileContent, string secondFileContent)
        {
            var firstBitmap = new Bitmap(firstFileContent);
            var secondBitmap = new Bitmap(secondFileContent);
            if (firstBitmap.Width != secondBitmap.Width || firstBitmap.Height != secondBitmap.Height) return 0;
            var result = CompareImages(firstBitmap, secondBitmap);

            firstBitmap.Dispose();
            secondBitmap.Dispose();

            return result;

        }




        private double CompareImages(Bitmap image1, Bitmap image2)
        {
            var tm = new ExhaustiveTemplateMatching(0);
            TemplateMatch[] matchings = tm.ProcessImage(image1, image2);
            double similarity = 100.0f * 0.9f * matchings[0].Similarity;


            return similarity;

        }




        private int LevenshtainDistance(string firstFileContent, string secondFileContent)
        {
            var n = firstFileContent.Length + 1;
            var m = secondFileContent.Length + 1;

            var matrixD = new int[n, m];

            const int deletionCost = 1;
            const int insertionCost = 1;

            for (var i = 0; i < n; i++)
            {
                matrixD[i, 0] = i;
            }

            for (var j = 0; j < m; j++)
            {
                matrixD[0, j] = j;
            }

            for (var i = 1; i < n; i++)
            {
                for (var j = 1; j < m; j++)
                {
                    var substitutionCost = firstFileContent[i - 1] == secondFileContent[j - 1] ? 0 : 1;

                    matrixD[i, j] = GetMin(matrixD[i - 1, j] + deletionCost,
                                            matrixD[i, j - 1] + insertionCost,
                                            matrixD[i - 1, j - 1] + substitutionCost);
                }
            }

            return matrixD[n - 1, m - 1];
        }

        private int GetMin(int del, int insert, int substiot)
        {
            if (del > insert) del = insert;
            if (del > substiot) del = substiot;
            return del;
        }

        private bool IsTwoImageFile(string firstFilePath, string secondFilePath) =>
            _imageFileResolutions.Any(filePath => firstFilePath.EndsWith(filePath)) && _imageFileResolutions.Any(filePath => secondFilePath.EndsWith(filePath));

        private bool IsOneImageFile(string firstFilePath, string secondFilePath)
        {
            return !_imageFileResolutions.Any(filePath => firstFilePath.EndsWith(filePath)) && _imageFileResolutions.Any(filePath => secondFilePath.EndsWith(filePath)) ||
                _imageFileResolutions.Any(filePath => firstFilePath.EndsWith(filePath)) && !_imageFileResolutions.Any(filePath => secondFilePath.EndsWith(filePath));
        }




    }
}
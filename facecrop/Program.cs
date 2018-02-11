using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace facecrop
{
    static class Program
    {
        static StreamWriter _logStream;

        const string _optFile            = "facecrop.ini";

        const string _optSourceFolder    = "SOURCE_FOLDER";
        const string _optSourceFileMask  = "FILE_MASK";
        const string _optHaarMinSize     = "HAAR_MINSIZE";
        const string _optHaarSearchMode  = "SEARCH_MODE";
        const string _optHaarScalingFact = "SCALING_FACTOR";
        const string _optHaarScalingMode = "SCALING_MODE";
        const string _optHaarParallel    = "PARALLEL_PROCESSING";
        const string _optHaarSuppression = "SUPPRESSION";
        const string _optDestFolder      = "DESTINATION_FOLDER";
        const string _optDestPrefix      = "OUTPUT_PREFIX";
        const string _optDestFileType    = "OUTPUT_TYPE";
        const string _optDestSize        = "OUTPUT_SIZE";

        static string _varSourceFolder    = @"c:\tmp\faces";
        static string _varSourceFileMask  = "*.*";
        static int    _varHaarMinSize     = 30;
        static int    _varHaarSearchMode  = 3;
        static float  _varHaarScalingFact = 1.2F;
        static int    _varHaarScalingMode = 0;
        static bool   _varHaarParallel    = true;
        static int    _varHaarSuppression = 2;
        static string _varDestFolder      = @"c:\tmp\faces\cropped";
        static string _varDestPrefix      = "outp";
        static string _varDestFileType    = "png";
        static int    _varDestSize        = 255;

        static long numImages = 0;

        static void Main(string[] args)
        {
            _logStream = new StreamWriter("facecrop.log");

            LogMessage("FACECROP - Face extraction program by /u/NihilistByte");
            LogMessage("==================================================");

            if (ReadOptions())
            {
                Directory.CreateDirectory(_varDestFolder);

                foreach (var f in Directory.GetFiles(_varSourceFolder, _varSourceFileMask))
                {
                    LogMessage("Processing file " + f);
                    Process(f);
                }

                foreach (var f in Directory.GetFiles(_varDestFolder, "*." + _varDestFileType))
                {
                    if (!File.Exists(f)) continue;

                    using (var bmp = new Bitmap(f))
                    {
                        foreach (var fdest in Directory.GetFiles(_varDestFolder, "*." + _varDestFileType))
                        {
                            if (fdest == f) continue;
                            if (ImageCompare(bmp, fdest))
                            {
                                try
                                {
                                    File.Delete(fdest);
                                    LogMessage(string.Format("Duplicate found, deleting {0}", fdest));
                                }
                                catch (Exception ex)
                                {
                                    LogMessage(string.Format("Error occurred in removing duplicate: " + ex.Message));
                                }
                            }
                        }
                    }
                }
            }

            _logStream.Close();
            _logStream.Dispose();
        }

        private static void LogMessage(string message)
        {
            var m = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message);
            Console.WriteLine(m);
            _logStream.WriteLine(m);
        }

        private static void Process(string imagePath)
        {
            try
            {
                var cascade = new FaceHaarCascade();
                var detector = new HaarObjectDetector(cascade, _varHaarMinSize);
                
                detector.SearchMode = (ObjectDetectorSearchMode)_varHaarSearchMode;
                detector.ScalingFactor = _varHaarScalingFact;
                detector.ScalingMode = (ObjectDetectorScalingMode)_varHaarScalingMode;
                detector.UseParallelProcessing = _varHaarParallel;
                detector.Suppression = _varHaarSuppression;

                using (var img = Image.FromFile(imagePath))
                {
                    using (var bmp = new Bitmap(img)) { 

                        var sw = Stopwatch.StartNew();
                        var faceObjects = detector.ProcessFrame(bmp);
                        sw.Stop();

                        LogMessage(string.Format("Detected {0} faces", faceObjects.Length));

                        foreach (var f in faceObjects)
                        {
                            Bitmap bmpImage = new Bitmap(img);
                            bmpImage = bmp.Clone(new Rectangle(f.Location, f.Size), bmpImage.PixelFormat);

                            string outPath = Path.Combine(_varDestFolder, _varDestPrefix + (++numImages).ToString("00000") + "." + _varDestFileType);
                            LogMessage("Output file " + outPath);

                            ImageFormat imageFormat = ImageFormat.Png;
                            switch (_varDestFileType.ToLower())
                            {
                                case "png":
                                    imageFormat = ImageFormat.Png;
                                    break;

                                case "jpg":
                                case "jpeg":
                                    imageFormat = ImageFormat.Jpeg;
                                    break;

                                case "bmp":
                                    imageFormat = ImageFormat.Bmp;
                                    break;
                            }

                            using (var resized = ResizeImage(bmpImage, _varDestSize, _varDestSize))
                            {
                                resized.Save(outPath, imageFormat);
                            }

                            bmpImage.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Error processing file " + imagePath + "\n" + ex.Message);
            }
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static int ToInt32(this string text)
        {
            int i = 0;
            int.TryParse(text, out i);
            return i;
        }

        public static float ToFloat(this string text)
        {
            float i = 0F;
            float.TryParse(text, out i);
            return i;
        }

        public static bool ToBool(this string text)
        {
            bool i = false;
            bool.TryParse(text, out i);
            return i;
        }

        public static bool ReadOptions()
        {
            LogMessage("Reading option file");

            if (!File.Exists(_optFile))
            {
                var appName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                var m = "Option file " + _optFile + " not found, now created by program.\nPlease check " + _optFile + " parameters then restart " + appName;
                MessageBox.Show(m, appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logStream.WriteLine(m);
                WriteOptions();
                return false;
            }

            using(var sr = new StreamReader(_optFile))
            {
                while (!sr.EndOfStream)
                {
                    var pars = sr.ReadLine().Split('=');
                    if (pars[0] != "") pars[0] = pars[0].Trim();
                    if (pars[1] != "") pars[1] = pars[1].Trim();

                    switch (pars[0])
                    {
                        case _optSourceFolder:
                            _varSourceFolder = pars[1];
                            break;

                        case _optSourceFileMask:
                            _varSourceFileMask = pars[1];
                            break;

                        case _optHaarMinSize:
                            _varHaarMinSize = pars[1].ToInt32();
                            break;

                        case _optHaarSearchMode:
                            _varHaarSearchMode = pars[1].ToInt32();
                            break;

                        case _optHaarScalingFact:
                            _varHaarScalingFact = pars[1].ToFloat();
                            break;

                        case _optHaarScalingMode:
                            _varHaarScalingMode = pars[1].ToInt32();
                            break;

                        case _optHaarParallel:
                            _varHaarParallel = pars[1].ToBool();
                            break;

                        case _optHaarSuppression:
                            _varHaarSuppression = pars[1].ToInt32();
                            break;

                        case _optDestFolder:
                            _varDestFolder = pars[1];
                            break;

                        case _optDestPrefix:
                            _varDestPrefix = pars[1];
                            break;

                        case _optDestFileType:
                            _varDestFileType = pars[1];
                            break;

                        case _optDestSize:
                            _varDestSize = pars[1].ToInt32();
                            break;
                    }
                }
            }

            return true;
        }

        public static string ParamNameVal(string name, object val)
        {
            return name + " = " + val;
        }

        public static void WriteOptions()
        {
            using (var sw = new StreamWriter(_optFile))
            {
                sw.WriteLine(ParamNameVal(_optSourceFolder, _varSourceFolder));
                sw.WriteLine(ParamNameVal(_optSourceFileMask, _varSourceFileMask));
                sw.WriteLine(ParamNameVal(_optHaarMinSize, _varHaarMinSize));
                sw.WriteLine(ParamNameVal(_optHaarSearchMode, _varHaarSearchMode));
                sw.WriteLine(ParamNameVal(_optHaarScalingFact, _varHaarScalingFact));
                sw.WriteLine(ParamNameVal(_optHaarScalingMode, _varHaarScalingMode));
                sw.WriteLine(ParamNameVal(_optHaarParallel, _varHaarParallel));
                sw.WriteLine(ParamNameVal(_optHaarSuppression, _varHaarSuppression));
                sw.WriteLine(ParamNameVal(_optDestFolder, _varDestFolder));
                sw.WriteLine(ParamNameVal(_optDestPrefix, _varDestPrefix));
                sw.WriteLine(ParamNameVal(_optDestFileType, _varDestFileType));
                sw.WriteLine(ParamNameVal(_optDestSize, _varDestSize));
            }
        }

        public static bool ImageCompare(Bitmap bmp1, string pathComparison)
        {
            Bitmap bmp2 = new Bitmap(pathComparison);

            bool equals = true;
            bool flag = true;  

            if (bmp1.Size == bmp2.Size)
            {
                for (int x = 0; x < bmp1.Width; ++x)
                {
                    for (int y = 0; y < bmp1.Height; ++y)
                    {
                        if (bmp1.GetPixel(x, y) != bmp2.GetPixel(x, y))
                        {
                            equals = false;
                            flag = false;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        break;
                    }
                }
            }
            else
            {
                equals = false;
            }

            bmp2.Dispose();
            return equals;
        }
    }
}

using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;
using CommandLine;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace facecrop
{
    class Options
    {
        [Option(Required = true, HelpText = "The source path for images to be cropped")]
        public string SOURCE_FOLDER { get; set; }

        [Option(HelpText = "The file pattern to search for in SOURCE_FOLDER", Default = "*.*")]
        public string FILE_MASK { get; set; }

        [Option(HelpText = "The minimum size of faces to detect", Default = 30)]
        public int FACE_MINSIZE { get; set; }

        [Option(HelpText = "HAAR search mode", Default = 3)]
        public int HAAR_SEARCHMODE { get; set; }

        [Option(HelpText = "Scaling factor", Default = 1.2F)]
        public float SCALING_FACTOR { get; set; }

        [Option(HelpText = "Scaling mode", Default = 0)]
        public int SCALING_MODE { get; set; }

        [Option(HelpText = "Use parallelism in face detection", Default = true)]
        public bool USE_PARALLEL { get; set; }

        [Option(HelpText = "Number of similar faces to be suppressed", Default = 2)]
        public int SUPPRESSION { get; set; }

        [Option(Required = true, HelpText = "The destination folder in which to save cropped faces")]
        public string DEST_FOLDER { get; set; }

        [Option(HelpText = "Prefix to impose to output files", Default = "outp")]
        public string OUT_PREFIX { get; set; }

        [Option(HelpText = "Output format for cropped faces (BMP; PNG; JPG; JPEG)", Default = "png")]
        public string OUT_FORMAT { get; set; }

        [Option(HelpText = "Output file size", Default = 256)]
        public int OUT_SIZE { get; set; }

        [Option(HelpText = "Delete duplicate output files", Default = true)]
        public bool REMOVE_DUPLICATES { get; set; }
    }

    static class Program
    {
        static StreamWriter _logStream;

        static string _varSourceFolder    = "";
        static string _varSourceFileMask  = "";
        static int    _varHaarMinSize     = 0;
        static int    _varHaarSearchMode  = 0;
        static float  _varHaarScalingFact = 0;
        static int    _varHaarScalingMode = 0;
        static bool   _varHaarParallel    = true;
        static int    _varHaarSuppression = 2;
        static string _varDestFolder      = "";
        static string _varDestPrefix      = "";
        static string _varDestFileType    = "";
        static int    _varDestSize        = 0;
        static bool   _varDeleteDup       = true;

        static long numImages = 0;
        
        static void Main(string[] args)
        {
            _logStream = new StreamWriter("facecrop.log");

            Parser.Default.ParseArguments<Options>(args)
                          .WithParsed<Options>(opts => RunCropping(opts));
        }

        private static void RunCropping(Options opts)
        {
            _varSourceFolder    = opts.SOURCE_FOLDER;
            _varSourceFileMask  = opts.FILE_MASK;
            _varHaarMinSize     = opts.FACE_MINSIZE;
            _varHaarSearchMode  = opts.HAAR_SEARCHMODE;
            _varHaarScalingFact = opts.SCALING_FACTOR;
            _varHaarScalingMode = opts.SCALING_MODE;
            _varHaarParallel    = opts.USE_PARALLEL;
            _varHaarSuppression = opts.SUPPRESSION;
            _varDestFolder      = opts.DEST_FOLDER;
            _varDestPrefix      = opts.OUT_PREFIX;
            _varDestFileType    = opts.OUT_FORMAT;
            _varDestSize        = opts.OUT_SIZE;

            LogMessage("FACECROP - Face extraction program by /u/NihilistByte");
            LogMessage("==================================================");

            Directory.CreateDirectory(_varDestFolder);

            foreach (var f in Directory.GetFiles(_varSourceFolder, _varSourceFileMask))
            {
                LogMessage("Processing file " + f);
                Process(f);
            }

            if (_varDeleteDup)
            {
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

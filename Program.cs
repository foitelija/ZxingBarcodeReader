using BitMiracle.LibTiff.Classic;
using SkiaSharp;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.SkiaSharp;

namespace ZxingBarcodeReader
{
    internal class Program
    {
        internal static readonly string image = Directory.GetCurrentDirectory() + "\\1.jpg";

        static void Main(string[] args)
        {
            CheckBarcodes(DecodeWithZxingContrast(image));
            CheckBarcodes(DecodeWithZxingNoContrast(image));

            Console.ReadKey();
        }

        private static void CheckBarcodes(Result[] barcodes)
        {
            if(barcodes == null || !barcodes.Any())
            {
                Console.WriteLine("Barcode not found");
            }
            else
            {
                foreach (var barcode in barcodes)
                {
                    Console.WriteLine("The barcode was found: " + barcode.Text);
                }
            }

        }

        private static Result[] DecodeWithZxingNoContrast(string imagePath)
        {
            using var imageStr = File.OpenRead(imagePath);
            using var image = OpenImage(imageStr, Path.GetExtension(imagePath));

            var result = BarcodeZxingReader.barcodeReaderSettings.DecodeMultiple(image);

            // If haven't found the barcode, then enlarge the picture and make the contrast again
            if (result == null || !result.Any())
            {
                //Resize for better barcode reading
                using var resizeImage = ResizeImage(image, (int)(image.Width * 1.5f), (int)(image.Height * 1.5f));

                return BarcodeZxingReader.barcodeReaderSettings.DecodeMultiple(image);
            }

            return result;

        }

        private static Result[] DecodeWithZxingContrast(string imagePath)
        {
            using var imageStr = File.OpenRead(imagePath);
            using var image = ContrastImage(OpenImage(imageStr, Path.GetExtension(imagePath)));

            var result = BarcodeZxingReader.barcodeReaderSettings.DecodeMultiple(image);

            // If haven't found the barcode, then enlarge the picture and make the contrast again
            if (result == null || !result.Any())
            {
                //Resize for better barcode reading
                using var resizeImage = ContrastImage(ResizeImage(image, (int)(image.Width * 1.5f), (int)(image.Height * 1.5f)));

                return BarcodeZxingReader.barcodeReaderSettings.DecodeMultiple(image);
            }

            return result;
  
        }

        public static SKBitmap ResizeImage(SKBitmap image, int width, int height)
        {
            return image.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
        }

        public static SKBitmap ContrastImage(SKBitmap originalBitmap)
        {
            var info = new SKImageInfo(originalBitmap.Width, originalBitmap.Height);

            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, 0, 0);


            // Увеличение контраста изображения
            float contrast = 1.5f; // Уровень контраста
            float translate = -0.5f * contrast + 0.5f;

            var colorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                contrast, 0, 0, 0, translate,
                0, contrast, 0, 0, translate,
                0, 0, contrast, 0, translate,
                0, 0, 0, 1, 0
            });

            var paint = new SKPaint
            {
                ColorFilter = colorFilter
            };

            canvas.DrawBitmap(originalBitmap, 0, 0, paint);
            canvas.Flush();

            // Возвращаем новый SKBitmap скопировав в него изображение с поверхности
            return SKBitmap.FromImage(surface.Snapshot());
        }



        public static SKBitmap OpenImage(Stream imageStream, string extension)
        {
            if (extension.Contains("tif"))
            {
                return OpenTiff(imageStream);
            }
            else
            {
                return SKBitmap.Decode(imageStream);
            }
        }

        private static SKBitmap OpenTiff(Stream tiffStream)
        {
            // open a TIFF stored in the stream
            using var tifImg = Tiff.ClientOpen("in-memory", "r", tiffStream, new TiffStream());

            // read the dimensions
            var width = tifImg.GetField(TiffTag.IMAGEWIDTH).First().ToInt();
            var height = tifImg.GetField(TiffTag.IMAGELENGTH).First().ToInt();

            // create the bitmap
            SKBitmap bitmap = new();
            SKImageInfo info = new(width, height);

            // create the buffer that will hold the pixels
            var raster = new int[width * height];

            // get a pointer to the buffer, and give it to the bitmap
            // not a valid TIF image if true.
            var ptr = GCHandle.Alloc(raster, GCHandleType.Pinned);
            bitmap.InstallPixels(info, ptr.AddrOfPinnedObject(), info.RowBytes, null, (addr, ctx) => ptr.Free(), null);

            // read the image into the memory buffer
            if (!tifImg.ReadRGBAImageOriented(width, height, raster, Orientation.TOPLEFT))
                return null;

            // swap the red and blue because SkiaSharp may differ from the tiff
            if (SKImageInfo.PlatformColorType == SKColorType.Bgra8888)
                SKSwizzle.SwapRedBlue(ptr.AddrOfPinnedObject(), raster.Length);

            return bitmap;
        }
    }
}

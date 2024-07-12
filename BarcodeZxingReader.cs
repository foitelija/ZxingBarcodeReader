using ZXing;
using ZXing.Common;

namespace ZxingBarcodeReader
{
    internal static class BarcodeZxingReader
    {
        public static BarcodeReaderGeneric barcodeReaderSettings { get; private set; } = new()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryInverted = true,
                TryHarder = true,
                PureBarcode = false,
                ReturnCodabarStartEnd = true,
                PossibleFormats = new List<BarcodeFormat>
                {
                    BarcodeFormat.EAN_13,
                },
            }
        };
    }
}

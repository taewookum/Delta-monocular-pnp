using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Protocol_Test
{
    internal sealed class CameraResponseHeader
    {
        [JsonProperty("CamX_1", Required = Required.Always)]
        public int CamX1 { get; set; }

        [JsonProperty("CamY_1", Required = Required.Always)]
        public int CamY1 { get; set; }

        [JsonProperty("CamDepth_1", Required = Required.Always)]
        public double CamDepth1 { get; set; }

        [JsonProperty("CamX_2", Required = Required.Always)]
        public int CamX2 { get; set; }

        [JsonProperty("CamY_2", Required = Required.Always)]
        public int CamY2 { get; set; }

        [JsonProperty("CamDepth_2", Required = Required.Always)]
        public double CamDepth2 { get; set; }

        [JsonProperty("JetsonTime", Required = Required.Always)]
        public double JetsonTime { get; set; }

        [JsonProperty("ImageProtocol", Required = Required.Always)]
        public string ImageProtocol { get; set; }

        [JsonProperty("ImageEncoding", Required = Required.Always)]
        public string ImageEncoding { get; set; }

        [JsonProperty("YoloImageLength", Required = Required.Always)]
        public int YoloImageLength { get; set; }

        [JsonProperty("DepthImageLength", Required = Required.Always)]
        public int DepthImageLength { get; set; }

        [JsonProperty("ImageWidth", Required = Required.Always)]
        public int ImageWidth { get; set; }

        [JsonProperty("ImageHeight", Required = Required.Always)]
        public int ImageHeight { get; set; }

        [JsonProperty("ImageCropX1", Required = Required.Always)]
        public int ImageCropX1 { get; set; }

        [JsonProperty("ImageCropY1", Required = Required.Always)]
        public int ImageCropY1 { get; set; }

        [JsonProperty("ImageCropX2", Required = Required.Always)]
        public int ImageCropX2 { get; set; }

        [JsonProperty("ImageCropY2", Required = Required.Always)]
        public int ImageCropY2 { get; set; }
    }

    internal sealed class CameraResponsePacket
    {
        public CameraResponsePacket(
            CameraResponseHeader header,
            byte[] yoloImageBytes,
            byte[] depthImageBytes)
        {
            Header = header ?? throw new ArgumentNullException("header");
            YoloImageBytes = yoloImageBytes ?? throw new ArgumentNullException("yoloImageBytes");
            DepthImageBytes = depthImageBytes ?? throw new ArgumentNullException("depthImageBytes");
        }

        public CameraResponseHeader Header { get; private set; }
        public byte[] YoloImageBytes { get; private set; }
        public byte[] DepthImageBytes { get; private set; }
    }

    internal static class CameraResponseProtocol
    {
        public const int MaximumJsonHeaderBytes = 64 * 1024;
        public const int MaximumPngBytes = 10 * 1024 * 1024;
        public const int ExpectedImageWidth = 299;
        public const int ExpectedImageHeight = 203;
        public const int ExpectedCropX1 = 209;
        public const int ExpectedCropY1 = 137;
        public const int ExpectedCropX2 = 508;
        public const int ExpectedCropY2 = 340;

        private static readonly Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        public static async Task<CameraResponsePacket> ReadResponseAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            string jsonHeader = await ReadJsonHeaderLineAsync(
                stream,
                cancellationToken).ConfigureAwait(false);
            CameraResponseHeader header = DeserializeAndValidateHeader(jsonHeader);

            byte[] yoloImageBytes = await ReadExactlyAsync(
                stream,
                header.YoloImageLength,
                "YOLO PNG",
                cancellationToken).ConfigureAwait(false);
            byte[] depthImageBytes = await ReadExactlyAsync(
                stream,
                header.DepthImageLength,
                "Depth PNG",
                cancellationToken).ConfigureAwait(false);

            return new CameraResponsePacket(
                header,
                yoloImageBytes,
                depthImageBytes);
        }

        public static Bitmap DecodePng(
            byte[] pngBytes,
            int expectedWidth,
            int expectedHeight,
            string imageName)
        {
            if (pngBytes == null)
            {
                throw new ArgumentNullException("pngBytes");
            }

            if (pngBytes.Length == 0)
            {
                return null;
            }

            try
            {
                using (MemoryStream imageStream = new MemoryStream(
                    pngBytes,
                    false))
                using (Image decoded = Image.FromStream(
                    imageStream,
                    false,
                    true))
                {
                    if (decoded.RawFormat.Guid != ImageFormat.Png.Guid)
                    {
                        throw new InvalidDataException(
                            imageName + " payload is not encoded as PNG.");
                    }

                    if (decoded.Width != expectedWidth ||
                        decoded.Height != expectedHeight)
                    {
                        throw new InvalidDataException(string.Format(
                            "{0} decoded to {1}x{2}; expected {3}x{4}.",
                            imageName,
                            decoded.Width,
                            decoded.Height,
                            expectedWidth,
                            expectedHeight));
                    }

                    // Image.FromStream retains a dependency on its stream.
                    // Clone it so the MemoryStream can be closed immediately.
                    return new Bitmap(decoded);
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is ExternalException ||
                ex is OutOfMemoryException)
            {
                throw new InvalidDataException(
                    imageName + " payload could not be decoded as PNG.",
                    ex);
            }
        }

        private static async Task<string> ReadJsonHeaderLineAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            byte[] oneByte = new byte[1];
            using (MemoryStream headerBytes = new MemoryStream())
            {
                while (true)
                {
                    int read = await stream.ReadAsync(
                        oneByte,
                        0,
                        1,
                        cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new EndOfStreamException(string.Format(
                            "Camera connection closed before the JSON header LF " +
                            "({0} header bytes received).",
                            headerBytes.Length));
                    }

                    if (oneByte[0] == (byte)'\n')
                    {
                        break;
                    }

                    if (headerBytes.Length >= MaximumJsonHeaderBytes)
                    {
                        throw new InvalidDataException(string.Format(
                            "Camera JSON header exceeds the {0}-byte limit.",
                            MaximumJsonHeaderBytes));
                    }

                    headerBytes.WriteByte(oneByte[0]);
                }

                byte[] bytes = headerBytes.ToArray();
                if (bytes.Length > 0 && bytes[bytes.Length - 1] == (byte)'\r')
                {
                    Array.Resize(ref bytes, bytes.Length - 1);
                }

                if (bytes.Length == 0)
                {
                    throw new InvalidDataException(
                        "Camera returned an empty JSON header line.");
                }

                try
                {
                    return StrictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException ex)
                {
                    throw new InvalidDataException(
                        "Camera JSON header is not valid UTF-8.",
                        ex);
                }
            }
        }

        private static CameraResponseHeader DeserializeAndValidateHeader(
            string jsonHeader)
        {
            CameraResponseHeader header;
            try
            {
                header = JsonConvert.DeserializeObject<CameraResponseHeader>(
                    jsonHeader);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Camera returned an invalid JSON response header.",
                    ex);
            }

            if (header == null)
            {
                throw new InvalidDataException(
                    "Camera returned an empty JSON response header.");
            }

            if (!string.Equals(
                header.ImageProtocol,
                "json-line+png-v1",
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Unsupported camera ImageProtocol: " +
                    (header.ImageProtocol ?? "<null>"));
            }

            if (!string.Equals(
                header.ImageEncoding,
                "png",
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Unsupported camera ImageEncoding: " +
                    (header.ImageEncoding ?? "<null>"));
            }

            ValidateImageLength("YoloImageLength", header.YoloImageLength);
            ValidateImageLength("DepthImageLength", header.DepthImageLength);

            if (header.ImageWidth != ExpectedImageWidth ||
                header.ImageHeight != ExpectedImageHeight ||
                header.ImageCropX1 != ExpectedCropX1 ||
                header.ImageCropY1 != ExpectedCropY1 ||
                header.ImageCropX2 != ExpectedCropX2 ||
                header.ImageCropY2 != ExpectedCropY2)
            {
                throw new InvalidDataException(string.Format(
                    "Unexpected camera image geometry: image={0}x{1}, " +
                    "crop=({2},{3})-({4},{5}); expected image={6}x{7}, " +
                    "crop=({8},{9})-({10},{11}).",
                    header.ImageWidth,
                    header.ImageHeight,
                    header.ImageCropX1,
                    header.ImageCropY1,
                    header.ImageCropX2,
                    header.ImageCropY2,
                    ExpectedImageWidth,
                    ExpectedImageHeight,
                    ExpectedCropX1,
                    ExpectedCropY1,
                    ExpectedCropX2,
                    ExpectedCropY2));
            }

            return header;
        }

        private static void ValidateImageLength(string fieldName, int length)
        {
            if (length < 0 || length > MaximumPngBytes)
            {
                throw new InvalidDataException(string.Format(
                    "{0}={1} is outside the allowed range [0,{2}].",
                    fieldName,
                    length,
                    MaximumPngBytes));
            }
        }

        private static async Task<byte[]> ReadExactlyAsync(
            Stream stream,
            int length,
            string payloadName,
            CancellationToken cancellationToken)
        {
            if (length == 0)
            {
                return new byte[0];
            }

            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int read = await stream.ReadAsync(
                    buffer,
                    offset,
                    length - offset,
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException(string.Format(
                        "Camera connection closed during {0}: expected {1} " +
                        "bytes, received {2}.",
                        payloadName,
                        length,
                        offset));
                }

                offset += read;
            }

            return buffer;
        }
    }
}

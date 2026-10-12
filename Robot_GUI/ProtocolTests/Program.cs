using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Protocol_Test
{
    internal static class Program
    {
        private const int TestWidth = CameraResponseProtocol.ExpectedImageWidth;
        private const int TestHeight = CameraResponseProtocol.ExpectedImageHeight;
        private static int passed;

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                RunAllAsync().GetAwaiter().GetResult();
                Console.WriteLine("PASS all camera protocol tests: " + passed);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
        }

        private static async Task RunAllAsync()
        {
            byte[] yoloPng = CreatePng(Color.LimeGreen);
            byte[] depthPng = CreatePng(Color.DarkMagenta);

            await RunAsync(
                "fragmented JSON and PNG reads",
                () => TestFragmentedResponseAsync(yoloPng, depthPng));
            await RunAsync(
                "single large combined read",
                () => TestCombinedResponseAsync(yoloPng, depthPng));
            await RunAsync(
                "two consecutive responses",
                () => TestConsecutiveResponsesAsync(yoloPng, depthPng));
            await RunAsync(
                "zero-length image payloads and missing UI state",
                TestZeroLengthImagesAsync);
            await RunAsync(
                "mid-payload connection close",
                () => TestMidPayloadEndOfStreamAsync(yoloPng));
            await RunAsync(
                "invalid and oversized lengths/header",
                TestInvalidLengthsAndHeaderAsync);
            await RunAsync(
                "PNG decoding matches expected image geometry",
                () => TestPngDecodeAsync(yoloPng, depthPng));
            await RunAsync(
                "composite preview is saved with unique names",
                () => TestCompositeCaptureSaveAsync(yoloPng, depthPng));
            await RunAsync(
                "PictureBox replacement disposes GDI images",
                () => TestImageReplacementAndGdiResourcesAsync(
                    yoloPng,
                    depthPng));
        }

        private static async Task TestFragmentedResponseAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            byte[] response = BuildResponse(
                101,
                yoloPng,
                depthPng);
            using (Stream stream = new ChunkedReadStream(
                response,
                new[] { 1, 2, 7, 3, 13, 5 }))
            {
                CameraResponsePacket packet =
                    await CameraResponseProtocol.ReadResponseAsync(
                        stream,
                        CancellationToken.None);
                Assert(packet.Header.CamX1 == 101, "CamX_1 mismatch.");
                AssertBytes(yoloPng, packet.YoloImageBytes, "YOLO bytes");
                AssertBytes(depthPng, packet.DepthImageBytes, "Depth bytes");
            }
        }

        private static async Task TestCombinedResponseAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            byte[] response = BuildResponse(
                102,
                yoloPng,
                depthPng);
            using (MemoryStream stream = new MemoryStream(response, false))
            {
                CameraResponsePacket packet =
                    await CameraResponseProtocol.ReadResponseAsync(
                        stream,
                        CancellationToken.None);
                Assert(packet.Header.CamX1 == 102, "CamX_1 mismatch.");
                Assert(stream.Position == stream.Length, "Response was not fully consumed.");
            }
        }

        private static async Task TestConsecutiveResponsesAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            byte[] first = BuildResponse(201, yoloPng, depthPng);
            byte[] second = BuildResponse(202, depthPng, yoloPng);
            byte[] joined = Join(first, second);
            using (Stream stream = new ChunkedReadStream(
                joined,
                new[] { joined.Length }))
            {
                CameraResponsePacket firstPacket =
                    await CameraResponseProtocol.ReadResponseAsync(
                        stream,
                        CancellationToken.None);
                CameraResponsePacket secondPacket =
                    await CameraResponseProtocol.ReadResponseAsync(
                        stream,
                        CancellationToken.None);

                Assert(firstPacket.Header.CamX1 == 201, "First response mismatch.");
                Assert(secondPacket.Header.CamX1 == 202, "Second response mismatch.");
                AssertBytes(depthPng, secondPacket.YoloImageBytes, "Second YOLO bytes");
                AssertBytes(yoloPng, secondPacket.DepthImageBytes, "Second Depth bytes");
            }
        }

        private static async Task TestZeroLengthImagesAsync()
        {
            byte[] response = BuildResponse(
                301,
                new byte[0],
                new byte[0]);
            CameraResponsePacket packet;
            using (MemoryStream stream = new MemoryStream(response, false))
            {
                packet = await CameraResponseProtocol.ReadResponseAsync(
                    stream,
                    CancellationToken.None);
            }

            Assert(packet.YoloImageBytes.Length == 0, "YOLO zero length was not preserved.");
            Assert(packet.DepthImageBytes.Length == 0, "Depth zero length was not preserved.");
            Assert(
                CameraResponseProtocol.DecodePng(
                    packet.YoloImageBytes,
                    TestWidth,
                    TestHeight,
                    "YOLO") == null,
                "Zero-length PNG should decode to null.");

            using (GripImagePreviewControl preview = new GripImagePreviewControl())
            {
                preview.SetImages(null, null);
                Assert(preview.YoloImageMissing, "YOLO missing state was not shown.");
                Assert(preview.DepthImageMissing, "Depth missing state was not shown.");
            }
        }

        private static async Task TestMidPayloadEndOfStreamAsync(byte[] yoloPng)
        {
            byte[] header = BuildHeaderBytes(
                401,
                yoloPng.Length + 7,
                0,
                null);
            byte[] truncated = Join(header, yoloPng);
            await AssertThrowsAsync<EndOfStreamException>(async () =>
            {
                using (MemoryStream stream = new MemoryStream(truncated, false))
                {
                    await CameraResponseProtocol.ReadResponseAsync(
                        stream,
                        CancellationToken.None);
                }
            });
        }

        private static async Task TestInvalidLengthsAndHeaderAsync()
        {
            byte[] negativeLength = BuildHeaderBytes(
                501,
                -1,
                0,
                null);
            await AssertThrowsAsync<InvalidDataException>(() => ReadSingleAsync(negativeLength));

            byte[] oversizedLength = BuildHeaderBytes(
                502,
                CameraResponseProtocol.MaximumPngBytes + 1,
                0,
                null);
            await AssertThrowsAsync<InvalidDataException>(() => ReadSingleAsync(oversizedLength));

            byte[] oversizedHeader = new byte[
                CameraResponseProtocol.MaximumJsonHeaderBytes + 2];
            for (int i = 0; i < oversizedHeader.Length - 1; i++)
            {
                oversizedHeader[i] = (byte)'x';
            }
            oversizedHeader[oversizedHeader.Length - 1] = (byte)'\n';
            await AssertThrowsAsync<InvalidDataException>(() => ReadSingleAsync(oversizedHeader));

            byte[] wrongGeometry = BuildHeaderBytes(
                503,
                0,
                0,
                new Dictionary<string, object>
                {
                    { "ImageWidth", TestWidth + 1 }
                });
            await AssertThrowsAsync<InvalidDataException>(() => ReadSingleAsync(wrongGeometry));
        }

        private static Task TestPngDecodeAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            using (Bitmap yolo = CameraResponseProtocol.DecodePng(
                yoloPng,
                TestWidth,
                TestHeight,
                "YOLO"))
            using (Bitmap depth = CameraResponseProtocol.DecodePng(
                depthPng,
                TestWidth,
                TestHeight,
                "Depth"))
            {
                Assert(yolo.Width == TestWidth && yolo.Height == TestHeight, "YOLO size mismatch.");
                Assert(depth.Width == TestWidth && depth.Height == TestHeight, "Depth size mismatch.");
            }

            return Task.CompletedTask;
        }

        private static async Task TestCompositeCaptureSaveAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            GripImageCaptureStore store = new GripImageCaptureStore();
            string firstPath = null;
            string secondPath = null;
            using (Bitmap yolo = CameraResponseProtocol.DecodePng(
                yoloPng,
                TestWidth,
                TestHeight,
                "YOLO"))
            using (Bitmap depth = CameraResponseProtocol.DecodePng(
                depthPng,
                TestWidth,
                TestHeight,
                "Depth"))
            {
                DateTime capturedAt = DateTime.Now;
                firstPath = await store.SaveCompositeAsync(
                    yolo,
                    depth,
                    capturedAt);
                secondPath = await store.SaveCompositeAsync(
                    null,
                    null,
                    capturedAt);
            }

            try
            {
                Assert(File.Exists(firstPath), "First composite file was not created.");
                Assert(File.Exists(secondPath), "Second composite file was not created.");
                Assert(!string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase),
                    "Composite filenames were not unique.");

                using (Image saved = Image.FromFile(firstPath))
                {
                    Assert(
                        saved.Width == GripImageCaptureStore.CompositeWidth &&
                        saved.Height == GripImageCaptureStore.CompositeHeight,
                        "Saved composite dimensions are incorrect.");
                }

                using (Image savedMissing = Image.FromFile(secondPath))
                {
                    Assert(
                        savedMissing.Width == GripImageCaptureStore.CompositeWidth &&
                        savedMissing.Height == GripImageCaptureStore.CompositeHeight,
                        "Saved missing-image composite dimensions are incorrect.");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(firstPath) && File.Exists(firstPath))
                {
                    File.Delete(firstPath);
                }

                if (!string.IsNullOrEmpty(secondPath) && File.Exists(secondPath))
                {
                    File.Delete(secondPath);
                }
            }
        }

        private static Task TestImageReplacementAndGdiResourcesAsync(
            byte[] yoloPng,
            byte[] depthPng)
        {
            using (GripImagePreviewControl preview = new GripImagePreviewControl())
            {
                preview.CreateControl();
                Application.DoEvents();
                int before = NativeMethods.GetGuiResources(
                    Process.GetCurrentProcess().Handle,
                    0);

                Bitmap previousYolo = CameraResponseProtocol.DecodePng(
                    yoloPng,
                    TestWidth,
                    TestHeight,
                    "YOLO");
                Bitmap previousDepth = CameraResponseProtocol.DecodePng(
                    depthPng,
                    TestWidth,
                    TestHeight,
                    "Depth");
                preview.SetImages(previousYolo, previousDepth);

                for (int i = 0; i < 100; i++)
                {
                    Bitmap nextYolo = CameraResponseProtocol.DecodePng(
                        yoloPng,
                        TestWidth,
                        TestHeight,
                        "YOLO");
                    Bitmap nextDepth = CameraResponseProtocol.DecodePng(
                        depthPng,
                        TestWidth,
                        TestHeight,
                        "Depth");
                    preview.SetImages(nextYolo, nextDepth);
                    AssertBitmapDisposed(previousYolo, "previous YOLO image");
                    AssertBitmapDisposed(previousDepth, "previous Depth image");
                    previousYolo = nextYolo;
                    previousDepth = nextDepth;
                }

                preview.SetImages(null, null);
                AssertBitmapDisposed(previousYolo, "last YOLO image");
                AssertBitmapDisposed(previousDepth, "last Depth image");
                Application.DoEvents();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                int after = NativeMethods.GetGuiResources(
                    Process.GetCurrentProcess().Handle,
                    0);
                Assert(
                    after <= before + 4,
                    string.Format(
                        "GDI handles grew from {0} to {1}.",
                        before,
                        after));
            }

            return Task.CompletedTask;
        }

        private static async Task ReadSingleAsync(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            {
                await CameraResponseProtocol.ReadResponseAsync(
                    stream,
                    CancellationToken.None);
            }
        }

        private static async Task RunAsync(string name, Func<Task> test)
        {
            await test();
            passed++;
            Console.WriteLine("PASS " + name);
        }

        private static byte[] BuildResponse(
            int camX1,
            byte[] yoloPng,
            byte[] depthPng)
        {
            return Join(
                BuildHeaderBytes(
                    camX1,
                    yoloPng.Length,
                    depthPng.Length,
                    null),
                yoloPng,
                depthPng);
        }

        private static byte[] BuildHeaderBytes(
            int camX1,
            int yoloLength,
            int depthLength,
            IDictionary<string, object> overrides)
        {
            Dictionary<string, object> header = new Dictionary<string, object>
            {
                { "CamX_1", camX1 },
                { "CamY_1", 121 },
                { "CamDepth_1", 1.101 },
                { "CamX_2", 220 },
                { "CamY_2", 122 },
                { "CamDepth_2", 1.102 },
                { "JetsonTime", 0.25 },
                { "ImageProtocol", "json-line+png-v1" },
                { "ImageEncoding", "png" },
                { "YoloImageLength", yoloLength },
                { "DepthImageLength", depthLength },
                { "ImageWidth", TestWidth },
                { "ImageHeight", TestHeight },
                { "ImageCropX1", CameraResponseProtocol.ExpectedCropX1 },
                { "ImageCropY1", CameraResponseProtocol.ExpectedCropY1 },
                { "ImageCropX2", CameraResponseProtocol.ExpectedCropX2 },
                { "ImageCropY2", CameraResponseProtocol.ExpectedCropY2 }
            };

            if (overrides != null)
            {
                foreach (KeyValuePair<string, object> pair in overrides)
                {
                    header[pair.Key] = pair.Value;
                }
            }

            string json = JsonConvert.SerializeObject(header) + "\n";
            return Encoding.UTF8.GetBytes(json);
        }

        private static byte[] CreatePng(Color color)
        {
            using (Bitmap bitmap = new Bitmap(TestWidth, TestHeight))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (MemoryStream stream = new MemoryStream())
            {
                graphics.Clear(color);
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static byte[] Join(params byte[][] arrays)
        {
            int totalLength = 0;
            foreach (byte[] array in arrays)
            {
                totalLength = checked(totalLength + array.Length);
            }

            byte[] joined = new byte[totalLength];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, joined, offset, array.Length);
                offset += array.Length;
            }

            return joined;
        }

        private static async Task AssertThrowsAsync<TException>(Func<Task> action)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "Expected exception " + typeof(TException).Name + ".");
        }

        private static void AssertBytes(
            byte[] expected,
            byte[] actual,
            string name)
        {
            Assert(expected.Length == actual.Length, name + " length mismatch.");
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    throw new InvalidOperationException(
                        name + " mismatch at byte " + i + ".");
                }
            }
        }

        private static void AssertBitmapDisposed(Bitmap bitmap, string name)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = bitmap.GetHbitmap();
            }
            catch (ArgumentException)
            {
                return;
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(handle);
                }
            }

            throw new InvalidOperationException(name + " was not disposed.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class ChunkedReadStream : Stream
        {
            private readonly MemoryStream inner;
            private readonly int[] chunkSizes;
            private int chunkIndex;

            public ChunkedReadStream(byte[] bytes, int[] chunkSizes)
            {
                inner = new MemoryStream(bytes, false);
                this.chunkSizes = chunkSizes;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return inner.Length; } }
            public override long Position
            {
                get { return inner.Position; }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int chunkSize = chunkSizes[chunkIndex % chunkSizes.Length];
                chunkIndex++;
                return inner.Read(buffer, offset, Math.Min(count, chunkSize));
            }

            public override Task<int> ReadAsync(
                byte[] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Read(buffer, offset, count));
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern int GetGuiResources(IntPtr process, int flags);

            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr objectHandle);
        }
    }
}

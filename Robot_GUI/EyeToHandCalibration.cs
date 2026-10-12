using System;

namespace Protocol_Test
{
    /// <summary>
    /// Fixed-camera calibration for converting pixel/depth measurements to
    /// robot flange coordinates.
    /// </summary>
    internal sealed class EyeToHandCalibration
    {
        public const string LogitechCalibrationId = "logitech_eye_to_hand";

        // Each camera keeps an independent intrinsic calibration.
        // OpenCV distortion order: k1, k2, p1, p2 (k3 = 0).
        private readonly string calibrationId;
        private readonly double fx;
        private readonly double fy;
        private readonly double cx;
        private readonly double cy;
        private readonly double k1;
        private readonly double k2;
        private readonly double p1;
        private readonly double p2;

        // Motion XYZ denotes the flange origin; the suction TCP extends in +Z.
        public const double FlangeToTcpZMm = 410.0;

        // Camera-to-robot translation (mm):
        // Xrobot = Xcamera - 13.56162254339462
        // Yrobot = -Ycamera + 166.7345930193968
        // Zrobot = Zcamera + 30
        private const double CameraToRobotTranslationXmm = -13.56162254339462;
        private const double CameraToRobotTranslationYmm = 166.7345930193968;
        private const double CameraToRobotTranslationZmm = 30.0;

        // Reference point used only to check calibration consistency.
        private const double ReferencePixelX = 312.0;
        private const double ReferencePixelY = 227.0;
        private const double ReferenceDepthMm = 1135.294;
        private const double ReferenceFlangeXmm = -50.3;
        private const double ReferenceFlangeYmm = 189.84;
        private const double ReferenceFlangeZmm = 719.38;

        // Physical camera rotation after applying the axis convention.
        private readonly double[,] residualRotation;
        private readonly double translationX;
        private readonly double translationY;
        private readonly double translationZ;

        private EyeToHandCalibration(
            string calibrationId,
            double fx,
            double fy,
            double cx,
            double cy,
            double k1,
            double k2,
            double p1,
            double p2,
            double[,] residualRotation,
            double translationX,
            double translationY,
            double translationZ)
        {
            this.calibrationId = calibrationId;
            this.fx = fx;
            this.fy = fy;
            this.cx = cx;
            this.cy = cy;
            this.k1 = k1;
            this.k2 = k2;
            this.p1 = p1;
            this.p2 = p2;
            this.residualRotation = residualRotation;
            this.translationX = translationX;
            this.translationY = translationY;
            this.translationZ = translationZ;
        }

        public string CalibrationProfileId
        {
            get { return calibrationId; }
        }

        public static EyeToHandCalibration CreateLogitechSetup()
        {
            return CreateSetup(
                LogitechCalibrationId,
                501.522,
                501.228,
                329.400,
                240.722,
                0.179733,
                -0.317512,
                -0.000516,
                -0.000929);
        }

        // Compatibility alias for the active Logitech setup.
        public static EyeToHandCalibration CreateCurrentSetup()
        {
            return CreateLogitechSetup();
        }

        private static EyeToHandCalibration CreateSetup(
            string calibrationId,
            double fx,
            double fy,
            double cx,
            double cy,
            double k1,
            double k2,
            double p1,
            double p2)
        {
            // Camera and robot axes are aligned after the image-Y conversion.
            double[,] rotation =
            {
                { 1.0, 0.0, 0.0 },
                { 0.0, 1.0, 0.0 },
                { 0.0, 0.0, 1.0 }
            };

            return new EyeToHandCalibration(
                calibrationId,
                fx,
                fy,
                cx,
                cy,
                k1,
                k2,
                p1,
                p2,
                rotation,
                CameraToRobotTranslationXmm,
                CameraToRobotTranslationYmm,
                CameraToRobotTranslationZmm);
        }

        /// <summary>
        /// Converts a distorted RGB pixel and optical-axis depth in millimetres
        /// to the flange position that places the suction TCP at that 3D point.
        /// Grasp over-travel and robot workspace clamps remain outside this class.
        /// </summary>
        public float[] PixelDepthToFlange(float pixelX, float pixelY, float depthMm)
        {
            ValidateInput(pixelX, pixelY, depthMm);

            double[] cameraPoint = UndistortAndDeproject(pixelX, pixelY, depthMm);
            double[] conventionPoint = ApplyRobotAxisConvention(cameraPoint);
            double[] rotatedPoint = Multiply(residualRotation, conventionPoint);

            double baseTcpX = rotatedPoint[0] + translationX;
            double baseTcpY = rotatedPoint[1] + translationY;
            double baseTcpZ = rotatedPoint[2] + translationZ;

            // The robot protocol receives flange XYZ; tool X/Y offsets are zero.
            double flangeX = baseTcpX;
            double flangeY = baseTcpY;
            double flangeZ = baseTcpZ - FlangeToTcpZMm;

            return new[]
            {
                checked((float)flangeX),
                checked((float)flangeY),
                checked((float)flangeZ)
            };
        }

        public double GetReferenceErrorMm()
        {
            float[] transformed = PixelDepthToFlange(
                (float)ReferencePixelX,
                (float)ReferencePixelY,
                (float)ReferenceDepthMm);

            double dx = transformed[0] - ReferenceFlangeXmm;
            double dy = transformed[1] - ReferenceFlangeYmm;
            double dz = transformed[2] - ReferenceFlangeZmm;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static void ValidateInput(float pixelX, float pixelY, float depthMm)
        {
            if (float.IsNaN(pixelX) || float.IsInfinity(pixelX) ||
                float.IsNaN(pixelY) || float.IsInfinity(pixelY))
            {
                throw new ArgumentOutOfRangeException(
                    "pixel",
                    "Camera pixel coordinates must be finite.");
            }

            if (float.IsNaN(depthMm) || float.IsInfinity(depthMm) || depthMm <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    "depthMm",
                    "Camera optical-axis depth must be a positive finite value in millimetres.");
            }
        }

        private double[] UndistortAndDeproject(
            double pixelX,
            double pixelY,
            double depthMm)
        {
            // Distorted normalized image coordinates.
            double distortedX = (pixelX - cx) / fx;
            double distortedY = (pixelY - cy) / fy;

            // Iteratively invert the OpenCV Brown-Conrady distortion model.
            double undistortedX = distortedX;
            double undistortedY = distortedY;

            for (int i = 0; i < 10; i++)
            {
                double x2 = undistortedX * undistortedX;
                double y2 = undistortedY * undistortedY;
                double xy = undistortedX * undistortedY;
                double r2 = x2 + y2;
                double r4 = r2 * r2;
                double radial = 1.0 + k1 * r2 + k2 * r4;

                if (Math.Abs(radial) < 1e-12)
                {
                    throw new InvalidOperationException(
                        "Camera distortion inversion became numerically unstable.");
                }

                double tangentialX = 2.0 * p1 * xy + p2 * (r2 + 2.0 * x2);
                double tangentialY = p1 * (r2 + 2.0 * y2) + 2.0 * p2 * xy;

                undistortedX = (distortedX - tangentialX) / radial;
                undistortedY = (distortedY - tangentialY) / radial;
            }

            // depthMm is assumed to be optical-axis Z, not Euclidean ray range.
            return new[]
            {
                undistortedX * depthMm,
                undistortedY * depthMm,
                depthMm
            };
        }

        private static double[] ApplyRobotAxisConvention(double[] cameraPoint)
        {
            // OpenCV camera: +X right, +Y image-down, +Z optical-forward.
            // Robot: +X right, +Y opposite image Y, +Z camera-looking direction.
            // Keep this reflection separate from the physical rotation matrix.
            return new[]
            {
                cameraPoint[0],
                -cameraPoint[1],
                cameraPoint[2]
            };
        }

        private static double[] Multiply(double[,] matrix, double[] vector)
        {
            return new[]
            {
                matrix[0, 0] * vector[0] +
                matrix[0, 1] * vector[1] +
                matrix[0, 2] * vector[2],

                matrix[1, 0] * vector[0] +
                matrix[1, 1] * vector[1] +
                matrix[1, 2] * vector[2],

                matrix[2, 0] * vector[0] +
                matrix[2, 1] * vector[1] +
                matrix[2, 2] * vector[2]
            };
        }
    }
}

using System;

namespace Protocol_Test
{
    /// <summary>
    /// Corrects measured XY workspace scale after the rigid Eye-to-Hand transform.
    /// </summary>
    internal sealed class WorkspaceResidualCorrection
    {
        public const string CalibrationId = "xy_workspace_scale";

        // Disable to use the rigid Eye-to-Hand result directly.
        public static readonly bool Enabled = true;

        // Workspace residual scale; not part of the camera extrinsic.
        private const double ScaleX = 0.96278008;
        private const double ScaleY = 0.95785761;

        // Robot-base XY pivot in millimetres.
        private const double PivotXmm = 0.25856039613401116;
        private const double PivotYmm = 174.9944736432339;

        public float[] ApplyToFlange(float[] handEyeFlangePoint)
        {
            if (handEyeFlangePoint == null || handEyeFlangePoint.Length < 3)
            {
                throw new ArgumentException(
                    "A Hand-Eye flange point with X, Y and Z is required.",
                    "handEyeFlangePoint");
            }

            double x = handEyeFlangePoint[0];
            double y = handEyeFlangePoint[1];
            float z = handEyeFlangePoint[2];

            if (double.IsNaN(x) || double.IsInfinity(x) ||
                double.IsNaN(y) || double.IsInfinity(y) ||
                float.IsNaN(z) || float.IsInfinity(z))
            {
                throw new ArgumentOutOfRangeException(
                    "handEyeFlangePoint",
                    "The Hand-Eye flange point must contain finite values.");
            }

            if (!Enabled)
            {
                return new[]
                {
                    handEyeFlangePoint[0],
                    handEyeFlangePoint[1],
                    handEyeFlangePoint[2]
                };
            }

            double correctedX = PivotXmm + ScaleX * (x - PivotXmm);
            double correctedY = PivotYmm + ScaleY * (y - PivotYmm);

            // The residual correction affects X and Y only.
            return new[]
            {
                checked((float)correctedX),
                checked((float)correctedY),
                z
            };
        }
    }
}

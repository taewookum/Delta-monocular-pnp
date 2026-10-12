using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Collections.Specialized;
using System.CodeDom.Compiler;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Globalization;
using System.Timers;

namespace Protocol_Test
{
    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private SerialPort serialPort2;
        private readonly object jetsonTtlSync = new object();
        private const int JetsonTtlFrameLength = 11;
        private const int JetsonTtlResponseTimeoutMs = 1000;
        private const string CameraServerAddress = "192.168.55.1";
        private const int CameraServerPort = 12345;
        private const int CameraResponseTimeoutMs = 5000;
        private const string ActiveRuntimeProfileId = "runtime_safety";
        private const string ActiveCoordinateProfileId = "adabins_logitech";
        // Minimum interval between motion-program upload frames.
        private const int RobotMotionUploadGapMs = 10;
        // Speed limits for pickup and loaded motion.
        private const int PickupApproachSpeedMax = 20;
        private const int LoadedLiftSpeedMax = 70;
        private const int LoadedTravelSpeedMax = 70;
        private const int RobotReadyRequiredFrames = 3;
        private const int RobotStationaryFallbackRequiredFrames = 5;
        private const int RobotStatusFreshnessMs = 250;
        private const int RobotStatusInitialProbeMs = 500;
        private const int RobotStatusRecoveryTimeoutMs = 1000;
        private const int RobotOriginReadyTimeoutMs = 15000;
        private const int RobotCycleReturnTimeoutMs = 60000;
        private const int RobotMotionNoProgressTimeoutMs = 5000;
        private const int RobotCommandDrainTimeoutMs = 1000;
        private const int RobotSerialDrainPollMs = 1;
        private const float RobotOriginToleranceMm = 5f;
        private const float RobotStationaryFrameToleranceMm = 0.05f;
        private const float RobotMotionProgressToleranceMm = 0.2f;
        private const int MaximumRobotFrameLength = 512;
        // Protocol Map (4-axis status): bits 0,1,2,6 and 8..15 are servo faults.
        // HomeSet/Busy/ServoOn/PowerLock (bits 3,4,5,7) are state bits.
        private const ushort RobotServoFaultMask = 0xFF47;
        // Downward grasp margin shared by automatic operation and Depth Test.
        private const float DefaultGripMarginMm = 10f;
        private const float SameTargetThresholdMm = 20f;
        private const float AdaBinsFrameWidthPixels = 640f;
        private const float AdaBinsFrameHeightPixels = 480f;
        private const float AdaBinsMinimumDepthMm = 925f;
        private const float AdaBinsMaximumDepthMm = 1230f;
        private TcpClient tcpClient;
        private NetworkStream cameraStream;
        private CancellationTokenSource cameraConnectionCancellationSource;
        private volatile bool cameraDisconnectRequested = true;
        private volatile bool formClosing;
        private GripImagePreviewForm gripImagePreviewForm;
        private readonly GripImageCaptureStore gripImageCaptureStore =
            new GripImageCaptureStore();
        private readonly SemaphoreSlim cameraRequestLock = new SemaphoreSlim(1, 1);
        private bool mainCycleRunning;
        private TcpClient tcpClient_motor;
        private readonly object robotCommandSync = new object();
        private readonly object robotStatusSync = new object();
        private readonly List<byte> robotReceiveBuffer = new List<byte>();
        private readonly Dictionary<byte, long> robotFrameCommandCounts =
            new Dictionary<byte, long>();
        private int robotIdleReadyFrameCount;
        private int robotOriginReadyFrameCount;
        private int robotStationaryIdleFrameCount;
        private int robotStationaryOriginFrameCount;
        private bool hasPreviousRobotStatusPosition;
        private float previousRobotStatusX;
        private float previousRobotStatusY;
        private float previousRobotStatusZ;
        private int robotStatusFrameSequence;
        private long lastRobotStatusTimestamp;
        private bool robotPortClosedStateShown;
        private long validRobotStatusFrameCount;
        private long validRobotNonStatusFrameCount;
        private string lastRobotNonStatusFrameHex = "<none>";
        private string lastRobotStatusFrameHex = "<none>";
        private ushort lastRobotIoInput;
        private ushort lastRobotIoOutput;
        private ushort lastRobotServo1Status;
        private ushort lastRobotServo2Status;
        private ushort lastRobotServo3Status;
        private ushort lastRobotServo4Status;
        private byte lastRobotErrorType;
        private ushort latchedRobotIoInputStopBits;
        private ushort latchedRobotIoOutputErrorBits;
        private ushort latchedRobotServo1Faults;
        private ushort latchedRobotServo2Faults;
        private ushort latchedRobotServo3Faults;
        private ushort latchedRobotServo4Faults;
        private byte latchedRobotErrorType;
        private long robotCommandWriteSequence;
        private string lastRobotCommandName = "<none>";
        private long lastRobotCommandTimestamp;
        private int maximumObservedRobotTxBytes;
        private readonly EyeToHandCalibration logitechEyeToHandCalibration =
            EyeToHandCalibration.CreateLogitechSetup();
        private readonly WorkspaceResidualCorrection workspaceResidualCorrection =
            new WorkspaceResidualCorrection();


        private string selectedPortName;
        private string selectedPortName2;
        private int delaytoupdate = 20;
        private int motion_num;
        private int Yellow_floor;

        private float posX;
        private float posY;
        private float posZ;
        private float posR;

        private bool motionEndBit = true;
        private bool BusyBit = false;
        private bool gripSent = false;
        private bool isoccupied = false;
        private bool isTray = false;
        private bool Working = true;


        private double dist;

        private float CamX;
        private float CamY;
        private float CamDepth;

        private float CamX_1;
        private float CamY_1;
        private float CamDepth_1;

        private float CamX_2;
        private float CamY_2;
        private float CamDepth_2;

        private float JetsonTime;


        private float x_ori = 0f;
        private float y_ori = -140f;
        private float z_ori = 400f;
        private float x_pass = 0f;
        private float y_pass = -20f;
        private float z_pass = 400f;
        private float z_ref = 430f;

        private byte[] Protocol_Stop = { 0x96, 0x0, 0x65, 0x2, 0x7F, 0x0, 0xE6 }; // Robot stop
        private byte[] protocol_GP = { 0x96, 0x0, 0x62, 0x2, 0x1, 0x0, 0x65 }; // Gripping motion : lines 1-9 (maximum)
        private byte[] protocol_RealtimePositionONOFF = { 0x96, 0x0, 0x84, 0x1, 0x1, 0x86 };
        private byte[] protocol_RobotON  = { 0x96, 0x0, 0x5A, 0x6, 0x1E, 0x0, 0x0, 0x0, 0x3, 0x1, 0x82 };
        private byte[] protocol_RobotOFF = { 0x96, 0x0, 0x5A, 0x6, 0x1E, 0x0, 0x0, 0x0, 0x0, 0x0, 0x7E };
        private byte[] protocol_AlramReset = { 0x96, 0x0, 0x83, 0x1, 0x0, 0x84 };
        private byte[] protocol_ZeroSet = { 0x96, 0x0, 0x77, 0x1, 0x2, 0x7A };
        private byte[] protocol_EmergencyStop = { 0x96, 0x0, 0x65, 0x2, 0x7F, 0x0, 0xE6 };
        private byte[] protocol_Motion1 = { 0x96, 0x0, 0x62, 0x2, 0x1, 0x0, 0x65 };
        private byte[] protocol_Motion2 = { 0x96, 0x0, 0x62, 0x2, 0xA, 0x0, 0x6E };
        private byte[] protocol_Motion3 = { 0x96, 0x0, 0x62, 0x2, 0x14, 0x0, 0x78 };
        private byte[] protocol_StopMotion = { 0x96, 0x0, 0x65, 0x2, 0x1, 0x0, 0x68 };
        private byte[] protocol_MoveEnable = { 0x96, 0x0, 0x5C, 0x4, 0xE, 0x0, 0x0, 0x0, 0x6E };
        private byte[] protocol_MoveDisable = { 0x96, 0x0, 0x5B, 0x4, 0xE, 0x0, 0x0, 0x0, 0x6D };
        private byte[] protocol_Origin = { 0x96, 0x0, 0x62, 0x2, 0xFF, 0x00, 0x63 }; // Line 1 (GRP)
        private byte[] protocol_Gripping =   { 0x96, 0x0, 0x5E, 0x22, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xFF, 0x7F, 0xFF, 0x7F, 0x0, 0x0, 0xFF, 0x7F, 0x1, 0x0, 0x0, 0x0, 0x1, 0x0, 0x0, 0x0, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0x7F, 0x1, 0x0, 0xF7};
        private byte[] protocol_Ungripping = { 0x96, 0x0, 0x5E, 0x22, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xFF, 0x7F, 0xFF, 0x7F, 0x0, 0x0, 0xFF, 0x7F, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0x7F, 0x1, 0x0, 0xF5};


        private byte[] protocol_StopperON = { 0x96, 0x0, 0x00, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x7F, 0x7F }; // Stopper ON
        private byte[] protocol_StopperOFF = { 0x96, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x8C, 0x8C }; // Stopper OFF
        private byte[] protocol_PhtoCheck = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6F, 0x6F };
        private byte[] protocol_PhtoON = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5F, 0x5F }; // Phtosensor ON
        private byte[] protocol_PhtoOFF = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4F, 0x4F }; // Phtosensor OFF

        private byte[] protocol_TrayCheck = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x3F };
        private byte[] protocol_TrayON = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2F, 0x2F };
        private byte[] protocol_TrayOFF = { 0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x1F };


        private double WhenSendTrue;
        private double WhenReceive;

        public Form1()
        {
            InitializeComponent();
            serialPort = new SerialPort();
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.WriteTimeout = RobotCommandDrainTimeoutMs;
            string[] portNames = SerialPort.GetPortNames();
            cbPort.Items.AddRange(portNames);
            const string defaultRobotPort = "COM3";
            int defaultRobotPortIndex = Array.FindIndex(
                portNames,
                portName => string.Equals(
                    portName,
                    defaultRobotPort,
                    StringComparison.OrdinalIgnoreCase));
            if (defaultRobotPortIndex < 0)
            {
                cbPort.Items.Add(defaultRobotPort);
                defaultRobotPortIndex = cbPort.Items.Count - 1;
            }
            cbPort.SelectedIndex = defaultRobotPortIndex;
            selectedPortName = cbPort.SelectedItem.ToString();
            serialPort2 = new SerialPort();
            serialPort2.BaudRate = 115200;
            serialPort2.Parity = Parity.None;
            serialPort2.DataBits = 8;
            serialPort2.StopBits = StopBits.One;
            string[] portNames2 = SerialPort.GetPortNames();
            cbPort2.Items.AddRange(portNames2);



            const string defaultJetsonPort = "COM5";
            int defaultJetsonPortIndex = Array.FindIndex(
                portNames2,
                portName => string.Equals(
                    portName,
                    defaultJetsonPort,
                    StringComparison.OrdinalIgnoreCase));
            if (defaultJetsonPortIndex < 0)
            {
                cbPort2.Items.Add(defaultJetsonPort);
                defaultJetsonPortIndex = cbPort2.Items.Count - 1;
            }
            cbPort2.SelectedIndex = defaultJetsonPortIndex;
            selectedPortName2 = cbPort2.SelectedItem.ToString();
        }

        private async void btStLi_Click(object sender, EventArgs e)
        {
            // Start resumes an existing paused cycle instead of creating a new loop.
            if (mainCycleRunning)
            {
                if (!Working)
                {
                    if (!await EnsureRobotControllerReadyForStartAsync("resume"))
                    {
                        return;
                    }

                    // Reconnect the Jetson TTL port before resuming sensor operations.
                    if (!EnsureJetsonTtlConnectedForStart())
                    {
                        return;
                    }

                    Working = true;
                    tbEco.Text += "Existing robot cycle resumed.\r\n";
                }
                else
                {
                    tbEco.Text += "Start ignored: robot cycle is already running.\r\n";
                }
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
                return;
            }

            if (!await EnsureRobotControllerReadyForStartAsync("start"))
            {
                return;
            }

            // Verify the Jetson TTL port before robot motion starts.
            if (!EnsureJetsonTtlConnectedForStart())
            {
                return;
            }

            mainCycleRunning = true;
            Working = true;
            var timer = new MotionRunTimer("DeltaRun");
            var timer2 = new MotionRunTimer2("ImageProcessingCheck");
            var timer3 = new MotionRunTimer3("AskTimeCheck");
            int targetIndex = 0;
            int targetIndex_old = -1;

            byte[] protocol_100 = Make_Protocol(100);
            int speed = int.Parse(robot_v.Text);
            int delayTime = int.Parse(DelayTime.Text);
            int waitTime = int.Parse(WaitDelay.Text);
            int stopperDelay = int.Parse(stopper_delay.Text);

            float[] target;
            float tar_x = 0f;
            float tar_y = 0f;
            float tar_z = 0f;

            float tar_x_clamp;
            float tar_y_clamp;
            float tar_z_clamp;

            float tar_z_ref;

            float tar_x_old = 1000f;
            float tar_y_old = 1000f;
            float tar_z_old = 0f;

            double dist_target = 1000f;
            double dist_current_target = 1000f;

            float CamX1=0f;
            float CamY1=0f;
            float CamDepth1=0f;

            float CamX2=0f;
            float CamY2=0f;
            float CamDepth2=0f;

            float usedCamX = 0f;
            float usedCamY = 0f;
            float usedCamDepth = 0f;

            int try_order = 1;

            int success = 0;
            int failure = 0;

            string result;

            float x1 = 0f; float y1 = 0f; float z1 = 0f;
            float x2 = 0f; float y2 = 0f; float z2 = 0f;

            string result_1 = "Null";
            string result_2 = "Null";

            double Motion_Start = 0f;
            double Motion_End = 0f;
            double Motion_Ungrip_time = 0f;

            double Motion_Elapsed = 0f;
            double Motion_Elapsed_1 = 0f;
            double Motion_Elapsed_2 = 0f;
            double ImageProcessingStart = 0f;
            double ImageProcessingEnd = 0f;
            double ImageProcessingElapsed = 0f;
            double CycleElapsed = 0f;

            double PhotoCheck_Start = 0f;
            double PhotoCheck_End = 0f;

            float [] RobotLocateIPstart = { 0f, 0f, 0f };
            float [] RobotLocateIPend = { 0f, 0f, 0f };

            Stopwatch moveStopwatch = new Stopwatch();
            Stopwatch gapStopwatch = new Stopwatch();

            double gapElapsed = 0f;

            bool reStart = false;
            bool trayWaiting = false;
            Task pendingCameraRequest = null;

            float grip_margin = DefaultGripMarginMm;
            float margin1 = 0f;
            float margin2 = 0f;

            int box_index = 1;

            List<float[]> FailListTwice = new List<float[]>();
            try
            {
                tbEco.Invoke(new Action(() =>
                {
                    tbEco.Text += $"Start. Profile={ActiveRuntimeProfileId}\r\n";
                    tbEco.Text += $"Coordinate profile={ActiveCoordinateProfileId}\r\n";
                    tbEco.Text += $"Robot motion upload gap={RobotMotionUploadGapMs} ms per frame.\r\n";
                    tbEco.Text += $"Robot speeds: outbound=Robot V, pickup max={PickupApproachSpeedMax}, loaded lift max={LoadedLiftSpeedMax}, loaded XY return max={LoadedTravelSpeedMax}.\r\n";
                    tbEco.SelectionStart = tbEco.Text.Length;
                    tbEco.ScrollToCaret();
                }));

                // Accept a stable idle state when MotionEnd is unavailable.
                await WaitForStableRobotIdleAsync(
                    waitTime,
                    "before entering robot cycle");

                while (true)
                {
                    if (!serialPort.IsOpen) { break; }
                    if (!Working)
                    {
                        reStart = true;
                        await Task.Delay(1000);
                        continue;
                    }

                    if (IsRobotCycleGateReady()) // Fresh stable idle, including MotionEnd=false.
                    {
                        if (!gripSent)
                        {
                            if (true) // !isoccupied
                            {
                                moveStopwatch.Restart();
                                if (targetIndex == 0 || reStart)
                                {
                                    speed = int.Parse(robot_v.Text);
                                    await EnsureSafeOriginBeforeCaptureAsync(
                                        waitTime,
                                        speed);
                                    reStart = false;
                                    await WebCamAskTargetAsync(true);
                                }

                                if (CamX_1 == 0 && CamY_1 == 0 && CamDepth_1 == 0)
                                {
                                    tbEco.Invoke(new Action(() =>
                                    {
                                        tbEco.Text += "No Target. New Box Required. Paused...\r\n";
                                        tbEco.SelectionStart = tbEco.Text.Length;
                                        tbEco.ScrollToCaret();
                                    }));
                                    Working = false;
                                    box_index++;
                                    FailListTwice = new List<float[]>();
                                    tar_x_old = 1000f; tar_y_old = 1000f; tar_z_old = 0f;
                                    dist_target = 1000f;
                                    continue;
                                }

                                usedCamX = CamX_1; usedCamY = CamY_1; usedCamDepth = CamDepth_1;
                                target = Pix2mm(usedCamX, usedCamY, usedCamDepth);
                                tar_x = target[0];
                                tar_y = target[1];
                                tar_z = target[2];

                                foreach (float[] failed_target in Enumerable.Reverse(FailListTwice).ToList())
                                {
                                    dist_target = CalculateDistance(tar_x, tar_y, tar_z, failed_target[0], failed_target[1], failed_target[2]);
                                    if (dist_target < SameTargetThresholdMm)
                                    {
                                        tbEco.Invoke(new Action(() =>
                                        {
                                            tbEco.Text += $"Failed Target. X: {failed_target[0]}, Y: {failed_target[1]}, Z: {failed_target[2]}. Skip this...\r\n";
                                            tbEco.SelectionStart = tbEco.Text.Length;
                                            tbEco.ScrollToCaret();
                                        }));
                                        usedCamX = CamX_2; usedCamY = CamY_2; usedCamDepth = CamDepth_2;
                                        target = Pix2mm(usedCamX, usedCamY, usedCamDepth);
                                        tar_x = target[0];
                                        tar_y = target[1];
                                        tar_z = target[2];
                                        foreach (float[] failed_target_2 in Enumerable.Reverse(FailListTwice).ToList())
                                        {
                                            dist_target = CalculateDistance(tar_x, tar_y, tar_z, failed_target_2[0], failed_target_2[1], failed_target_2[2]);
                                            if (dist_target < SameTargetThresholdMm)
                                            {
                                                Working = false;
                                                tbEco.Invoke(new Action(() =>
                                                {
                                                    tbEco.Text += $"Failed Target. X: {failed_target[0]}, Y: {failed_target[1]}, Z: {failed_target[2]}. Skip this...\r\n";
                                                    tbEco.Text += $"Both Targets Failed. Paused...\r\n";
                                                    tbEco.SelectionStart = tbEco.Text.Length;
                                                    tbEco.ScrollToCaret();
                                                }));
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (!Working) { continue; }


                                dist_target = CalculateDistance(tar_x, tar_y, 0, tar_x_old, tar_y_old, 0);


                                if (dist_target < SameTargetThresholdMm)
                                {
                                    if (try_order == 1)
                                    {
                                        tar_z = Math.Max(tar_z, tar_z_old) + 10f;
                                        try_order++;
                                    }
                                    else if (try_order == 2)
                                    {
                                        if (CamX_1 == CamX_2 && CamY_1 == CamY_2 && CamDepth_1 == CamDepth_2)
                                        {
                                            Working = false;

                                            tbEco.Invoke(new Action(() =>
                                            {
                                                tbEco.Text += $"Same Target. Paused...\r\n";
                                                tbEco.SelectionStart = tbEco.Text.Length;
                                                tbEco.ScrollToCaret();
                                            }));
                                            continue;
                                        }
                                        FailListTwice.Add(new float[] { tar_x, tar_y, tar_z });
                                        tbEco.Invoke(new Action(() =>
                                        {
                                            tbEco.Text += $"Failed Added. List Count : {FailListTwice.Count}\r\n";
                                            tbEco.SelectionStart = tbEco.Text.Length;
                                            tbEco.ScrollToCaret();
                                        }));

                                        if (usedCamX == CamX_2 && usedCamY == CamY_2 && usedCamDepth == CamDepth_2)
                                        {
                                            tbEco.Invoke(new Action(() =>
                                            {
                                                tbEco.Text += $"2nd Target failed twice. Paused...\r\n";
                                                tbEco.SelectionStart = tbEco.Text.Length;
                                                tbEco.ScrollToCaret();
                                            }));
                                            Working = false;
                                            continue;
                                        }
                                        else
                                        {
                                            usedCamX = CamX_2; usedCamY = CamY_2; usedCamDepth = CamDepth_2;
                                            target = Pix2mm(usedCamX, usedCamY, usedCamDepth);
                                            tar_x = target[0];
                                            tar_y = target[1];
                                            tar_z = target[2];
                                        }
                                        try_order = 1;
                                        dist_target = 1000f;
                                        targetIndex++;
                                    }
                                }
                                else if (dist_target >= 20)
                                {
                                    if (targetIndex > 0 && targetIndex == targetIndex_old)
                                    {
                                        CamX2 = 0f; CamY2 = 0f; CamDepth2 = 0f;
                                        x2 = 0f; y2 = 0f; z2 = 0f;
                                        margin2 = 0f;
                                        Motion_Elapsed_2 = 0f;
                                        result_2 = "None";
                                        timer.EndCycle(box_index, targetIndex, speed,
                                            result_1, Motion_Elapsed_1, CamX1, CamY1, CamDepth1, x1, y1, z1, margin1,
                                            result_2, Motion_Elapsed_2, CamX2, CamY2, CamDepth2, x2, y2, z2, margin2,
                                            success, failure);
                                        targetIndex_old = -1;

                                        tbvision.Invoke(new Action(() =>
                                        {
                                            tbvision.Text += $"{targetIndex}. 1st Try {result_1}, 2nd Try {result_2}.\r\n";
                                            tbvision.SelectionStart = tbvision.Text.Length;
                                            tbvision.ScrollToCaret();
                                        }));

                                        tbvision.Invoke(new Action(() =>
                                        {
                                            tbvision.Text += $"Another Target Detected.\r\n";
                                            tbvision.SelectionStart = tbvision.Text.Length;
                                            tbvision.ScrollToCaret();
                                        }));
                                    }
                                    try_order = 1;
                                    targetIndex++;
                                }
                                tar_x_old = tar_x;
                                tar_y_old = tar_y;
                                tar_z_old = tar_z;

                                tbvision.Invoke(new Action(() =>
                                {
                                    tbvision.Text += $"{targetIndex}. x: {tar_x:F2}, y: {tar_y:F2}, z: {tar_z:F2}, Try #{try_order}.\r\n";
                                    tbvision.SelectionStart = tbvision.Text.Length;
                                    tbvision.ScrollToCaret();
                                }));

                                tar_x_clamp = Math.Max(Math.Min(tar_x, 200f), -200f);
                                tar_y_clamp = Math.Max(Math.Min(tar_y, 280f), 45f);

                                grip_margin = DefaultGripMarginMm;
                                tar_z_clamp = tar_z + grip_margin; // AdaBins target Z plus the conservative downward grasp margin.
                                tar_z_clamp = Math.Min(tar_z_clamp, 775f);

                                tar_z_ref = z_ref + (tar_z_clamp - z_ref) * 0.8f;
                                if (serialPort.IsOpen)
                                {
                                    // Require fresh stable Origin status before target motion.
                                    await WaitForStableOriginReadyAsync(
                                        waitTime,
                                        "before target motion");

                                    // Apply the current GUI speed to the next trajectory.
                                    speed = int.Parse(robot_v.Text);
                                    int loadedLiftSpeed = Math.Min(
                                        speed,
                                        LoadedLiftSpeedMax);
                                    int loadedTravelSpeed = Math.Min(
                                        speed,
                                        LoadedTravelSpeedMax);
                                    ResetRobotControllerFaultEvidence();
                                    createMotion(100, 101, x: x_pass, y: y_pass, z: z_pass, v: speed, run_io: 0, end_io: 0);
                                    createMotion(101, 102, x: tar_x_clamp, y: tar_y_clamp, z: z_ref, v: speed, run_io: 0, end_io: 0);
                                    createMotion(102, 103, x: tar_x_clamp, y: tar_y_clamp, z: tar_z_ref, v: speed, run_io: 0, end_io: 0);
                                    createMotion(103, 104, x: tar_x_clamp, y: tar_y_clamp, z: tar_z_clamp, v: Math.Min(speed, PickupApproachSpeedMax), run_io: 1, end_io: 1);
                                    createMotion(104, 105, x: tar_x_clamp, y: tar_y_clamp, z: z_ref, v: loadedLiftSpeed, run_io: 1, end_io: 1);
                                    createMotion(105, 106, x: x_pass, y: y_pass, z: z_pass, v: loadedTravelSpeed, run_io: 1, end_io: 1);
                                    createMotion(106, 32767, x: x_ori, y: y_ori, z: z_ori, v: loadedTravelSpeed, run_io: 1, end_io: 0);

                                    Motion_Start = moveStopwatch.Elapsed.TotalSeconds;
                                    ResetRobotOriginReadyEvidence();
                                    WriteRobotCommandFrame(
                                        protocol_100,
                                        "Execute motion chain 100",
                                        0);
                                    gapStopwatch.Stop();
                                    gapElapsed = gapStopwatch.Elapsed.TotalSeconds;

                                    await Task.Delay(delayTime);

                                    do
                                    {
                                        if (!serialPort.IsOpen) { break; }
                                        // Compare with the clamped coordinates sent to the robot.
                                        dist_current_target = CalculateDistance(
                                            posX,
                                            posY,
                                            posZ,
                                            tar_x_clamp,
                                            tar_y_clamp,
                                            tar_z_clamp);
                                        await Task.Delay(waitTime);
                                    } while (dist_current_target > 100);

                                    // Confirm stable Origin status before the next capture.
                                    await WaitForStableOriginReadyAsync(
                                        waitTime,
                                        "after return to Origin",
                                        RobotCycleReturnTimeoutMs,
                                        monitorMotionProgress: true);

                                    Motion_End = moveStopwatch.Elapsed.TotalSeconds;
                                    Motion_Elapsed = Motion_End - Motion_Start;
                                }
                                else if (!serialPort.IsOpen) { break; }
                                gripSent = true;
                            }
                            else
                            {
                                Stopper_operation();
                                await Task.Delay(100);
                                tbEco.Invoke(new Action(() =>
                                {
                                    tbEco.Text += $"GripSent = false, isoccupied = true. \r\n";
                                    tbEco.SelectionStart = tbEco.Text.Length;
                                    tbEco.ScrollToCaret();
                                }));
                            }
                        }
                        else
                        {
                            try
                            {
                                isoccupied = Phtosensor_check();
                            }
                            catch (Exception ex) when (IsJetsonTtlCommunicationException(ex))
                            {
                                HandleJetsonTtlFailure("PhotoCheck", ex);
                                continue;
                            }

                            if (isoccupied)
                            {
                                try
                                {
                                    isTray = Tray_check();
                                }
                                catch (Exception ex) when (IsJetsonTtlCommunicationException(ex))
                                {
                                    HandleJetsonTtlFailure("TrayCheck", ex);
                                    continue;
                                }

                                await Task.Delay(10);
                                if (isTray)
                                {
                                    // Release before capturing the next target.
                                    Ungripping();
                                    Motion_Ungrip_time = moveStopwatch.Elapsed.TotalSeconds;
                                    await WaitForRobotTransmitDrainAsync(
                                        waitTime,
                                        "Ungrip");
                                    RobotLocateIPstart[0] = posX;
                                    RobotLocateIPstart[1] = posY;
                                    RobotLocateIPstart[2] = posZ;
                                    ImageProcessingStart = moveStopwatch.Elapsed.TotalSeconds;
                                    pendingCameraRequest = WebCamAskTargetAsync();
                                    if (trayWaiting) {  await Task.Delay(100); trayWaiting = false; }
                                    Stopper_operation();
                                    await Task.Delay(100);
                                }
                                else
                                {
                                    if (pendingCameraRequest != null)
                                    {
                                        await pendingCameraRequest;
                                        pendingCameraRequest = null;
                                    }
                                    ImageProcessingEnd = moveStopwatch.Elapsed.TotalSeconds;
                                    RobotLocateIPend[0] = posX; RobotLocateIPend[1] = posY; RobotLocateIPend[2] = posZ;
                                    ImageProcessingElapsed = ImageProcessingEnd - ImageProcessingStart;
                                    moveStopwatch.Stop();
                                    CycleElapsed = moveStopwatch.Elapsed.TotalSeconds;
                                    trayWaiting = true;
                                    tbEco.Invoke(new Action(() =>
                                    {
                                        tbEco.Text += "No Tray. Waiting for new tray...\r\n";
                                        tbEco.SelectionStart = tbEco.Text.Length;
                                        tbEco.ScrollToCaret();
                                    }));
                                    await Task.Delay(1000);
                                    continue;
                                }

                                success++;
                                result = "Success";
                            }
                            else
                            {
                                failure++;
                                result = "Fail";

                                // Capture the next target after returning to stable Origin.
                                RobotLocateIPstart[0] = posX;
                                RobotLocateIPstart[1] = posY;
                                RobotLocateIPstart[2] = posZ;
                                ImageProcessingStart = moveStopwatch.Elapsed.TotalSeconds;
                                pendingCameraRequest = WebCamAskTargetAsync();
                            }
                            gapStopwatch.Restart();
                            if (pendingCameraRequest != null)
                            {
                                await pendingCameraRequest;
                                pendingCameraRequest = null;
                            }
                            if (moveStopwatch.IsRunning)
                            {
                                ImageProcessingEnd = moveStopwatch.Elapsed.TotalSeconds;
                                RobotLocateIPend[0] = posX; RobotLocateIPend[1] = posY; RobotLocateIPend[2] = posZ;
                                ImageProcessingElapsed = ImageProcessingEnd - ImageProcessingStart;
                                CycleElapsed = moveStopwatch.Elapsed.TotalSeconds;
                            }

                            // Arm the next cycle after camera and robot readiness complete.
                            await WaitForStableOriginReadyAsync(
                                waitTime,
                                "before arming next cycle");
                            gripSent = false;

                            timer2.EndCycle(box_index, targetIndex, speed, gapElapsed, result,
                                Motion_Start, ImageProcessingStart, Motion_End, Motion_Ungrip_time,ImageProcessingEnd,
                                Motion_Elapsed, ImageProcessingElapsed, CycleElapsed,
                                RobotLocateIPstart, RobotLocateIPend);

                            timer3.EndCycle(targetIndex, WhenSendTrue, WhenReceive);


                            if (try_order == 1)
                            {
                                CamX1 = usedCamX; CamY1 = usedCamY; CamDepth1 = usedCamDepth;
                                x1 = tar_x; y1 = tar_y; z1 = tar_z;
                                margin1 = grip_margin;
                                Motion_Elapsed_1 = Motion_Elapsed;
                                result_1 = result;
                                if (result_1 == "Success")
                                {
                                    CamX2 = 0f; CamY2 = 0f; CamDepth2 = 0f;
                                    x2 = 0f; y2 = 0f; z2 = 0f;
                                    margin2 = 0f;
                                    Motion_Elapsed_2 = 0f;
                                    result_2 = "None";
                                }
                                else if (result_1 == "Fail")
                                {
                                    targetIndex_old = targetIndex;
                                    continue;
                                }
                            }
                            else if (try_order == 2)
                            {
                                CamX2 = usedCamX; CamY2 = usedCamY; CamDepth2 = usedCamDepth;
                                x2 = tar_x; y2 = tar_y; z2 = tar_z;
                                margin2 = grip_margin;
                                Motion_Elapsed_2 = Motion_Elapsed;
                                result_2 = result;
                            }

                            timer.EndCycle(box_index, targetIndex, speed,
                                result_1, Motion_Elapsed_1, CamX1, CamY1, CamDepth1, x1, y1, z1, margin1,
                                result_2, Motion_Elapsed_2, CamX2, CamY2, CamDepth2, x2, y2, z2, margin2,
                                success, failure);
                            targetIndex_old = -1;
                            tbvision.Invoke(new Action(() =>
                            {
                                tbvision.Text += $"{targetIndex}. 1st Try {result_1}, 2nd Try {result_2}.\r\n";
                                tbvision.SelectionStart = tbvision.Text.Length;
                                tbvision.ScrollToCaret();
                            }));

                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }

                }
                Ungripping();
                tbEco.Text += "End.\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
            catch (Exception ex)
            {
                Working = false;
                string diagnosticPath = SaveRobotFaultDiagnostic(ex);
                string diagnosticSuffix = string.IsNullOrEmpty(diagnosticPath)
                    ? string.Empty
                    : "\r\nRobot diagnostic saved: " + diagnosticPath;
                tbEco.Text += "Robot cycle stopped safely: " + ex.Message +
                    diagnosticSuffix + "\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
                MessageBox.Show(
                    ex.Message + diagnosticSuffix,
                    "Robot cycle stopped safely",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                mainCycleRunning = false;
                timer.Save();
                timer2.Save();
                timer3.Save();
                
                tbEco.Text += $"Motion times saved: {timer.FilePath}\r\n";
                tbEco.Text += $"ImageProc times saved: {timer2.FilePath}\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
        }
        private async void CameraConnect_Click(object sender, EventArgs e)
        {
            cameraDisconnectRequested = false;
            await cameraRequestLock.WaitAsync();
            try
            {
                await ReconnectCameraCoreAsync();
                tbEco.Text += "CAM TCP connected.\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
            catch (Exception ex)
            {
                CloseCameraConnection();
                if (!formClosing)
                {
                    MessageBox.Show(
                        ex.Message,
                        "CAM TCP connection failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                cameraRequestLock.Release();
            }
        }

        private async Task ReconnectCameraCoreAsync()
        {
            DisposeCameraTransport();
            ClearGripImagePreview();
            if (cameraDisconnectRequested || formClosing)
            {
                throw new OperationCanceledException(
                    "Camera connection was cancelled.");
            }

            TcpClient newClient = new TcpClient();
            newClient.NoDelay = true;
            try
            {
                await newClient.ConnectAsync(
                    CameraServerAddress,
                    CameraServerPort);
                if (cameraDisconnectRequested || formClosing)
                {
                    throw new OperationCanceledException(
                        "Camera connection was cancelled.");
                }

                tcpClient = newClient;
                cameraStream = newClient.GetStream();
                cameraConnectionCancellationSource =
                    new CancellationTokenSource();
            }
            catch
            {
                try { newClient.Close(); } catch { }
                throw;
            }
        }

        private void CloseCameraConnection()
        {
            cameraDisconnectRequested = true;
            DisposeCameraTransport();
            ClearGripImagePreview();
        }

        private void DisposeCameraTransport()
        {
            CancellationTokenSource cancellationSource =
                cameraConnectionCancellationSource;
            NetworkStream stream = cameraStream;
            TcpClient client = tcpClient;

            cameraConnectionCancellationSource = null;
            cameraStream = null;
            tcpClient = null;

            try { cancellationSource?.Cancel(); } catch { }
            try { stream?.Dispose(); } catch { }
            try { client?.Close(); } catch { }
            try { cancellationSource?.Dispose(); } catch { }
        }

        private async Task WebCamAskTargetAsync(bool logCoordinates = false)
        {
            await cameraRequestLock.WaitAsync();
            try
            {
                try
                {
                    await RequestCameraTargetOnCurrentConnectionAsync(logCoordinates);
                }
                catch (Exception ex) when (
                    IsRecoverableCameraException(ex) &&
                    !cameraDisconnectRequested &&
                    !formClosing)
                {
                    tbEco.Text += "CAM request failed; reconnecting once: " + ex.Message + "\r\n";
                    tbEco.SelectionStart = tbEco.Text.Length;
                    tbEco.ScrollToCaret();
                    await ReconnectCameraCoreAsync();
                    await RequestCameraTargetOnCurrentConnectionAsync(logCoordinates);
                }
            }
            finally
            {
                cameraRequestLock.Release();
            }
        }

        private async Task RequestCameraTargetOnCurrentConnectionAsync(bool logCoordinates)
        {
            TcpClient activeClient = tcpClient;
            NetworkStream activeStream = cameraStream;
            CancellationTokenSource activeCancellationSource =
                cameraConnectionCancellationSource;
            if (activeClient == null ||
                activeStream == null ||
                activeCancellationSource == null ||
                cameraDisconnectRequested ||
                !activeClient.Connected)
            {
                throw new InvalidOperationException(
                    "CAM TCP is not connected. Start the Jetson server and press CAM Connect first.");
            }

            CancellationToken cancellationToken =
                activeCancellationSource.Token;
            Stopwatch sw = Stopwatch.StartNew();
            byte[] trigger = Encoding.UTF8.GetBytes("True");
            WhenSendTrue = sw.Elapsed.TotalSeconds;
            await activeStream.WriteAsync(
                trigger,
                0,
                trigger.Length,
                cancellationToken);

            CameraResponsePacket packet =
                await ReadCameraResponseWithTimeoutAsync(
                    activeStream,
                    cancellationToken);
            sw.Stop();
            WhenReceive = sw.Elapsed.TotalSeconds;

            Bitmap yoloImage = null;
            Bitmap depthImage = null;
            try
            {
                yoloImage = CameraResponseProtocol.DecodePng(
                    packet.YoloImageBytes,
                    packet.Header.ImageWidth,
                    packet.Header.ImageHeight,
                    "YOLO PNG");
                depthImage = CameraResponseProtocol.DecodePng(
                    packet.DepthImageBytes,
                    packet.Header.ImageWidth,
                    packet.Header.ImageHeight,
                    "Depth PNG");

                try
                {
                    string capturePath =
                        await gripImageCaptureStore.SaveCompositeAsync(
                            yoloImage,
                            depthImage,
                            DateTime.Now);
                    if (!formClosing && !IsDisposed && !Disposing)
                    {
                        tbEco.Text += "Grip image saved: " + capturePath + "\r\n";
                        tbEco.SelectionStart = tbEco.Text.Length;
                        tbEco.ScrollToCaret();
                    }
                }
                catch (Exception saveException)
                {
                    // Image-archive failure does not stop coordinate processing.
                    if (!formClosing && !IsDisposed && !Disposing)
                    {
                        tbEco.Text += "Grip image save failed: " +
                            saveException.Message + "\r\n";
                        tbEco.SelectionStart = tbEco.Text.Length;
                        tbEco.ScrollToCaret();
                    }
                }

                UpdateGripImagePreview(yoloImage, depthImage);
                yoloImage = null;
                depthImage = null;
            }
            finally
            {
                yoloImage?.Dispose();
                depthImage?.Dispose();
            }

            CamX_1 = packet.Header.CamX1;
            CamY_1 = packet.Header.CamY1;
            CamDepth_1 = (float)packet.Header.CamDepth1 * 1000f;

            CamX_2 = packet.Header.CamX2;
            CamY_2 = packet.Header.CamY2;
            CamDepth_2 = (float)packet.Header.CamDepth2 * 1000f;

            JetsonTime = (float)packet.Header.JetsonTime;
            if (logCoordinates)
            {
                tbEco.Text += $"1. x: {CamX_1}pix, y: {CamY_1}pix, depth: {CamDepth_1}mm.\r\n";
                tbEco.Text += $"2. x: {CamX_2}pix, y: {CamY_2}pix, depth: {CamDepth_2}mm.\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
        }

        private async Task<CameraResponsePacket> ReadCameraResponseWithTimeoutAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            Task<CameraResponsePacket> responseTask =
                CameraResponseProtocol.ReadResponseAsync(
                    stream,
                    cancellationToken);
            Task timeoutTask = Task.Delay(
                CameraResponseTimeoutMs,
                cancellationToken);
            Task completed = await Task.WhenAny(
                responseTask,
                timeoutTask).ConfigureAwait(false);

            if (completed == responseTask)
            {
                return await responseTask.ConfigureAwait(false);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            // Discard a timed-out stream before the next request.
            DisposeCameraTransport();
            ObserveFault(responseTask);
            throw new TimeoutException(string.Format(
                "Jetson camera response was not completed within {0} ms.",
                CameraResponseTimeoutMs));
        }

        private static void ObserveFault(Task task)
        {
            task.ContinueWith(
                completedTask =>
                {
                    Exception ignored = completedTask.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                    TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void UpdateGripImagePreview(
            Bitmap yoloImage,
            Bitmap depthImage)
        {
            if (formClosing || IsDisposed || Disposing)
            {
                yoloImage?.Dispose();
                depthImage?.Dispose();
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(
                        new Action<Bitmap, Bitmap>(UpdateGripImagePreview),
                        yoloImage,
                        depthImage);
                }
                catch
                {
                    yoloImage?.Dispose();
                    depthImage?.Dispose();
                    throw;
                }

                return;
            }

            bool imagesTransferred = false;
            try
            {
                if (gripImagePreviewForm == null ||
                    gripImagePreviewForm.IsDisposed)
                {
                    GripImagePreviewForm previewForm =
                        new GripImagePreviewForm();
                    previewForm.FormClosed += GripImagePreviewForm_FormClosed;
                    gripImagePreviewForm = previewForm;
                    previewForm.Show(this);
                }

                gripImagePreviewForm.SetImages(
                    yoloImage,
                    depthImage);
                imagesTransferred = true;
            }
            finally
            {
                if (!imagesTransferred)
                {
                    yoloImage?.Dispose();
                    depthImage?.Dispose();
                }
            }
        }

        private void GripImagePreviewForm_FormClosed(
            object sender,
            FormClosedEventArgs e)
        {
            GripImagePreviewForm closedForm =
                sender as GripImagePreviewForm;
            if (ReferenceEquals(gripImagePreviewForm, closedForm))
            {
                gripImagePreviewForm = null;
            }
        }

        private void ClearGripImagePreview()
        {
            if (InvokeRequired && IsHandleCreated && !IsDisposed)
            {
                try
                {
                    BeginInvoke(new Action(ClearGripImagePreview));
                }
                catch
                {
                }

                return;
            }

            if (gripImagePreviewForm == null ||
                gripImagePreviewForm.IsDisposed)
            {
                return;
            }

            gripImagePreviewForm.SetImages(null, null);
        }

        private static bool IsRecoverableCameraException(Exception ex)
        {
            return ex is TimeoutException ||
                   ex is IOException ||
                   ex is SocketException ||
                   ex is ObjectDisposedException;
        }


        private async void btMotion_Test_Depth_Click(object sender, EventArgs e)
        {
            try
            {
                if (tcpClient == null || !tcpClient.Connected)
                {
                    throw new InvalidOperationException(
                        "CAM TCP is not connected. Start the Jetson server and press CAM Connect first.");
                }

                byte[] protocol_100 = Make_Protocol(100);
                int speed = int.Parse(robot_v.Text);
                int delayTime = int.Parse(DelayTime.Text);
                int waitTime = int.Parse(WaitDelay.Text);

                for (int k = 0; k < 1; k++)
                {
                    while (BusyBit || !motionEndBit)
                    {
                        await Task.Delay(100);
                    }
                    await WebCamAskTargetAsync(true);

                    if (!IsValidAdaBinsTarget(CamX_1, CamY_1, CamDepth_1))
                    {
                        tbvision.Text += string.Format(
                            CultureInfo.InvariantCulture,
                            "Primary target rejected: x={0}, y={1}, depth={2} mm. " +
                            "AdaBins requires a finite pixel inside 640x480 and depth in " +
                            "[925, 1230] mm; no robot motion was issued.\r\n",
                            CamX_1,
                            CamY_1,
                            CamDepth_1);
                        tbvision.SelectionStart = tbvision.Text.Length;
                        tbvision.ScrollToCaret();
                        return;
                    }

                    float[] target1 = Pix2mm(CamX_1, CamY_1, CamDepth_1);
                    float tar_x1 = target1[0];
                    float tar_y1 = target1[1];
                    float tar_z1 = target1[2];

                    tbvision.Text += $"1. x: {tar_x1:F2}mm, y: {tar_y1:F2}mm, z: {tar_z1:F2}mm.\r\n";
                    tbvision.SelectionStart = tbvision.Text.Length;
                    tbvision.ScrollToCaret();

                    if (IsValidAdaBinsTarget(CamX_2, CamY_2, CamDepth_2))
                    {
                        float[] target2 = Pix2mm(CamX_2, CamY_2, CamDepth_2);
                        float tar_x2 = target2[0];
                        float tar_y2 = target2[1];
                        float tar_z2 = target2[2];

                        tbvision.Text += $"2. x: {tar_x2:F2}mm, y: {tar_y2:F2}mm, z: {tar_z2:F2}mm.\r\n\r\n";
                    }
                    else
                    {
                        tbvision.Text += string.Format(
                            CultureInfo.InvariantCulture,
                            "2. target skipped: x={0}, y={1}, depth={2} mm.\r\n\r\n",
                            CamX_2,
                            CamY_2,
                            CamDepth_2);
                    }
                    tbvision.SelectionStart = tbvision.Text.Length;
                    tbvision.ScrollToCaret();

                float tar_x_clamp = Math.Max(Math.Min(tar_x1, 200f), -200f);
                float tar_y_clamp = Math.Max(Math.Min(tar_y1, 280f), 45f);
                float grip_margin;

                grip_margin = DefaultGripMarginMm;
                float tar_z_clamp = tar_z1 + grip_margin; // Depth Test target Z plus the common downward grasp margin.
                tar_z_clamp = Math.Min(tar_z_clamp, 775f);

                    if (serialPort.IsOpen)
                    {
                        createMotion(motionNum_int: 100, nextMotion_int: 101, x: x_pass, y: y_pass, z: z_pass, v: speed, run_io: 0, end_io: 0);
                        createMotion(motionNum_int: 101, nextMotion_int: 102, x: tar_x_clamp, y: tar_y_clamp, z: z_ref, v: speed, run_io: 0, end_io: 0);
                        createMotion(motionNum_int: 102, nextMotion_int: 103, x: tar_x_clamp, y: tar_y_clamp, z: tar_z_clamp, v: Math.Min(speed, PickupApproachSpeedMax), run_io: 1, end_io: 1);
                        createMotion(motionNum_int: 103, nextMotion_int: 104, x: tar_x_clamp, y: tar_y_clamp, z: z_ref, v: Math.Min(speed, LoadedLiftSpeedMax), run_io: 1, end_io: 1);
                        createMotion(motionNum_int: 104, nextMotion_int: 105, x: x_pass, y: y_pass, z: z_pass, v: Math.Min(speed, LoadedTravelSpeedMax), run_io: 1, end_io: 1);
                        createMotion(motionNum_int: 105, nextMotion_int: 32767, x: x_ori, y: y_ori, z: z_ori, v: Math.Min(speed, LoadedTravelSpeedMax), run_io: 1, end_io: 0);

                        WriteRobotCommandFrame(
                            protocol_100,
                            "Depth Test execute motion chain 100",
                            0);
                    }
                }
            }
            catch (Exception ex)
            {
                tbvision.Text += "Depth Test failed safely: " + ex.Message + "\r\n";
                tbvision.SelectionStart = tbvision.Text.Length;
                tbvision.ScrollToCaret();
                MessageBox.Show(
                    ex.Message,
                    "Depth Test - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static bool IsValidAdaBinsTarget(
            float pixelX,
            float pixelY,
            float depthMm)
        {
            return !float.IsNaN(pixelX) && !float.IsInfinity(pixelX) &&
                !float.IsNaN(pixelY) && !float.IsInfinity(pixelY) &&
                !float.IsNaN(depthMm) && !float.IsInfinity(depthMm) &&
                pixelX >= 0f && pixelX < AdaBinsFrameWidthPixels &&
                pixelY >= 0f && pixelY < AdaBinsFrameHeightPixels &&
                depthMm >= AdaBinsMinimumDepthMm &&
                depthMm <= AdaBinsMaximumDepthMm;
        }
        
        private void Stopper_operation() 
        {
            lock (jetsonTtlSync)
            {
                if (serialPort2 == null || !serialPort2.IsOpen)
                {
                    throw new InvalidOperationException("Jetson TTL serial port is not open.");
                }

                serialPort2.Write(protocol_StopperON, 0, protocol_StopperON.Length);
            }
        }

        private byte[] ExchangeJetsonTtl(
            string operation,
            byte[] request,
            params byte[][] expectedResponses)
        {
            lock (jetsonTtlSync)
            {
                if (serialPort2 == null || !serialPort2.IsOpen)
                {
                    throw new InvalidOperationException("Jetson TTL serial port is not open.");
                }

                if (request == null || request.Length != JetsonTtlFrameLength)
                {
                    throw new ArgumentException("Jetson TTL request must be an 11-byte frame.", nameof(request));
                }

                if (expectedResponses == null || expectedResponses.Length == 0 ||
                    expectedResponses.Any(response => response == null || response.Length != JetsonTtlFrameLength))
                {
                    throw new ArgumentException("At least one valid 11-byte response frame is required.", nameof(expectedResponses));
                }

                int previousReadTimeout = serialPort2.ReadTimeout;
                List<byte> rawBytes = new List<byte>();
                List<byte> frame = new List<byte>(JetsonTtlFrameLength);
                List<string> rejectedFrames = new List<string>();
                int totalBytesRead = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    serialPort2.DiscardInBuffer();
                    serialPort2.Write(request, 0, request.Length);

                    while (stopwatch.ElapsedMilliseconds < JetsonTtlResponseTimeoutMs)
                    {
                        int remainingMs = JetsonTtlResponseTimeoutMs - (int)stopwatch.ElapsedMilliseconds;
                        serialPort2.ReadTimeout = Math.Max(1, remainingMs);

                        int value;
                        try
                        {
                            value = serialPort2.ReadByte();
                        }
                        catch (TimeoutException)
                        {
                            break;
                        }

                        byte received = (byte)value;
                        totalBytesRead++;
                        if (rawBytes.Count < 256)
                        {
                            rawBytes.Add(received);
                        }

                        if (received == 0x96)
                        {
                            frame.Clear();
                            frame.Add(received);
                            continue;
                        }

                        if (frame.Count == 0)
                        {
                            continue;
                        }

                        frame.Add(received);
                        if (frame.Count < JetsonTtlFrameLength)
                        {
                            continue;
                        }

                        byte[] candidate = frame.ToArray();
                        foreach (byte[] expectedResponse in expectedResponses)
                        {
                            if (candidate.SequenceEqual(expectedResponse))
                            {
                                return candidate;
                            }
                        }

                        if (rejectedFrames.Count < 8)
                        {
                            rejectedFrames.Add(BitConverter.ToString(candidate));
                        }
                        frame.Clear();
                    }
                }
                finally
                {
                    stopwatch.Stop();
                    try
                    {
                        if (serialPort2 != null && serialPort2.IsOpen)
                        {
                            serialPort2.ReadTimeout = previousReadTimeout;
                        }
                    }
                    catch (IOException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                string rawHex = rawBytes.Count == 0
                    ? "<no bytes>"
                    : BitConverter.ToString(rawBytes.ToArray());
                string rejected = rejectedFrames.Count == 0
                    ? "<none>"
                    : string.Join(" | ", rejectedFrames);
                string protocolHint = totalBytesRead == 1 &&
                    (rawBytes[0] == 0x00 || rawBytes[0] == 0x01)
                    ? " Jetson returned a one-byte 00/01 status, but this robot safety protocol requires an 11-byte framed response."
                    : string.Empty;

                throw new TimeoutException(
                    operation + " response timeout/validation failure. " +
                    "bytes=" + totalBytesRead + ", raw=" + rawHex +
                    ", rejected frames=" + rejected + "." + protocolHint);
            }
        }

        private bool Phtosensor_check()
        {
            byte[] response = ExchangeJetsonTtl(
                "PhotoCheck",
                protocol_PhtoCheck,
                protocol_PhtoON,
                protocol_PhtoOFF);

            return response.SequenceEqual(protocol_PhtoON);
        }

        private bool Tray_check()
        {
            byte[] response = ExchangeJetsonTtl(
                "TrayCheck",
                protocol_TrayCheck,
                protocol_TrayON,
                protocol_TrayOFF);

            return response.SequenceEqual(protocol_TrayON);
        }

        private static bool IsJetsonTtlCommunicationException(Exception ex)
        {
            return ex is TimeoutException ||
                ex is IOException ||
                ex is InvalidOperationException;
        }

        private void HandleJetsonTtlFailure(string operation, Exception ex)
        {
            Working = false;
            string message = operation + " failed. Robot cycle paused; stopper was not commanded.\r\n" + ex.Message;

            Action appendLog = () =>
            {
                tbEco.Text += message + "\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            };

            if (tbEco.InvokeRequired)
            {
                tbEco.Invoke(appendLog);
            }
            else
            {
                appendLog();
            }

            MessageBox.Show(
                message,
                "Jetson TTL protocol error - Robot paused",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void Photo_Click(object sender, EventArgs e)
        {
            try
            {
                isoccupied = Phtosensor_check();
            }
            catch (Exception ex) when (IsJetsonTtlCommunicationException(ex))
            {
                HandleJetsonTtlFailure("PhotoCheck", ex);
                return;
            }

            if (isoccupied)
            {
                tbvision.Text += $"Tray is occupied";
                tbvision.Text += "\r\n";
                tbvision.SelectionStart = tbvision.Text.Length;
                tbvision.ScrollToCaret();
            }
            else
            {
                tbvision.Text += $"Tray is empty";
                tbvision.Text += "\r\n";
                tbvision.SelectionStart = tbvision.Text.Length;
                tbvision.ScrollToCaret();
            }
        }
        private void ResetRobotOriginReadyEvidence()
        {
            lock (robotStatusSync)
            {
                robotIdleReadyFrameCount = 0;
                robotOriginReadyFrameCount = 0;
                robotStationaryIdleFrameCount = 0;
                robotStationaryOriginFrameCount = 0;
                hasPreviousRobotStatusPosition = false;
                lastRobotStatusTimestamp = 0;
            }
        }

        private void ResetRobotControllerFaultEvidence()
        {
            lock (robotStatusSync)
            {
                latchedRobotIoInputStopBits = 0;
                latchedRobotIoOutputErrorBits = 0;
                latchedRobotServo1Faults = 0;
                latchedRobotServo2Faults = 0;
                latchedRobotServo3Faults = 0;
                latchedRobotServo4Faults = 0;
                latchedRobotErrorType = 0;
            }
        }

        private string GetRobotControllerStatusDiagnostics()
        {
            ushort ioInput;
            ushort ioOutput;
            ushort servo1;
            ushort servo2;
            ushort servo3;
            ushort servo4;
            byte errorType;
            ushort latchedInput;
            ushort latchedOutput;
            ushort latchedServo1;
            ushort latchedServo2;
            ushort latchedServo3;
            ushort latchedServo4;
            byte latchedError;
            string rawStatus;

            lock (robotStatusSync)
            {
                ioInput = lastRobotIoInput;
                ioOutput = lastRobotIoOutput;
                servo1 = lastRobotServo1Status;
                servo2 = lastRobotServo2Status;
                servo3 = lastRobotServo3Status;
                servo4 = lastRobotServo4Status;
                errorType = lastRobotErrorType;
                latchedInput = latchedRobotIoInputStopBits;
                latchedOutput = latchedRobotIoOutputErrorBits;
                latchedServo1 = latchedRobotServo1Faults;
                latchedServo2 = latchedRobotServo2Faults;
                latchedServo3 = latchedRobotServo3Faults;
                latchedServo4 = latchedRobotServo4Faults;
                latchedError = latchedRobotErrorType;
                rawStatus = lastRobotStatusFrameHex;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Controller status: IOin=0x{0:X4} [{1}], IOout=0x{2:X4} [{3}], ErrorType=0x{4:X2} [{5}].\r\n" +
                "Servo faults: S1=0x{6:X4} [{7}], S2=0x{8:X4} [{9}], S3=0x{10:X4} [{11}], S4=0x{12:X4} [{13}].\r\n" +
                "Latched this motion: stopInput=0x{14:X4}, outputError=0x{15:X4}, " +
                "S1=0x{16:X4}, S2=0x{17:X4}, S3=0x{18:X4}, S4=0x{19:X4}, ErrorType=0x{20:X2}.\r\n" +
                "Last raw status: {21}",
                ioInput,
                DecodeRobotInputFlags(ioInput),
                ioOutput,
                DecodeRobotOutputFlags(ioOutput),
                errorType,
                DecodeRobotErrorType(errorType),
                servo1,
                DecodeRobotServoFaults(servo1),
                servo2,
                DecodeRobotServoFaults(servo2),
                servo3,
                DecodeRobotServoFaults(servo3),
                servo4,
                DecodeRobotServoFaults(servo4),
                latchedInput,
                latchedOutput,
                latchedServo1,
                latchedServo2,
                latchedServo3,
                latchedServo4,
                latchedError,
                rawStatus);
        }

        private static string DecodeRobotInputFlags(ushort value)
        {
            List<string> flags = new List<string>();
            if ((value & (1 << 8)) != 0) flags.Add("HomeReturn");
            if ((value & (1 << 9)) != 0) flags.Add("ServoOn");
            if ((value & (1 << 10)) != 0) flags.Add("MoveEnable");
            if ((value & (1 << 11)) != 0) flags.Add("MotionStop");
            if ((value & (1 << 12)) != 0) flags.Add("EmergencyStop");
            if ((value & (1 << 13)) != 0) flags.Add("AlarmReset");
            return flags.Count == 0 ? "none" : string.Join("|", flags);
        }

        private static string DecodeRobotOutputFlags(ushort value)
        {
            List<string> flags = new List<string>();
            if ((value & (1 << 8)) != 0) flags.Add("PowerOn");
            if ((value & (1 << 9)) != 0) flags.Add("ServoOn");
            if ((value & (1 << 10)) != 0) flags.Add("MoveEnable");
            if ((value & (1 << 11)) != 0) flags.Add("Ready");
            if ((value & (1 << 12)) != 0) flags.Add("MotionEnd");
            if ((value & (1 << 13)) != 0) flags.Add("Busy");
            if ((value & (1 << 14)) != 0) flags.Add("Error");
            if ((value & (1 << 15)) != 0) flags.Add("PowerLock");
            return flags.Count == 0 ? "none" : string.Join("|", flags);
        }

        private static string DecodeRobotServoFaults(ushort value)
        {
            List<string> faults = new List<string>();
            if ((value & (1 << 0)) != 0) faults.Add("SignalForm");
            if ((value & (1 << 1)) != 0) faults.Add("SignalSum");
            if ((value & (1 << 2)) != 0) faults.Add("SignalInterval");
            if ((value & (1 << 6)) != 0) faults.Add("Other");
            if ((value & (1 << 8)) != 0) faults.Add("PositionMin");
            if ((value & (1 << 9)) != 0) faults.Add("PositionMax");
            if ((value & (1 << 10)) != 0) faults.Add("OverTemp");
            if ((value & (1 << 11)) != 0) faults.Add("OverTorque");
            if ((value & (1 << 12)) != 0) faults.Add("VoltageMin");
            if ((value & (1 << 13)) != 0) faults.Add("VoltageMax");
            if ((value & (1 << 14)) != 0) faults.Add("OverCurrent");
            if ((value & (1 << 15)) != 0) faults.Add("OverSpeed");
            return faults.Count == 0 ? "none" : string.Join("|", faults);
        }

        private static string DecodeRobotErrorType(byte value)
        {
            List<string> faults = new List<string>();
            if ((value & (1 << 4)) != 0) faults.Add("Servo");
            if ((value & (1 << 5)) != 0) faults.Add("PositionLimit");
            if ((value & (1 << 6)) != 0) faults.Add("PositionMin");
            if ((value & (1 << 7)) != 0) faults.Add("ServoLock");
            return faults.Count == 0 ? "none" : string.Join("|", faults);
        }

        private string SaveRobotFaultDiagnostic(Exception exception)
        {
            try
            {
                string directory = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "RobotFaultDiagnostics");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(
                    directory,
                    "RobotFault_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + ".txt");
                string contents =
                    "Timestamp=" + DateTime.Now.ToString("O", CultureInfo.InvariantCulture) + "\r\n" +
                    "Profile=" + ActiveRuntimeProfileId + "\r\n" +
                    "CoordinateProfile=" + ActiveCoordinateProfileId + "\r\n" +
                    "Exception=" + exception + "\r\n\r\n" +
                    GetRobotControllerStatusDiagnostics() + "\r\n" +
                    GetRobotSerialBufferDiagnostics() + "\r\n";
                File.WriteAllText(path, contents, new UTF8Encoding(true));
                return path;
            }
            catch (Exception saveException)
            {
                Debug.WriteLine("[RobotFaultDiagnostic] Save failed: " + saveException);
                return string.Empty;
            }
        }

        private bool IsRobotCycleGateReady()
        {
            lock (robotStatusSync)
            {
                if (lastRobotStatusTimestamp == 0 || BusyBit)
                {
                    return false;
                }

                double statusAgeMs =
                    (Stopwatch.GetTimestamp() - lastRobotStatusTimestamp) * 1000.0 /
                    Stopwatch.Frequency;
                if (statusAgeMs > RobotStatusFreshnessMs)
                {
                    return false;
                }

                return robotIdleReadyFrameCount >= RobotReadyRequiredFrames ||
                    robotStationaryIdleFrameCount >= RobotStationaryFallbackRequiredFrames;
            }
        }

        private async Task WaitForStableOriginReadyAsync(
            int pollDelayMs,
            string stage,
            int timeoutMs = RobotOriginReadyTimeoutMs,
            bool monitorMotionProgress = false)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            Stopwatch noProgressTimeout = Stopwatch.StartNew();
            int readyFrames = 0;
            int stationaryFrames = 0;
            int frameSequence = 0;
            double statusAgeMs = double.PositiveInfinity;
            float currentX = 0f;
            float currentY = 0f;
            float currentZ = 0f;
            bool currentBusy = true;
            bool currentMotionEnd = false;
            bool hasProgressPosition = false;
            float progressX = 0f;
            float progressY = 0f;
            float progressZ = 0f;

            while (timeout.ElapsedMilliseconds < timeoutMs)
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    throw new IOException(
                        "Delta Robot serial port closed while waiting for " + stage + ".");
                }

                long now = Stopwatch.GetTimestamp();
                lock (robotStatusSync)
                {
                    readyFrames = robotOriginReadyFrameCount;
                    stationaryFrames = robotStationaryOriginFrameCount;
                    frameSequence = robotStatusFrameSequence;
                    currentX = posX;
                    currentY = posY;
                    currentZ = posZ;
                    currentBusy = BusyBit;
                    currentMotionEnd = motionEndBit;
                    statusAgeMs = lastRobotStatusTimestamp == 0
                        ? double.PositiveInfinity
                        : (now - lastRobotStatusTimestamp) * 1000.0 / Stopwatch.Frequency;
                }

                if (readyFrames >= RobotReadyRequiredFrames &&
                    statusAgeMs <= RobotStatusFreshnessMs)
                {
                    return;
                }

                if (stationaryFrames >= RobotStationaryFallbackRequiredFrames &&
                    statusAgeMs <= RobotStatusFreshnessMs)
                {
                    tbEco.Text += string.Format(
                        CultureInfo.InvariantCulture,
                        "Robot stable-idle fallback accepted at {0}: MotionEnd={1}, Busy={2}, stableFrames={3}, XYZ=({4:F3},{5:F3},{6:F3}).\r\n",
                        stage,
                        currentMotionEnd,
                        currentBusy,
                        stationaryFrames,
                        currentX,
                        currentY,
                        currentZ);
                    tbEco.SelectionStart = tbEco.Text.Length;
                    tbEco.ScrollToCaret();
                    return;
                }

                if (monitorMotionProgress &&
                    statusAgeMs <= RobotStatusFreshnessMs)
                {
                    if (!hasProgressPosition)
                    {
                        progressX = currentX;
                        progressY = currentY;
                        progressZ = currentZ;
                        hasProgressPosition = true;
                        noProgressTimeout.Restart();
                    }
                    else
                    {
                        double progressDistance = CalculateDistance(
                            currentX,
                            currentY,
                            currentZ,
                            progressX,
                            progressY,
                            progressZ);
                        if (progressDistance >= RobotMotionProgressToleranceMm)
                        {
                            progressX = currentX;
                            progressY = currentY;
                            progressZ = currentZ;
                            noProgressTimeout.Restart();
                        }
                        else if (currentBusy &&
                            noProgressTimeout.ElapsedMilliseconds >=
                                RobotMotionNoProgressTimeoutMs)
                        {
                            string stallMessage = string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Robot motion stalled at {0}: no XYZ progress >= {1:F1} mm for {2} ms while Busy=true. " +
                                    "XYZ=({3:F3},{4:F3},{5:F3}), statusAge={6:F1} ms, sequence={7}.",
                                    stage,
                                    RobotMotionProgressToleranceMm,
                                    RobotMotionNoProgressTimeoutMs,
                                    currentX,
                                    currentY,
                                    currentZ,
                                    statusAgeMs,
                                    frameSequence);
                            throw new TimeoutException(
                                stallMessage + "\r\n" +
                                GetRobotControllerStatusDiagnostics() + "\r\n" +
                                GetRobotSerialBufferDiagnostics());
                        }
                        else if (!currentBusy)
                        {
                            // Busy=false is handled by the normal/stationary
                            // completion tests above, not by the motion-stall timer.
                            noProgressTimeout.Restart();
                        }
                    }
                }

                await Task.Delay(Math.Max(1, Math.Min(pollDelayMs, 50)));
            }

            throw new TimeoutException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Robot Ready timeout at {0} after {1} ms. motionEndFrames={2}/{3}, stableFrames={4}/{5}, statusAge={6:F1} ms, " +
                    "MotionEnd={7}, Busy={8}, XYZ=({9:F3},{10:F3},{11:F3}), Origin tolerance={12:F1} mm, sequence={13}.",
                    stage,
                    timeoutMs,
                    readyFrames,
                    RobotReadyRequiredFrames,
                    stationaryFrames,
                    RobotStationaryFallbackRequiredFrames,
                    statusAgeMs,
                    currentMotionEnd,
                    currentBusy,
                    currentX,
                    currentY,
                    currentZ,
                    RobotOriginToleranceMm,
                    frameSequence) + "\r\n" +
                GetRobotControllerStatusDiagnostics() + "\r\n" +
                GetRobotSerialBufferDiagnostics());
        }

        private async Task WaitForStableRobotIdleAsync(int pollDelayMs, string stage)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            int readyFrames = 0;
            int stationaryFrames = 0;
            int frameSequence = 0;
            double statusAgeMs = double.PositiveInfinity;
            float currentX = 0f;
            float currentY = 0f;
            float currentZ = 0f;
            bool currentBusy = true;
            bool currentMotionEnd = false;

            while (timeout.ElapsedMilliseconds < RobotOriginReadyTimeoutMs)
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    throw new IOException(
                        "Delta Robot serial port closed while waiting for " + stage + ".");
                }

                long now = Stopwatch.GetTimestamp();
                lock (robotStatusSync)
                {
                    readyFrames = robotIdleReadyFrameCount;
                    stationaryFrames = robotStationaryIdleFrameCount;
                    frameSequence = robotStatusFrameSequence;
                    currentX = posX;
                    currentY = posY;
                    currentZ = posZ;
                    currentBusy = BusyBit;
                    currentMotionEnd = motionEndBit;
                    statusAgeMs = lastRobotStatusTimestamp == 0
                        ? double.PositiveInfinity
                        : (now - lastRobotStatusTimestamp) * 1000.0 / Stopwatch.Frequency;
                }

                if (readyFrames >= RobotReadyRequiredFrames &&
                    statusAgeMs <= RobotStatusFreshnessMs)
                {
                    return;
                }

                if (stationaryFrames >= RobotStationaryFallbackRequiredFrames &&
                    statusAgeMs <= RobotStatusFreshnessMs)
                {
                    tbEco.Text += string.Format(
                        CultureInfo.InvariantCulture,
                        "Robot stable-idle fallback accepted at {0}: MotionEnd={1}, Busy={2}, stableFrames={3}, XYZ=({4:F3},{5:F3},{6:F3}).\r\n",
                        stage,
                        currentMotionEnd,
                        currentBusy,
                        stationaryFrames,
                        currentX,
                        currentY,
                        currentZ);
                    tbEco.SelectionStart = tbEco.Text.Length;
                    tbEco.ScrollToCaret();
                    return;
                }

                await Task.Delay(Math.Max(1, Math.Min(pollDelayMs, 50)));
            }

            throw new TimeoutException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Robot Idle timeout at {0}. motionEndFrames={1}/{2}, stableFrames={3}/{4}, statusAge={5:F1} ms, " +
                    "MotionEnd={6}, Busy={7}, XYZ=({8:F3},{9:F3},{10:F3}), sequence={11}.",
                    stage,
                    readyFrames,
                    RobotReadyRequiredFrames,
                    stationaryFrames,
                    RobotStationaryFallbackRequiredFrames,
                    statusAgeMs,
                    currentMotionEnd,
                    currentBusy,
                    currentX,
                    currentY,
                    currentZ,
                    frameSequence));
        }

        private async Task EnsureSafeOriginBeforeCaptureAsync(
            int pollDelayMs,
            int speed)
        {
            // Return an idle, empty gripper to Origin before capture.
            await WaitForStableRobotIdleAsync(
                pollDelayMs,
                "before automatic Safe Origin");

            float currentX;
            float currentY;
            float currentZ;
            int originFrames;
            int stationaryOriginFrames;
            long statusTimestamp;
            lock (robotStatusSync)
            {
                currentX = posX;
                currentY = posY;
                currentZ = posZ;
                originFrames = robotOriginReadyFrameCount;
                stationaryOriginFrames = robotStationaryOriginFrameCount;
                statusTimestamp = lastRobotStatusTimestamp;
            }

            double statusAgeMs = statusTimestamp == 0
                ? double.PositiveInfinity
                : (Stopwatch.GetTimestamp() - statusTimestamp) * 1000.0 /
                    Stopwatch.Frequency;
            if ((originFrames >= RobotReadyRequiredFrames ||
                    stationaryOriginFrames >= RobotStationaryFallbackRequiredFrames) &&
                statusAgeMs <= RobotStatusFreshnessMs)
            {
                return;
            }

            if (serialPort == null || !serialPort.IsOpen)
            {
                throw new IOException(
                    "Delta Robot serial port closed before automatic Safe Origin.");
            }

            float safeTravelZ = Math.Min(currentZ, z_pass);
            tbEco.Text += string.Format(
                CultureInfo.InvariantCulture,
                "Safe Origin: current=({0:F3},{1:F3},{2:F3}), liftZ={3:F3}.\r\n",
                currentX,
                currentY,
                currentZ,
                safeTravelZ);
            tbEco.SelectionStart = tbEco.Text.Length;
            tbEco.ScrollToCaret();

            createMotion(
                100,
                101,
                x: currentX,
                y: currentY,
                z: safeTravelZ,
                v: speed,
                run_io: 0,
                end_io: 0);
            createMotion(
                101,
                32767,
                x: x_ori,
                y: y_ori,
                z: z_ori,
                v: speed,
                run_io: 0,
                end_io: 0);

            ResetRobotOriginReadyEvidence();
            byte[] safeOriginProtocol = Make_Protocol(100);
            WriteRobotCommandFrame(
                safeOriginProtocol,
                "Safe Origin execute motion chain 100",
                0);

            await WaitForStableOriginReadyAsync(
                pollDelayMs,
                "automatic Safe Origin");

            tbEco.Text += "Safe Origin reached; capture is now allowed.\r\n";
            tbEco.SelectionStart = tbEco.Text.Length;
            tbEco.ScrollToCaret();
        }

        private async void UpdateXYZPositions()
        {
            while (true)
            {
                try
                {
                    if (serialPort.IsOpen && serialPort.BytesToRead > 0)
                    {
                        int available = serialPort.BytesToRead;
                        byte[] incoming = new byte[available];
                        int bytesRead = serialPort.Read(incoming, 0, incoming.Length);
                        if (bytesRead > 0)
                        {
                            if (bytesRead != incoming.Length)
                            {
                                Array.Resize(ref incoming, bytesRead);
                            }
                            ProcessRobotStatusBytes(incoming);
                        }
                    }
                    else if (!serialPort.IsOpen)
                    {
                        ResetRobotOriginReadyEvidence();
                        bool updateClosedState;
                        lock (robotStatusSync)
                        {
                            updateClosedState = !robotPortClosedStateShown;
                            robotPortClosedStateShown = true;
                        }
                        if (updateClosedState && !IsDisposed && !Disposing && IsHandleCreated)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                pbMotion.BackColor = Color.Transparent;
                                pbBusy.BackColor = Color.Transparent;
                            }));
                        }
                    }
                }
                catch (IOException)
                {
                    ResetRobotOriginReadyEvidence();
                }
                catch (InvalidOperationException)
                {
                    ResetRobotOriginReadyEvidence();
                }
                await Task.Delay(delaytoupdate);
            }
        }

        private void ProcessRobotStatusBytes(byte[] incoming)
        {
            float displayX = 0f;
            float displayY = 0f;
            float displayZ = 0f;
            bool displayMotionEnd = false;
            bool displayBusy = false;
            bool statusUpdated = false;

            lock (robotStatusSync)
            {
                robotReceiveBuffer.AddRange(incoming);

                while (true)
                {
                    int headerIndex = robotReceiveBuffer.IndexOf(0x96);
                    if (headerIndex < 0)
                    {
                        robotReceiveBuffer.Clear();
                        break;
                    }
                    if (headerIndex > 0)
                    {
                        robotReceiveBuffer.RemoveRange(0, headerIndex);
                    }
                    if (robotReceiveBuffer.Count < 4)
                    {
                        break;
                    }

                    int frameLength = robotReceiveBuffer[3] + 5;
                    if (frameLength < 5 || frameLength > MaximumRobotFrameLength)
                    {
                        robotReceiveBuffer.RemoveAt(0);
                        continue;
                    }
                    if (robotReceiveBuffer.Count < frameLength)
                    {
                        break;
                    }

                    byte[] frame = robotReceiveBuffer.GetRange(0, frameLength).ToArray();
                    int checksum = 0;
                    for (int i = 1; i < frame.Length - 1; i++)
                    {
                        checksum = (checksum + frame[i]) & 0xFF;
                    }
                    if ((byte)checksum != frame[frame.Length - 1])
                    {
                        robotReceiveBuffer.RemoveAt(0);
                        continue;
                    }

                    robotReceiveBuffer.RemoveRange(0, frameLength);
                    byte frameCommand = frame[2];
                    long commandCount;
                    robotFrameCommandCounts.TryGetValue(
                        frameCommand,
                        out commandCount);
                    robotFrameCommandCounts[frameCommand] = commandCount + 1;
                    if (frame.Length < 33 || frame[2] != 0x00)
                    {
                        validRobotNonStatusFrameCount++;
                        int diagnosticLength = Math.Min(frame.Length, 64);
                        lastRobotNonStatusFrameHex = BitConverter.ToString(
                            frame,
                            0,
                            diagnosticLength);
                        continue;
                    }

                    float parsedX = BitConverter.ToSingle(frame, 10);
                    float parsedY = BitConverter.ToSingle(frame, 16);
                    float parsedZ = BitConverter.ToSingle(frame, 22);
                    if (float.IsNaN(parsedX) || float.IsInfinity(parsedX) ||
                        float.IsNaN(parsedY) || float.IsInfinity(parsedY) ||
                        float.IsNaN(parsedZ) || float.IsInfinity(parsedZ))
                    {
                        continue;
                    }

                    // Protocol_Map_Delta_20190717_1.xlsx, 4-axis interleaved
                    // automatic status frame (34 bytes):
                    // IO input=4, IO output=6, S1=8, X=10, S2=14, Y=16,
                    // S3=20, Z=22, S4=26, R=28, ErrorType=32.
                    ushort ioInput = BitConverter.ToUInt16(frame, 4);
                    ushort ioOutput = BitConverter.ToUInt16(frame, 6);
                    ushort servo1Status = BitConverter.ToUInt16(frame, 8);
                    ushort servo2Status = BitConverter.ToUInt16(frame, 14);
                    ushort servo3Status = BitConverter.ToUInt16(frame, 20);
                    ushort servo4Status = frame.Length >= 34
                        ? BitConverter.ToUInt16(frame, 26)
                        : (ushort)0;
                    float parsedR = frame.Length >= 34
                        ? BitConverter.ToSingle(frame, 28)
                        : 0f;
                    byte errorType = frame.Length >= 34 ? frame[32] : (byte)0;
                    validRobotStatusFrameCount++;
                    lastRobotStatusFrameHex = BitConverter.ToString(frame);
                    lastRobotIoInput = ioInput;
                    lastRobotIoOutput = ioOutput;
                    lastRobotServo1Status = servo1Status;
                    lastRobotServo2Status = servo2Status;
                    lastRobotServo3Status = servo3Status;
                    lastRobotServo4Status = servo4Status;
                    lastRobotErrorType = errorType;
                    latchedRobotIoInputStopBits |= (ushort)(ioInput & 0x1800);
                    latchedRobotIoOutputErrorBits |= (ushort)(ioOutput & 0x4000);
                    latchedRobotServo1Faults |= (ushort)(servo1Status & RobotServoFaultMask);
                    latchedRobotServo2Faults |= (ushort)(servo2Status & RobotServoFaultMask);
                    latchedRobotServo3Faults |= (ushort)(servo3Status & RobotServoFaultMask);
                    latchedRobotServo4Faults |= (ushort)(servo4Status & RobotServoFaultMask);
                    latchedRobotErrorType |= errorType;
                    bool parsedMotionEnd = (ioOutput & (1 << 12)) != 0;
                    bool parsedBusy = (ioOutput & (1 << 13)) != 0;
                    double originDistance = CalculateDistance(
                        parsedX,
                        parsedY,
                        parsedZ,
                        x_ori,
                        y_ori,
                        z_ori);
                    double positionDelta = hasPreviousRobotStatusPosition
                        ? CalculateDistance(
                            parsedX,
                            parsedY,
                            parsedZ,
                            previousRobotStatusX,
                            previousRobotStatusY,
                            previousRobotStatusZ)
                        : double.PositiveInfinity;
                    bool stationaryPosition =
                        positionDelta <= RobotStationaryFrameToleranceMm;

                    posX = parsedX;
                    posY = parsedY;
                    posZ = parsedZ;
                    posR = parsedR;
                    motionEndBit = parsedMotionEnd;
                    BusyBit = parsedBusy;
                    robotStatusFrameSequence++;
                    lastRobotStatusTimestamp = Stopwatch.GetTimestamp();
                    robotPortClosedStateShown = false;

                    if (!parsedBusy && parsedMotionEnd)
                    {
                        robotIdleReadyFrameCount = Math.Min(
                            RobotReadyRequiredFrames,
                            robotIdleReadyFrameCount + 1);
                    }
                    else
                    {
                        robotIdleReadyFrameCount = 0;
                    }

                    if (robotIdleReadyFrameCount > 0 &&
                        originDistance <= RobotOriginToleranceMm)
                    {
                        robotOriginReadyFrameCount = Math.Min(
                            RobotReadyRequiredFrames,
                            robotOriginReadyFrameCount + 1);
                    }
                    else
                    {
                        robotOriginReadyFrameCount = 0;
                    }

            // Require consecutive stationary frames when MotionEnd remains false.
                    if (!parsedBusy && stationaryPosition)
                    {
                        robotStationaryIdleFrameCount = Math.Min(
                            RobotStationaryFallbackRequiredFrames,
                            robotStationaryIdleFrameCount + 1);
                    }
                    else
                    {
                        robotStationaryIdleFrameCount = 0;
                    }

                    if (!parsedBusy && stationaryPosition &&
                        originDistance <= RobotOriginToleranceMm)
                    {
                        robotStationaryOriginFrameCount = Math.Min(
                            RobotStationaryFallbackRequiredFrames,
                            robotStationaryOriginFrameCount + 1);
                    }
                    else
                    {
                        robotStationaryOriginFrameCount = 0;
                    }

                    previousRobotStatusX = parsedX;
                    previousRobotStatusY = parsedY;
                    previousRobotStatusZ = parsedZ;
                    hasPreviousRobotStatusPosition = true;

                    displayX = parsedX;
                    displayY = parsedY;
                    displayZ = parsedZ;
                    displayMotionEnd = parsedMotionEnd;
                    displayBusy = parsedBusy;
                    statusUpdated = true;
                }
            }

            if (statusUpdated && !IsDisposed && !Disposing && IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    tbposX.Text = displayX.ToString();
                    tbposY.Text = displayY.ToString();
                    tbposZ.Text = displayZ.ToString();
                    pbMotion.BackColor = displayMotionEnd
                        ? Color.Red
                        : Color.Transparent;
                    pbBusy.BackColor = displayBusy
                        ? Color.Red
                        : Color.Transparent;
                }));
            }
        }
        private double CalculateDistance(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
        }
        private void cbPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedPortName = cbPort.SelectedItem.ToString();
        }
        private async void btConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbPort.SelectedItem == null)
                {
                    throw new InvalidOperationException(
                        "Select the Delta Robot COM port first (normally COM3)." );
                }

                if (serialPort.IsOpen) { serialPort.Close(); }
                selectedPortName = cbPort.SelectedItem.ToString();
                serialPort.PortName = selectedPortName;
                serialPort.Open();
                lock (robotStatusSync)
                {
                    robotReceiveBuffer.Clear();
                    robotFrameCommandCounts.Clear();
                    robotIdleReadyFrameCount = 0;
                    robotOriginReadyFrameCount = 0;
                    robotStationaryIdleFrameCount = 0;
                    robotStationaryOriginFrameCount = 0;
                    hasPreviousRobotStatusPosition = false;
                    validRobotStatusFrameCount = 0;
                    validRobotNonStatusFrameCount = 0;
                    lastRobotNonStatusFrameHex = "<none>";
                    lastRobotStatusFrameHex = "<none>";
                    lastRobotIoInput = 0;
                    lastRobotIoOutput = 0;
                    lastRobotServo1Status = 0;
                    lastRobotServo2Status = 0;
                    lastRobotServo3Status = 0;
                    lastRobotServo4Status = 0;
                    lastRobotErrorType = 0;
                    lastRobotStatusTimestamp = 0;
                    robotPortClosedStateShown = false;
                }

                btConnect.Text = "Connected";
                AppendRobotLog("Delta Robot connected: " + selectedPortName + ".");

                bool statusAvailable = await EnsureRobotStatusStreamingAsync(
                    "after COM connection",
                    showFailureDialog: true);
                if (statusAvailable)
                {
                    AppendCurrentRobotControllerState("Connected controller state");
                }
            }
            catch (Exception ex)
            {
                btConnect.Text = "Connect";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AppendRobotLog(string message)
        {
            if (tbEco.InvokeRequired)
            {
                tbEco.BeginInvoke(new Action<string>(AppendRobotLog), message);
                return;
            }

            tbEco.Text += message + "\r\n";
            tbEco.SelectionStart = tbEco.Text.Length;
            tbEco.ScrollToCaret();
        }

        private bool TryGetFreshRobotStatus(
            out ushort ioInput,
            out ushort ioOutput,
            out double statusAgeMs)
        {
            long timestamp;
            lock (robotStatusSync)
            {
                ioInput = lastRobotIoInput;
                ioOutput = lastRobotIoOutput;
                timestamp = lastRobotStatusTimestamp;
            }

            statusAgeMs = timestamp == 0
                ? double.PositiveInfinity
                : (Stopwatch.GetTimestamp() - timestamp) * 1000.0 /
                    Stopwatch.Frequency;
            return timestamp != 0 && statusAgeMs <= RobotStatusFreshnessMs;
        }

        private async Task<bool> WaitForFreshRobotStatusAsync(int timeoutMs)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < timeoutMs)
            {
                ushort ignoredInput;
                ushort ignoredOutput;
                double ignoredAge;
                if (TryGetFreshRobotStatus(
                    out ignoredInput,
                    out ignoredOutput,
                    out ignoredAge))
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return false;
        }

        private async Task<bool> EnsureRobotStatusStreamingAsync(
            string stage,
            bool showFailureDialog)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                return false;
            }

            if (await WaitForFreshRobotStatusAsync(RobotStatusInitialProbeMs))
            {
                return true;
            }

            // Protocol 0x84 toggles status streaming; send only after a timeout.
            AppendRobotLog(
                "No realtime robot status received " + stage +
                "; sending exactly one status-stream toggle.");
            WriteRobotCommandFrame(
                protocol_RealtimePositionONOFF,
                "Recover realtime robot status stream",
                0);

            if (await WaitForFreshRobotStatusAsync(RobotStatusRecoveryTimeoutMs))
            {
                AppendRobotLog("Realtime robot status stream is active.");
                return true;
            }

            string message =
                "COM3 is open, but no valid realtime robot status frame was received. " +
                "No robot motion was authorized. Check controller power/cable and the controller display. " +
                "Do not press Pos Read repeatedly because it is an ON/OFF toggle.";
            AppendRobotLog(message);
            if (showFailureDialog)
            {
                MessageBox.Show(
                    message,
                    "Delta Robot status unavailable - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            return false;
        }

        private void AppendCurrentRobotControllerState(string prefix)
        {
            ushort ioInput;
            ushort ioOutput;
            double statusAgeMs;
            bool fresh = TryGetFreshRobotStatus(
                out ioInput,
                out ioOutput,
                out statusAgeMs);
            AppendRobotLog(string.Format(
                CultureInfo.InvariantCulture,
                "{0}: fresh={1}, age={2:F1} ms, IOin=0x{3:X4} [{4}], IOout=0x{5:X4} [{6}].",
                prefix,
                fresh,
                statusAgeMs,
                ioInput,
                DecodeRobotInputFlags(ioInput),
                ioOutput,
                DecodeRobotOutputFlags(ioOutput)));
        }

        private async Task<bool> EnsureRobotControllerReadyForStartAsync(string stage)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                string connectionMessage =
                    "Delta Robot COM is not connected. Select COM3 and press the Delta Robot Connect button. " +
                    "No robot motion was sent.";
                AppendRobotLog(connectionMessage);
                MessageBox.Show(
                    connectionMessage,
                    "Delta Robot connection required - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (!await EnsureRobotStatusStreamingAsync(
                "before " + stage,
                showFailureDialog: false))
            {
                string statusMessage =
                    "Robot status is unavailable. Reconnect COM3 and verify the controller before " +
                    stage + ". No robot motion was sent.";
                MessageBox.Show(
                    statusMessage,
                    "Delta Robot status required - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            ushort ioInput;
            ushort ioOutput;
            double statusAgeMs;
            TryGetFreshRobotStatus(out ioInput, out ioOutput, out statusAgeMs);

            List<string> blockers = new List<string>();
            if ((ioOutput & (1 << 14)) != 0)
            {
                blockers.Add("controller Error is active (remove the cause, then use Alarm Reset)");
            }
            if ((ioOutput & (1 << 8)) == 0)
            {
                blockers.Add("PowerOn is OFF (press Robot ON)");
            }
            if ((ioOutput & (1 << 9)) == 0)
            {
                blockers.Add("ServoOn is OFF (confirm Robot ON/controller state)");
            }
            if ((ioOutput & (1 << 10)) == 0)
            {
                blockers.Add("MoveEnable is OFF (press Move Enable)");
            }
            if ((ioOutput & (1 << 13)) != 0)
            {
                blockers.Add(
                    "Busy remains ON; the previous controller motion may still be stalled. " +
                    "Do not resend motion. Use the controller's verified Stop/Reset procedure or safely restart the robot controller");
            }

            if (blockers.Count > 0)
            {
                string blockedMessage =
                    "Robot " + stage + " was blocked before motion:\r\n- " +
                    string.Join("\r\n- ", blockers) +
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "\r\nStatus age={0:F1} ms, IOout=0x{1:X4} [{2}].",
                        statusAgeMs,
                        ioOutput,
                        DecodeRobotOutputFlags(ioOutput));
                AppendRobotLog(blockedMessage.Replace("\r\n", " | "));
                MessageBox.Show(
                    blockedMessage,
                    "Robot controller is not ready - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            AppendCurrentRobotControllerState("Robot " + stage + " preflight passed");
            return true;
        }
        private void cbPort2_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedPortName2 = cbPort2.SelectedItem.ToString();
        }

        private bool EnsureJetsonTtlConnectedForStart()
        {
            try
            {
                bool newlyOpened = OpenJetsonTtlPort();
                if (newlyOpened)
                {
                    tbEco.Text += "Jetson TTL connected automatically: " + selectedPortName2 + "\r\n";
                    tbEco.SelectionStart = tbEco.Text.Length;
                    tbEco.ScrollToCaret();
                }
                return true;
            }
            catch (Exception ex)
            {
                Working = false;
                btConnect2.Text = "Connect";
                string message =
                    "Jetson TTL " + (string.IsNullOrWhiteSpace(selectedPortName2) ? "port" : selectedPortName2) +
                    " could not be opened. Robot cycle was not started.\r\n" + ex.Message;
                tbEco.Text += message + "\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
                MessageBox.Show(
                    message,
                    "Jetson TTL connection required - No Robot Motion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private bool OpenJetsonTtlPort()
        {
            string portName = cbPort2.SelectedItem == null
                ? selectedPortName2
                : cbPort2.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new InvalidOperationException("Select the Jetson TTL COM port first.");
            }

            lock (jetsonTtlSync)
            {
                if (serialPort2 == null)
                {
                    throw new InvalidOperationException("Jetson TTL serial port is not initialized.");
                }

                if (serialPort2.IsOpen)
                {
                    if (string.Equals(serialPort2.PortName, portName, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPortName2 = portName;
                        btConnect2.Text = "Connected";
                        return false;
                    }

                    serialPort2.Close();
                }

                serialPort2.PortName = portName;
                serialPort2.Open();
                serialPort2.DiscardInBuffer();
                selectedPortName2 = portName;
                btConnect2.Text = "Connected";
                return true;
            }
        }

        private void btConnect2_Click(object sender, EventArgs e)
        {
            try
            {
                bool newlyOpened = OpenJetsonTtlPort();
                tbEco.Text += newlyOpened
                    ? "Jetson TTL connected: " + selectedPortName2 + "\r\n"
                    : "Jetson TTL is already connected: " + selectedPortName2 + "\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
            catch (Exception ex)
            {
                btConnect2.Text = "Connect";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TrySendManualRobotCommand(byte[] command, string operation)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                string message =
                    operation + " was not sent because the Delta Robot COM port is closed. " +
                    "Select COM3 and press Connect first.";
                AppendRobotLog(message);
                MessageBox.Show(
                    message,
                    "Delta Robot is not connected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                WriteRobotCommandFrame(command, operation, 0);
                AppendRobotLog(
                    operation +
                    " command sent; controller acceptance must be confirmed from the status flags.");
                return true;
            }
            catch (Exception ex)
            {
                string message = operation + " failed: " + ex.Message;
                AppendRobotLog(message);
                MessageBox.Show(
                    message,
                    "Delta Robot command failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private async void btPosRead_Click(object sender, EventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show(
                    "Connect the Delta Robot COM port first (normally COM3).",
                    "Delta Robot is not connected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            ushort ignoredInput;
            ushort ignoredOutput;
            double statusAgeMs;
            if (TryGetFreshRobotStatus(
                out ignoredInput,
                out ignoredOutput,
                out statusAgeMs))
            {
                AppendRobotLog(
                    "Pos Read: realtime status is already active; toggle was not sent.");
                AppendCurrentRobotControllerState("Current controller state");
                return;
            }

            bool statusAvailable = await EnsureRobotStatusStreamingAsync(
                "after Pos Read",
                showFailureDialog: true);
            if (statusAvailable)
            {
                AppendCurrentRobotControllerState("Pos Read controller state");
            }
        }
        private void btrobotON_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_RobotON, "Robot ON");
        }
        private void btrobotOFF_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_RobotOFF, "Robot OFF");
        }
        private void btAR_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_AlramReset, "Alarm reset");
        }
        private void btZero_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_ZeroSet, "Zero setting");
        }
        private void btStop_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "The GUI Emergency STOP command is not enabled in this program. " +
                "Use the physical E-stop or the verified robot-controller stop control.",
                "Use physical E-stop",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        private void btOrigin_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_Origin, "Origin command");
        }

        private void btGrippingMotion_Click(object sender, EventArgs e)
        {
            int delaytime = int.Parse(DelayTime.Text) ;
            int waitTime = int.Parse(WaitDelay.Text);
            int speed = int.Parse(robot_v.Text);
            byte[] protocol_100 = Make_Protocol(100);

            float tar_x = 0f;
            float tar_y = 160f;
            float tar_z = 550f;

            if (serialPort.IsOpen)
            {
                createMotion(motionNum_int: 100, nextMotion_int: 101,    x: x_pass, y: y_pass, z: z_pass, v: speed, run_io: 0, end_io: 0);
                createMotion(motionNum_int: 101, nextMotion_int: 102,    x: tar_x, y: tar_y, z: z_ref, v: speed, run_io: 0, end_io: 0);
                createMotion(motionNum_int: 102, nextMotion_int: 103,    x: tar_x, y: tar_y, z: tar_z, v: Math.Min(speed, PickupApproachSpeedMax), run_io: 0, end_io: 1);
                createMotion(motionNum_int: 103, nextMotion_int: 104,    x: tar_x, y: tar_y, z: z_ref, v: Math.Min(speed, LoadedLiftSpeedMax), run_io: 1, end_io: 1);
                createMotion(motionNum_int: 104, nextMotion_int: 105,    x: x_pass, y: y_pass, z: z_pass, v: Math.Min(speed, LoadedTravelSpeedMax), run_io: 1, end_io: 1);
                createMotion(motionNum_int: 105, nextMotion_int: 32767 , x: x_ori, y: y_ori, z: z_ori, v: Math.Min(speed, LoadedTravelSpeedMax), run_io: 1, end_io: 0);

                WriteRobotCommandFrame(
                    protocol_100,
                    "Manual gripping execute motion chain 100",
                    0);
            }
        }

        private void btMove_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_MoveEnable, "Move enable");
        }
        private void btNotmove_Click(object sender, EventArgs e)
        {
            TrySendManualRobotCommand(protocol_MoveDisable, "Move disable");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            motionEndBit = true;
            Task.Run(() => UpdateXYZPositions());
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            formClosing = true;
            CloseCameraConnection();
            GripImagePreviewForm previewForm = gripImagePreviewForm;
            gripImagePreviewForm = null;
            if (previewForm != null && !previewForm.IsDisposed)
            {
                previewForm.Close();
            }
            try { serialPort.Close(); } catch { }
            try { serialPort2.Close(); } catch { }
            base.OnFormClosing(e);
        }

        private void btstopOn_Click(object sender, EventArgs e)
        {
            Stopper_operation();
        }

        private void btGrip_Click(object sender, EventArgs e)
        {
            Gripping();
        }

        private void btUngrip_Click(object sender, EventArgs e)
        {
            try
            {
                Ungripping();
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ungrip was not sent",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void tb1Clear_Click(object sender, EventArgs e)
        {
            tbEco.Text = null;
        }

        private void tb2Clear_Click(object sender, EventArgs e)
        {
            tbvision.Text = null;
        }
    
        private void Ungripping()
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                throw new InvalidOperationException(
                    "Delta Robot COM is closed. Ungrip command was not sent. Use the hardware-safe release procedure before reconnecting.");
            }

            WriteRobotCommandFrame(
                protocol_Ungripping,
                "Ungrip",
                0);
        }

        private void WriteRobotCommandFrame(
            byte[] command,
            string operation,
            int postTransmitGapMs)
        {
            if (command == null || command.Length == 0)
            {
                throw new ArgumentException(
                    "Robot command frame must not be empty.",
                    nameof(command));
            }

            lock (robotCommandSync)
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    throw new InvalidOperationException(
                        "Delta Robot COM is closed. " + operation + " was not sent.");
                }

                WaitForRobotTransmitDrainBlocking(operation + " pre-write");
                serialPort.Write(command, 0, command.Length);
                robotCommandWriteSequence++;
                lastRobotCommandName = operation;
                lastRobotCommandTimestamp = Stopwatch.GetTimestamp();
                maximumObservedRobotTxBytes = Math.Max(
                    maximumObservedRobotTxBytes,
                    serialPort.BytesToWrite);
                WaitForRobotTransmitDrainBlocking(operation + " post-write");

                if (postTransmitGapMs > 0)
                {
                    Thread.Sleep(postTransmitGapMs);
                }
            }
        }

        private void WaitForRobotTransmitDrainBlocking(string stage)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (serialPort.BytesToWrite > 0)
            {
                maximumObservedRobotTxBytes = Math.Max(
                    maximumObservedRobotTxBytes,
                    serialPort.BytesToWrite);
                if (timeout.ElapsedMilliseconds >= RobotCommandDrainTimeoutMs)
                {
                    throw new TimeoutException(
                        "Robot COM transmit buffer did not drain at " + stage +
                        " within " + RobotCommandDrainTimeoutMs + " ms. " +
                        "BytesToWrite=" + serialPort.BytesToWrite + ".");
                }
                Thread.Sleep(RobotSerialDrainPollMs);
            }
        }

        private string GetRobotSerialBufferDiagnostics()
        {
            int txBytes = -1;
            int rxBytes = -1;
            int parserBytes;
            long commandSequence;
            string commandName;
            long commandTimestamp;
            int maximumTxBytes;
            long statusFrames;
            long nonStatusFrames;
            ushort ioOutput;
            string responseCounts;
            string nonStatusHex;

            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    txBytes = serialPort.BytesToWrite;
                    rxBytes = serialPort.BytesToRead;
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is InvalidOperationException)
            {
                // Keep the original stall diagnostic when buffer inspection fails.
            }

            lock (robotStatusSync)
            {
                parserBytes = robotReceiveBuffer.Count;
                statusFrames = validRobotStatusFrameCount;
                nonStatusFrames = validRobotNonStatusFrameCount;
                ioOutput = lastRobotIoOutput;
                responseCounts = string.Join(
                    ",",
                    robotFrameCommandCounts
                        .OrderBy(pair => pair.Key)
                        .Select(pair => string.Format(
                            CultureInfo.InvariantCulture,
                            "0x{0:X2}:{1}",
                            pair.Key,
                            pair.Value)));
                nonStatusHex = lastRobotNonStatusFrameHex;
            }
            lock (robotCommandSync)
            {
                commandSequence = robotCommandWriteSequence;
                commandName = lastRobotCommandName;
                commandTimestamp = lastRobotCommandTimestamp;
                maximumTxBytes = maximumObservedRobotTxBytes;
            }

            double commandAgeMs = commandTimestamp == 0
                ? double.PositiveInfinity
                : (Stopwatch.GetTimestamp() - commandTimestamp) * 1000.0 /
                    Stopwatch.Frequency;
            return string.Format(
                CultureInfo.InvariantCulture,
                "COM3 buffers: tx={0}, rx={1}, parser={2}, maxObservedTx={3}; lastCommand=#{4} {5}, age={6:F1} ms.\r\n" +
                "COM3 frames: status={7}, nonStatus={8}, commands=[{9}], lastIO=0x{10:X4}, lastNonStatus={11}.",
                txBytes,
                rxBytes,
                parserBytes,
                maximumTxBytes,
                commandSequence,
                commandName,
                commandAgeMs,
                statusFrames,
                nonStatusFrames,
                responseCounts,
                ioOutput,
                nonStatusHex);
        }

        private async Task WaitForRobotTransmitDrainAsync(
            int pollDelayMs,
            string operation)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < RobotCommandDrainTimeoutMs)
            {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    throw new IOException(
                        "Delta Robot COM closed before " + operation + " was transmitted.");
                }
                if (serialPort.BytesToWrite == 0)
                {
                    return;
                }
                await Task.Delay(Math.Max(1, Math.Min(pollDelayMs, 20)));
            }

            throw new TimeoutException(
                operation + " remained in the Delta Robot serial transmit buffer for more than " +
                RobotCommandDrainTimeoutMs + " ms.");
        }
        private void Gripping()
        {
            if (serialPort.IsOpen)
            {
                WriteRobotCommandFrame(
                    protocol_Gripping,
                    "Grip",
                    0);
            }
        }

        private void CameraDisconnect_Click(object sender, EventArgs e)
        {
            CloseCameraConnection();
        }
        private void SerClose_Click(object sender, EventArgs e)
        {
            if (serialPort.IsOpen) { serialPort.Close(); }
        }

        private void Ser2Close_Click(object sender, EventArgs e)
        {
            lock (jetsonTtlSync)
            {
                if (serialPort2.IsOpen) { serialPort2.Close(); }
            }
            btConnect2.Text = "Connect";
            tbEco.Text += "Jetson TTL disconnected.\r\n";
            tbEco.SelectionStart = tbEco.Text.Length;
            tbEco.ScrollToCaret();
        }

        private byte[] Make_Protocol (int motion_num)
        {
            byte startNo_x = (byte)(motion_num / 0x100); ;
            byte startNo_y = (byte)(motion_num % 0x100);
            byte checkSum = (byte)((0x62 + 0x2 + startNo_y + startNo_x) % 0x100);
            byte[] protocol_100 = { 0x96, 0x0, 0x62, 0x2, startNo_y, startNo_x, checkSum };
            return protocol_100;
        }
        private void SingleMove(float? x = null, float? y = null, float? z = null, int? v = null, int? run_io = null, int? end_io = null) 
        {
            if (serialPort.IsOpen)
            {
                int speedInt;
                if (v.HasValue) { speedInt = v.Value; }
                else { speedInt = 20; }
                byte speed = (byte)speedInt;

                byte[] byteX;
                if (x.HasValue) { byteX = BitConverter.GetBytes(x.Value); }
                else { byteX = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

                byte[] byteY;
                if (y.HasValue) { byteY = BitConverter.GetBytes(y.Value); }
                else { byteY = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

                byte[] byteZ;
                if (z.HasValue) { byteZ = BitConverter.GetBytes(z.Value); }
                else { byteZ = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

                int run_io_int;
                if (run_io.HasValue) { run_io_int = run_io.Value; }
                else { run_io_int = 0; }
                byte run_io_byte = (byte)run_io_int;

                int end_io_int;
                if (end_io.HasValue) { end_io_int = end_io.Value; }
                else { end_io_int = 0; }
                byte end_io_byte = (byte)end_io_int;

                int x1 = byteX[0];
                int x2 = byteX[1];
                int x3 = byteX[2];
                int x4 = byteX[3];

                int y1 = byteY[0];
                int y2 = byteY[1];
                int y3 = byteY[2];
                int y4 = byteY[3];

                int z1 = byteZ[0];
                int z2 = byteZ[1];
                int z3 = byteZ[2];
                int z4 = byteZ[3];

                int checksumInt = 
                    (x1 + x2 + x3 + x4 + 
                    y1 + y2 + y3 + y4 + 
                    z1 + z2 + z3 + z4 + 
                    0xff + 0xff + 0xff + 0x7f + 
                    0x5E + 0x32 + 0x1E + speedInt + 
                    0xFF + 0x7F + 
                    0xFF + 0x7F + 
                    0xFF + 0x7F + 
                    0xFF + 0xFF + 0xFF + 0x7F +  
                    0xFF + 0x7F + 
                    0x1 + 
                    run_io_int + end_io_int) % 0x100; 
                byte checksum = (byte)checksumInt;
                byte[] protocol_Single = 
                    { 0x96, 0x0, 0x5E, 0x32, 
                    0x0, 0x0, 
                    0x1E, 0x0, 0x0, 0x0, 
                    speed, 0x0, 
                    0x0, 0x0, 
                    0xFF, 0x7F, 
                    0xFF, 0x7F, 
                    0x0, 0x0, 
                    0xFF, 0x7F, 
                    run_io_byte, 0x0, 0x0, 0x0, 
                    end_io_byte, 0x0, 0x0, 0x0,
                    0xFF, 0xFF, 0xFF, 0x7F,
                    0xFF, 0x7F, 0x1, 0x0, 
                    byteX[0], byteX[1], byteX[2], byteX[3], 
                    byteY[0], byteY[1], byteY[2], byteY[3], 
                    byteZ[0], byteZ[1], byteZ[2], byteZ[3], 
                    0xff, 0xff, 0xff, 0x7f, checksum };
                WriteRobotCommandFrame(
                    protocol_Single,
                    "Single motion",
                    RobotMotionUploadGapMs);
            }
        }
        private void createMotion(int motionNum_int, int nextMotion_int, float? x = null, float? y = null, float? z = null, int? v = null, int? run_io = null, int? end_io = null)
        {
            byte motionNum_byte_1 = (byte)(motionNum_int % 0x100);
            byte motionNum_byte_2 = (byte)(motionNum_int / 0x100);

            byte nextMotion_byte_1 = (byte)(nextMotion_int % 0x100);
            byte nextMotion_byte_2 = (byte)(nextMotion_int / 0x100);

            int motionNum_int_1 = (int)motionNum_byte_1;
            int motionNum_int_2 = (int)motionNum_byte_2;
            int nextMotion_int_1 = (int)nextMotion_byte_1;
            int nextMotion_int_2 = (int)nextMotion_byte_2;

            int speedInt;
            if (v.HasValue) { speedInt = v.Value; }
            else { speedInt = 20; }
            byte speed = (byte)speedInt;

            byte[] byteX;
            if (x.HasValue) { byteX = BitConverter.GetBytes(x.Value); }
            else { byteX = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

            byte[] byteY;
            if (y.HasValue) { byteY = BitConverter.GetBytes(y.Value); }
            else { byteY = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

            byte[] byteZ;
            if (z.HasValue) { byteZ = BitConverter.GetBytes(z.Value); }
            else { byteZ = new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }; }

            int run_io_int;
            if (run_io.HasValue) { run_io_int = run_io.Value; }
            else { run_io_int = 0; }
            byte run_io_byte = (byte)run_io_int;

            int end_io_int;
            if (end_io.HasValue) { end_io_int = end_io.Value; }
            else { end_io_int = 0; }
            byte end_io_byte = (byte)end_io_int;

            int x1 = byteX[0];
            int x2 = byteX[1];
            int x3 = byteX[2];
            int x4 = byteX[3];

            int y1 = byteY[0];
            int y2 = byteY[1];
            int y3 = byteY[2];
            int y4 = byteY[3];

            int z1 = byteZ[0];
            int z2 = byteZ[1];
            int z3 = byteZ[2];
            int z4 = byteZ[3];

            int checksumInt =
                (0x0 + 0x5E + 0x2E +
                    motionNum_int_1 + motionNum_int_2 +
                    0xE + 0x0 + 0x0 + 0x0 +
                    speedInt + 0x0 +
                    0x0 + 0x0 +
                    nextMotion_int_1 + nextMotion_int_2 +
                    0xFF + 0x7F +
                    0x0 + 0x0 +
                    0xFF + 0x7F +
                    run_io_int + 0x0 + 0x0 + 0x0 +
                    end_io_int + 0x0 + 0x0 + 0x0 +
                    0xFF + 0xFF + 0xFF + 0x7F +
                    0xFF + 0x7F + 
                    0x0 + 0x0 +
                    x1 + x2 + x3 + x4 +
                    y1 + y2 + y3 + y4 +
                    z1 + z2 + z3 + z4 ) % 0x100;
            byte checksum = (byte)checksumInt;

            byte[] protocol_motion =
                { 0x96, 0x0, 0x5E, 0x2E,
                    motionNum_byte_1, motionNum_byte_2,
                    0xE, 0x0, 0x0, 0x0,
                    speed, 0x0,
                    0x0, 0x0,
                    nextMotion_byte_1, nextMotion_byte_2,
                    0xFF, 0x7F,
                    0x0, 0x0,
                    0xFF, 0x7F,
                    run_io_byte, 0x0, 0x0, 0x0,
                    end_io_byte, 0x0, 0x0, 0x0,
                    0xFF, 0xFF, 0xFF, 0x7F,
                    0xFF, 0x7F,
                    0x0, 0x0,
                    byteX[0], byteX[1], byteX[2], byteX[3],
                    byteY[0], byteY[1], byteY[2], byteY[3],
                    byteZ[0], byteZ[1], byteZ[2], byteZ[3], checksum };

            WriteRobotCommandFrame(
                protocol_motion,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Program motion {0}->{1}",
                    motionNum_int,
                    nextMotion_int),
                RobotMotionUploadGapMs);
        }
        private void Stop_Click(object sender, EventArgs e)
        {
            Working = !Working;
            if (Working == false)
            {
                tbEco.Text += "Process stopped by user.\r\n";
                tbEco.SelectionStart = tbEco.Text.Length;
                tbEco.ScrollToCaret();
            }
        }
        // Alternative empirical AdaBins conversion.
        private float[] Pix2mmAdaBinsEmpirical(float CamX, float CamY, float CamDepth)
        {
            if (!IsValidAdaBinsTarget(CamX, CamY, CamDepth))
            {
                throw new InvalidDataException(string.Format(
                    CultureInfo.InvariantCulture,
                    "AdaBins target is outside the calibrated input range: " +
                    "x={0}, y={1}, depth={2} mm; expected x=[0,640), " +
                    "y=[0,480), depth=[925,1230] mm.",
                    CamX,
                    CamY,
                    CamDepth));
            }

            float x_robot_ref = 75.7f;
            float y_robot_ref = 147.43f;
            int u0 = 320;
            int v0 = 240;

            float z_top = 925f;
            float z_ref = 1230f;

            float Sx_ref = 2.36342f;
            float Sx_top = 1.73931f;
            float Sy_ref = -2.36367f;
            float Sy_top = -1.73025f;

            float u_ref = 362f;
            float v_ref = 203f;
            float z_img_ref = 1238f;

            float Sx_z = Sx_ref + ((CamDepth - z_ref) / (z_top - z_ref)) * (Sx_top - Sx_ref);
            float Sy_z = Sy_ref + ((CamDepth - z_ref) / (z_top - z_ref)) * (Sy_top - Sy_ref);

            float x_cam = (CamX - u0) * Sx_z;
            float y_cam = (CamY - v0) * Sy_z;
            float z_cam = CamDepth; // 



            float Sx_ref_z = Sx_ref + ((z_img_ref - z_ref) / (z_top - z_ref)) * (Sx_top - Sx_ref);
            float Sy_ref_z = Sy_ref + ((z_img_ref - z_ref) / (z_top - z_ref)) * (Sy_top - Sy_ref);

            float x_cam_ref = (u_ref - u0) * Sx_ref_z;
            float y_cam_ref = (v_ref - v0) * Sy_ref_z;
            float offsetX = x_robot_ref - x_cam_ref;
            float offsetY = y_robot_ref - y_cam_ref;
            float offsetZ = -(float)EyeToHandCalibration.FlangeToTcpZMm;

            float x_robot = x_cam + offsetX;
            float y_robot = y_cam + offsetY;
            float z_robot = z_cam + offsetZ;

            return new float[] { x_robot, y_robot, z_robot };
        }


        // AdaBins depth with Logitech intrinsics and Eye-to-Hand XY calibration.
        private float[] Pix2mm(float CamX, float CamY, float CamDepth)
        {
            return Pix2mmLogitechAdaBinsDepth(CamX, CamY, CamDepth);
        }

        private float[] Pix2mmLogitechAdaBinsDepth(
            float CamX,
            float CamY,
            float CamDepth)
        {
            if (!IsValidAdaBinsTarget(CamX, CamY, CamDepth))
            {
                throw new InvalidDataException(string.Format(
                    CultureInfo.InvariantCulture,
                    "AdaBins/Logitech target is outside the calibrated input range: " +
                    "x={0}, y={1}, depth={2} mm; expected x=[0,640), " +
                    "y=[0,480), depth=[925,1230] mm.",
                    CamX,
                    CamY,
                    CamDepth));
            }

            float[] handEyeFlangePoint =
                Pix2mmLogitechHandEyeOnly(CamX, CamY, CamDepth);
            float[] correctedFlangePoint =
                workspaceResidualCorrection.ApplyToFlange(handEyeFlangePoint);

            // Apply the physical TCP length; grip margin and clamps are caller-side.
            return new[]
            {
                correctedFlangePoint[0],
                correctedFlangePoint[1],
                CamDepth - (float)EyeToHandCalibration.FlangeToTcpZMm
            };
        }

        private float[] Pix2mmLogitechHandEyeOnly(
            float CamX,
            float CamY,
            float CamDepth)
        {
            // Camera pixel/depth to robot flange position without XY residual correction.
            return logitechEyeToHandCalibration.PixelDepthToFlange(
                CamX,
                CamY,
                CamDepth);
        }

        public sealed class MotionRunTimer
        {
            private readonly string _filePath;
            private readonly object _sync = new object();

            public MotionRunTimer(string runLabel = "DeltaRun")
            {
                string baseDir = AppDomain.CurrentDomain != null ? AppDomain.CurrentDomain.BaseDirectory : null;
                if (string.IsNullOrEmpty(baseDir))
                    baseDir = Directory.GetCurrentDirectory();

                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _filePath = Path.Combine(baseDir, runLabel + "_" + ts + ".csv");

                string dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string header = string.Join(",", new[]
                {
                    "BoxIndex", "targetIndex","Speed",
                    "1st Try","Result","Motion Time(s)",
                    "Coordinates(Cam)","X(pix)","Y(pix)","Depth(mm)",
                    "Coordinates(Robot)","X(mm)","Y(mm)","Z(mm)","margin_z(mm)",
                    "2nd Try","Result","Motion Time(s)",
                    "Coordinates(Cam)","X(pix)","Y(pix)","Depth(mm)",
                    "Coordinates(Robot)","X(mm)","Y(mm)","Z(mm)","margin_z(mm)",
                    "Count","# Success","# Failure"
                });
                File.WriteAllText(_filePath, header + Environment.NewLine);
            }


            private static string F(string s) { return s ?? ""; }
            private static string F(double v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(float v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(int v) { return v.ToString(CultureInfo.InvariantCulture); }

            public void EndCycle(
                int boxIndex,
                int targetIndex,
                int speed,
                string result_1, double Motion_Elapsed_1, float CamX1, float CamY1, float CamDepth1, float x1, float y1, float z1, float margin_1,
                string result_2, double Motion_Elapsed_2, float CamX2, float CamY2, float CamDepth2, float x2, float y2, float z2, float margin_2,
                int success, int failure)
            {
                string row = string.Join(",", new[]
                {
                    F(boxIndex),
                    F(targetIndex),
                    F(speed),

                    "",
                    F(result_1),
                    F(Motion_Elapsed_1),

                    "",                 // "Coordinates(Cam)"
                    F(CamX1),
                    F(CamY1),
                    F(CamDepth1),

                    "",                 // "Coordinates(Robot)"
                    F(x1),
                    F(y1),
                    F(z1),
                    F(margin_1),

                    "",
                    F(result_2),
                    F(Motion_Elapsed_2),

                    "",                 // "Coordinates(Cam)"
                    F(CamX2),
                    F(CamY2),
                    F(CamDepth2),

                    "",                 // "Coordinates(Robot)"
                    F(x2),
                    F(y2),
                    F(z2),
                    F(margin_2),

                    "",
                    F(success),
                    F(failure)
                });

                lock (_sync)
                {
                    AppendWithRetry(row);
                }
            }

            public void Save() { }

            public string FilePath { get { return _filePath; } }

            private void AppendWithRetry(string line)
            {
                const int maxTries = 5;
                for (int i = 0; i < maxTries; i++)
                {
                    try
                    {
                        File.AppendAllText(_filePath, line + Environment.NewLine);
                        return;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
                Debug.WriteLine("[MotionRunTimer] Append failed after retries.");
            }
        }


        public sealed class MotionRunTimer2
        {
            private readonly string _filePath;
            private readonly object _sync = new object();

            public MotionRunTimer2(string runLabel = "ImageProcessingCheck")
            {
                string baseDir = AppDomain.CurrentDomain != null ? AppDomain.CurrentDomain.BaseDirectory : null;
                if (string.IsNullOrEmpty(baseDir))
                    baseDir = Directory.GetCurrentDirectory();

                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _filePath = Path.Combine(baseDir, runLabel + "_" + ts + ".csv");

                string dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string header = string.Join(",", new[]
                {
                    "BoxIndex","TargeteIndex","Speed","GapTime(s)","Result",
                    "RobotMoveStartTime", "ImageProcessingStartTime", "RobotMoveEndTime", "RobotUngripTime", "ImageProcessingEndTime", "CycleElaspsedTime",
                    "RobotMotionElapsedTime", "ImageProcessingElapsedTime", 
                    "ImageProcessing Start Robot Locate", "X(mm)","Y(mm)","Z(mm)",
                    "ImageProcessing End Robot Locate", "X(mm)","Y(mm)","Z(mm)"
                });
                File.WriteAllText(_filePath, header + Environment.NewLine);
            }


            private static string F(string s) { return s ?? ""; }
            private static string F(double v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(float v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(int v) { return v.ToString(CultureInfo.InvariantCulture); }

            public void EndCycle(
                int boxIndex,
                int targetIndex,
                int speed,
                double GapTime,
                string Result,
                double RobotStartTime, double IPstartTime, double RobotEndTime, double RobotUngripTime, double IPendTime,
                double RobotTime, double IPelapsedTime, double CycleElapsedTime,
                float[] IPstartRobotLocate, float[] IPendRobotLocate)
            {
                string row = string.Join(",", new[]
                {
                    F(boxIndex),
                    F(targetIndex),
                    F(speed),
                    F(GapTime),
                    Result,

                    F(RobotStartTime),
                    F(IPstartTime),
                    F(RobotEndTime),
                    F(RobotUngripTime),
                    F(IPendTime),
                    F(CycleElapsedTime),
                    F(RobotTime),
                    F(IPelapsedTime),
                    "",                 // "ImageProcessing Start Robot Locate"
                    F(IPstartRobotLocate[0]),
                    F(IPstartRobotLocate[1]),
                    F(IPstartRobotLocate[2]),
                    "",                 // "ImageProcessing End Robot Locate"
                    F(IPendRobotLocate[0]),
                    F(IPendRobotLocate[1]),
                    F(IPendRobotLocate[2])
                });

                lock (_sync)
                {
                    AppendWithRetry(row);
                }
            }

            public void Save() { }

            public string FilePath { get { return _filePath; } }

            private void AppendWithRetry(string line)
            {
                const int maxTries = 5;
                for (int i = 0; i < maxTries; i++)
                {
                    try
                    {
                        File.AppendAllText(_filePath, line + Environment.NewLine);
                        return;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
                Debug.WriteLine("[MotionRunTimer] Append failed after retries.");
            }
        }


        public sealed class MotionRunTimer3
        {
            private readonly string _filePath;
            private readonly object _sync = new object();

            public MotionRunTimer3(string runLabel = "DeltaRun")
            {
                string baseDir = AppDomain.CurrentDomain != null ? AppDomain.CurrentDomain.BaseDirectory : null;
                if (string.IsNullOrEmpty(baseDir))
                    baseDir = Directory.GetCurrentDirectory();

                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _filePath = Path.Combine(baseDir, runLabel + "_" + ts + ".csv");

                string dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string header = string.Join(",", new[]
                {
                    "TargetIndex", "WhenSendTrue", "WhenReceive"
                });
                File.WriteAllText(_filePath, header + Environment.NewLine);
            }


            private static string F(string s) { return s ?? ""; }
            private static string F(double v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(float v) { return v.ToString(CultureInfo.InvariantCulture); }
            private static string F(int v) { return v.ToString(CultureInfo.InvariantCulture); }

            public void EndCycle(int targetIndex, double WhenSendTrue, double WhenReceive)
            {
                string row = string.Join(",", new[]
                {
                    F(targetIndex),
                    F(WhenSendTrue),
                    F(WhenReceive)
                });

                lock (_sync)
                {
                    AppendWithRetry(row);
                }
            }

            public void Save() { }

            public string FilePath { get { return _filePath; } }

            private void AppendWithRetry(string line)
            {
                const int maxTries = 5;
                for (int i = 0; i < maxTries; i++)
                {
                    try
                    {
                        File.AppendAllText(_filePath, line + Environment.NewLine);
                        return;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(100);
                    }
                }
                Debug.WriteLine("[MotionRunTimer] Append failed after retries.");
            }
        }

    }
}

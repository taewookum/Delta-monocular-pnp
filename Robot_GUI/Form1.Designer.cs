namespace Protocol_Test
{
    partial class Form1
    {
        /// <summary>
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// </summary>
        private void InitializeComponent()
        {
            this.btrobotON = new System.Windows.Forms.Button();
            this.cbPort = new System.Windows.Forms.ComboBox();
            this.btConnect = new System.Windows.Forms.Button();
            this.btrobotOFF = new System.Windows.Forms.Button();
            this.btAR = new System.Windows.Forms.Button();
            this.btZero = new System.Windows.Forms.Button();
            this.btStop = new System.Windows.Forms.Button();
            this.btGrippingMotion = new System.Windows.Forms.Button();
            this.btMove = new System.Windows.Forms.Button();
            this.btNotmove = new System.Windows.Forms.Button();
            this.tbposX = new System.Windows.Forms.TextBox();
            this.tbposY = new System.Windows.Forms.TextBox();
            this.tbposZ = new System.Windows.Forms.TextBox();
            this.btPosRead = new System.Windows.Forms.Button();
            this.tbposR = new System.Windows.Forms.TextBox();
            this.btOrigin = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.tbEco = new System.Windows.Forms.TextBox();
            this.pbMotion = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.pbBusy = new System.Windows.Forms.PictureBox();
            this.tbvision = new System.Windows.Forms.TextBox();
            this.btstopOn = new System.Windows.Forms.Button();
            this.cbPort2 = new System.Windows.Forms.ComboBox();
            this.btConnect2 = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.Photo = new System.Windows.Forms.Button();
            this.btUngrip = new System.Windows.Forms.Button();
            this.tb1Clear = new System.Windows.Forms.Button();
            this.btGrip = new System.Windows.Forms.Button();
            this.tb2Clear = new System.Windows.Forms.Button();
            this.btMotion_Test_Depth = new System.Windows.Forms.Button();
            this.btStLi = new System.Windows.Forms.Button();
            this.CameraConnect = new System.Windows.Forms.Button();
            this.CameraDisconnect = new System.Windows.Forms.Button();
            this.SerClose = new System.Windows.Forms.Button();
            this.Ser2Close = new System.Windows.Forms.Button();
            this.Stop = new System.Windows.Forms.Button();
            this.DelayTime = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.robot_v = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.WaitDelay = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.stopper_delay = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbMotion)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBusy)).BeginInit();
            this.SuspendLayout();
            // 
            // btrobotON
            // 
            this.btrobotON.Location = new System.Drawing.Point(19, 117);
            this.btrobotON.Margin = new System.Windows.Forms.Padding(4);
            this.btrobotON.Name = "btrobotON";
            this.btrobotON.Size = new System.Drawing.Size(107, 62);
            this.btrobotON.TabIndex = 0;
            this.btrobotON.Text = "Robot ON";
            this.btrobotON.UseVisualStyleBackColor = true;
            this.btrobotON.Click += new System.EventHandler(this.btrobotON_Click);
            // 
            // cbPort
            // 
            this.cbPort.FormattingEnabled = true;
            this.cbPort.Location = new System.Drawing.Point(129, 20);
            this.cbPort.Margin = new System.Windows.Forms.Padding(4);
            this.cbPort.Name = "cbPort";
            this.cbPort.Size = new System.Drawing.Size(171, 26);
            this.cbPort.TabIndex = 2;
            this.cbPort.SelectedIndexChanged += new System.EventHandler(this.cbPort_SelectedIndexChanged);
            // 
            // btConnect
            // 
            this.btConnect.Location = new System.Drawing.Point(311, 20);
            this.btConnect.Margin = new System.Windows.Forms.Padding(4);
            this.btConnect.Name = "btConnect";
            this.btConnect.Size = new System.Drawing.Size(107, 34);
            this.btConnect.TabIndex = 3;
            this.btConnect.Text = "Connect";
            this.btConnect.UseVisualStyleBackColor = true;
            this.btConnect.Click += new System.EventHandler(this.btConnect_Click);
            // 
            // btrobotOFF
            // 
            this.btrobotOFF.Location = new System.Drawing.Point(136, 117);
            this.btrobotOFF.Margin = new System.Windows.Forms.Padding(4);
            this.btrobotOFF.Name = "btrobotOFF";
            this.btrobotOFF.Size = new System.Drawing.Size(107, 62);
            this.btrobotOFF.TabIndex = 4;
            this.btrobotOFF.Text = "Robot OFF";
            this.btrobotOFF.UseVisualStyleBackColor = true;
            this.btrobotOFF.Click += new System.EventHandler(this.btrobotOFF_Click);
            // 
            // btAR
            // 
            this.btAR.Location = new System.Drawing.Point(251, 117);
            this.btAR.Margin = new System.Windows.Forms.Padding(4);
            this.btAR.Name = "btAR";
            this.btAR.Size = new System.Drawing.Size(107, 62);
            this.btAR.TabIndex = 5;
            this.btAR.Text = "Alram Reset";
            this.btAR.UseVisualStyleBackColor = true;
            this.btAR.Click += new System.EventHandler(this.btAR_Click);
            // 
            // btZero
            // 
            this.btZero.Location = new System.Drawing.Point(369, 117);
            this.btZero.Margin = new System.Windows.Forms.Padding(4);
            this.btZero.Name = "btZero";
            this.btZero.Size = new System.Drawing.Size(107, 62);
            this.btZero.TabIndex = 6;
            this.btZero.Text = "Zero Setting";
            this.btZero.UseVisualStyleBackColor = true;
            this.btZero.Click += new System.EventHandler(this.btZero_Click);
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(1142, 436);
            this.btStop.Margin = new System.Windows.Forms.Padding(4);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(224, 71);
            this.btStop.TabIndex = 7;
            this.btStop.Text = "Emergency STOP";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // btGrippingMotion
            // 
            this.btGrippingMotion.Location = new System.Drawing.Point(19, 188);
            this.btGrippingMotion.Margin = new System.Windows.Forms.Padding(4);
            this.btGrippingMotion.Name = "btGrippingMotion";
            this.btGrippingMotion.Size = new System.Drawing.Size(107, 62);
            this.btGrippingMotion.TabIndex = 8;
            this.btGrippingMotion.Text = "Gripping Motion";
            this.btGrippingMotion.UseVisualStyleBackColor = true;
            this.btGrippingMotion.Click += new System.EventHandler(this.btGrippingMotion_Click);
            // 
            // btMove
            // 
            this.btMove.Location = new System.Drawing.Point(546, 28);
            this.btMove.Margin = new System.Windows.Forms.Padding(4);
            this.btMove.Name = "btMove";
            this.btMove.Size = new System.Drawing.Size(107, 62);
            this.btMove.TabIndex = 11;
            this.btMove.Text = "Move Enable";
            this.btMove.UseVisualStyleBackColor = true;
            this.btMove.Click += new System.EventHandler(this.btMove_Click);
            // 
            // btNotmove
            // 
            this.btNotmove.Location = new System.Drawing.Point(546, 99);
            this.btNotmove.Margin = new System.Windows.Forms.Padding(4);
            this.btNotmove.Name = "btNotmove";
            this.btNotmove.Size = new System.Drawing.Size(107, 62);
            this.btNotmove.TabIndex = 11;
            this.btNotmove.Text = "Move Disable";
            this.btNotmove.UseVisualStyleBackColor = true;
            this.btNotmove.Click += new System.EventHandler(this.btNotmove_Click);
            // 
            // tbposX
            // 
            this.tbposX.Location = new System.Drawing.Point(1222, 575);
            this.tbposX.Margin = new System.Windows.Forms.Padding(4);
            this.tbposX.Name = "tbposX";
            this.tbposX.Size = new System.Drawing.Size(141, 28);
            this.tbposX.TabIndex = 12;
            // 
            // tbposY
            // 
            this.tbposY.Location = new System.Drawing.Point(1222, 616);
            this.tbposY.Margin = new System.Windows.Forms.Padding(4);
            this.tbposY.Name = "tbposY";
            this.tbposY.Size = new System.Drawing.Size(141, 28);
            this.tbposY.TabIndex = 12;
            // 
            // tbposZ
            // 
            this.tbposZ.Location = new System.Drawing.Point(1222, 656);
            this.tbposZ.Margin = new System.Windows.Forms.Padding(4);
            this.tbposZ.Name = "tbposZ";
            this.tbposZ.Size = new System.Drawing.Size(141, 28);
            this.tbposZ.TabIndex = 12;
            // 
            // btPosRead
            // 
            this.btPosRead.Location = new System.Drawing.Point(1222, 527);
            this.btPosRead.Margin = new System.Windows.Forms.Padding(4);
            this.btPosRead.Name = "btPosRead";
            this.btPosRead.Size = new System.Drawing.Size(143, 30);
            this.btPosRead.TabIndex = 13;
            this.btPosRead.Text = "Pos Read";
            this.btPosRead.UseVisualStyleBackColor = true;
            this.btPosRead.Click += new System.EventHandler(this.btPosRead_Click);
            // 
            // tbposR
            // 
            this.tbposR.Location = new System.Drawing.Point(1222, 696);
            this.tbposR.Margin = new System.Windows.Forms.Padding(4);
            this.tbposR.Name = "tbposR";
            this.tbposR.Size = new System.Drawing.Size(141, 28);
            this.tbposR.TabIndex = 12;
            // 
            // btOrigin
            // 
            this.btOrigin.Location = new System.Drawing.Point(661, 28);
            this.btOrigin.Margin = new System.Windows.Forms.Padding(4);
            this.btOrigin.Name = "btOrigin";
            this.btOrigin.Size = new System.Drawing.Size(221, 132);
            this.btOrigin.TabIndex = 15;
            this.btOrigin.Text = "Origin";
            this.btOrigin.UseVisualStyleBackColor = true;
            this.btOrigin.Click += new System.EventHandler(this.btOrigin_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1164, 580);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 18);
            this.label3.TabIndex = 16;
            this.label3.Text = "PosX";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1164, 624);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(51, 18);
            this.label4.TabIndex = 16;
            this.label4.Text = "PosY";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(1164, 665);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 18);
            this.label5.TabIndex = 16;
            this.label5.Text = "PosZ";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(1164, 704);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(51, 18);
            this.label6.TabIndex = 16;
            this.label6.Text = "PosR";
            // 
            // tbEco
            // 
            this.tbEco.Location = new System.Drawing.Point(19, 273);
            this.tbEco.Margin = new System.Windows.Forms.Padding(4);
            this.tbEco.Multiline = true;
            this.tbEco.Name = "tbEco";
            this.tbEco.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbEco.Size = new System.Drawing.Size(706, 267);
            this.tbEco.TabIndex = 1;
            // 
            // pbMotion
            // 
            this.pbMotion.Location = new System.Drawing.Point(-1, -2);
            this.pbMotion.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pbMotion.Name = "pbMotion";
            this.pbMotion.Size = new System.Drawing.Size(26, 26);
            this.pbMotion.TabIndex = 17;
            this.pbMotion.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.pbMotion);
            this.panel1.Location = new System.Drawing.Point(941, 33);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(26, 24);
            this.panel1.TabIndex = 18;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(974, 36);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(100, 18);
            this.label9.TabIndex = 16;
            this.label9.Text = "Motion End";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(974, 69);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(49, 18);
            this.label10.TabIndex = 16;
            this.label10.Text = "Busy";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.pbBusy);
            this.panel2.Location = new System.Drawing.Point(941, 64);
            this.panel2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(26, 24);
            this.panel2.TabIndex = 18;
            // 
            // pbBusy
            // 
            this.pbBusy.Location = new System.Drawing.Point(-1, -2);
            this.pbBusy.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pbBusy.Name = "pbBusy";
            this.pbBusy.Size = new System.Drawing.Size(26, 26);
            this.pbBusy.TabIndex = 17;
            this.pbBusy.TabStop = false;
            // 
            // tbvision
            // 
            this.tbvision.Location = new System.Drawing.Point(19, 616);
            this.tbvision.Margin = new System.Windows.Forms.Padding(4);
            this.tbvision.Multiline = true;
            this.tbvision.Name = "tbvision";
            this.tbvision.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbvision.Size = new System.Drawing.Size(706, 264);
            this.tbvision.TabIndex = 1;
            // 
            // btstopOn
            // 
            this.btstopOn.Location = new System.Drawing.Point(884, 476);
            this.btstopOn.Margin = new System.Windows.Forms.Padding(4);
            this.btstopOn.Name = "btstopOn";
            this.btstopOn.Size = new System.Drawing.Size(199, 64);
            this.btstopOn.TabIndex = 0;
            this.btstopOn.Text = "Stopper ON";
            this.btstopOn.UseVisualStyleBackColor = true;
            this.btstopOn.Click += new System.EventHandler(this.btstopOn_Click);
            // 
            // cbPort2
            // 
            this.cbPort2.FormattingEnabled = true;
            this.cbPort2.Location = new System.Drawing.Point(129, 64);
            this.cbPort2.Margin = new System.Windows.Forms.Padding(4);
            this.cbPort2.Name = "cbPort2";
            this.cbPort2.Size = new System.Drawing.Size(171, 26);
            this.cbPort2.TabIndex = 2;
            this.cbPort2.SelectedIndexChanged += new System.EventHandler(this.cbPort2_SelectedIndexChanged);
            // 
            // btConnect2
            // 
            this.btConnect2.Location = new System.Drawing.Point(311, 64);
            this.btConnect2.Margin = new System.Windows.Forms.Padding(4);
            this.btConnect2.Name = "btConnect2";
            this.btConnect2.Size = new System.Drawing.Size(107, 34);
            this.btConnect2.TabIndex = 3;
            this.btConnect2.Text = "Connect";
            this.btConnect2.UseVisualStyleBackColor = true;
            this.btConnect2.Click += new System.EventHandler(this.btConnect2_Click);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(20, 24);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(101, 18);
            this.label11.TabIndex = 20;
            this.label11.Text = "Delta Robot";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(20, 69);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(97, 18);
            this.label12.TabIndex = 20;
            this.label12.Text = "Jetson TTL";
            // 
            // Photo
            // 
            this.Photo.Location = new System.Drawing.Point(884, 554);
            this.Photo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Photo.Name = "Photo";
            this.Photo.Size = new System.Drawing.Size(199, 64);
            this.Photo.TabIndex = 21;
            this.Photo.Text = "Photo request";
            this.Photo.UseVisualStyleBackColor = true;
            this.Photo.Click += new System.EventHandler(this.Photo_Click);
            // 
            // btUngrip
            // 
            this.btUngrip.Location = new System.Drawing.Point(1257, 380);
            this.btUngrip.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btUngrip.Name = "btUngrip";
            this.btUngrip.Size = new System.Drawing.Size(108, 48);
            this.btUngrip.TabIndex = 26;
            this.btUngrip.Text = "Ungrip";
            this.btUngrip.UseVisualStyleBackColor = true;
            this.btUngrip.Click += new System.EventHandler(this.btUngrip_Click);
            // 
            // tb1Clear
            // 
            this.tb1Clear.Location = new System.Drawing.Point(661, 554);
            this.tb1Clear.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tb1Clear.Name = "tb1Clear";
            this.tb1Clear.Size = new System.Drawing.Size(64, 32);
            this.tb1Clear.TabIndex = 31;
            this.tb1Clear.Text = "Clear";
            this.tb1Clear.UseVisualStyleBackColor = true;
            this.tb1Clear.Click += new System.EventHandler(this.tb1Clear_Click);
            // 
            // btGrip
            // 
            this.btGrip.Location = new System.Drawing.Point(1142, 380);
            this.btGrip.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btGrip.Name = "btGrip";
            this.btGrip.Size = new System.Drawing.Size(114, 48);
            this.btGrip.TabIndex = 33;
            this.btGrip.Text = "Grip";
            this.btGrip.UseVisualStyleBackColor = true;
            this.btGrip.Click += new System.EventHandler(this.btGrip_Click);
            // 
            // tb2Clear
            // 
            this.tb2Clear.Location = new System.Drawing.Point(661, 887);
            this.tb2Clear.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tb2Clear.Name = "tb2Clear";
            this.tb2Clear.Size = new System.Drawing.Size(64, 32);
            this.tb2Clear.TabIndex = 35;
            this.tb2Clear.Text = "Clear";
            this.tb2Clear.UseVisualStyleBackColor = true;
            this.tb2Clear.Click += new System.EventHandler(this.tb2Clear_Click);
            // 
            // btMotion_Test_Depth
            // 
            this.btMotion_Test_Depth.Location = new System.Drawing.Point(885, 333);
            this.btMotion_Test_Depth.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btMotion_Test_Depth.Name = "btMotion_Test_Depth";
            this.btMotion_Test_Depth.Size = new System.Drawing.Size(199, 68);
            this.btMotion_Test_Depth.TabIndex = 46;
            this.btMotion_Test_Depth.Text = "Depth Test";
            this.btMotion_Test_Depth.UseVisualStyleBackColor = true;
            this.btMotion_Test_Depth.Click += new System.EventHandler(this.btMotion_Test_Depth_Click);
            // 
            // btStLi
            // 
            this.btStLi.Location = new System.Drawing.Point(1142, 273);
            this.btStLi.Margin = new System.Windows.Forms.Padding(4);
            this.btStLi.Name = "btStLi";
            this.btStLi.Size = new System.Drawing.Size(114, 103);
            this.btStLi.TabIndex = 49;
            this.btStLi.Text = "Start";
            this.btStLi.UseVisualStyleBackColor = true;
            this.btStLi.Click += new System.EventHandler(this.btStLi_Click);
            // 
            // CameraConnect
            // 
            this.CameraConnect.Location = new System.Drawing.Point(885, 273);
            this.CameraConnect.Margin = new System.Windows.Forms.Padding(4);
            this.CameraConnect.Name = "CameraConnect";
            this.CameraConnect.Size = new System.Drawing.Size(156, 52);
            this.CameraConnect.TabIndex = 53;
            this.CameraConnect.Text = "CAM Connect";
            this.CameraConnect.UseVisualStyleBackColor = true;
            this.CameraConnect.Click += new System.EventHandler(this.CameraConnect_Click);
            // 
            // CameraDisconnect
            // 
            this.CameraDisconnect.Location = new System.Drawing.Point(1049, 273);
            this.CameraDisconnect.Margin = new System.Windows.Forms.Padding(4);
            this.CameraDisconnect.Name = "CameraDisconnect";
            this.CameraDisconnect.Size = new System.Drawing.Size(34, 52);
            this.CameraDisconnect.TabIndex = 54;
            this.CameraDisconnect.Text = "X";
            this.CameraDisconnect.UseVisualStyleBackColor = true;
            this.CameraDisconnect.Click += new System.EventHandler(this.CameraDisconnect_Click);
            // 
            // SerClose
            // 
            this.SerClose.Location = new System.Drawing.Point(427, 20);
            this.SerClose.Margin = new System.Windows.Forms.Padding(4);
            this.SerClose.Name = "SerClose";
            this.SerClose.Size = new System.Drawing.Size(33, 34);
            this.SerClose.TabIndex = 55;
            this.SerClose.Text = "X";
            this.SerClose.UseVisualStyleBackColor = true;
            this.SerClose.Click += new System.EventHandler(this.SerClose_Click);
            // 
            // Ser2Close
            // 
            this.Ser2Close.Location = new System.Drawing.Point(427, 64);
            this.Ser2Close.Margin = new System.Windows.Forms.Padding(4);
            this.Ser2Close.Name = "Ser2Close";
            this.Ser2Close.Size = new System.Drawing.Size(33, 34);
            this.Ser2Close.TabIndex = 56;
            this.Ser2Close.Text = "X";
            this.Ser2Close.UseVisualStyleBackColor = true;
            this.Ser2Close.Click += new System.EventHandler(this.Ser2Close_Click);
            // 
            // Stop
            // 
            this.Stop.Location = new System.Drawing.Point(1257, 273);
            this.Stop.Name = "Stop";
            this.Stop.Size = new System.Drawing.Size(108, 103);
            this.Stop.TabIndex = 58;
            this.Stop.Text = "Pause";
            this.Stop.UseVisualStyleBackColor = true;
            this.Stop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // DelayTime
            // 
            this.DelayTime.Location = new System.Drawing.Point(236, 192);
            this.DelayTime.Name = "DelayTime";
            this.DelayTime.Size = new System.Drawing.Size(61, 28);
            this.DelayTime.TabIndex = 59;
            this.DelayTime.Text = "10";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(134, 195);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 18);
            this.label1.TabIndex = 60;
            this.label1.Text = "Delay (ms)";
            // 
            // robot_v
            // 
            this.robot_v.Location = new System.Drawing.Point(236, 229);
            this.robot_v.Name = "robot_v";
            this.robot_v.Size = new System.Drawing.Size(61, 28);
            this.robot_v.TabIndex = 61;
            this.robot_v.Text = "70";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(143, 232);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 18);
            this.label2.TabIndex = 62;
            this.label2.Text = "Robot V";
            // 
            // WaitDelay
            // 
            this.WaitDelay.Location = new System.Drawing.Point(409, 192);
            this.WaitDelay.Name = "WaitDelay";
            this.WaitDelay.Size = new System.Drawing.Size(65, 28);
            this.WaitDelay.TabIndex = 63;
            this.WaitDelay.Text = "10";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(318, 195);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(84, 18);
            this.label7.TabIndex = 64;
            this.label7.Text = "Wait (ms)";
            // 
            // stopper_delay
            // 
            this.stopper_delay.Location = new System.Drawing.Point(409, 229);
            this.stopper_delay.Name = "stopper_delay";
            this.stopper_delay.Size = new System.Drawing.Size(74, 28);
            this.stopper_delay.TabIndex = 65;
            this.stopper_delay.Text = "100";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(318, 232);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 18);
            this.label8.TabIndex = 66;
            this.label8.Text = "Stopper";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1377, 951);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.stopper_delay);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.WaitDelay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.robot_v);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DelayTime);
            this.Controls.Add(this.Stop);
            this.Controls.Add(this.Ser2Close);
            this.Controls.Add(this.SerClose);
            this.Controls.Add(this.CameraDisconnect);
            this.Controls.Add(this.CameraConnect);
            this.Controls.Add(this.btStLi);
            this.Controls.Add(this.btMotion_Test_Depth);
            this.Controls.Add(this.tb2Clear);
            this.Controls.Add(this.btGrip);
            this.Controls.Add(this.tb1Clear);
            this.Controls.Add(this.btUngrip);
            this.Controls.Add(this.Photo);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.btOrigin);
            this.Controls.Add(this.btPosRead);
            this.Controls.Add(this.tbposR);
            this.Controls.Add(this.tbposZ);
            this.Controls.Add(this.tbposY);
            this.Controls.Add(this.tbposX);
            this.Controls.Add(this.btNotmove);
            this.Controls.Add(this.btMove);
            this.Controls.Add(this.btGrippingMotion);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.btZero);
            this.Controls.Add(this.btAR);
            this.Controls.Add(this.btrobotOFF);
            this.Controls.Add(this.btConnect2);
            this.Controls.Add(this.btConnect);
            this.Controls.Add(this.cbPort2);
            this.Controls.Add(this.cbPort);
            this.Controls.Add(this.tbvision);
            this.Controls.Add(this.tbEco);
            this.Controls.Add(this.btstopOn);
            this.Controls.Add(this.btrobotON);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Main_PC_V2.0.5";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbMotion)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbBusy)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btrobotON;
        private System.Windows.Forms.ComboBox cbPort;
        private System.Windows.Forms.Button btConnect;
        private System.Windows.Forms.Button btrobotOFF;
        private System.Windows.Forms.Button btAR;
        private System.Windows.Forms.Button btZero;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button btGrippingMotion;
        private System.Windows.Forms.Button btMove;
        private System.Windows.Forms.Button btNotmove;
        private System.Windows.Forms.TextBox tbposX;
        private System.Windows.Forms.TextBox tbposY;
        private System.Windows.Forms.TextBox tbposZ;
        private System.Windows.Forms.Button btPosRead;
        private System.Windows.Forms.TextBox tbposR;
        private System.Windows.Forms.Button btOrigin;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbEco;
        private System.Windows.Forms.PictureBox pbMotion;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.PictureBox pbBusy;
        private System.Windows.Forms.TextBox tbvision;
        private System.Windows.Forms.Button btstopOn;
        private System.Windows.Forms.ComboBox cbPort2;
        private System.Windows.Forms.Button btConnect2;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button Photo;
        private System.Windows.Forms.Button btUngrip;
        private System.Windows.Forms.Button tb1Clear;
        private System.Windows.Forms.Button btGrip;
        private System.Windows.Forms.Button tb2Clear;
        private System.Windows.Forms.Button btMotion_Test_Depth;
        private System.Windows.Forms.Button btStLi;
        private System.Windows.Forms.Button CameraConnect;
        private System.Windows.Forms.Button CameraDisconnect;
        private System.Windows.Forms.Button SerClose;
        private System.Windows.Forms.Button Ser2Close;
        private System.Windows.Forms.Button Stop;
        private System.Windows.Forms.TextBox DelayTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox robot_v;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox WaitDelay;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox stopper_delay;
        private System.Windows.Forms.Label label8;
    }
}


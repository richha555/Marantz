namespace MarantzController
{
    partial class frmControler
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.lstSend = new System.Windows.Forms.ListBox();
            this.lstReceive = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabVolume = new System.Windows.Forms.TabPage();
            this.butReset = new System.Windows.Forms.Button();
            this.txtFrontRight = new System.Windows.Forms.TextBox();
            this.txtFrontLeft = new System.Windows.Forms.TextBox();
            this.txtBalanceFrontRear = new System.Windows.Forms.TextBox();
            this.txtBassLeft = new System.Windows.Forms.TextBox();
            this.txtMasterVolume = new System.Windows.Forms.TextBox();
            this.txtMasterBalance = new System.Windows.Forms.TextBox();
            this.txtCenterVolume = new System.Windows.Forms.TextBox();
            this.txtBassRight = new System.Windows.Forms.TextBox();
            this.txtBackRight = new System.Windows.Forms.TextBox();
            this.txtRearBalance = new System.Windows.Forms.TextBox();
            this.txtBackLeft = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.zBalanceFrontRear = new System.Windows.Forms.VScrollBar();
            this.zMasterVolume = new System.Windows.Forms.HScrollBar();
            this.zBackRight = new System.Windows.Forms.VScrollBar();
            this.zBackLeft = new System.Windows.Forms.VScrollBar();
            this.zCenterVolume = new System.Windows.Forms.VScrollBar();
            this.zBassRight = new System.Windows.Forms.VScrollBar();
            this.zBassLeft = new System.Windows.Forms.VScrollBar();
            this.zMasterBalance = new System.Windows.Forms.HScrollBar();
            this.zRearBalance = new System.Windows.Forms.HScrollBar();
            this.panel7 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.tabCommands = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabVolume.SuspendLayout();
            this.tabCommands.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.Location = new System.Drawing.Point(3, 3);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(611, 622);
            this.treeView1.TabIndex = 0;
            this.treeView1.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView1_NodeMouseDoubleClick);
            // 
            // lstSend
            // 
            this.lstSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstSend.FormattingEnabled = true;
            this.lstSend.Location = new System.Drawing.Point(0, 0);
            this.lstSend.Name = "lstSend";
            this.lstSend.Size = new System.Drawing.Size(329, 82);
            this.lstSend.TabIndex = 2;
            this.lstSend.SelectedIndexChanged += new System.EventHandler(this.LstSend_SelectedIndexChanged);
            // 
            // lstReceive
            // 
            this.lstReceive.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstReceive.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lstReceive.HideSelection = false;
            this.lstReceive.Location = new System.Drawing.Point(0, 88);
            this.lstReceive.Name = "lstReceive";
            this.lstReceive.Size = new System.Drawing.Size(329, 566);
            this.lstReceive.TabIndex = 3;
            this.lstReceive.UseCompatibleStateImageBehavior = false;
            this.lstReceive.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Send / Receive ";
            this.columnHeader1.Width = 1000;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lstReceive);
            this.splitContainer1.Panel2.Controls.Add(this.lstSend);
            this.splitContainer1.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.SplitContainer1_Panel2_Paint);
            this.splitContainer1.Size = new System.Drawing.Size(958, 654);
            this.splitContainer1.SplitterDistance = 625;
            this.splitContainer1.TabIndex = 4;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabVolume);
            this.tabControl1.Controls.Add(this.tabCommands);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(625, 654);
            this.tabControl1.TabIndex = 0;
            // 
            // tabVolume
            // 
            this.tabVolume.BackColor = System.Drawing.Color.RosyBrown;
            this.tabVolume.Controls.Add(this.butReset);
            this.tabVolume.Controls.Add(this.txtFrontRight);
            this.tabVolume.Controls.Add(this.txtFrontLeft);
            this.tabVolume.Controls.Add(this.txtBalanceFrontRear);
            this.tabVolume.Controls.Add(this.txtBassLeft);
            this.tabVolume.Controls.Add(this.txtMasterVolume);
            this.tabVolume.Controls.Add(this.txtMasterBalance);
            this.tabVolume.Controls.Add(this.txtCenterVolume);
            this.tabVolume.Controls.Add(this.txtBassRight);
            this.tabVolume.Controls.Add(this.txtBackRight);
            this.tabVolume.Controls.Add(this.txtRearBalance);
            this.tabVolume.Controls.Add(this.txtBackLeft);
            this.tabVolume.Controls.Add(this.label11);
            this.tabVolume.Controls.Add(this.label15);
            this.tabVolume.Controls.Add(this.label13);
            this.tabVolume.Controls.Add(this.label14);
            this.tabVolume.Controls.Add(this.label5);
            this.tabVolume.Controls.Add(this.label12);
            this.tabVolume.Controls.Add(this.label10);
            this.tabVolume.Controls.Add(this.label7);
            this.tabVolume.Controls.Add(this.label8);
            this.tabVolume.Controls.Add(this.label6);
            this.tabVolume.Controls.Add(this.label4);
            this.tabVolume.Controls.Add(this.label3);
            this.tabVolume.Controls.Add(this.label2);
            this.tabVolume.Controls.Add(this.label1);
            this.tabVolume.Controls.Add(this.zBalanceFrontRear);
            this.tabVolume.Controls.Add(this.zMasterVolume);
            this.tabVolume.Controls.Add(this.zBackRight);
            this.tabVolume.Controls.Add(this.zBackLeft);
            this.tabVolume.Controls.Add(this.zCenterVolume);
            this.tabVolume.Controls.Add(this.zBassRight);
            this.tabVolume.Controls.Add(this.zBassLeft);
            this.tabVolume.Controls.Add(this.zMasterBalance);
            this.tabVolume.Controls.Add(this.zRearBalance);
            this.tabVolume.Controls.Add(this.panel7);
            this.tabVolume.Controls.Add(this.panel6);
            this.tabVolume.Controls.Add(this.panel5);
            this.tabVolume.Controls.Add(this.panel3);
            this.tabVolume.Controls.Add(this.panel4);
            this.tabVolume.Controls.Add(this.panel2);
            this.tabVolume.Controls.Add(this.panel1);
            this.tabVolume.Location = new System.Drawing.Point(4, 22);
            this.tabVolume.Name = "tabVolume";
            this.tabVolume.Padding = new System.Windows.Forms.Padding(3);
            this.tabVolume.Size = new System.Drawing.Size(617, 628);
            this.tabVolume.TabIndex = 1;
            this.tabVolume.Text = "Volume";
            this.tabVolume.Click += new System.EventHandler(this.TabVolume_Click);
            // 
            // butReset
            // 
            this.butReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.butReset.BackColor = System.Drawing.SystemColors.Control;
            this.butReset.Location = new System.Drawing.Point(536, 597);
            this.butReset.Name = "butReset";
            this.butReset.Size = new System.Drawing.Size(75, 23);
            this.butReset.TabIndex = 32;
            this.butReset.Text = "RESET";
            this.butReset.UseVisualStyleBackColor = false;
            this.butReset.Click += new System.EventHandler(this.ButReset_Click);
            // 
            // txtFrontRight
            // 
            this.txtFrontRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFrontRight.BackColor = System.Drawing.Color.LightBlue;
            this.txtFrontRight.Location = new System.Drawing.Point(456, 23);
            this.txtFrontRight.Multiline = true;
            this.txtFrontRight.Name = "txtFrontRight";
            this.txtFrontRight.Size = new System.Drawing.Size(40, 49);
            this.txtFrontRight.TabIndex = 31;
            this.txtFrontRight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtFrontLeft
            // 
            this.txtFrontLeft.BackColor = System.Drawing.Color.LightBlue;
            this.txtFrontLeft.Location = new System.Drawing.Point(122, 23);
            this.txtFrontLeft.Multiline = true;
            this.txtFrontLeft.Name = "txtFrontLeft";
            this.txtFrontLeft.Size = new System.Drawing.Size(40, 49);
            this.txtFrontLeft.TabIndex = 30;
            this.txtFrontLeft.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBalanceFrontRear
            // 
            this.txtBalanceFrontRear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBalanceFrontRear.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.txtBalanceFrontRear.Location = new System.Drawing.Point(555, 500);
            this.txtBalanceFrontRear.Multiline = true;
            this.txtBalanceFrontRear.Name = "txtBalanceFrontRear";
            this.txtBalanceFrontRear.Size = new System.Drawing.Size(40, 49);
            this.txtBalanceFrontRear.TabIndex = 29;
            this.txtBalanceFrontRear.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBassLeft
            // 
            this.txtBassLeft.BackColor = System.Drawing.Color.LightBlue;
            this.txtBassLeft.Location = new System.Drawing.Point(15, 66);
            this.txtBassLeft.Multiline = true;
            this.txtBassLeft.Name = "txtBassLeft";
            this.txtBassLeft.Size = new System.Drawing.Size(40, 49);
            this.txtBassLeft.TabIndex = 29;
            this.txtBassLeft.Text = "100\r\n88.8 >\r\n99.9 <";
            this.txtBassLeft.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtMasterVolume
            // 
            this.txtMasterVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMasterVolume.BackColor = System.Drawing.Color.Thistle;
            this.txtMasterVolume.Location = new System.Drawing.Point(471, 315);
            this.txtMasterVolume.Multiline = true;
            this.txtMasterVolume.Name = "txtMasterVolume";
            this.txtMasterVolume.Size = new System.Drawing.Size(40, 49);
            this.txtMasterVolume.TabIndex = 29;
            this.txtMasterVolume.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtMasterBalance
            // 
            this.txtMasterBalance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMasterBalance.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.txtMasterBalance.Location = new System.Drawing.Point(448, 238);
            this.txtMasterBalance.Multiline = true;
            this.txtMasterBalance.Name = "txtMasterBalance";
            this.txtMasterBalance.Size = new System.Drawing.Size(40, 49);
            this.txtMasterBalance.TabIndex = 29;
            this.txtMasterBalance.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtCenterVolume
            // 
            this.txtCenterVolume.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtCenterVolume.BackColor = System.Drawing.Color.LightBlue;
            this.txtCenterVolume.Location = new System.Drawing.Point(286, 39);
            this.txtCenterVolume.Multiline = true;
            this.txtCenterVolume.Name = "txtCenterVolume";
            this.txtCenterVolume.Size = new System.Drawing.Size(40, 49);
            this.txtCenterVolume.TabIndex = 29;
            this.txtCenterVolume.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBassRight
            // 
            this.txtBassRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBassRight.BackColor = System.Drawing.Color.LightBlue;
            this.txtBassRight.Location = new System.Drawing.Point(564, 67);
            this.txtBassRight.Multiline = true;
            this.txtBassRight.Name = "txtBassRight";
            this.txtBassRight.Size = new System.Drawing.Size(40, 49);
            this.txtBassRight.TabIndex = 29;
            this.txtBassRight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBackRight
            // 
            this.txtBackRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBackRight.BackColor = System.Drawing.Color.LightBlue;
            this.txtBackRight.Location = new System.Drawing.Point(455, 446);
            this.txtBackRight.Multiline = true;
            this.txtBackRight.Name = "txtBackRight";
            this.txtBackRight.Size = new System.Drawing.Size(40, 49);
            this.txtBackRight.TabIndex = 29;
            this.txtBackRight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtRearBalance
            // 
            this.txtRearBalance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRearBalance.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.txtRearBalance.Location = new System.Drawing.Point(336, 496);
            this.txtRearBalance.Multiline = true;
            this.txtRearBalance.Name = "txtRearBalance";
            this.txtRearBalance.Size = new System.Drawing.Size(40, 49);
            this.txtRearBalance.TabIndex = 29;
            this.txtRearBalance.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtBackLeft
            // 
            this.txtBackLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtBackLeft.BackColor = System.Drawing.Color.LightBlue;
            this.txtBackLeft.Location = new System.Drawing.Point(78, 435);
            this.txtBackLeft.Multiline = true;
            this.txtBackLeft.Name = "txtBackLeft";
            this.txtBackLeft.Size = new System.Drawing.Size(40, 49);
            this.txtBackLeft.TabIndex = 29;
            this.txtBackLeft.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(512, 550);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(99, 13);
            this.label11.TabIndex = 28;
            this.label11.Text = "Front Rear Balance";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(19, 287);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 13);
            this.label15.TabIndex = 27;
            this.label15.Text = "Level";
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(570, 287);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(33, 13);
            this.label13.TabIndex = 27;
            this.label13.Text = "Level";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(18, 274);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(36, 13);
            this.label14.TabIndex = 27;
            this.label14.Text = "Lower";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(22, 261);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(25, 13);
            this.label5.TabIndex = 27;
            this.label5.Text = "Left";
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(568, 274);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(36, 13);
            this.label12.TabIndex = 27;
            this.label12.Text = "Lower";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(570, 261);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(32, 13);
            this.label10.TabIndex = 27;
            this.label10.Text = "Right";
            // 
            // label7
            // 
            this.label7.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(290, 241);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(33, 13);
            this.label7.TabIndex = 25;
            this.label7.Text = "Level";
            // 
            // label8
            // 
            this.label8.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(288, 226);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(38, 13);
            this.label8.TabIndex = 25;
            this.label8.Text = "Center";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(125, 274);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(81, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Master Balance";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 351);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 21;
            this.label4.Text = "Master Volume";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(411, 431);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "Right Surround Level";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 420);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Left Surround Level";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(142, 532);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "Surround Balance";
            // 
            // zBalanceFrontRear
            // 
            this.zBalanceFrontRear.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zBalanceFrontRear.Cursor = System.Windows.Forms.Cursors.SizeNS;
            this.zBalanceFrontRear.Location = new System.Drawing.Point(533, 262);
            this.zBalanceFrontRear.Name = "zBalanceFrontRear";
            this.zBalanceFrontRear.Size = new System.Drawing.Size(18, 285);
            this.zBalanceFrontRear.TabIndex = 17;
            this.zBalanceFrontRear.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZBalanceFrontRear_Scroll);
            // 
            // zMasterVolume
            // 
            this.zMasterVolume.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zMasterVolume.Location = new System.Drawing.Point(22, 367);
            this.zMasterVolume.Name = "zMasterVolume";
            this.zMasterVolume.Size = new System.Drawing.Size(487, 18);
            this.zMasterVolume.TabIndex = 16;
            this.zMasterVolume.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZMasterVolume_Scroll);
            // 
            // zBackRight
            // 
            this.zBackRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.zBackRight.Location = new System.Drawing.Point(433, 447);
            this.zBackRight.Name = "zBackRight";
            this.zBackRight.Size = new System.Drawing.Size(18, 173);
            this.zBackRight.TabIndex = 15;
            this.zBackRight.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZBackRight_Scroll);
            // 
            // zBackLeft
            // 
            this.zBackLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.zBackLeft.Location = new System.Drawing.Point(56, 436);
            this.zBackLeft.Name = "zBackLeft";
            this.zBackLeft.Size = new System.Drawing.Size(18, 173);
            this.zBackLeft.TabIndex = 14;
            this.zBackLeft.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZBackLeft_Scroll);
            // 
            // zCenterVolume
            // 
            this.zCenterVolume.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.zCenterVolume.Location = new System.Drawing.Point(297, 91);
            this.zCenterVolume.Name = "zCenterVolume";
            this.zCenterVolume.Size = new System.Drawing.Size(19, 131);
            this.zCenterVolume.TabIndex = 13;
            this.zCenterVolume.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZCenterVolume_Scroll);
            // 
            // zBassRight
            // 
            this.zBassRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.zBassRight.Location = new System.Drawing.Point(575, 119);
            this.zBassRight.Name = "zBassRight";
            this.zBassRight.Size = new System.Drawing.Size(18, 138);
            this.zBassRight.TabIndex = 12;
            this.zBassRight.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZBassRight_Scroll);
            // 
            // zBassLeft
            // 
            this.zBassLeft.Location = new System.Drawing.Point(25, 119);
            this.zBassLeft.Name = "zBassLeft";
            this.zBassLeft.Size = new System.Drawing.Size(18, 138);
            this.zBassLeft.TabIndex = 9;
            this.zBassLeft.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZBassLeft_Scroll);
            // 
            // zMasterBalance
            // 
            this.zMasterBalance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zMasterBalance.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.zMasterBalance.Location = new System.Drawing.Point(128, 290);
            this.zMasterBalance.Name = "zMasterBalance";
            this.zMasterBalance.Size = new System.Drawing.Size(358, 18);
            this.zMasterBalance.TabIndex = 8;
            this.zMasterBalance.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZMasterBalance_Scroll);
            // 
            // zRearBalance
            // 
            this.zRearBalance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zRearBalance.Location = new System.Drawing.Point(146, 548);
            this.zRearBalance.Name = "zRearBalance";
            this.zRearBalance.Size = new System.Drawing.Size(229, 18);
            this.zRearBalance.TabIndex = 7;
            this.zRearBalance.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ZRearBalance_Scroll);
            // 
            // panel7
            // 
            this.panel7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel7.BackColor = System.Drawing.Color.Black;
            this.panel7.Location = new System.Drawing.Point(465, 566);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(34, 54);
            this.panel7.TabIndex = 6;
            // 
            // panel6
            // 
            this.panel6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panel6.BackColor = System.Drawing.Color.Black;
            this.panel6.Location = new System.Drawing.Point(8, 499);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(34, 54);
            this.panel6.TabIndex = 5;
            // 
            // panel5
            // 
            this.panel5.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.panel5.BackColor = System.Drawing.Color.Black;
            this.panel5.Location = new System.Drawing.Point(267, 3);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(78, 31);
            this.panel5.TabIndex = 4;
            // 
            // panel3
            // 
            this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel3.BackColor = System.Drawing.Color.Black;
            this.panel3.Location = new System.Drawing.Point(498, 136);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(55, 107);
            this.panel3.TabIndex = 3;
            // 
            // panel4
            // 
            this.panel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel4.BackColor = System.Drawing.Color.Black;
            this.panel4.Location = new System.Drawing.Point(498, 23);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(55, 107);
            this.panel4.TabIndex = 2;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Black;
            this.panel2.Location = new System.Drawing.Point(65, 136);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(55, 107);
            this.panel2.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Black;
            this.panel1.Location = new System.Drawing.Point(65, 23);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(55, 107);
            this.panel1.TabIndex = 0;
            // 
            // tabCommands
            // 
            this.tabCommands.BackColor = System.Drawing.Color.Transparent;
            this.tabCommands.Controls.Add(this.treeView1);
            this.tabCommands.Location = new System.Drawing.Point(4, 22);
            this.tabCommands.Name = "tabCommands";
            this.tabCommands.Padding = new System.Windows.Forms.Padding(3);
            this.tabCommands.Size = new System.Drawing.Size(617, 628);
            this.tabCommands.TabIndex = 0;
            this.tabCommands.Text = "Commands";
            // 
            // frmControler
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(958, 654);
            this.Controls.Add(this.splitContainer1);
            this.Name = "frmControler";
            this.Text = "Marantz Controller";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmControler_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabVolume.ResumeLayout(false);
            this.tabVolume.PerformLayout();
            this.tabCommands.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ListBox lstSend;
        private System.Windows.Forms.ListView lstReceive;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabCommands;
        private System.Windows.Forms.TabPage tabVolume;
        private System.Windows.Forms.VScrollBar zBalanceFrontRear;
        private System.Windows.Forms.HScrollBar zMasterVolume;
        private System.Windows.Forms.VScrollBar zBackRight;
        private System.Windows.Forms.VScrollBar zBackLeft;
        private System.Windows.Forms.VScrollBar zCenterVolume;
        private System.Windows.Forms.VScrollBar zBassRight;
        private System.Windows.Forms.VScrollBar zBassLeft;
        private System.Windows.Forms.HScrollBar zMasterBalance;
        private System.Windows.Forms.HScrollBar zRearBalance;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox txtBalanceFrontRear;
        private System.Windows.Forms.TextBox txtBassLeft;
        private System.Windows.Forms.TextBox txtMasterVolume;
        private System.Windows.Forms.TextBox txtMasterBalance;
        private System.Windows.Forms.TextBox txtCenterVolume;
        private System.Windows.Forms.TextBox txtBassRight;
        private System.Windows.Forms.TextBox txtBackRight;
        private System.Windows.Forms.TextBox txtRearBalance;
        private System.Windows.Forms.TextBox txtBackLeft;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtFrontRight;
        private System.Windows.Forms.TextBox txtFrontLeft;
        private System.Windows.Forms.Button butReset;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}
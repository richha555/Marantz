﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace MarantzController
{
    public partial class frmControler : Form
    {
        const Boolean BACKGROUND_CONNECT = true;

        Dispatcher d;
        Controller c;
        SendReceive sr;
        Resizer rz;
        System.Threading.Thread send_receive_thread;

        List<string> command_list = new List<string>();
        Dictionary<string,string> command_lookup = new Dictionary<string,string>();

        Dictionary<String, LevelController> levctrls = new Dictionary<string, LevelController>();
        // factory has it's own dictionary, but it uses a different key for each level-controller

        Dictionary<String, MultiLevelController> balance_ctrls = new Dictionary<string, MultiLevelController>();

        private delegate void SafeCallDelegate (object sender, MessageTranferEventArgs e);

        public frmControler ()
        {
            InitializeComponent();
        }

        private void Form1_Load (object sender, EventArgs e)
        {
            if (Globs.NO_SEND) {
                this.chkSEND.Checked = false;
            } else {
                this.chkSEND.Checked = true;
            }
            if (System.IO.File.Exists(Globs.json_file3)) {
                Load_Tree(Globs.json_file3);
            } else if (System.IO.File.Exists(Globs.json_file2)) {
                Load_Tree(Globs.json_file2);
            } else {
                Load_Tree(Globs.json_file);
            }

            ShowSliderLevelLegend(this.txtScrollLegend);

            this.d = new Dispatcher();  // Dispatcher handles events
            this.d.ui = this;

            this.d.MessageSender += AddToSendList;
            this.d.MessageReceiver += AddToReceiveList;
            this.d.SendRefresher += RefreshSendList;
            this.d.SendClearer += ClearSendList;
            this.d.ConnectNotifier += Connected;
            this.d.SliderSetter += SetSliderLevel;
            this.d.ResizeHandler += SetSliderLevel;
            this.d.ParameterShower += ShowParameterValues;
            this.d.ReceiveProcessor += ProcessReceivedMessage;
            this.d.MarantzLevelUpdater += UpdateMarantzLevels;
            this.d.ThumbResizer += ResizeAllThumbs;

            this.c = new Controller(this.d);

            this.d.queue = this.c;

            this.rz = new Resizer(this.d);

            this.sr = new SendReceive(this.d);   // Connection manages connection to remote host
            this.d.connection = this.sr;

            if (!BACKGROUND_CONNECT) {
                if (!sr.Connect(Globs.marantz_host)) {
                    string err = sr.Errors;
                    return;
                }
            }
            this.send_receive_thread = new System.Threading.Thread(Send_Receive_Thread);

            LevelController lc;
            MultiLevelController mlc;

            LevelController.Orient vert = LevelController.Orient.vert;
            LevelController.Orient horiz = LevelController.Orient.horiz;

            // FL = Front-Left
            // FR = Front-Right
            // C  = Center
            // SW = Sub-Woofer
            // SL = Surround-Left
            // SR = Surround-Right
            // SBL = Surround-Back-Left  -> Front-Lower-Half-Left
            // SBR = Surround-Back-Right -> Front-Lower-Half-Right
            // FHL = Front-Height-Left
            // FHR = Front-Height-Right
            // FWL = Front-Width-Left
            // FWR = Front-Width-Right

            lc = LevelControllerFactory.New("Main Volume", "MV", horiz, (ScrollBar)this.zMasterVolume, this.txtMasterVolume, this.d);
            this.levctrls.Add("zMasterVolume", lc);
            lc = LevelControllerFactory.New("Front Left harder|softer", "CVFL", vert, null, this.txtFrontLeft, this.d);
            this.levctrls.Add("zFrontLeft", lc);
            lc = LevelControllerFactory.New("Front Right harder|softer", "CVFR", vert, null, this.txtFrontRight, this.d);
            this.levctrls.Add("zFrontRight", lc);
            lc = LevelControllerFactory.New("Back Left Surround Speaker harder|softer", "CVSL", vert, (ScrollBar)this.zBackLeft, this.txtBackLeft, this.d);
            this.levctrls.Add("zBackLeft", lc);
            lc = LevelControllerFactory.New("Back Right Surround Speaker harder|softer", "CVSR", vert, (ScrollBar)this.zBackRight, this.txtBackRight, this.d);
            this.levctrls.Add("zBackRight", lc);
            lc = LevelControllerFactory.New("Front Left Lower-Half harder|softer", "CVSBL", vert, (ScrollBar)this.zBassLeft, this.txtBassLeft, this.d);
            this.levctrls.Add("zBassLeft", lc);
            lc = LevelControllerFactory.New("Front Right Lower-Half harder|softer", "CVSBR", vert, (ScrollBar)this.zBassRight, this.txtBassRight, this.d);
            this.levctrls.Add("zBassRight", lc);
            lc = LevelControllerFactory.New("Center-Speaker harder|softer", "CVC", vert, (ScrollBar)this.zCenterVolume, this.txtCenterVolume, this.d);
            this.levctrls.Add("zCenterVolume", lc);

            lc = LevelControllerFactory.New("Test Balance Control", "-", horiz, (ScrollBar)this.sbSlider, this.txtSlider, this.d);
            this.levctrls.Add("sbSlider", lc);

            int n = 0;
            mlc = new MultiLevelController("Master L|R Balance", 
                                           new string[] { "CVFL", "CVSBL" },  /* left top */
                                           new string[] { "CVFR", "CVSBR" },  /* right top */
                                           new string[] { "CVSL" },           /* left bottom */
                                           new string[] { "CVSR" },           /* right bottom */
                                           horiz, (ScrollBar)this.zMasterBalance, this.txtMasterBalance, this.d);
            foreach (LevelController tlc in mlc.level_controllers()) {
                this.levctrls.Add(String.Format("L|R {0}",n++), lc);
            }
            this.levctrls.Add("zMasterBalance", mlc);
            this.balance_ctrls.Add("zMasterBalance", mlc);

            //                                                   Back Commands                    Front Commands
            mlc = new MultiLevelController("Front|Back Balance", 
                                           new string[] { "CVFL", "CVSBL", "CVC" },  /* left top */
                                           new string[] { "CVFR", "CVSBR", "CVC" },  /* right top */
                                           new string[] { "CVSL" },                  /* left bottom */
                                           new string[] { "CVSR" },                  /* right bottom */
                                           vert, (ScrollBar)this.zBalanceFrontRear, this.txtBalanceFrontRear, this.d);
            foreach (LevelController tlc in mlc.level_controllers()) {
                this.levctrls.Add(String.Format("F|B {0}", n++), lc);
            }
            this.levctrls.Add("zBalanceFrontRear", mlc);
            this.balance_ctrls.Add("zBalanceFrontRear", mlc);

            //                                                     Left Commands            Right Commands
            mlc = new MultiLevelController("Surround L|R Balance", new string[] { "CVSL" }, new string[] { "CVSR" },
                                           new string[] { }, new string[] { },
                                           horiz, (ScrollBar)this.zRearBalance, this.txtRearBalance, this.d);
            foreach (LevelController tlc in mlc.level_controllers()) {
                this.levctrls.Add(String.Format("Surr L|R {0}", n++), lc);
            }
            this.levctrls.Add("zRearBalance", mlc);
            this.balance_ctrls.Add("zRearBalance", mlc);

            foreach (string levctrl in this.levctrls.Keys) {
                LevelController tlc = this.levctrls[levctrl];
                tlc.balance_controlers = this.balance_ctrls;
            }

            this.d.ResizeAllThumbs();  // need to manually trigger resize so thumbs are the correct size

            this.send_receive_thread.Start(this.sr);

            mlc.test_balance();

            //this.d.Start_Session();  // tell ReceiveThread to expect output from Receiver

            //if (!pr.Go()) {
            //    String errs = pr.Errors;
            //    if (errs == "")
            //        errs = "Unknown Processor Error";
            //    ui.Add_to_Received(errs);
            //}
        }
        static void Connected (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = e.Form;
            Controller c = e.Dispatcher.queue;

            c.Command_List = frm.command_list;
            c.Command_Lookup = frm.command_lookup;

            foreach (string fullpath in c.Command_Lookup.Keys) {
                string cmd = c.Command_Lookup[fullpath];
                if (cmd.EndsWith("?")) {
                    Console.WriteLine("{0} - {1}", cmd, fullpath);
                }
            }

            c.SetCurrentMarantzLevels(LevelControllerFactory.lev_ctrls, "MV");  // should only need to send "MV?" message
            c.SetCurrentMarantzLevels(LevelControllerFactory.lev_ctrls, "CV");  // reply will be sent to ProcessReceivedMessage

            foreach (string levctrl in frm.levctrls.Keys) {
                LevelController lc = frm.levctrls[levctrl];
                Console.WriteLine(String.Format("{0} - {1}",lc.descr,lc.marantz_current_balance));
            }
        }

        private void FrmControler_FormClosing (object sender, FormClosingEventArgs e)
        {
            this.d.Exit();  // tell everyone to quit

            System.Threading.Thread.Sleep(2000);

            if (this.send_receive_thread.IsAlive) {

                Console.WriteLine("Killing background processes.");

                this.send_receive_thread.Abort();
            }
            this.sr.Disconnect();
        }

        public void Send_Receive_Thread (object o)
        {
            SendReceive m_sr = (SendReceive)o;

            if (BACKGROUND_CONNECT) {
                if (!sr.Connect(Globs.marantz_host)) {
                    string err = sr.Errors;
                    return;
                }
            }
            m_sr.Init_Sender();
            m_sr.Run_Receiver();
        }

        static void AddToReceiveList (object sender, MessageTranferEventArgs e)
        {
            String s = e.msg.Message;
            MsgType msgtyp = e.MessageType;
            frmControler frm = (frmControler)e.Form;

            if (frm.lstReceive.InvokeRequired) {
                var d = new SafeCallDelegate(AddToReceiveList);
                frm.lstReceive.Invoke(d, new object[] { sender, e });
            } else {
                frm.lstReceive.Items.Add(s);   // add to bottom of received list
                int n = frm.lstReceive.Items.Count - 1;
                frm.lstReceive.Items[n].BackColor = Globs.MsgColors[msgtyp];
            }
        }

        static void ResizeAllThumbs(object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            Controller c = e.Dispatcher.queue;

            c.ResizeAllThumbs(LevelControllerFactory.lev_ctrls);
        }

        static void UpdateMarantzLevels(object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            Controller c = e.Dispatcher.queue;

            c.UpdateMarantzLevels(LevelControllerFactory.lev_ctrls);
        }

        static void ProcessReceivedMessage(object sender, MessageTranferEventArgs e)
        {
            String msg = e.msg.Message;       //  SendReceive.Run_Receiver | this.d.Process_Received(tmsg);
            String desc = e.msg.Description;
            String id = e.msg.Id;
            String response = e.msg.Reply;
            frmControler frm = (frmControler)e.Form;
            Controller c = e.Dispatcher.queue;

            c.ProcessMarantzLevels(LevelControllerFactory.lev_ctrls, e.msg);
        }

        static void SetSliderLevel (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            LevelController lc = (LevelController)e.LC;
            ScrollBar sbar = lc.sbar;
            TextBox txt = lc.txt;

            if ((sbar != null) && (lc.gui_flags[LevelController.fMin] || lc.gui_flags[LevelController.fMax] || lc.gui_flags[LevelController.fLev] || lc.gui_flags[LevelController.fResize])) {
                if (sbar.InvokeRequired) {
                    var d = new SafeCallDelegate(SetSliderLevel);
                    sbar.Invoke(d, new object[] { sender, e });
                } else {
                    if (lc.gui_flags[LevelController.fMin] || lc.gui_flags[LevelController.fMax] || lc.gui_flags[LevelController.fResize]) {
                        // sliders don't return a level between min_level & max_level
                        // they return the top of a page where the page is located between min_level & max_level
                        int minSbar = lc.slider_min_level;
                        int maxSbar = lc.slider_max_level;

                        LevelMapper tmapper = new LevelMapper((double)minSbar, (double)maxSbar, 0.0, ((sbar.Width > sbar.Height) ? (double)sbar.Width : (double)sbar.Height));
                    //  pageSize = tmapper.B_to_intA(1.2 * (double)((sbar.Width > sbar.Height) ? (double)sbar.Height : (double)sbar.Width));

                        int pageSize = tmapper.B_to_intA(30.0);  // level -> slider

                    //  maxSbar = maxSbar + pageSize;  --- let controller take care of fact that max value is (max -  pagesize)
                        Console.WriteLine("Scroll-bar: length = {0}  => 30 =>  pageSize = {1}", ((sbar.Width > sbar.Height) ? (double)sbar.Width : (double)sbar.Height), pageSize);

                        sbar.LargeChange = pageSize;
                        sbar.Minimum = minSbar;
                        sbar.Maximum = maxSbar;

                    //  if (lc.gui_flags[LevelController.fLev]) {
                    //     sbar.Value = lc.last_slider_value; // load_slider_parms is going to trash last_slider_value !!!
                    //  }

                        lc.load_slider_parms(); // corrects last_slider_value based on new pageSize
                    }
                    if (lc.gui_flags[LevelController.fMin]) {
                    }
                    if (lc.gui_flags[LevelController.fMax]) {
                    }
                    if (lc.gui_flags[LevelController.fLev]) {
                        sbar.Value = lc.last_slider_value;
                    }
                }
            }
            if ((txt != null) && lc.gui_flags[LevelController.fTxt]) {
                ShowParameterValues(sender, e);
            }
            if (sbar != null) {
                if (sbar.Value != lc.last_slider_value) {
                    Console.WriteLine("Slider level doesn't match slider position: {0} <> {1}", lc.last_slider_value, sbar.Value);
                }
            }
            lc.gui_flags[LevelController.fMin] = false;
            lc.gui_flags[LevelController.fMax] = false;
            lc.gui_flags[LevelController.fLev] = false;
            lc.gui_flags[LevelController.fTxt] = false;
            lc.gui_flags[LevelController.fResize] = false;
        }

        static void ShowParameterValues(object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            LevelController lc = (LevelController)e.LC;
            ScrollBar sbar = lc.sbar;
            TextBox txt = lc.txt;

            if (txt.InvokeRequired) {
                var d = new SafeCallDelegate(SetSliderLevel);
                txt.Invoke(d, new object[] { sender, e });
            } else {
                //  if (lc.gui_flags[LevelController.fTxt]) {
                // 100
                // 12.5 >
                // 10.6 <
                if (lc.last_slider_value < -50 || lc.last_slider_value > 100) {
                    bool stop_here = true;
                }
                string slider = lc.is_unknown(lc.last_slider_value) ? "---" : String.Format("{0:00}", lc.last_slider_value);
                string requested = lc.is_unknown(lc.marantz_current_balance) ? "--.-" : String.Format("{0:00.0}", lc.marantz_current_balance);
                if (!lc.is_unknown(lc.balance_adjusted_level)) {
                    if (lc.marantz_current_balance != lc.balance_adjusted_level) {
                        requested = lc.is_unknown(lc.balance_adjusted_level) ? "--.-" : String.Format("{0:00.0}", lc.balance_adjusted_level);
                    }
                }
                string reported = lc.is_unknown(lc.reported_level) ? "--.-" : String.Format("{0:00.0}", lc.reported_level);
                string s = String.Format("{0}\r\n{1} >\r\n{2} <", slider, requested, reported);
                txt.Text = s; // s.Replace(".0", ""); 
                    //  txt.Text = lc.last_slider_value.ToString();
            //  }
                lc.gui_flags[LevelController.fTxt] = false;
            }
        }

        static void ShowSliderLevelLegend(TextBox txt)
        {
            string s = String.Format("{0}\r\n{1} >\r\n{2} <", "current slider value", "value sent to marantz", "value received from marantz");
            txt.Text = s; // s.Replace(".0", ""); 
                          //  txt.Text = lc.last_slider_value.ToString();
        }

        static void AddToSendList (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;

            if (frm.lstSend.InvokeRequired) {
                var d = new SafeCallDelegate(AddToSendList);
                frm.lstSend.Invoke(d, new object[] { sender, e });
            } else {
                Console.WriteLine("Adding command to top of send-list: {0} ({1})", e.msg.Message, e.msg.Description);
                string s = String.Format("{0}  ( {1} )", e.msg.Message, e.msg.Description);
                frm.lstSend.Items.Insert(0, s); // add to top of to-be-sent list
            }
        }

        static void ClearSendList (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;

            if (frm.lstSend.InvokeRequired) {
                var d = new SafeCallDelegate(ClearSendList);
                frm.lstSend.Invoke(d, new object[] { sender, e });
            } else {
                Console.WriteLine("Clearing send-list...");
                frm.lstSend.Items.Clear();
            }
        }

        static void RefreshSendList (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;

            if (frm.lstSend.InvokeRequired) {
                var d = new SafeCallDelegate(RefreshSendList);
                frm.lstSend.Invoke(d, new object[] { sender, e });
            } else {
                Console.WriteLine("Copying Queue to send-list...");
                frm.lstSend.Items.Clear();

                QueueMessage[] arr = e.Queue.Waiting_Commands();

                foreach (QueueMessage msg in arr) {
                    string Id = msg.Id;
                    string Message = msg.Message;
                    string Description = msg.Description;
                    string Reply = msg.Reply;
                    frm.lstSend.Items.Add(Message);
                }
            }
        }

        private void TreeView1_NodeMouseDoubleClick (object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((string)e.Node.Tag != "CMD")
                return;
            String cmd = e.Node.Text;
            String descr = e.Node.Parent.Text;
            QueueMessage msg = new QueueMessage();

            String Reply = get_value(cmd,descr,false,ref msg);

            if (Reply != "") {
                MessageBox.Show(String.Format("Received [ {0} ]", Globs.Super_Clean(Reply)));
            }
        }

        private void Load_Tree (String tree_file)
        {
            String json = "";
            try {
                json = System.IO.File.ReadAllText(tree_file);
            } catch (Exception ex) {
                MessageBox.Show(String.Format("Error loading {0}\n{1}", tree_file, ex.Message));
            }

            if (json == "") {
                return;
            }

            JObject tree_list = JObject.Parse(json);

            IList<JToken> results = tree_list["categoryies"].Children().ToList();

            IList<CmdTreeCat> cats = new List<CmdTreeCat>();

            foreach (JToken result in results) {
                // JToken.ToObject is a helper method that uses JsonSerializer internally
                CmdTreeCat cat = result.ToObject<CmdTreeCat>();
                cats.Add(cat);
            }

            this.treeView1.BeginUpdate();

            foreach (CmdTreeCat cat in cats) {
                this.treeView1.Nodes.Add(cat.cat);
                foreach (CmdTreeSubCat subcat in cat.subcats) {
                    int n1 = treeView1.Nodes.Count - 1;
                    TreeNode catNode = treeView1.Nodes[n1];
                    catNode.ForeColor = Color.Green;
                    catNode.Tag = "CAT";
                    catNode.Nodes.Add(subcat.subcat);
                    foreach (CmdTreeCont cont in subcat.contents) {
                        int n2 = catNode.Nodes.Count - 1;
                        TreeNode subCatNode = catNode.Nodes[n2];
                        subCatNode.ForeColor = Color.DarkGreen;
                        subCatNode.Tag = "SUBCAT";
                        subCatNode.Nodes.Add(cont.desc);
                        foreach (CmdTreeCommand cmd in cont.cmds) {
                            int n3 = subCatNode.Nodes.Count - 1;
                            TreeNode contentNode = subCatNode.Nodes[n3];
                            contentNode.ForeColor = Color.Blue;
                            contentNode.Tag = "CONT";
                            contentNode.Nodes.Add(cmd.cmd);
                            int n4 = contentNode.Nodes.Count - 1;
                            TreeNode cmdNode = contentNode.Nodes[n4];
                            cmdNode.ForeColor = Color.DarkRed;
                            cmdNode.Tag = "CMD";
                            string[] elems = cmd.cmd.Split(new char[] {' ', ':'});
                            string command = elems[0];

                            string full_path = String.Format("{0}\\{1}\\{2}\\{3}", 
                                cat.cat.Replace("\\","|"), 
                                subcat.subcat.Replace("\\", "|"), 
                                cont.desc.Replace("\\", "|"), 
                                command.Replace("\\", "|"));

                            if (!command_lookup.ContainsKey(full_path)) {
                                command_lookup.Add(full_path, command);
                            }

                            if (!command_list.Contains(command)) {
                                command_list.Add(command);
                            }
                        }
                    }
                }
            }
            this.treeView1.EndUpdate();
        }

        private void ZBackLeft_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zBackLeft");
        }

        private void ZRearBalance_Scroll (object sender, ScrollEventArgs e)
        {
            set_balance("zRearBalance");

        }

        private void ZBackRight_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zBackRight");

        }

        private void ZBalanceFrontRear_Scroll (object sender, ScrollEventArgs e)
        {
            set_balance("zBalanceFrontRear");
        }

        private void ZMasterVolume_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zMasterVolume");
        }

        private void ZMasterBalance_Scroll (object sender, ScrollEventArgs e)
        {
            set_balance("zMasterBalance");
        }

        private void ZBassLeft_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zBassLeft");
        }

        private void ZBassRight_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zBassRight");
        }

        private void ZCenterVolume_Scroll (object sender, ScrollEventArgs e)
        {
            set_volume("zCenterVolume");
        }

        void set_volume (String controller)
        {
            LevelController lc = this.levctrls[controller];

            ScrollBar sbar = lc.sbar;

            int new_lev = sbar.Value;  // Math.Min(lc.slider_max_level, Math.Max(lc.slider_min_level, sbar.Value));

            lc.change_level(new_lev);

            // compare current val to lastval  .. calculate % increase/descrease
            // increase/decrease current marantz volume by same %
        }

        void set_balance (String controller)
        {
            MultiLevelController mlc = (MultiLevelController)this.levctrls[controller];

            ScrollBar sbar = mlc.sbar;

            int new_lev = Math.Min(mlc.slider_max_level, Math.Max(mlc.slider_min_level, sbar.Value));

            mlc.change_balance(new_lev);

            // compare current val to lastval  .. calculate % increase/descrease
            // increase all left|top marantz volumes by same %
            // decrease all right|bottom marantz volumes by same %
        }

        private string get_value (string cmd, string descr, Boolean askForLevels, ref QueueMessage msg)
        {
            return this.c.get_value(cmd, descr, askForLevels, ref msg);
        }

        private void TabVolume_Click (object sender, EventArgs e)
        {

        }

        private void ButReset_Click(object sender, EventArgs e)
        {
            foreach (string code in LevelControllerFactory.lev_ctrls.Keys) {
                LevelController lc = LevelControllerFactory.lev_ctrls[code];
                lc.reset_control();
            }
        }

        private void SplitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void LstSend_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void frmControler_ResizeEnd(object sender, EventArgs e)
        {
            this.rz.HandleResize();
        }

        private void chkSEND_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSEND.Checked) {
                Globs.NO_SEND = false;
            } else {
                Globs.NO_SEND = true;
            }
        }

        private void sbSlider_Scroll(object sender, ScrollEventArgs e)
        {
            LevelController lc = this.levctrls["sbSlider"];
            ScrollBar sbar = lc.sbar;
            TextBox txt = lc.txt;

            double lev = lc.SliderValue_to_Level((double)sbar.Value);

            string slider = String.Format("{0:00}", sbar.Value);
            string requested = String.Format("{0:00.0}", lev);
            string reported = "--.-";
            string s = String.Format("{0}\r\n{1} >\r\n{2} <", slider, requested, reported);
            txt.Text = s;
        }
    }
}
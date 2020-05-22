// minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
//
// http://www.corebvba.be

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.ComponentModel;

namespace MarantzController
{
    public enum MsgType { sent, received, error, message }

    public class QueueMessage
    {
        public string Id = "";
        public string Message = "";
        public string Description = "";
        public string Reply = "";
        public DateTime sendTime = DateTime.MinValue;
        public DateTime receiveTime = DateTime.MinValue;
        public Boolean askNewLevel = false;

        public QueueMessage(string id, string message, string description, string reply, Boolean askForLevel)
        {
            this.Id = id;
            this.Message = message;
            this.Description = description;
            this.Reply = reply;
            this.askNewLevel = askForLevel;
        }
        public QueueMessage()
        {
            this.Id = Globs.New_Id();
            this.Message = "<MSG>";
        }
    }
    static class Globs
    {
        public static int receive_timeout_ms = 300;  // pause after receiving a char before checking if there is another character
        public static int user_timeout_ms = 1500;
        public static int loop_pause_ms = 3000;
        public static System.Collections.Concurrent.ConcurrentQueue<QueueMessage> cmdQueue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();
        public static System.Collections.Concurrent.ConcurrentQueue<QueueMessage> responseQueue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();
        //  public static System.Collections.Concurrent.ConcurrentQueue<string> responseQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();
        public static string json_file = @"C:\Users\Richard\Documents\Visual Studio 2010\Projects\Marantz\MarantzController\Commands.json";
        public static string json_file2 = @"\\REH_Akoya\C\Users\Richard\Documents\Visual Studio 2010\Projects\Marantz\MarantzController\Commands.json";
        public static string marantz_host = "marantz"; // "192.168.1.109";  // marantz
        public static string marantz_connect_message = "BridgeCo AG Telnet server";

        public static readonly Dictionary<MsgType,System.Drawing.Color> MsgColors = new Dictionary<MsgType, System.Drawing.Color>
        {
            { MsgType.sent, System.Drawing.Color.DarkSeaGreen },
            { MsgType.received, System.Drawing.Color.LightBlue },
            { MsgType.message, System.Drawing.Color.LightGoldenrodYellow },
            { MsgType.error, System.Drawing.Color.LightPink }
        };
        public static readonly Dictionary<Byte,string> ControlMsgs = new Dictionary<Byte, string>
        {
            { (Byte)Verbs.WILL, "will" },
            { (Byte)Verbs.WONT, "won't" },
            { (Byte)Verbs.DO,   "do" },
            { (Byte)Verbs.DONT, "don't" },
            { (Byte)Verbs.IAC,  "command..." },
            { (Byte)Verbs.GA,   "go-ahead" },
            { (Byte)Verbs.NOP,  "no-operation" },
            { (Byte)Verbs.DM,   "data-mark" },
            { (Byte)Verbs.BRK,  "break" },
            { (Byte)Verbs.IP,   "suspend/interrupt/abort" },
            { (Byte)Verbs.AO,   "abort-output" },
            { (Byte)Verbs.AYT,  "are-you-there?" },
            { (Byte)Verbs.EC,   "erase-char" },
            { (Byte)Verbs.EL,   "erase-line" },
            { (Byte)Verbs.END,  "SE/End" },
            { (Byte)Verbs.SB,   "subnegotiation-follows" },
            { (Byte)Verbs.IS,   "is" },
            { (Byte)Options.ECHO,     "echo" },
            { (Byte)Options.SGA,      "supress go-ahead" },
            { (Byte)Options.TM,       "timing-mark" },
            { (Byte)Options.TT,       "terminal-type" },
            { (Byte)Options.NAWS,     "window-size" },
            { (Byte)Options.TS,       "terminal-speed" },
            { (Byte)Options.FLOW,     "remote-flow-control" },
            { (Byte)Options.LINE,     "line-mode" },
            { (Byte)Options.ENV,      "environment-var's" },
            { (Byte)Options.CHARSET,  "char-set" },
            { (Byte)Options.NENV,     "new-environment" }
        };

        public static string New_Id ()
        {
            long nid = DateTime.Now.Ticks;
            return String.Format("{0:N0}", nid);
        }

        public static string Encode_Message (string id, string message, string description, string reply)
        {
            return String.Format("{0}\t{1}\t{2}\t{3}", id, message.Replace("\t", "<<<tab>>>"), description.Replace("\t", "<<<tab>>>"), reply.Replace("\t", "<<<tab>>>"));
        }

        public static void Decode_Message (string encoded_msg, ref string id, ref string message, ref string description, ref string reply)
        {
            if (String.IsNullOrEmpty(encoded_msg))
                return;
            string[] s = encoded_msg.Split('\t');
            id = s[0];
            message = s[1].Replace("<<<tab>>>", "\t");
            description = s[2].Replace("<<<tab>>>", "\t");
            reply = s[3].Replace("<<<tab>>>", "\t");
        }

        public static string Super_Clean (string s)
        {
            char[] chrs = s.ToCharArray();
            string outs = "";
            Boolean found_ch = false;

            for (int i = chrs.Length - 1; i >= 0; i--) {
                char ch = chrs[i];
                if (Char.IsControl(ch) || (ch == ' ')) {
                    if (found_ch) {
                        // replace embedded ctrl-char
                        if (ch == ' ') {
                            outs = " " + outs;
                        } else if ((ch == '\n') || (ch == '\r')) {
                            if (!outs.StartsWith("|")) {
                                outs = "|" + outs;
                            }
                        } else if (ch == '\t') {
                            if (!outs.StartsWith(" ")) {
                                outs = " " + outs;
                            }
                        } else {
                            if (!outs.StartsWith(".")) {
                                outs = "." + outs;
                            }
                        }
                    } else { 
                        // discard trailing ctrl-char    
                    }
                } else {
                    found_ch = true;
                    outs = ch + outs;
                }
            }
            while (outs.StartsWith(".") || outs.StartsWith("|") || outs.StartsWith(" ")) {
                outs = outs.Substring(1);
            }
            return outs;
        }
    }

    public class LevelMapper
    {
        public double minA;
        public double maxA;
        public double minB;
        public double maxB;

        private double mA_to_B;
        private double mB_to_A;

        public LevelMapper(double _minA, double _maxA, double _minB, double _maxB)
        {
            minA = _minA;  maxA = _maxA; minB = _minB; maxB = _maxB;
            mA_to_B = ((maxB - minB) / (maxA - minA));
            mB_to_A = ((maxA - minA) / (maxB - minB));
        }

        public double A_to_B (double A)
        {
            double B = (A - minA) * mA_to_B + minB;
            return Math.Min(maxB, Math.Max(minB, B));
        }
        public double B_to_A(double B)
        {
            double A = (B - minB) * mB_to_A + minA;
            return Math.Min(maxA, Math.Max(minA, A));
        }
        public double intA_to_B(int A)
        {
            return A_to_B((double)A);
        }
        public double intB_to_A(int B)
        {
            return B_to_A((double)B);
        }
        public int A_to_intB(double A)
        {
            double B = A_to_B(A);
            return (int)Math.Truncate(B);
        }
        public int B_to_intA(double B)
        {
            double A = B_to_A(B);
            return (int)Math.Truncate(A);
        }
        public int intA_to_intB(int A)
        {
            double B = A_to_B((double)A);
            return (int)Math.Truncate(B);
        }
        public int intB_to_intA(int B)
        {
            double A = B_to_A((double)B);
            return (int)Math.Truncate(A);
        }
    }


    public class CmdTree
    {
        public CmdTreeCat[] categoryies;
    }
    public class CmdTreeCat
    {
        public String cat;
        public CmdTreeSubCat[] subcats;
    }
    public class CmdTreeSubCat
    {
        public String subcat;
        public CmdTreeCont[] contents;
    }
    public class CmdTreeCont
    {
        public String desc;
        public CmdTreeCommand[] cmds;
    }
    public class CmdTreeCommand
    {
        public String cmd;
    }

    //public class Controller
    //{
    //    frmControler frm;
    //    System.Windows.Forms.TreeView treeView1;
    //    System.Windows.Forms.ListBox lstReceive;
    //    System.Windows.Forms.ListBox lstSend;
    //    Add_to_Queue m_add_to_received;
    //    Add_to_Queue m_add_to_sent;
    //    Take_from_Queue m_take_from_received;
    //    Take_from_Queue m_take_from_sent;
    //    Clear_Queue m_clear_received;
    //    Clear_Queue m_clear_sent;

    //    public Controller (frmControler _frm,
    //                       Add_to_Queue _add_to_received,
    //                       Add_to_Queue _add_to_sent,
    //                       Take_from_Queue _take_from_received,
    //                       Take_from_Queue _take_from_sent,
    //                       Clear_Queue _clear_received,
    //                       Clear_Queue _clear_sent)
    //    {
    //        this.frm = _frm;

    //        this.m_add_to_received = _add_to_received;
    //        this.m_add_to_sent = _add_to_sent;
    //        this.m_take_from_received = _take_from_received;
    //        this.m_take_from_sent = _take_from_sent;
    //        this.m_clear_received = _clear_received;
    //        this.m_clear_sent = _clear_sent;

    //        Init_Form();
    //    }

    //    private void Init_Form ()
    //    {
    //        System.Windows.Forms.Control[] ctrls;

    //        ctrls = this.frm.Controls.Find("treeView1", true);
    //        if (ctrls.Length > 0)
    //            this.treeView1 = (System.Windows.Forms.TreeView)ctrls[0];

    //        ctrls = this.frm.Controls.Find("lstReceive", true);
    //        if (ctrls.Length > 0)
    //            this.lstReceive = (System.Windows.Forms.ListBox)ctrls[0];

    //        ctrls = this.frm.Controls.Find("lstSend", true);
    //        if (ctrls.Length > 0)
    //            this.lstSend = (System.Windows.Forms.ListBox)ctrls[0];
    //    }
    //    public void Add_to_Received (string s)
    //    {
    //        this.m_add_to_received(s);
    //    }
    //    public void Add_to_Sent (string s)
    //    {
    //        this.m_add_to_sent(s);
    //    }
    //    public string Take_from_Received ()
    //    {
    //        return this.m_take_from_received();
    //    }
    //    public string Take_from_Sent ()
    //    {
    //        return this.m_take_from_sent();
    //    }
    //    public void Clear_Received ()
    //    {
    //        this.m_clear_received();
    //    }
    //    public void Clear_Sent ()
    //    {
    //        this.m_clear_sent();
    //    }

    //    //public void Go ()
    //    //{
    //    //    if (this.frm == null) {
    //    //        Init_Form();
    //    //    }
    //    //    this.frm.Show();
    //    //}

    //    //public Boolean Still_Open ()
    //    //{
    //    //    if ((this.frm == null) || !this.frm.Visible) {
    //    //        return false;
    //    //    }
    //    //    return true;
    //    //}

    //    public System.Windows.Forms.TreeView TreeView { get => this.treeView1; }

    //}

    //public class Proccessor
    //{
    //    TelnetConnection tc = null;
    //    Controller ui = null;
    //    string host = "";

    //    StringBuilder sberr = new StringBuilder();

    //    System.Threading.Thread thread = null;

    //    public Proccessor (Controller _ui, string _host)
    //    {
    //        this.host = _host;
    //        this.ui = _ui;
    //    }

    //    public Boolean Go ()
    //    {
    //        if (!Queue_Processing()) {
    //            this.sberr.Append(String.Format("Queue_Processing failed.\n"));
    //            return false;
    //        }
    //        int cnt = 0;
    //        while ((this.tc == null) || !tc.IsConnected) {
    //            System.Threading.Thread.Sleep(500);
    //            if (++cnt >= 30) {
    //                this.sberr.Append(String.Format("Timeout waiting for background-process to initialize.\n"));
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    public Boolean Still_Processing ()
    //    {
    //        if ((this.tc == null) || !tc.IsConnected) {
    //            return false;
    //        }
    //        return true;
    //    }
    //    public string Errors
    //    {
    //        get {
    //            String errs = this.sberr.ToString();
    //            this.sberr.Clear();
    //            return errs;
    //        }
    //        set {
    //            this.sberr.Clear();
    //            if (!String.IsNullOrEmpty(value)) {
    //                this.sberr.Append(value);
    //            }
    //        }
    //    }

    //    //public static void Processing_Callback (TelnetConnection _tc)
    //    //{
    //    //    this.tc = _tc;
    //    //}

    //    private Boolean Queue_Processing ()
    //    {
    //        if ((this.thread == null) || !this.thread.IsAlive) {
    //            Background_Processor bpr = new Background_Processor(this.tc,this.ui,this.host,this.sberr);

    //            this.thread = new System.Threading.Thread(new System.Threading.ThreadStart(bpr.Process_Queue));
    //            if (this.thread == null) {
    //                this.sberr.Append(String.Format("Background process for {0} was not created.\n", this.host));
    //                return false;
    //            }
    //            this.thread.Start();
    //        }
    //        return (this.thread != null) && this.thread.IsAlive;
    //    }
    //}

    //public class Background_Processor
    //{
    //    TelnetConnection tc = null;
    //    Controller ui = null;
    //    string host = "";

    //    StringBuilder sberr = new StringBuilder();

    //    public Background_Processor(TelnetConnection _tc,
    //                                Controller _ui,
    //                                String _host,
    //                                StringBuilder _sberr)
    //    {
    //        this.tc = _tc;
    //        this.ui = _ui;
    //        this.host = _host;
    //        this.sberr = _sberr;
    //    }

    //    public void Process_Queue ()
    //    {
    //        bool receivedCommand = false;

    //        if ((this.tc == null) || !tc.IsConnected) {
    //            try {
    //                this.tc = new TelnetConnection(this.host, 23);
    //            } catch (Exception ex) {
    //                this.sberr.Append(String.Format("Error connecting to {0}: {1}\n",host,ex.Message));
    //                this.tc = null;
    //            }
    //            if (this.tc == null) {
    //                this.sberr.Append(String.Format("No connection to {0}\n", host));
    //                String errs = this.Errors;
    //                this.ui.Add_to_Received(errs);
    //                return;
    //            }
    //            if (this.tc.IsConnected) {
    //                this.tc.InitConversation();
    //            } else {
    //                this.sberr.Append(String.Format("Not connected to {0}\n", host));
    //                String errs = this.Errors;
    //                this.ui.Add_to_Received(errs);
    //                return;
    //            }
    //            receivedCommand = true; // force loop to begin with read
    //        }

    //        // while connected
    //        while (this.tc.IsConnected) {
    //            // display server output
    //            if (receivedCommand) {
    //                String response = this.tc.Read();  // keeps reading from stream until nothing more arrives
    //                if (response != "") {
    //                    this.ui.Add_to_Received(String.Format("GOT> {0}\n", response));
    //                }
    //            } else {
    //                System.Threading.Thread.Sleep(Globs.loop_pause_ms);
    //            }
    //            receivedCommand = false;

    //            if (Globs.cmdQueue.Count > 0) {

    //                string cmd = this.ui.Take_from_Sent();

    //                if (cmd != "") {

    //                    this.ui.Add_to_Received(String.Format("CMD> {0}\n", cmd));

    //                    if (cmd == "ëxit") {
    //                        break;
    //                    }
    //                    this.tc.WriteLine(cmd);

    //                    receivedCommand = true; // stay awake
    //                }
    //            }
    //        }
    //        this.ui.Add_to_Received("***DISCONNECTED");
    //    }
    //    public string Errors
    //    {
    //        get {
    //            String errs = this.sberr.ToString();
    //            this.sberr.Clear();
    //            return errs;
    //        }
    //        set {
    //            this.sberr.Clear();
    //            if ( !String.IsNullOrEmpty(value)) {
    //                this.sberr.Append(value);
    //            }
    //        }
    //    }

    //}

    public static class LevelControllerFactory
    {
        public static Dictionary<string,LevelController> lev_ctrls = new Dictionary<string,LevelController>();

        public static LevelController New(string _descr, string _cmd, LevelController.Orient _orientation, System.Windows.Forms.ScrollBar _sbar, System.Windows.Forms.TextBox _txt, Dispatcher _d)
        {
            if (lev_ctrls.ContainsKey(_cmd)) {
                return lev_ctrls[_cmd];
            }
            LevelController lc = new LevelController(_descr, _cmd, _orientation, _sbar, _txt, _d);
            lev_ctrls.Add(_cmd,lc);
            return lc;
        }
    }

    public class LevelController
    {
        public Dispatcher d = null;
        public Controller c = null;

        public Boolean setting_level = false;

        public enum Orient { vert, horiz };

        public const int fMin = 0;
        public const int fMax = 1;
        public const int fLev = 2;
        public const int fTxt = 3;

        //                             min    max    level  text
        public Boolean[] gui_flags = { false, false, false, false };

        public double balance_value = 1.0;
        public double marantz_start_level = -1;
        public double marantz_min_level = -1;
        public double marantz_max_level = -1;
        public double marantz_current_level = -1;   // what we think level is
        public double balance_adjusted_level = -1;  // what level should be adjusted by balance ... what we told marantz to use
        public double reported_level = -1;          // what Marantz currently says its level is
        public int last_slider_value = -1;
        public int slider_min_level = -1;
        public int slider_max_level = -1;
        public string command = "";
        public string descr = "";
        public System.Windows.Forms.ScrollBar sbar = null;
        public System.Windows.Forms.TextBox txt = null;
        public Orient orientation = Orient.horiz;

        private LevelMapper sliderMarantzMapper;

        public LevelController(string _descr, string _cmd, Orient _orientation, System.Windows.Forms.ScrollBar _sbar, System.Windows.Forms.TextBox _txt, Dispatcher _d)
        {
            this.command = _cmd;
            this.descr = _descr;
            this.orientation = _orientation;
            this.sbar = _sbar;
            this.txt = _txt;
            this.d = _d;
            this.c = this.d.queue;

            this.sliderMarantzMapper = new LevelMapper(0.0, 100.0, 0.0, 100.0);

            if (sbar != null) {
                this.slider_min_level = this.sbar.Minimum;
                this.slider_max_level = this.sbar.Maximum;
                this.last_slider_value = this.sbar.Value;

                this.sliderMarantzMapper = new LevelMapper(this.slider_min_level, this.slider_max_level, 0.0, 100.0);
            } else {
                this.slider_min_level = 0;
                this.slider_max_level = 100;
            }
        }

        public Boolean is_unknown(double val)
        {
            double diff = val - (-1.0);
            if (diff < 0.01 && diff > -0.01) {
                return true;
            }
            return false;
        }

        public void change_level(int new_level)
        {
            // adjust volume based on slider change

            if (this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
                new_level = this.slider_max_level - (new_level - this.slider_min_level);
            }

            if (this.setting_level) {

                this.last_slider_value = new_level;

                this.d.ShowParameters(this);  // frmController::ShowParameterValues

                return;
            }

            // chg % = 100 * (new_level - last_slider_value) / (slider_max_level - marantz_min_level)

            double chg_percent = 100.0 * (double)(new_level - this.last_slider_value) / (double)(this.slider_max_level - this.slider_min_level);

            if (this.slider_max_level < 0.0 || this.slider_min_level < 0.0) {
                chg_percent = 100.0 * (double)(new_level - this.last_slider_value) / (double)(100 - 0);
            }

            this.last_slider_value = new_level;

            this.d.ShowParameters(this);  // frmController::ShowParameterValues

            double chg_amt = (chg_percent / 100.0) * (this.marantz_max_level - this.marantz_min_level);

            if (this.marantz_max_level < 0.0 || this.marantz_min_level < 0.0) {
                chg_amt = (chg_percent / 100.0) * (100.0 - 0.0);
            }

            if ((chg_amt < 0.1) && (chg_amt > -0.1))
                return;

            send_currlev_to_marantz(chg_amt);
        }

        public void reset_control()
        {
            this.marantz_current_level = marantz_start_level;
            this.balance_value = 1.0;
            this.balance_adjusted_level = marantz_current_level;

            this.marantz_start_level = -1.0;

            this.d.ShowParameters(this);  // frmController::ShowParameterValues

            set_gui_levels(-1.0, -1.0, this.marantz_current_level, null);

            send_currlev_to_marantz(0.0);
        }

        public void send_currlev_to_marantz(double chg_amt)
        {
            //if (this.marantz_current_level < 0.0) {
            //    return;
            //}
            this.marantz_current_level = this.marantz_current_level + chg_amt;

            this.balance_adjusted_level = this.balance_value * this.marantz_current_level;

            this.balance_adjusted_level = Math.Min(100.0, Math.Max(0.0, this.balance_adjusted_level));

            int int_lev = (int)Math.Truncate(this.balance_adjusted_level);
            double remainder = this.balance_adjusted_level - (double)int_lev;
            int int_remain = 0;

            if (remainder >= 0.8) {
                int_lev = Math.Min(100, int_lev + 1);
                int_remain = 0;
            } else if (remainder >= 0.3) {
                int_remain = 5;
            } else {
                int_remain = 0;
            }
            this.balance_adjusted_level = (double)int_lev + 0.1 * (double)int_remain;

            this.d.ShowParameters(this);  // frmController::ShowParameterValues

            System.Windows.Forms.Application.DoEvents();

            string cmd = this.command;
            if (int_remain > 0)
                cmd += String.Format("{0:000}", 10 * int_lev + int_remain);
            else cmd += String.Format("{0:00}", int_lev);

            // string response = this.c.get_value(cmd, descr);  <<< forces this routine to hang waiting for a response !

            Boolean askForLevels = (cmd.StartsWith("MV") ? false : true);

        //  this.d.Send(cmd, descr, askForLevels);  // just append to cmdQueue, and display on GUI out-list
        }

        public void set_gui_levels(double min_lev, double max_lev, double lev, QueueMessage msg)
        {
            //  this.gui_flags[fMin] = false;  -- let slider clear them after they are processed
            //  this.gui_flags[fMax] = false;
            //  this.gui_flags[fLev] = false;
            //  this.gui_flags[fTxt] = false;
            if (is_unknown(marantz_start_level) && !is_unknown(lev)) {
                marantz_start_level = lev;
                marantz_current_level = lev;
                reported_level = lev;
                this.gui_flags[fLev] = true;
            }
            if (!is_unknown(min_lev)) {
                marantz_min_level = min_lev;
                this.gui_flags[fMin] = true;
            }
            if (!is_unknown(max_lev)) {
                marantz_max_level = max_lev;
                this.gui_flags[fMax] = true;
            }
            if (!is_unknown(lev)) {
                if (is_unknown(marantz_min_level)) {
                    marantz_min_level = 0.0;    // default min
                    this.gui_flags[fMin] = true;
                }
                if (is_unknown(marantz_max_level)) {
                    marantz_max_level = 100.0;  // default max
                    this.gui_flags[fMax] = true;
                }
                reported_level = lev;
                this.gui_flags[fTxt] = true;
            }
            if (this.gui_flags[fMin] || this.gui_flags[fMax]) {
                this.sliderMarantzMapper = new LevelMapper(this.slider_min_level, this.slider_max_level, marantz_min_level, marantz_max_level);
            }
            if ((this.gui_flags[0] == false) && (this.gui_flags[1] == false) && (this.gui_flags[2] == false) && (this.gui_flags[3] == false)) {
                return;  // nothing changed or nothing recognized in response
            }

            show_currlev_from_marantz(msg);
        }

        public void show_currlev_from_marantz(QueueMessage msg)
        {
            double diff;

            Boolean our_message = true;
            string message = (msg == null) ? "<NONE>" : msg.Message;

            if (msg == null) {
                our_message = false;
            } else if (msg.Message == "<UPDATE>") {
                our_message = false;  // force recalculation & update-slider [deferred update]
            } else if ((msg.Message == "") || (msg.Message == "<MSG>")) {
                our_message = false;  // message from Marantz ... update sliders
            } else if (msg.Message.EndsWith("?")) {
                our_message = false;  // query means fetch and show values (*** ????)
            }
            // if value was received due to a volume change we sent
            // then we need to leave "marantz_current_level" and leave the scroller where it it
            // when nothing has happened for a while, use "reported_level" to (try and) update "marantz_current_level"

            if (our_message) {
                // only update Parameter-Values (text-boxes)
                // leave variables as they are, don't touch scroll-bars

                Console.WriteLine("OWN MESSAGE [{0}] ... ShowParameters", message);

                this.d.ShowParameters(this);  // frmController::ShowParameterValues

                return;
            }

            Console.WriteLine("MARANTZ MESSAGE [{0}] ... set marantz_current_level & update_slider", message);

            // if value was received from Marantz because of something that happened on Marantz (outside of this program)
            // then we need to (try and) replace "marantz_current_level" with "reported_level" (what Marantz says it was changed to)
            // we need to also immediately move the scroll-bar to where Marantz says it should be

            // balance_adjusted_level = balance_value * marantz_current_level
            // balance_adjusted_level >> desired_level

            // reported_level >> balance_adjusted_level
            // marantz_current_level = balance_adjusted_level / balance_value

            this.balance_adjusted_level = this.reported_level;  // balance_adjusted_level is a calculated level
                                                                // marantz_current_level is the desired level based on the slider
            if (this.balance_value < 1.0) {
            //  --- calculate level based on balance
                double new_current_level;
                if (this.balance_value < 0.01) {
                    new_current_level = 100.0; // ???
                } else {
                    new_current_level = Math.Max(0.0, Math.Min(100.0, this.balance_adjusted_level / this.balance_value));
                }
                diff = new_current_level - this.marantz_current_level;
            //  if diff is zero, our calculations worked ok
            //  *** until we are certain that our calculations are correct, leave sliders alone
            } else {
                diff = this.reported_level - this.marantz_current_level;
            //  if diff is zero, marantz did what we asked it to
                this.marantz_current_level = this.balance_adjusted_level;
            //  schedule change to slider position
            }

            this.update_slider(this.marantz_current_level);
        }

        public void update_slider(double marantz_lev)
        {
            // update slider to match current amplifier levels

            this.last_slider_value = this.sliderMarantzMapper.B_to_intA(marantz_lev);

            if (this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
                this.last_slider_value = this.slider_max_level - (this.last_slider_value - this.slider_min_level);
            }
            if (this.command == "CVSBL") {
                Console.WriteLine("STOP HERE");
            }
            this.gui_flags[fTxt] = true;
            this.gui_flags[fLev] = true;  // also update slider

            this.d.SetVolumeLevel(this);  // copy last_slider_value to scrollbar.Value
                                          // frmController::SetSliderLevel

            System.Windows.Forms.Application.DoEvents();

            this.setting_level = false;
        }
    }

    public class MultiLevelController : LevelController
    {
        string[] left_top_cmds;
        string[] right_bot_cmds;
        public List<LevelController> left_top_lcs = new List<LevelController>();
        public List<LevelController> right_bot_lcs = new List<LevelController>();

        private LevelMapper sliderToBalanceMapper;
        private LevelMapper balanceToMarantzMapper;

        //public Dictionary<string,int> marantz_start_levels;
        //public Dictionary<string,int> marantz_min_levels;
        //public Dictionary<string,int> marantz_max_levels;
        //public Dictionary<string,int> marantz_current_levels;
        public MultiLevelController (string _descr, string[] _left_top_cmds, string[] _right_bot_cmds, LevelController.Orient _orientation, System.Windows.Forms.ScrollBar _sbar, System.Windows.Forms.TextBox _txt, Dispatcher _d) :
            base(_descr, "XX", _orientation, _sbar, _txt, _d)
        {
            sliderToBalanceMapper = new LevelMapper(this.sbar.Minimum, this.sbar.Maximum, -50.0, 50.0);
            balanceToMarantzMapper = new LevelMapper(-50.0, 50.0, -50.0, 50.0);

            LevelController.Orient noorient = Orient.horiz;
            left_top_cmds = _left_top_cmds;
            right_bot_cmds = _right_bot_cmds;

            foreach (string cmd in left_top_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                left_top_lcs.Add(lc);
            }
            foreach (string cmd in right_bot_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                right_bot_lcs.Add(lc);
            }

            base.set_gui_levels(-50.0,50.0,0.0,null);
        }

        public List<LevelController> level_controllers ()
        {
            List<LevelController> lcs = new List<LevelController>();

            foreach (LevelController lc in left_top_lcs) {
                lcs.Add(lc);
            }
            foreach (LevelController lc in right_bot_lcs) {
                lcs.Add(lc);
            }
            return lcs;
        }

        public void change_level(double new_level)
        {
            // adjust balance based on slider change

            if (this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
                new_level = this.slider_max_level - (new_level - this.slider_min_level);
            }

            this.last_slider_value = this.sliderToBalanceMapper.A_to_intB(new_level);
            this.marantz_current_level = this.balanceToMarantzMapper.A_to_B(this.sliderToBalanceMapper.A_to_B(new_level));
            this.reported_level = -1.0; // this.marantz_current_level;

            this.d.ShowParameters(this);

            if (this.setting_level) {
                return;
            }
            double l_lev, r_lev;

            if (new_level <= 50.0) {
                l_lev = 1.0;
                r_lev = new_level / 50.0;
            } else {
                l_lev = 1.0 - (new_level - 50.0) / 50.0;
                r_lev = 1.0;
            }

            foreach (LevelController lc in this.left_top_lcs) {
                lc.balance_value = l_lev;
                lc.send_currlev_to_marantz(0);
            }
            foreach (LevelController lc in this.left_top_lcs) {
                lc.balance_value = r_lev;
                lc.send_currlev_to_marantz(0);
            }
        }
    }


    public class SendReceive
    {
        public String host = "";
        public TelnetConnection tc = null;

        Dispatcher d = null;
        Controller c = null;  // set in Run_Receiver by copying from Dispatcher

        private StringBuilder sberr = new StringBuilder();

        public Boolean expect_messages { get; set; }
        public Boolean stop_receiving { get; set; }

        public SendReceive (Dispatcher _d)
        {
            this.expect_messages = false;
            this.stop_receiving = false;
            this.d = _d;
            this.d.Expecter += ExpectToReceive;  // async (s, e) => await ExpectToReceive(s,e);
            this.d.Terminator += StopReceiver;
        }

        public string Errors
        {
            get => sberr.ToString();
        }

        public Boolean Connect (String _host)
        {
            this.host = _host;
            string msg;
            msg = String.Format("Connecting to {0}...", this.host);
            this.d.Show_Message(msg);
            try {
                this.tc = new TelnetConnection(this.host, 23);
                this.tc.dispatcher = this.d;
            } catch (Exception ex) {
                msg = String.Format("Error connecting to {0}: {1}\n", this.host, ex.Message);
                this.d.Show_Error(msg);
                this.tc = null;
            }
            if (this.tc == null) {
                msg = String.Format("No connection to {0}\n", this.host);
                this.d.Show_Error(msg);
                return false;
            }
            if (!this.tc.IsConnected) {
                msg = String.Format("Not connected to {0}\n", host);
                this.d.Show_Error(msg);
                return false;
            }
            msg = String.Format("Connected to {0}", this.host);
            this.d.Show_Message(msg);

            System.Threading.Thread.Sleep(500);  // try to stop first control-packet from getting corrupted

            this.tc.InitConversation();

            return true;
        }
        public void Init_Sender ()
        {
            string msg = String.Format("Ready to send messages from {0}...", this.d.connection.host);
            Console.WriteLine(msg);
            this.d.Show_Message(msg);
        }
        void SendToReceiver (string cmd)
        {
            string msg = String.Format("Sending to {0}... {1}", this.host, cmd);
            Console.WriteLine(msg);

            this.d.Show_Message(msg);

            this.tc.WriteLine(cmd);
            System.Threading.Thread.Sleep(50);  // pause between commands
        }
        static void ExpectToReceive (object sender, MessageTranferEventArgs e)
        {
            Console.WriteLine("Expecting to receive message from receiver...");

            e.Connection.expect_messages = true;
        }
        static void StopReceiver (object sender, MessageTranferEventArgs e)
        {
            Console.WriteLine("Terminating receiver.");

            e.Connection.stop_receiving = true;
        }

        public void Run_Receiver ()
        {
            System.Collections.Concurrent.ConcurrentQueue<QueueMessage> expectQueue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();

            const int NO_SLEEP = 0;
            const int SHORT_SLEEP = 10;
            const int LONG_SLEEP = 100;

            int sleep_time = NO_SLEEP;

            DateTime start_sleep;
            DateTime last_ask = DateTime.MinValue;
            long total_sleep = 0;

            Boolean display_dirty = false;

            this.c = this.d.queue;

            string msg = String.Format("Ready to send/receive messages to/from {0}...", this.d.connection.host);
            Console.WriteLine(msg);
            this.d.Show_Message(msg);

            this.expect_messages = true;  // should receive [BridgeCo AG Telnet server\n\r]

            sleep_time = LONG_SLEEP;
            start_sleep = DateTime.UtcNow;

            while (!this.stop_receiving && this.tc.IsConnected) {

            //  Console.Write(".");

                if (this.c.have_commands() && !this.stop_receiving) {

                    Console.WriteLine("x");
                    Console.WriteLine("Taking command from queue...");

                    QueueMessage send_msg = this.c.Take_from_Sent();

                    if (send_msg != null) {

                        string cmd = send_msg.Message;

                        Console.WriteLine("---");
                        Console.WriteLine("Pulled command [" + cmd + "] from queue ... sending to Marantz...");

                        this.d.ReefreshSendList();

                        send_msg.sendTime = DateTime.Now;

                        this.tc.WriteLine(cmd);

                        Console.WriteLine("Sent command [" + cmd + "] to Marantz.");

                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(0);

                        expectQueue.Enqueue(send_msg);

                        sleep_time = NO_SLEEP;  // wakeup and expect more commands
                        start_sleep = DateTime.UtcNow;

                        if (!String.IsNullOrEmpty(cmd)) {
                            //  Globs.responseQueue.Enqueue(encoded_message);  --- wait until we get a response
                            this.expect_messages = cmd.EndsWith("?");  // marantz only responds to inquiries
                        } else {
                            // no 4 second wait below, wait at least 100 ms for Marantz to respond if it wants
                            int wcnt = 0;  // wait 100 * 40 ms's (4 sec's) for receiver to reply
                            while (this.expect_messages && !this.stop_receiving && !this.tc.HaveData && (wcnt++ < 10)) {
                                System.Windows.Forms.Application.DoEvents();
                                System.Threading.Thread.Sleep(10);
                            }
                        }
                    }
                }
                if (this.expect_messages) {
                    Console.WriteLine("Expecting response from Marantz ... waiting");
                } else {
                //  Console.WriteLine("Not expecting response from Marantz ... no wait");
                }

                // expect-messages are queries, wait up to ## sec's for a reply to a query
                int cnt = 0;  // wait 100 * 40 ms's (4 sec's) for receiver to reply
                while (this.expect_messages && !this.stop_receiving && !this.tc.HaveData && (cnt++ < 40)) {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                if (this.tc.HaveData && !this.stop_receiving) {  // if cmds arrive from queue, interrupt waiting for something to arrive but don't skip reading
                    Console.WriteLine("Getting responze from Marantz...");
                    String response = this.tc.Read();  // keeps reading from stream until nothing more arrives
                    if (response.Contains(Globs.marantz_connect_message)) {
                        this.d.Connected();
                    }
                    Console.WriteLine("Got [" + response + "] from Marantz.");
                    string rmsg = (String.Format("received [{0}] from {1}", response, this.d.connection.host));
                    this.d.Show_Message(rmsg);

                    QueueMessage tmsg;
                    if ( ! expectQueue.TryDequeue(out tmsg) ) {  // *** NOTE: gives oldest send-message , not neccessarily the one the response belongs to
                        tmsg = new QueueMessage();               //           to keep the chance that this happens low, we remove stale expect messages below
                    }
                    tmsg.Reply = response;
                    tmsg.receiveTime = DateTime.Now;
                    Globs.responseQueue.Enqueue(tmsg);

                    Console.WriteLine("Processing message...");

                    this.d.Process_Received(tmsg);   // frmController.ProcessReceivedMessage | c.ProcessMarantzLevels

                    Console.WriteLine("Processed message.");

                    display_dirty = true;  // may need to update control-values

                } else if (this.expect_messages && !this.stop_receiving) {
                    this.d.Show_Error("no response from receiver.");
                }
                this.expect_messages = false;  // cancel wait and look for new messages to send

                Boolean askForLevels = false;
                foreach (QueueMessage tmsg in expectQueue) {  // 
                    if (tmsg.askNewLevel) {
                    //  don't need to check .Reply because if there was a response, then msg is no longer in expectQueue
                        askForLevels = true;
                        // the hope is that we will ask for the levels and when they arrive, they will get matched to this message
                    }
                    TimeSpan tDiff = DateTime.Now - tmsg.sendTime;
                    long total_wait = Convert.ToInt32(tDiff.TotalMilliseconds);
                    if (total_wait > (3 * 1000)) {
                        Console.WriteLine("Throwing out expect [" + tmsg.Message + "].");
                        // anything that arrives within N sec's of sent message is mapped to that sent message
                        // after N sec's anything that arrives is just info sent by Marantz
                        while (true) {
                            QueueMessage tmsg2;
                            if ( !expectQueue.TryDequeue(out tmsg2) ) {  // discard all msgs up to and including stale message(s)
                                break;
                            } else {
                                if (tmsg.Id == tmsg2.Id) {
                                    break;
                                }
                            }
                        }
                    }
                //  Console.Write("*");
                }

                if (askForLevels) {
                    TimeSpan askDiff = DateTime.Now - last_ask;
                    if (askDiff.TotalSeconds > 3) {
                        last_ask = DateTime.Now;
                        String id = this.d.Send("CV?", "Get Current Levels", false);
                    }
                }

                // wakeup after sending commands
                // after 30 secs go into shallow sleep
                // after 2 minutes go into deep sleep

                TimeSpan timeDiff = DateTime.UtcNow - start_sleep;
                total_sleep = Convert.ToInt32(timeDiff.TotalMilliseconds);

                if (display_dirty) {
                    if (total_sleep > 5 * 1000) {
                        // if we haven't taken any messages off the queue in a while, then we should 
                        // copy reported_level to balance_adjusted_level and 
                        // and use balance_value to calculate marantz_current_level
                        // ...for all level-controllers...
                        // and update their respective sliders
                        // balance-controllers are special ... they only belong to this program and not Marantz
                        // they don't need updating

                    //  this.d.Update_MarantzLevels();

                        display_dirty = false;
                    }
                }

                if ((total_sleep >= (30 * 1000)) && (sleep_time == NO_SLEEP)) {
                    sleep_time = SHORT_SLEEP;
                } else if ((total_sleep >= (120 * 1000)) && (sleep_time == SHORT_SLEEP)) {
                    sleep_time = LONG_SLEEP;
                }
                if (!this.stop_receiving && !this.tc.HaveData && !this.c.have_commands()) {
                    if (sleep_time == 0) {
                        System.Windows.Forms.Application.DoEvents();
                    } else {
                    //  Console.WriteLine("Sleeping " + sleep_time + "ms ...");
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(sleep_time);
                    }
                }
            }
            string emsg = "Receiver terminated.";
            Console.WriteLine(emsg);
            this.d.Show_Message(emsg);
        }
        public void Disconnect ()
        {
            Console.WriteLine("Disconnecting from {0}...", this.host);
            this.stop_receiving = true;
        }
    }

    public class Controller
    {
        private Dispatcher d;
        private List<string> command_list = new List<string>();
        private Dictionary<string,string> command_lookup = new Dictionary<string,string>();  // cmd_lookup[fullpath] = command

        public Controller (Dispatcher _d)
        {
            this.d = _d;

            this.d.MessageSender += AddToSendQueue;
            //  this.d.MessageReceiver += AddToReceiveQueue;
            this.d.SendClearer += ClearSendQueue;
        }

        public List<string> Command_List
        {
            get { return this.command_list; }
            set { this.command_list = value; }
        }
        public Dictionary<string,string> Command_Lookup {
            get { return this.command_lookup; }
            set { this.command_lookup = value; }
        }

        public void SetCurrentMarantzLevels(Dictionary<String, LevelController> levctrls, string command)
        {
            QueueMessage msg = new QueueMessage();

            string val = get_value(command + "?", "Current Levels", false /* ask for levels ? */, ref msg);

            ProcessMarantzLevels(levctrls, msg);
        }

        public void UpdateMarantzLevels(Dictionary<String, LevelController> levctrls)
        {
            QueueMessage tmsg = new QueueMessage();
            tmsg.Message = "<UPDATE>";

            foreach (string levctrl in levctrls.Keys) {
                LevelController lc = levctrls[levctrl];

                lc.show_currlev_from_marantz(tmsg);
            }
        }

        public void ProcessMarantzLevels(Dictionary<String, LevelController> levctrls, QueueMessage msg)
        {
            string command = msg.Message;
            string val = msg.Reply;

            if (String.IsNullOrWhiteSpace(val))
                return;

            //  CVFL?   ...returns...
            // "CVFL 515\rBridgeCo AG Telnet server\n\rCVFR 52\rCVC 50\rCVSW 50\rCVSL 39\rCVSR 40\rCVSBL 50\rCVSBR 50\rCVSB 50\rCVFHL 503\rCVFHR 387\rCVFWL 00\rCVFWR 505\rMVMAX 765\rDCAUTO\r@DCM:3\r"

            string[] vals = val.Split(new char[] {'\r', '\n', ',' });

            foreach (string s in vals) {
                if (!String.IsNullOrEmpty(s)) {
                    string[] elems = s.Split(new char[] { ' ', ':' });
                    if (elems.Length == 1) {
                        if (!String.IsNullOrEmpty(command)) {
                            if (s.StartsWith(command)) {
                                elems = new string[] { command, s.Substring(command.Length) };
                            }
                        }
                    }
                    if (elems.Length == 1) {
                        foreach (string levctrl in levctrls.Keys) {
                            LevelController lc = levctrls[levctrl];
                            if (!String.IsNullOrEmpty(lc.command)) {
                                if (s.StartsWith(lc.command)) {
                                    elems = new string[] { lc.command, s.Substring(lc.command.Length) };
                                    break;
                                }
                            }
                        }
                    }
                    if (elems.Length >= 2) {
                        string cmd = elems[0];
                        string slev = elems[1];
                        int lev = 0;
                        if (int.TryParse(slev, out lev)) {
                            double level = lev;
                            if (slev.Length >= 3) {  // 801 >> 80.1
                                level = level / 10.0;
                            }
                            // 0 >= level >= 10
                            if (level > 100.0)
                                level = 100.0;
                            Boolean found_command = false;
                            foreach (string levctrl in levctrls.Keys) {
                                LevelController lc = levctrls[levctrl];
                                if (lc.command == cmd) {
                                    string rmsg = String.Format("{0} -> {1:0.0} - {2}", cmd, level, lc.descr);
                                    Console.WriteLine(rmsg);
                                    this.d.Show_Message(rmsg);
                                    found_command = true;

                                    lc.set_gui_levels(-1.0, -1.0, level, msg);

                                    //  this.d.SetVolumeLevel(lc);
                                    //  lc.marantz_current_level = lev;
                                }
                            }
                            if (!found_command) {  // unrecognized response
                                if (this.command_list.Contains(cmd)) {
                                    string fullpath = "???";
                                    foreach (string fpath in this.command_lookup.Keys) {
                                        if (this.command_lookup[fpath] == cmd) {
                                            fullpath = fpath;  break;
                                        }
                                    }
                                    Console.WriteLine(String.Format("{0} -> {1:0.0} - {2}", cmd, level, fullpath));
                                } else {
                                    Console.WriteLine(String.Format("{0} -> {1:0.0} - UNKNOWN CMD", cmd, level));
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("---");
        }

        public string get_value (string cmd, string descr, Boolean askForLevels, ref QueueMessage msg)
        {
            DateTime send_time;
            Boolean got_response = false;
            string Reply = "";
            int t;

            msg = null;

            String id = this.d.Send(cmd, descr, askForLevels);  // doen't actually send cmd, just puts it into queue

            for (t = 0; t < 10; t++) {
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(0);
            }

            send_time = DateTime.UtcNow;

            if (true) {  // cmd.EndsWith("?")
                while (true) {
                    if (this.have_replys()) {
                        foreach (QueueMessage received_message in Globs.responseQueue) {

                            string r_id = received_message.Id;

                            if (r_id == id) {
                                msg = received_message;
                                Reply = received_message.Reply;
                                got_response = true;
                                break;
                            }
                        }
                    }
                    if (got_response) {
                        break;
                    }
                    TimeSpan timeDiff = DateTime.UtcNow - send_time;
                    double wait_secs = timeDiff.TotalSeconds;

                    if (wait_secs >= 180) {  // timeout to wait for command to be sent , marantz to process, send response & us to receive it
                        break;
                    }
                    for (t = 0; t < 10; t++) {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(0);
                    }
                //  Console.WriteLine("waiting for response to " + cmd);
                }
            }
            return Reply;
        }

        static void AddToSendQueue (object sender, MessageTranferEventArgs e)
        {
            Console.WriteLine("Pushing onto send-queue: {0} ({1})", e.msg.Message, e.msg.Description);
            Globs.cmdQueue.Enqueue(e.msg);
        }

        //public string Take_from_Received ()
        //{
        //    string s = "";
        //    if (Globs.responseQueue.TryDequeue(out s)) {
        //        if (this.lstReceive.Items.Count > 0) {
        //            this.lstReceive.Items.Remove(this.lstReceive.Items[0]);  // take off top of received list  FIFO
        //        }
        //    }
        //    return s;
        //}

        public QueueMessage[] Waiting_Commands ()
        {
            return Globs.cmdQueue.ToArray();
        }
        public Boolean have_commands ()
        {
            return Globs.cmdQueue.Count > 0;
        }
        public QueueMessage Take_from_Sent ()
        {
            QueueMessage msg;
            if (Globs.cmdQueue.TryDequeue(out msg)) {
                return msg;
            } 
            return null;
        }
        public Boolean have_replys ()
        {
            return Globs.responseQueue.Count > 0;
        }
        public QueueMessage Take_from_Reply ()
        {
            QueueMessage msg;
            if (Globs.responseQueue.TryDequeue(out msg)) {
                return msg;
            }
            return null;
        }
        static void ClearSendList (object sender, MessageTranferEventArgs e)
        {
            Controller c = e.Queue;

            while (Globs.cmdQueue.Count > 0) {
                QueueMessage msg = c.Take_from_Sent();
            }
        }

        //public void Old_ReceiveMessages()
        //{
        //    String cmd = "";
        //    String msg = "";
        //    long cnt = 0;

        //    this.c.Received("Receiver ready to receive commands.");

        //    Console.WriteLine(">> enter \"exit\" to quit...");

        //    while (true) {
        //        Console.Write("CND>> ");
        //        cmd = Console.ReadLine();
        //        if (cmd == "exit") break;
        //        msg = String.Format("command-{0}", ++cnt);

        //        this.c.Send(cmd, msg);
        //        this.c.ExpectToReceive();

        //    }
        //    this.c.SendClear();
        //}

        static void ClearSendQueue (object sender, MessageTranferEventArgs e)
        {
            Console.WriteLine("CLEARING send-queue");
        }
        //static void AddToReceiveQueue (object sender, MessageTranferEventArgs e)
        //{
        //    Console.WriteLine("Pushing onto receive-queue: {0}", e.Message);
        //}
    }


    public class Dispatcher
    {
        public event EventHandler<MessageTranferEventArgs> MessageSender;
        public event EventHandler<MessageTranferEventArgs> MessageReceiver;
        public event EventHandler<MessageTranferEventArgs> ReceiveProcessor;
        public event EventHandler<MessageTranferEventArgs> MarantzLevelUpdater;
        public event EventHandler<MessageTranferEventArgs> SendClearer;
        public event EventHandler<MessageTranferEventArgs> SendRefresher;
        public event EventHandler<MessageTranferEventArgs> Expecter;
        public event EventHandler<MessageTranferEventArgs> Terminator;
        public event EventHandler<MessageTranferEventArgs> ConnectNotifier;
        public event EventHandler<MessageTranferEventArgs> SliderSetter;
        public event EventHandler<MessageTranferEventArgs> ParameterShower;

        //private delegate void WorkerEventHandler (
        //    int numberToCheck,
        //    AsyncOperation asyncOp);
        private delegate void WorkerEventHandler (MessageTranferEventArgs args, AsyncOperation asyncOp);
        //public delegate void CalculatePrimeCompletedEventHandler (
        //    object sender,
        //    CalculatePrimeCompletedEventArgs e);
        public delegate void CompletedWorkerEventHandler (object sender, ConnectCompletedEventArgs args);

        //public event CalculatePrimeCompletedEventHandler CalculatePrimeCompleted;
        public event CompletedWorkerEventHandler ConnectCompletedHandler = null;

        //private SendOrPostCallback onCompletedDelegate;
        private System.Threading.SendOrPostCallback onCompletedDelegate;

        private System.Collections.Specialized.HybridDictionary userStateToLifetime = new System.Collections.Specialized.HybridDictionary();

        public SendReceive connection { get; set; }
        public frmControler ui { get; set; }
        public Controller queue { get; set; }

        public String last_command = "";


        //public void Start_Session ()
        //{
        //    MessageTranferEventArgs args = new MessageTranferEventArgs();
        //    args.Connection = this.connection;
        //    args.Queue = this.queue;
        //    args.Form = this.ui;
        //    args.Dispatcher = this;
        //    OnMessageExpect(args);  // ReceiveThread.ExpectToReceive
        //}

        public Dispatcher ()
        {
            //this.primeNumberCalculator1.CalculatePrimeCompleted +=
            //    new CalculatePrimeCompletedEventHandler(
            //    primeNumberCalculator1_CalculatePrimeCompleted);
            // ---------------------------------------- no event handler needed
            //ConnectCompletedHandler +=
            //    new CompletedWorkerEventHandler(
            //    Handle_ConnectCompleted);
            //onCompletedDelegate =
            //   new SendOrPostCallback(CalculateCompleted);
            onCompletedDelegate =
               new System.Threading.SendOrPostCallback(ConnectCompleted);
        }
                                                                                      // << Run_Receiver
        public string Send (string message, string description, Boolean askForLevels) // << send_currlev_to_marantz
        {                                                                             // << get_value
            QueueMessage msg = new QueueMessage();
            msg.Message = message;
            msg.Description = description;
            msg.askNewLevel = askForLevels;
            msg.Id = Globs.New_Id();

            MessageTranferEventArgs args = new MessageTranferEventArgs();
            this.last_command = message;
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.msg = msg;

            OnMessageSend(args);    // Form.AddToSendList / Controller.AddToSendQueue / SendThread.SendToReceiver 
                                    // OnMessageExpect(args);  // ReceiveThread.ExpectToReceive  << don't do this, sender does it
            return msg.Id;
        }

        public string Process_Received(QueueMessage msg) // << Run_Receiver
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.msg = msg;

            OnProcessReceived(args);    // frmController.ProcessReceivedMessage | c.ProcessMarantzLevels 

            return args.msg.Id;
        }

        public void Update_MarantzLevels()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;

            OnUpdateMarantzLevels(args);    // frmController.UpdateMarantzLevels 
        }

        public void Show_Message (string message)
        {
            Received(message, MsgType.message);
        }

        public void Show_Error (string message)
        {
            Received(message, MsgType.error);
        }

        public void Show_Sent (string message)
        {
            Received(message, MsgType.sent);
        }

        public void Show_Received (string message)
        {
            Received(message, MsgType.received);
        }
        public void Received (string message, MsgType msgtyp)
        {
            QueueMessage msg = new QueueMessage();
            msg.Message = message;

            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.msg = msg;
            args.MessageType = msgtyp;
            OnMessageReceive(args); //  Form.AddToReceiveList / Controller.AddToReceiveQueue
        }

        public void ReefreshSendList ()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            OnMessageRefreshSend(args);  // Form.RefreshSendList
        }
        public void SetVolumeLevel (LevelController lc) // << update_slider
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.LC = lc;
            OnSetVolumeLevel(args);  // Form.SetSliderLevel
        }

        public void ShowParameters(LevelController lc)
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.LC = lc;
            OnShowParameters(args);  // Form.ShowParameterValues
        }

        public void SendClear ()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            OnMessageClear(args);  // Form.ClearSendList
        }

        //public virtual void CalculatePrimeAsync (
        //    int numberToTest,
        //    object taskId)
        public void Connected ()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            //  OnConnected(args);  // Form.Connected

            object taskId = new object();

            // Create an AsyncOperation for taskId.
            AsyncOperation asyncOp =
                AsyncOperationManager.CreateOperation(taskId);

            // Multiple threads will access the task dictionary,
            // so it must be locked to serialize access.
            lock (userStateToLifetime.SyncRoot) {
                if (userStateToLifetime.Contains(taskId)) {
                    throw new ArgumentException(
                        "Task ID parameter must be unique",
                        "taskId");
                }
                userStateToLifetime[taskId] = asyncOp;
            }

            // Start the asynchronous operation.
            //WorkerEventHandler workerDelegate = new WorkerEventHandler(CalculateWorker);
            WorkerEventHandler workerDelegate = new WorkerEventHandler(OnConnected);

            workerDelegate.BeginInvoke(args, asyncOp, null, null);

            Console.WriteLine("invoked OnConnected in the background");
        }

        public void Exit ()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            OnExit(args);  // ReceiveThread.StopReceiver
        }

        //private void CalculateCompleted (object operationState)
        private void ConnectCompleted (object operationState)
        {
            ConnectCompletedEventArgs e =
                operationState as ConnectCompletedEventArgs;

            //OnCalculatePrimeCompleted(e);
            OnConnectCompleted(e);
        }
        //protected void OnCalculatePrimeCompleted (
        //    CalculatePrimeCompletedEventArgs e)
        //{
        //    if (CalculatePrimeCompleted != null) {
        //        CalculatePrimeCompleted(this, e);
        //    }
        //}
        protected void OnConnectCompleted (
            ConnectCompletedEventArgs e)
        {
            if (ConnectCompletedHandler != null) {
                ConnectCompletedHandler(this, e);
            }
        }


        //private void CalculateWorker (
        //    int numberToTest,
        //    AsyncOperation asyncOp)
        protected virtual void OnConnected (MessageTranferEventArgs e, AsyncOperation asyncOp)
        {
            Exception exception = null;

            EventHandler<MessageTranferEventArgs> handler = ConnectNotifier;
            if (handler != null) {
                handler(this, e);
            }

            //      this.CompletionMethod(
            //          numberToTest,
            //          firstDivisor,
            //          isPrime,
            //          e,
            //          TaskCanceled(asyncOp.UserSuppliedState),
            //          asyncOp);

            lock (userStateToLifetime.SyncRoot) {
                userStateToLifetime.Remove(asyncOp.UserSuppliedState);
            }
            ConnectCompletedEventArgs eres =
                new ConnectCompletedEventArgs(e, exception, false, asyncOp.UserSuppliedState);

            //asyncOp.PostOperationCompleted(onCompletedDelegate, e);
            asyncOp.PostOperationCompleted(onCompletedDelegate, eres);
        }

        // ----------------------------- moved up inside OnConnected
        //private void CompletionMethod (
        //        MessageTranferEventArgs args,
        //        Exception exception,
        //        bool canceled,
        //        AsyncOperation asyncOp)

        //{
        //    // If the task was not previously canceled,
        //    // remove the task from the lifetime collection.
        //    if (!canceled) {
        //        lock (userStateToLifetime.SyncRoot) {
        //            userStateToLifetime.Remove(asyncOp.UserSuppliedState);
        //        }
        //    }

        //    // Package the results of the operation in a 
        //    // ConnectCompletedEventArgs.
        //    //ConnectCompletedEventArgs e =
        //    //    new ConnectCompletedEventArgs(
        //    //    numberToTest,
        //    //    firstDivisor,
        //    //    isPrime,
        //    //    exception,
        //    //    canceled,
        //    //    asyncOp.UserSuppliedState);
        //    ConnectCompletedEventArgs e =
        //        new ConnectCompletedEventArgs(
        //        args,
        //        exception,
        //        canceled,
        //        asyncOp.UserSuppliedState);

        //    // End the task. The asyncOp object is responsible 
        //    // for marshaling the call.
        //    asyncOp.PostOperationCompleted(onCompletedDelegate, e);

        //    // Note that after the call to OperationCompleted, 
        //    // asyncOp is no longer usable, and any attempt to use it
        //    // will cause an exception to be thrown.
        //}


        protected virtual void OnSetVolumeLevel (MessageTranferEventArgs e)  // Form::SetSliderLevel
        {
            EventHandler<MessageTranferEventArgs> handler = SliderSetter;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnShowParameters (MessageTranferEventArgs e)  // Form::ShowParameterValues
        {
            EventHandler<MessageTranferEventArgs> handler = ParameterShower;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMessageRefreshSend (MessageTranferEventArgs e)  // Form::RefreshSendList
        {
            EventHandler<MessageTranferEventArgs> handler = SendRefresher;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMessageClear (MessageTranferEventArgs e)  // Form::ClearSendList
        {
            EventHandler<MessageTranferEventArgs> handler = SendClearer;
            if (handler != null) {
                handler(this, e);
            }
        }
                                                                          // two listeners...
        protected virtual void OnMessageSend (MessageTranferEventArgs e)  // Form::AddToSendList
        {                                                                 // AddToSendQueue
            EventHandler<MessageTranferEventArgs> handler = MessageSender;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnMessageReceive (MessageTranferEventArgs e)  // Form::AddToReceiveList
        {
            EventHandler<MessageTranferEventArgs> handler = MessageReceiver;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnProcessReceived(MessageTranferEventArgs e)  // Form::ProcessReceivedMessages
        {
            EventHandler<MessageTranferEventArgs> handler = ReceiveProcessor;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnUpdateMarantzLevels(MessageTranferEventArgs e)  // Form::UpdateMarantzLevels
        {
            EventHandler<MessageTranferEventArgs> handler = MarantzLevelUpdater;
            if (handler != null) {
                handler(this, e);
            }
        }
        
        protected virtual void OnMessageExpect (MessageTranferEventArgs e)
        {
            EventHandler<MessageTranferEventArgs> handler = Expecter;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnExit (MessageTranferEventArgs e)
        {
            EventHandler<MessageTranferEventArgs> handler = Terminator;
            if (handler != null) {
                handler(this, e);
            }
        }
    }

    public class MessageTranferEventArgs : EventArgs
    {
        public QueueMessage msg { get; set; }
        public MsgType MessageType { get; set; }
        public frmControler Form { get; set; }
        public Controller Queue { get; set; }
        public Dispatcher Dispatcher { get; set; }
        public SendReceive Connection { get; set; }
        public LevelController LC { get; set; }
        public MultiLevelController MLC { get; set; }
    }


    public class ConnectCompletedEventArgs : AsyncCompletedEventArgs
    {
        private MessageTranferEventArgs args;
        public ConnectCompletedEventArgs (MessageTranferEventArgs _args, Exception e, bool canceled, object state) :
            base(e, canceled, state)
        {
            this.args = _args;
        }
        public MessageTranferEventArgs event_args
        {
            get {
                return this.args;
            }
        }
    }

    class Program
    {
        static void Main (string[] args)
        {
            frmControler frm = new frmControler();

            frm.ShowDialog();

            //String host = Globs.marantz_host;

            //Controller ui = new Controller();
            //Proccessor pr = new Proccessor(ui,host);

            //ui.Go();
            //pr.Go();

            //Boolean ui_open;
            //Boolean processing;

            //while (true) {
            //    ui_open = ui.Still_Open();
            //    processing = pr.Still_Processing();
            //    if (!ui_open || !processing) {
            //        break;
            //    }
            //    for (int t = 0; t < 100; t++) {
            //        System.Windows.Forms.Application.DoEvents();
            //    }
            //    System.Threading.Thread.Sleep(2000);
            //}
        }
    }
}
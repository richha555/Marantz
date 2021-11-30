mlc = "Master L|R Balance", 
      new string[] { "CVFL", "CVSBL" },  /* left top */
      new string[] { "CVFR", "CVSBR" },  /* right top */
      new string[] { "CVSL" },           /* left bottom */
      new string[] { "CVSR" },           /* right bottom */
      horiz, (ScrollBar)this.zMasterBalance, this.txtMasterBalance, this.d);



ask L & R  ===>  calculate initial balance
           ===>  show calculated balance on balance control

                 bal = BalanceMapper.LR_to_Balance(L,R)

user changes balance slider

           ===>  calculate new L & R

                 BalanceMapper.new_LR_Balance_w_Power(initialL,initialR,
                                                      balance,
                                                      ref newL, ref newR)

INIT - Form1_Load
	lc = LevelControllerFactory.New("Main Volume", "MV", horiz, (ScrollBar)this.zMasterVolume, this.txtMasterVolume, this.d);
    mlc = new MultiLevelController("Master L|R Balance", 
                                    new string[] { "CVFL", "CVSBL" },  /* left top */
                                    new string[] { "CVFR", "CVSBR" },  /* right top */
                                    new string[] { "CVSL" },           /* left bottom */
                                    new string[] { "CVSR" },           /* right bottom */
                                    horiz, (ScrollBar)this.zMasterBalance, this.txtMasterBalance, this.d);
		set_gui_levels(min=-50,max=50,lev=0)  // there is no marantz balance control, so we report marantz levels as -50 to 50 with a start of "0"
            marantz_min_level = min_lev;
            marantz_max_level = max_lev;

	Connected
		c.SetCurrentMarantzLevels(lev_ctrls,"MV")
			 val = get_value(command + "?",ref msg)
			 ProcessMarantzLevels(levctrls, msg)
				val = msg.Reply
				parse to find levels for each parm
				find lc for the cmmand

				lc.set_gui_levels(min=unknown,max=unknown,level,msg)
				    marantz_start_level = lev;
					marantz_current_level = lev;
					reported_level = lev;




ZMasterVolume_Scroll
	set_volume
		lev = sbar.Value
		lc.change_level(lev)
			chg_amt = f(lev,last_slider_level)
			send_currlev_to_marantz(chg_amt)
				marantz_current_level += chg_amt
				balance_adjusted_level = balance_value * marantz_current_level
				
				cmd = "XXX" + int_lev

				d.Send(cmd)	
				
ZMasterBalance_Scroll
	set_balance
		lev = sbar.Value
		mlc.change_balance(lev)
            if (new_level <= 50.0) {
                l_lev = 1.0;
                r_lev = new_level / 50.0;
            } else {
                l_lev = 1.0 - (new_level - 50.0) / 50.0;
                r_lev = 1.0;
            }
            foreach (LevelController lc in this.left_bot_lcs) {
                lc.balance_value = l_lev;
                lc.send_currlev_to_marantz(0);
            }
            foreach (LevelController lc in this.left_bot_lcs) {
                lc.balance_value = r_lev;
                lc.send_currlev_to_marantz(0);
            }


Send_Receive_Thread
	SendReceive.Run_Receiver
		Dispatcher.Process_Received
			Dispatcher.OnProcessReceived --v
			frmController.ProcessReceivedMessage    // this.d.ReceiveProcessor += ProcessReceivedMessage;
				Controller.ProcessMarantzLevels
					foreach (lc)
						if (lc.command == cmd)
							lc.set_gui_levels(-1.0, -1.0, level)
								reported_level = lev

								show_currlev_from_marantz

									if (ourmessage)
										Dispatcher.ShowParameters
											ParameterShower
											frmController::ShowParameterValues
												last_slider_value/marantz_current_level/reported_level >> slider_text

									balance_adjusted_level = reported_level
									if (balance_value == 1)
										marantz_current_level = balance_adjusted_level
									this.gui_flags[fTxt] = true;

									update_slider(mlev)
										last_slider_value = m * mlev + b  // marantz level to slider value
										Dispatcher.SetVolumeLevel
											SliderSetter
											frmController.SetSliderLevel
												last_slider_value >> slider.Value
												frmController::ShowParameterValues
													last_slider_value/marantz_current_level/reported_level >> slider_text

frmControler_Resize()
	Resizer.HandleResize
		Dispatcher.ResizeAllThumbs
			Dispatcher.OnResizeAllThumb --v
			frmController.ResizeAllThumbs     // this.d.ThumbResizer += ResizeAllThumbs;
				Controller.ResizeAllThumbs
					foreach (lc)
						resize_thumb()
							this.gui_flags[fResize] = true
							Dispatcher.ResizeThumb
								ResizeHandler
								frmController.SetSliderLevel
									recalculate pageSize
									lc.load_slider_parms



    public partial class frmControler : Form
    {
        Dispatcher d;
        Controller c;
        SendReceive sr;
        System.Threading.Thread send_receive_thread;


        private void Form1_Load (object sender, EventArgs e)
        {
            this.d = new Dispatcher();  // Dispatcher handle events

            this.d.ReceiveProcessor += ProcessReceivedMessage;
            this.d.SliderSetter += SetSliderLevel;
            this.d.ResizeHandler += SetSliderLevel;
            this.d.ThumbResizer += ResizeAllThumbs;

            this.c = new Controller(this.d);

            this.rz = new Resizer(this.d);

            this.sr = new SendReceive(this.d);   // Connection manages connection to remote host

            this.send_receive_thread = new System.Threading.Thread(Send_Receive_Thread);

            this.send_receive_thread.Start(this.sr);
		}

        private void frmControler_Resize(object sender, EventArgs e)
        {
            this.rz.HandleResize();
        }

        public void Send_Receive_Thread (object o)
        {
            SendReceive m_sr = (SendReceive)o;
            m_sr.Run_Receiver();
        }

        static void ResizeAllThumbs(object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            Controller c = e.Dispatcher.queue;

            c.ResizeAllThumbs(LevelControllerFactory.lev_ctrls);
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

        static void ResizeAllThumbs(object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            Controller c = e.Dispatcher.queue;

            c.ResizeAllThumbs(LevelControllerFactory.lev_ctrls);
        }

        static void SetSliderLevel (object sender, MessageTranferEventArgs e)
        {
            frmControler frm = (frmControler)e.Form;
            LevelController lc = (LevelController)e.LC;
            ScrollBar sbar = lc.sbar;
            TextBox txt = lc.txt;

                if (sbar.InvokeRequired) {
                    var d = new SafeCallDelegate(SetSliderLevel);
                    sbar.Invoke(d, new object[] { sender, e });
                } else {
                    if (lc.gui_flags[LevelController.fLev]) {
                        sbar.Value = lc.last_slider_value;
                    }
                }
            }
            lc.gui_flags[LevelController.fLev] = false;
        }

    }

    public class Resizer
    {
        Dispatcher d = null;

        public void HandleResize ()
        {
            this.d.ResizeAllThumbs();
        }
    }

	public class SendReceive
    {
        Dispatcher d = null;
        Controller c = null;  // set in Run_Receiver by copying from Dispatcher

        public void Run_Receiver ()
        {
            while (read_msg) {
                this.d.Process_Received(tmsg);   // frmController.ProcessReceivedMessage | c.ProcessMarantzLevels
            }
        }
	}

    public class Dispatcher
    {
        public event EventHandler<MessageTranferEventArgs> ReceiveProcessor;
        public event EventHandler<MessageTranferEventArgs> SliderSetter;
        public event EventHandler<MessageTranferEventArgs> ResizeHandler;

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

        protected virtual void OnProcessReceived(MessageTranferEventArgs e)  // Form::ProcessReceivedMessages
        {
            EventHandler<MessageTranferEventArgs> handler = this.ReceiveProcessor;
            if (handler != null) {
                handler(this, e);
            }
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

        protected virtual void OnSetVolumeLevel (MessageTranferEventArgs e)  // Form::SetSliderLevel
        {
            EventHandler<MessageTranferEventArgs> handler = this.SliderSetter;
            if (handler != null) {
                handler(this, e);
            }
        }

        public void ResizeThumb() // << resize_thumb
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;

            OnResizeThumb(args);  // reuse Form.SetSliderLevel to change thumb size
        }

        protected virtual void OnResizeAllhumbs(MessageTranferEventArgs e)  // Form::ResizeAllThumbs  (reuse OnSetVolumeLevel handler)
        {
            EventHandler<MessageTranferEventArgs> handler = this.ThumbResizer;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnResizeThumb(MessageTranferEventArgs e)  // Form::SetSliderLevel  (reuse OnSetVolumeLevel handler)
        {
            EventHandler<MessageTranferEventArgs> handler = this.ResizeHandler;
            if (handler != null) {
                handler(this, e);
            }
        }
 
    }

    public class Controller
    {
        public void ProcessMarantzLevels(Dictionary<String, LevelController> levctrls, QueueMessage msg)
        {
            foreach (string levctrl in levctrls.Keys) {
                LevelController lc = levctrls[levctrl];
                lc.set_gui_levels(Globs.unknown, Globs.unknown, level, msg);
            }
        }
        public void ResizeAllThumbs(Dictionary<String, LevelController> levctrls)
        {
            foreach (string levctrl in levctrls.Keys) {
                LevelController lc = levctrls[levctrl];
                lc.resize_thumb();
            }
        }
    }


    public class LevelController
    {
        public Dispatcher d = null;
        public Controller c = null;

        public Boolean[] gui_flags = { false, false, false, false, false };

        public void set_gui_levels(double min_lev, double max_lev, double lev, QueueMessage msg)
        {
           if (is_unknown(marantz_start_level) && !is_unknown(lev)) {
                marantz_start_level = lev;
                marantz_current_level = lev;
                reported_level = lev;
                this.gui_flags[fLev] = true;
            }
            show_currlev_from_marantz(msg);
        }

        public void show_currlev_from_marantz(QueueMessage msg)
        {
            this.update_slider(this.marantz_current_level);
        }

        public void resize_thumb()
        {
            this.gui_flags[fResize] = true;

            this.d.ResizeThumb();
        }

        public void update_slider(double marantz_lev)
        {
            this.last_slider_value = this.sliderMarantzMapper.B_to_intA(marantz_lev);
            this.gui_flags[fLev] = true;  // also update slider

            this.d.SetVolumeLevel(this);  // copy last_slider_value to scrollbar.Value
        }
    }




CVFL -> 51.5 - Volume Settings\Channel Volume FL\CHANNEL VOLUME UP/DOWN , direct change to **dB
CVFR -> 52.0 - Volume Settings\Channel Volume FR\
CVC -> 50.0 - Center-Speaker harder|softer
CVSW -> 50.0 - Volume Settings\Channel Volume SW\
CVSL -> 45.0 - Back Left Surround Speaker harder|softer
CVSR -> 40.0 - Back Right Surround Speaker harder|softer
CVSBL -> 50.0 - Front Left Lower-Half harder|softer
CVSBR -> 50.0 - Front Right Lower-Half harder|softer
CVSB -> 50.0 - Volume Settings\Channel Volume SB\---SURROUND BACK ch      (SBch 1SP)
CVFHL -> 53.4 - Volume Settings\Channel Volume FHL\---FRONT HEIGHT Lch  
CVFHR -> 50.0 - Volume Settings\Channel Volume FHR\---FRONT HEIGHT Rch  
CVFWL -> 50.0 - Volume Settings\Channel Volume FWL\---FRONT WIDE Lch
CVFWR -> 50.5 - Volume Settings\Channel Volume FWR\---FRONT WIDE Rch
MVMAX -> 75.0 - UNKNOWN CMD
@DCM -> 3.0 - UNKNOWN CMD


						
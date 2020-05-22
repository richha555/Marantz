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
		mlc.change_level(lev)
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



SendReceive.Run_Receiver
	Dispatcher.Process_Received
		frmController.ProcessReceivedMessage
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


						
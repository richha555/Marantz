// minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
//
// http://www.corebvba.be



using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace MarantzController
{
    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255,   // Interpret the following as a command
        GA = 249,    // Go Ahead
        NOP = 241,   // No Operation
        DM = 242,    // Data Mark
        BRK = 243,   // Break key
        IP = 244,    // Suspend/Interrupt/Abort
        AO = 245,    // Abort output
        AYT = 246,   // Are you there?
        EC = 247,    // Erase Char
        EL = 248,    // Erase Line
        END = 240,   // SE
        SB = 250,    // subnegotiation follows
        IS = 0       // is
    }

    enum Options
    {
        ECHO = 1,
        SGA = 3,      // Supress "Go Ahead"
        STAT = 5,     // status
        TM = 6,       // timing-mark
        TT = 24,      // Terminal Type
        NAWS = 31,    // Windows Size
        TS = 32,      // Terminal Speed  hex 20 = dec 32
        FLOW = 33,    // remote flow control
        LINE = 34,    // line-mode
        ENV = 36,     // environment var's
        CHARSET = 42, // Character-Set
        NENV = 39     // New Environment
    }

    public class TelnetConnection
    {
        TcpClient tcpSocket;
        Dispatcher d = null;

        int TimeOutMs = Globs.receive_timeout_ms; // 300;

        public TelnetConnection (string Hostname, int Port)
        {
            tcpSocket = new TcpClient(Hostname, Port);
        }

        public Dispatcher dispatcher 
        {
            set { this.d = value; }
        }

        public string Msg (Byte msg)
        {
            if (Globs.ControlMsgs.ContainsKey(msg)) {
                return String.Format("<{0}>", Globs.ControlMsgs[msg] );
            }
            return String.Format("<{0}>", (int)msg);
        }

        public string Login (string Username, string Password, int LoginTimeOutMs)
        {
            int oldTimeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            string s = Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);

            s += Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);

            s += Read();
            TimeOutMs = oldTimeOutMs;
            return s;
        }

        public void InitConversation ()
        {
            // PuTTY...
            // Will Negotiate Window Size
            // Will Terminal Speed
            // Will Terminal Type
            // Will New Environment Option
            // Do Echo
            // Will Suppress Go Ahead
            // Do Supress Go Ahead
            // MARANTZ...
            // Terminal Type
            // 00 XTERM End
            // PuTTY...
            // Terminal Type
            // End
            // MARANTZ...
            // Will Suppress Go Ahead
            // MARANTZ...
            // BridgeCo AG Telnet server\n\r
            // PuTTY...
            // MS?\r
            // MARANTZ...
            // MSDTS NEO:6\r

            tcpSocket.NoDelay = false;  // try using flush instead of NoDelay

            /// US II...
            SendOptions(Verbs.WILL, Options.NAWS);
            SendOptions(Verbs.WILL, Options.TS);
            SendOptions(Verbs.WILL, Options.TT);
            SendOptions(Verbs.WILL, Options.NENV);
            SendOptions(Verbs.DO, Options.ECHO);
            SendOptions(Verbs.WILL, Options.SGA);
            SendOptions(Verbs.DO, Options.SGA);

            tcpSocket.GetStream().Flush();

            /// US...
            /// Do Echo
            /// Dont Supress Go Ahead
            /// All Will/Wonts are handled by ParseTelnet
            //  SendOptions(Verbs.DO, Options.ECHO);
            //  SendOptions(Verbs.DONT, Options.SGA);   // try supressing go-ahead ... doesn't help
            //  SendOptions(Verbs.DO, Options.SGA);
        }

        void SendOptions (Verbs do_dont, Options option)
        {
            if (!tcpSocket.Connected)
                return;

            this.d.Show_Sent(String.Format("SEND: {0}{1}{2}", this.Msg((byte)Verbs.IAC), this.Msg((byte)do_dont), this.Msg((byte)option)));

            tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);  // here comes a command
            tcpSocket.GetStream().WriteByte((byte)do_dont);
            tcpSocket.GetStream().WriteByte((byte)option);
        //  tcpSocket.GetStream().Flush();
        }

        void SendVerb (Verbs verb)
        {
            if (!tcpSocket.Connected)
                return;

            this.d.Show_Sent(String.Format("SEND: {0}", this.Msg((byte)verb)));

            tcpSocket.GetStream().WriteByte((byte)verb);
        //  tcpSocket.GetStream().Flush();
        }

        void SendEndOfData ()
        {
            if (!tcpSocket.Connected)
                return;

            this.d.Show_Sent(String.Format("SEND: {0}{1}", this.Msg((byte)Verbs.IAC), this.Msg((byte)Verbs.END)));

            tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
            tcpSocket.GetStream().WriteByte((byte)Verbs.END);
        //  tcpSocket.GetStream().Flush();
        }

        public void WriteLine (string cmd)
        {
        //  Write(cmd + "\n");  // marantz uses \r not \n
            Write(cmd + "\r");
        }

        public void Write (string cmd)
        {
            if (!tcpSocket.Connected)
                return;

            this.d.Show_Sent(String.Format("SEND: {0}", cmd.Replace("\n","<LF>").Replace("\r", "<CR>")));

            byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF","\0xFF\0xFF"));
            try {
                tcpSocket.GetStream().Write(buf, 0, buf.Length);
            //  tcpSocket.GetStream().Flush();
            } catch (SocketException ex) {
                this.d.Show_Error(String.Format("SEND-ERROR: {0}", ex.Message));
            } catch (Exception ex) {
                this.d.Show_Error(String.Format("SEND-ERROR: {0}", ex.Message));
            }
        }

        public string Read ()
        {
            if (!tcpSocket.Connected)
                return null;
            StringBuilder sb=new StringBuilder();
            do {
                ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);  // wait for next char
            } while (tcpSocket.Available > 0);

            string msg = sb.ToString();
            if (!String.IsNullOrEmpty(msg)) {
                this.d.Show_Received(String.Format("GOT: {0}", msg.Replace("\n", "<LF>").Replace("\r", "<CR>")));
                Console.WriteLine(msg);
            }
            return msg;
        }

        public bool IsConnected
        {
            get { return tcpSocket.Connected; }
        }

        public bool HaveData
        {
            get { return tcpSocket.Available > 0; }
        }

        void ParseTelnet (StringBuilder sb)
        {
            while (tcpSocket.Available > 0) {
                int input = tcpSocket.GetStream().ReadByte();
                switch (input) {
                    case -1:
                        this.d.Show_Received(String.Format("GOT: {0}", "-1"));
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1) {
                            this.d.Show_Received(String.Format("GOT: {0}{1}", this.Msg((byte)input), "-1"));
                            break;
                        }
                        switch (inputverb) {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) {
                                    this.d.Show_Received(String.Format("GOT: {0}{1}{2}", this.Msg((byte)input), this.Msg((byte)inputverb), "-1"));
                                    break;
                                }
                                this.d.Show_Received(String.Format("GOT: {0}{1}{2}", this.Msg((byte)input), this.Msg((byte)inputverb), this.Msg((byte)inputoption)));
                                if ((inputverb == (int)Verbs.WILL) && (inputoption == (int)Options.SGA)) {
                                    this.d.Show_Sent("SEND: <nothing back>");
                                    break;
                                }
                                if (inputoption == (int)Options.SGA) {
                                    this.d.Show_Sent(String.Format("SEND: {0}{1}{2}", this.Msg((byte)Verbs.IAC), this.Msg(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO), this.Msg((byte)inputoption)));
                                } else {
                                    this.d.Show_Sent(String.Format("SEND: {0}{1}{2}", this.Msg((byte)Verbs.IAC), this.Msg(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT), this.Msg((byte)inputoption)));
                                }
                                tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA) {
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                } else {
                                    tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                }
                                tcpSocket.GetStream().WriteByte((byte)inputoption);
                                tcpSocket.GetStream().Flush();
                                break;
                            case (int)Verbs.SB:
                                // RECEIVE...
                                // ff 255  Verbs.IAC
                                // fa 250  Verbs.SB   ...follwed by...
                                // ------------------------------------- 
                                // 01 1    Options.ECHO  << should be Options.TT followed by Options.ECHO
                                // ff 255  Verbs.IAC
                                // f0 240  Verbs.END
                                this.d.Show_Received(String.Format("GOT: {0}{1}", this.Msg((byte)input), this.Msg((byte)inputverb)));
                                while (tcpSocket.Available > 0) {
                                    int inputoption2 = tcpSocket.GetStream().ReadByte();
                                    if (inputoption2 == -1) {
                                        break;
                                    }
                                    this.d.Show_Received(String.Format("GOT: ...{0}", this.Msg((byte)inputoption2)));
                                }
                                // SEND...               IAC SB TERMINAL-TYPE IS ... IAC SE
                                // ff 255  Verbs.IAC
                                // fa 250  Verbs.TT
                                // 18 24   Options.TT
                                // 00 00
                                // XTERM
                                // ff 255  Verbs.IAC
                                // f0 240  Verbs.END
                                System.Threading.Thread.Sleep(50);  // pause briefly
                                SendOptions(Verbs.SB,Options.TT);
                                SendVerb(Verbs.IS);
                             // this.d.Show_Sent(String.Format("SEND: XTERM"));
                                Write("XTERM"); // <<<< terminal type
                                SendEndOfData();
                                tcpSocket.GetStream().Flush();
                                break;
                            default:
                                this.d.Show_Received(String.Format("GOT: {0}{1}", this.Msg((byte)input), this.Msg((byte)inputverb)));
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }
    }
}

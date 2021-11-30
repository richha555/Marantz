// minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
//
// http://www.corebvba.be

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

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
        public static Boolean NO_SEND = true;

        public static int receive_timeout_ms = 300;  // pause after receiving a char before checking if there is another character
        public static int user_timeout_ms = 1500;
        public static int loop_pause_ms = 3000;
        public static System.Collections.Concurrent.ConcurrentQueue<QueueMessage> cmdQueue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();
        public static System.Collections.Concurrent.ConcurrentQueue<QueueMessage> responseQueue = new System.Collections.Concurrent.ConcurrentQueue<QueueMessage>();
        //  public static System.Collections.Concurrent.ConcurrentQueue<string> responseQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();
        public static string json_file = @"C:\Users\Richard\Documents\Visual Studio 2010\Projects\Marantz\MarantzController\Commands.json";
        public static string json_file2 = @"\\REH_Akoya\C\Users\Richard\Documents\Visual Studio 2010\Projects\Marantz\MarantzController\Commands.json";
        public static string json_file3 = @"C:\Users\user\Documents\Visual Studio 2017\Projects\Marantz\MarantzController\Commands.json";
        public static string marantz_host = "marantz"; // "192.168.1.109";  // marantz
        public static string marantz_connect_message = "BridgeCo AG Telnet server";

        //  public static Boolean NO_SEND = false;

        public static double unknown = (double)int.MinValue;

        public static readonly Dictionary<MsgType, System.Drawing.Color> MsgColors = new Dictionary<MsgType, System.Drawing.Color>
        {
            { MsgType.sent, System.Drawing.Color.DarkSeaGreen },
            { MsgType.received, System.Drawing.Color.LightBlue },
            { MsgType.message, System.Drawing.Color.LightGoldenrodYellow },
            { MsgType.error, System.Drawing.Color.LightPink }
        };
        public static readonly Dictionary<Byte, string> ControlMsgs = new Dictionary<Byte, string>
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
        private double cA_to_B;
        private double mB_to_A;
        private double cB_to_A;

        public LevelMapper(double _minA, double _maxA, double _minB, double _maxB)
        {
            //  m = (maxB - minB) / (maxA - minA)
            //  B = m * A + c
            //  c = minB - m * minA
            minA = _minA;  maxA = _maxA; minB = _minB; maxB = _maxB;

            Calc_Parms();
        }

        public void Adjust_A(double _minA, double _maxA)
        {
            minA = _minA; maxA = _maxA;  Calc_Parms();
        }

        public void Adjust_B(double _minB, double _maxB)
        {
            minB = _minB; maxB = _maxB;  Calc_Parms();
        }

        // y = m * x + b
        // ymin = m * xmin + b
        // b = ymin - m * xmin
        // y = m * x + (ymin - m * xmin)
        // y = (m * x - m * xmin) + ymin
        // y = m * (x - xmin) + ymin
        // B = m * (A - minA) + minB
        // (B - minB) = m * (A - minA)
        // (B - minB) / m = A - minA
        // A = (B - minB) / m + minA
        // A = (1/m) + (B - minB) + minA

        private void Calc_Parms()
        {
            mA_to_B = ((maxB - minB) / (maxA - minA));
            mB_to_A = ((maxA - minA) / (maxB - minB));
            Console.WriteLine("Level-Mapper: minA = {0} maxA = {1}  minB = {2} maxB = {3}", minA, maxA, minB, maxB);
            Console.WriteLine("Level-Mapper: B = {0} * (A - {1}) + {2}", mA_to_B, minA, minB);
            Console.WriteLine("Level-Mapper: A = {0} * (B - {1}) + {2}", mB_to_A, minB, minA);
        }

        public double Limit_A (double A)
        {
            if (this.minA < this.maxA) {
                return Math.Min(this.maxA, Math.Max(this.minA, A));
            } else {
                return Math.Min(this.minA, Math.Max(this.maxA, A));
            }
        }

        public double Limit_B (double B)
        {
            if (this.minB < this.maxB) {
                return Math.Min(this.maxB, Math.Max(this.minB, B));
            } else {
                return Math.Min(this.minB, Math.Max(this.maxB, B));
            }
        }

        public double A_change_percent (int oldA, int newA)
        {
            double oldB = A_to_B((double)oldA);
            double newB = A_to_B((double)newA);
            double chg_perc = 100.0 * ((newB - oldB) / (this.maxB - this.minB));
            return chg_perc;
        }

        public double A_to_B (double A)
        {                                              // B = m * (A - minA) + minB
            double B = (A - minA) * mA_to_B + minB;    // y = m (x - x1) + y1
            return Limit_B( B );
        }
        public double B_to_A(double B)
        {                                            // A = (1/m) + (B - minB) + minA
            double A = mB_to_A * (B - minB) + minA;  // x = (y - y1) / m  + x1
            return Limit_A( A );
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
            return (int)Limit_B( Math.Round(B) );
        }
        public int B_to_intA(double B)
        {
            double A = B_to_A(B);
            return (int)Limit_A( Math.Round(A) );
        }
        public int intA_to_intB(int A)
        {
            double B = A_to_B((double)A);
            return (int)Limit_B( Math.Round(B) );
        }
        public int intB_to_intA(int B)
        {
            double A = B_to_A((double)B);
            return (int)Limit_A( Math.Round(A) );
        }
    }

    public class PlaneMapper
    {
        public double A;
        public double B;
        public double C;
        public double D;

        public PlaneMapper (double x1, double y1, double z1,
                            double x2, double y2, double z2,
                            double x3, double y3, double z3)
        {
            double[,] m = new double[4, 4];

            // x = left  y = right  z = balance

            //        |  x   y   z   1  |
            //  DET ( |  x1  y1  z1  1  | ) = 0
            //        |  x2  y2  z2  1  |
            //        |  x3  y3  z3  1  |

            m[0, 0] = 1111; // "x"
            m[0, 1] = 2222; // "y"
            m[0, 2] = 3333; // "z"
            m[0, 3] = 1;

            m[1, 0] = x1;
            m[1, 1] = y1;
            m[1, 2] = z1;
            m[1, 3] = 1;

            m[2, 0] = x2;
            m[2, 1] = y2;
            m[2, 2] = z2;
            m[2, 3] = 1;

            m[3, 0] = x3;
            m[3, 1] = y3;
            m[3, 2] = z3;
            m[3, 3] = 1;

            //  det[ m ] = 0
            //  x [ d0 ] - y [ d1 ] + z [ d2 ] - 1 [ d3 ] = 0  //  + - + -
            //  x   A    - y   B    + z   C    -     D    = 0

            A = det(0, m);
            B = det(1, m);
            C = det(2, m);
            D = det(3, m);

            // plane is:   Ax - By + Cz = D

            Console.WriteLine("Level-Mapper: x1 = {0} y1 = {1} z1 = {2}", x1, y1, z1);
            Console.WriteLine("Level-Mapper: x2 = {0} y2 = {1} z2 = {2}", x2, y2, z2);
            Console.WriteLine("Level-Mapper: x3 = {0} y3 = {1} z3 = {2}", x3, y3, z3);
            Console.WriteLine("Level-Mapper: {0} x - {1} y + {2} z - {3} = 0", A, B, C, D);
        }

        public double x_y_to_z (double x, double y)
        {
            // A * x  -  B * y  +  C * z  =  D
            // z = (D  -  (A * x)  +  (B * y)) / C
            return ((D - (A * x) + (B * y)) / C);
        }
        public double x_z_to_y(double x, double z)
        {
            // A * x  -  B * y  +  C * z  =  D
            // y = ((A * x)  +  (C * z) - D) / B
            return (((A * x) + (C * z) - D) / B);
        }
        public double y_z_to_x(double y, double z)
        {
            // A * x  -  B * y  +  C * z  =  D
            // x = (D  +  (B * y)  -  (C * z))  / A
            return ((D + (B * y) - (C * z)) / A);
        }

        double det (int n, double[,] m)
        {
            double d;
            double[,] m2 = new double[3, 5];

            // skip row 0 & column n

            int c2 = 0;
            for (int c = 0; c <= 3; c++) {
                if (c != n) {
                    int r2 = 0;
                    for (int r = 1; r <= 3; r++) {
                        m2[r2, c2] = m[r, c];
                        r2++;
                    }
                    c2++;
                }
            }
            // duplicate 1st two columns on the right
            c2 = 3;
            for (int c = 0; c <= 1; c++) {
                int r2 = 0;
                for (int r = 0; r <= 2; r++) {
                    m2[r2, c2] = m2[r, c];
                    r2++;
                }
                c2++;
            }
            // now do cross-product

            d = (m2[0, 0] * m2[1, 1] * m2[2, 2]) + 
                (m2[0, 1] * m2[1, 2] * m2[2, 3]) + 
                (m2[0, 2] * m2[1, 3] * m2[2, 4]) - 
                (m2[0, 2] * m2[1, 1] * m2[2, 0]) - 
                (m2[0, 3] * m2[1, 2] * m2[2, 1]) - 
                (m2[0, 4] * m2[1, 3] * m2[2, 2]);

            return d;
        }
    }

    public class XFadeMapper
    {   //                              -50.0 .. +50.0
        public double LBbal;  // BLUE:  1.138 .. 0.000
        public double RFbal;  // RED :  0.000 .. 1.138

        public XFadeMapper(double balance)
        {
            //  y = 0,138 * LOG(-5 * x + 251,8; 2) + -0,1
            this.LBbal = 0.138 * Math.Log(-5.0 * balance + 251.8, 2) + -0.1;   // 0 ... 1     x = 50  y = 0   x = 0  y = 1  blue
            //  y = 0,138 * LOG(5 * x + 251,8; 2) + -0,1
            this.RFbal = 0.138 * Math.Log(5.0 * balance + 251.8, 2) + -0.1;    // 0 ... 1    x = -50  y = 0   x = 0  y = 1  brown
        }
        public double L_to_R (double L)
        {
            double A, R;

            if (this.LBbal < 0.01) {
                R = (double)int.MaxValue;
            } else {
                //  L = A * LBbal
                A = L / this.LBbal; // scale up to actual L
                //  R = A * RFbal
                R = this.RFbal * A; // scale up R by same amount
            }
            return R;
        }
        public double B_to_F(double B)
        {
            return L_to_R(B);
        }
        public double R_to_L(double R)
        {
            double A, L;

            if (this.RFbal < 0.01) {
                L = (double)int.MaxValue;
            } else {
                //  R = A * RFbal
                A = R / this.RFbal;    // scale up to actual R
                //  L = A * LBbal
                L = this.LBbal * A; // scale up R by same amount
            }
            return L;
        }
        public double F_to_B(double F)
        {
            return R_to_L(F);
        }
    }

    public class BalanceMapper
    {
        //private PlaneMapper pm;

        public double initialL = -1000.0;
        public double initialR = -1000.0;
        public double initialB = -1000.0;
        public double initialF = -1000.0;


        delegate double PowerCalc(double lbal,double rbal, XFadeMapper xp);

        public BalanceMapper (double minBalance, double maxBalance)
        { //                      L/B     R/F  Balance    // supply 3 points to determine 3-dimensional plane
          //pm = new PlaneMapper(   0,    100, maxBalance,
          //                      100,      0, minBalance,
          //                      100,    100, 0.5 * (minBalance + maxBalance));
        //  Debugger.Break();
        }

        public double LR_to_Balance(double totL, double totR)
        {
            if (initialL < 0.0) { initialL = totL; }
            if (initialR < 0.0) { initialR = totR; }

            return LR_to_Balance_Generic(totL, totR);
        }

        public double LR_to_Balance_Generic(double totL, double totR)
        {
            double totLnew = totL;
            double totRnew = totR;
            double delta;
            double newBalance = 0;
            double maxB;
            double minB;
            string move = "";

            //  balance -50   ... L = 100  R = 0   L > R
            //  balance  50   ... L = 0  R = 100   L < R

            if (Math.Abs(totL - totR) <= 1.0) {
                // levels equal:  balance is 0
                newBalance = 0;
                return newBalance;
            }
            if (totL < 1.0) {
                // assume panned hard right
                newBalance = 50;
                return newBalance;
            }
            if (totR < 1.0) {
                // assume panned hard left
                newBalance = -50;
                return newBalance;
            }

            minB = -50;
            maxB = 50;

            if (totR < totL) {
                // need tp start searching in the left side
                maxB = 25;
            } else {
                // need tp start searching in the right side
                minB = -25;
            }
            newBalance = 0.5 * (minB + maxB);
            delta = Double.MaxValue;

            while (Math.Abs(delta) > 0.01) {

                XFadeMapper xmap = new XFadeMapper(newBalance);

                if (totR < totL) {
                //  point we are searching for is on the Left
                    totLnew = totL;
                    totRnew = xmap.L_to_R(totLnew);
                    delta = totRnew - totR;
                    if (totRnew < totR) {
                        // we are too far to the left ... move right
                        move = "R";
                    } else {
                        move = "L";
                    }
                } else {
                //  point we are searching for is on the Right
                    totRnew = totR;
                    totLnew = xmap.R_to_L(totRnew);
                    delta = totLnew - totL;
                    if (totLnew < totL) {
                        // we are too far to the right ... move left
                        move = "L";
                    } else {
                        move = "R";
                    }
                }
                if (move == "L") {
                    maxB = newBalance;
                } else {
                    minB = newBalance;
                }
                newBalance = 0.5 * (minB + maxB);
                if (Math.Abs(minB - maxB) < 0.1) {
                    break;  // no room to move
                }
            }
            return newBalance;
        }
        public double BF_to_Balance(double totB, double totF)
        {
            if (initialB < 0.0) { initialB = totB; }
            if (initialF < 0.0) { initialF = totF; }

            return LR_to_Balance_Generic(totB, totF);
        }

        public void test_L_R_to_Balance()
        {
            double balance;

            for (int L = 0; L <= 120; L += 20) {
                for (int R = 0; R <= 120; R += 20) {
                    balance = LR_to_Balance(L, R);
                    Console.WriteLine("L: {0}   R: {1}  =>  Balance-slider:  {2}   ", L, R, (int)balance);
                }
            }
            initialL = -1000.0;
            initialR = -1000.0;
        }

        public void test_Balance_w_Power()
        {
            double totL, totR, newLbal = 0.0, newRbal = 0.0;
            Boolean bres;

            totL = 40;  // initialL
            totR = 60;  // initialR

            double initB = LR_to_Balance(totL, totR);

            Console.WriteLine("Initial Total L:  {0}   Initial Total R: {1}", totL, totR);
            Console.WriteLine("Initial Balance:  {0}", (int)Math.Round(initB));

            for (int b = -50; b <= 50; b += 10) {
                bres = new_LR_Balance_w_Power(totL,  totR,  (double)b, ref newLbal, ref newRbal);
                Console.WriteLine("Balance-slider:  {0}   L-bal: {1:0.0}   R-bal: {2:0.0}", b, newLbal, newRbal);
            }
        }

        // L is currently totL
        // R is currently totR
        // user is requesting Balance to be set to newBalance
        // we need to change L and R so that balance is as requested, but we boost or lower both L & R so total power has not changed

        public Boolean new_LR_Balance_w_Power(double totL, double totR, double newBalance, ref double totLnew, ref double totRnew)
        {
            if (initialL < 0.0) { initialL = totL; }
            if (initialR < 0.0) { initialR = totR; }

            return generic_new_LR_Balance_w_Power(initialL, initialR, newBalance, ref totLnew, ref totRnew);
        }

        public Boolean generic_new_LR_Balance_w_Power(double totL, double totR, double newBalance, ref double totLnew, ref double totRnew)
        {
            double newPower;
            double oldPower = totL + totR;
            double A = 1.0;
            Boolean failed = true;
  
            XFadeMapper xmap = new XFadeMapper(newBalance);

            if ((totL <= 1.0) && (totR <= 1.0)) {
                totLnew = totL;
                totRnew = totR;
            } else if (totR < totL) {
                //  calc R using L
                totLnew = totL;
                totRnew = xmap.L_to_R(totLnew);
            } else {
                totRnew = totR;
                totLnew = xmap.R_to_L(totRnew);
            }
            if ((totRnew <= 1.0) && (totLnew <= 1.0)) {
                return !failed;
            }
            failed = false;

            //  oldPower = A * totLnew + A * totRnew;

            //  oldPower = A * (totLnew + totRnew);
            A = oldPower / (totLnew + totRnew);

            if ((A * totLnew) > 100.0) {
                // A is too high
                totLnew = 100.0;
                A = oldPower / (totLnew + totRnew);
                failed = true;
            }
            if ((A * totRnew) > 100.0) {
                // A is too high
                totRnew = 100.0;
                A = oldPower / (totLnew + totRnew);
                failed = true;
            }
            totLnew = A * totLnew;
            totRnew = A * totRnew;

            return !failed;
        }

        public Boolean new_BF_Balance_w_Power(double totB, double totF, double newBalance, ref double totBnew, ref double totFnew)
        {
            if (initialB < 0.0) { initialB = totB; }
            if (initialF < 0.0) { initialF = totF; }

            return generic_new_LR_Balance_w_Power(initialB, initialF, newBalance, ref totBnew, ref totFnew);
        }


        //public double L_Balance_to_R(double L, double balance)
        //{
        //    return pm.x_z_to_y(L, balance);
        //}
        //public double R_Balance_to_L(double R, double balance)
        //{
        //    return pm.y_z_to_x(R, balance);
        //}
        //public Boolean Old_LR_new_LR_via_Balance(double oldL, double oldR, double oldBalance, double newBalance, ref double newL, ref double newR)
        //{
        //    double oldPower = oldL + oldR;
        //    double prevPower = oldPower;
        //    double newPower;
        //    Boolean failed = false;

        //    if (oldBalance < newBalance) {
        //        // Left ++
        //        // Right --
        //        // slowly increase L, calculate R and continue until total power is same as it was before
        //        newL = 0;
        //        newR = pm.x_z_to_y(newL, newBalance);
        //        newPower = newL + oldL;
        //        while ((oldPower - newPower) > 0.5) {
        //            // raise power on Left channel
        //            if (newL < 99.5) {
        //                newL += 0.5;
        //                newR = pm.x_z_to_y(newL, newBalance);
        //            } else {
        //                newR += 0.5;
        //                newL = pm.y_z_to_x(newR, newBalance);
        //            }
        //            newPower = newL + newR;
        //            if (Math.Abs(newPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = newPower;
        //            if ((newL > 99.5) && (newR > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newL < 0.5) && (newR < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((newPower - oldPower) > 0.5) {
        //            // reduce power on Left channel (shouldn't occur but could)
        //            if (newL > 0.5) {
        //                newL -= 0.5;
        //                newR = pm.x_z_to_y(newL, newBalance);
        //            } else {
        //                newR -= 0.5;
        //                newL = pm.y_z_to_x(newR, newBalance);
        //            }
        //            newPower = newL + newR;
        //            if (Math.Abs(newPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = newPower;
        //            if ((newL > 99.5) && (newR > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newL < 0.5) && (newR < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    } else {
        //        // Left --
        //        // Right ++
        //        // slowly increase R, calculate L and continue until total power is same as it was before
        //        newR = 0;
        //        newL = pm.y_z_to_x(newR, newBalance);
        //        newPower = newR + oldR;
        //        while ((oldPower - newPower) > 0.5) {
        //            // raise power
        //            if (newR < 99.5) {
        //                newR += 0.5;
        //                newL = pm.y_z_to_x(newR, newBalance);
        //            } else {
        //                newL += 0.5;
        //                newR = pm.x_z_to_y(newL, newBalance);
        //            }
        //            newPower = newR + newL;
        //            if (Math.Abs(newPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = newPower;
        //            if ((newR > 99.5) && (newL > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newR < 0.5) && (newL < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((newPower - oldPower) > 0.5) {
        //            // reduce power
        //            if (newR > 0.5) {
        //                newR -= 0.5;
        //                newL = pm.y_z_to_x(newR, newBalance);
        //            } else {
        //                newL -= 0.5;
        //                newR = pm.x_z_to_y(newL, newBalance);
        //            }
        //            newPower = newR + newL;
        //            if (Math.Abs(newPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = newPower;
        //            if ((newR > 99.5) && (newL > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newR < 0.5) && (newL < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    }
        //    return !failed;
        //}


        //public Boolean Old_LR_new_LR_w_Power_Old2(double oldL, double oldR, double newBalance, ref double newL, ref double newR)
        //{
        //    double newPower;
        //    double oldPower = oldL + oldR;
        //    Boolean failed = false;

        //    if (newBalance > 0) { //          R stays what it is (or increases or decreses slightly)  L goes up & down in response to balance chg
        //        //                            balance control is on right side.  This means user is moving left-channel up and down
        //        //
        //        // slowly increase L, calculate R and continue until total power is same as it was before
        //        newR = oldR;
        //        newL = pm.y_z_to_x(newR, newBalance); // x = LB  y = RF  z = B      L = f(R,B)
        //        newPower = newL + newR;
        //        while ((oldPower - newPower) > 0.5) { // power went down, bring it back up
        //            // raise power on Right channel
        //            if (newR < 99.5) {
        //                newR += 0.5;
        //            } else {
        //                newL += 0.5; // R has hit a ceiling, have to bring up L
        //            }
        //            newPower = newL + newR;
        //            if ((newL > 99.5) && (newR > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((newPower - oldPower) > 0.5) { // power went up, bring it back down
        //                                              // reduce power on Right channel
        //            if (newR > 0.5) {
        //                newR -= 0.5;
        //            } else {
        //                newL -= 0.5; // R has hit the floor, have to bring down L
        //            }
        //            newPower = newL + newR;
        //            if ((newL < 0.5) && (newR < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    } else {                        // L stays what it is (or increases or decreses slightly)  R goes up & down in response to balance chg
        //                                    //                             balance control is on left side.  This means user is moving right-channel up and down
        //                                    // 
        //                                    // slowly increase R, calculate L and continue until total power is same as it was before
        //        newL = oldL;
        //        newR = pm.x_z_to_y(newL, newBalance); // x = LB  y = RF  z = B      R = f(L,B)
        //        newPower = newL + newR;
        //        while ((oldPower - newPower) > 0.5) {
        //            // raise power
        //            if (newL < 99.5) {
        //                newL += 0.5;
        //            } else {
        //                newR += 0.5;
        //            }
        //            newPower = newR + newL;
        //            if ((newR > 99.5) && (newL > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((newPower - oldPower) > 0.5) {
        //            // reduce power
        //            if (newL > 0.5) {
        //                newL -= 0.5;
        //            } else {
        //                newR -= 0.5;
        //            }
        //            newPower = newR + newL;
        //            if ((newR < 0.5) && (newL < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    }
        //    if (failed) {
        //        if (Math.Abs(oldPower - newPower) < 0.5) {
        //            failed = false;
        //        }
        //    }
        //    return !failed;
        //}

        //public Boolean Old_LR_new_LR_w_Power_Old(double oldL, double oldR, double Balance, double newPower, ref double newL, ref double newR)
        //{
        //    double oldPower = oldL + oldR;
        //    double prevPower = oldPower;
        //    double tPower;
        //    Boolean failed = false;

        //    if (Balance < 0.0) {
        //        // assume left is more important than right, increase left until required power is obtained
        //        // Left ++
        //        // Right --
        //        // slowly increase L, calculate R and continue until total power is same as it was before
        //        newL = 0;
        //        newR = pm.x_z_to_y(newL, Balance);
        //        tPower = newL + newR;
        //        while ((newPower - tPower) > 0.5) {
        //            // raise power on Left channel
        //            if (newL < 99.5) {
        //                newL += 0.5;
        //                newR = pm.x_z_to_y(newL, Balance);
        //            } else {
        //                newR += 0.5;
        //                newL = pm.y_z_to_x(newR, Balance);
        //            }
        //            tPower = newL + newR;
        //            if (Math.Abs(tPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = tPower;
        //            if ((newL > 99.5) && (newR > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newL < 0.5) && (newR < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((tPower - newPower) > 0.5) {
        //            // reduce power on Left channel
        //            if (newL > 0.5) {
        //                newL -= 0.5;
        //                newR = pm.x_z_to_y(newL, Balance);
        //            } else {
        //                newR -= 0.5;
        //                newL = pm.y_z_to_x(newR, Balance);
        //            }
        //            tPower = newL + newR;
        //            if (Math.Abs(tPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = tPower;
        //            if ((newL > 99.5) && (newR > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newL < 0.5) && (newR < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    } else {
        //        // Left --
        //        // Right ++
        //        // slowly increase R, calculate L and continue until total power is same as it was before
        //        newR = 0;
        //        newL = pm.y_z_to_x(newR, Balance);
        //        tPower = newR + oldR;
        //        while ((newPower - tPower) > 0.5) {
        //            // raise power
        //            if (newR < 99.5) {
        //                newR += 0.5;
        //                newL = pm.y_z_to_x(newR, Balance);
        //            } else {
        //                newL += 0.5;
        //                newR = pm.x_z_to_y(newL, Balance);
        //            }
        //            tPower = newR + newL;
        //            if (Math.Abs(tPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = tPower;
        //            if ((newR > 99.5) && (newL > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newR < 0.5) && (newL < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //        while ((tPower - newPower) > 0.5) {
        //            // reduce power
        //            if (newR > 0.5) {
        //                newR -= 0.5;
        //                newL = pm.y_z_to_x(newR, Balance);
        //            } else {
        //                newL -= 0.5;
        //                newR = pm.x_z_to_y(newL, Balance);
        //            }
        //            tPower = newR + newL;
        //            if (Math.Abs(tPower - prevPower) < 1.5) {
        //                failed = true;
        //                break;  // power isn't changing, give up
        //            }
        //            prevPower = tPower;
        //            if ((newR > 99.5) && (newL > 99.5)) {
        //                failed = true;
        //                break;
        //            }
        //            if ((newR < 0.5) && (newL < 0.5)) {
        //                failed = true;
        //                break;
        //            }
        //        }
        //    }
        //    return !failed;
        //}
        //public double B_Balance_to_F(double B, double balance)
        //{
        //    return L_Balance_to_R(B, balance);
        //}
        //public double F_Balance_to_B(double F, double balance)
        //{
        //    return R_Balance_to_L(F, balance);
        //}
        //public Boolean Old_BF_new_BF_via_Balance(double oldB, double oldF, double oldBalance, double newBalance, ref double newB, ref double newF)
        //{
        //    return Old_LR_new_LR_via_Balance(oldB,oldF,oldBalance,newBalance,ref newB, ref newF);
        //}
        //public Boolean Old_BF_new_BF_w_Power(double oldB, double oldF, double Balance, ref double newB, ref double newF)
        //{
        //    return Old_LR_new_LR_w_Power(oldB, oldF, Balance, ref newB, ref newF);
        //}
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
        public const int fResize = 4;

        //                             min    max    level  text   resize
        public Boolean[] gui_flags = { false, false, false, false, false };

        public double lr_balance_value = 1.0;
        public double bf_balance_value = 1.0;
        public double marantz_start_level = Globs.unknown;
        public double marantz_min_level = Globs.unknown;
        public double marantz_max_level = Globs.unknown;
        public double marantz_current_balance = Globs.unknown;   // what we think level is
        public double balance_adjusted_level = Globs.unknown;  // what level should be adjusted by balance ... what we told marantz to use
        public double reported_level = Globs.unknown;          // what Marantz currently says its level is
        public int last_slider_value = (int)Globs.unknown;
        public int slider_min_level = (int)Globs.unknown;
        public int slider_max_level = (int)Globs.unknown;
        public int slider_page_size = (int)Globs.unknown;
        public string command = "";
        public string descr = "";
        public System.Windows.Forms.ScrollBar sbar = null;
        public System.Windows.Forms.TextBox txt = null;
        public Orient orientation = Orient.horiz;

        public Dictionary<String, MultiLevelController> balance_controlers;

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
            //                                         A = slider  B = level
            if (_orientation == Orient.vert) {
                this.sliderMarantzMapper = new LevelMapper(100.0, 0.0, 0.0, 100.0);
            } else {
                this.sliderMarantzMapper = new LevelMapper(0.0, 100.0, 0.0, 100.0);
            }

            if (sbar != null) {
                load_slider_parms();
            } else {
                this.slider_min_level = 0;
                this.slider_max_level = 100;
            }
        }

        public double SliderValue_to_Level(double value)
        {
            // A = slider  B = level
            return this.sliderMarantzMapper.A_to_B(value);
        }
        public double Level_to_SliderValue(double lev)
        {
            // A = slider  B = level
            return this.sliderMarantzMapper.B_to_A(lev);
        }
        public int Level_to_intSliderValue(double lev)
        {
            // A = slider  B = level
            return this.sliderMarantzMapper.B_to_intA(lev);
        }

        public double Slider_Change_Percent(int oldA, int newA)
        {
            // A = slider  B = level
            // convert old & new slider values to levels & calc change percent
            return this.sliderMarantzMapper.A_change_percent(oldA, newA);
        }

        public void load_slider_parms()
        {
            this.slider_min_level = this.sbar.Minimum;
            this.slider_max_level = this.sbar.Maximum; // slider goes from Min to (Max - pageSize)
         // this.last_slider_value = this.sbar.Value;
            this.slider_page_size = this.sbar.LargeChange;

            // A = slider value   B = level

            double mval0 = this.SliderValue_to_Level((double)this.sbar.Value);
            double mvalN = this.SliderValue_to_Level((double)this.last_slider_value);

            if (this.orientation == Orient.vert) {
                this.sliderMarantzMapper.Adjust_A(this.slider_max_level - this.slider_page_size, this.slider_min_level);
            } else {
                this.sliderMarantzMapper.Adjust_A(this.slider_min_level, this.slider_max_level - this.slider_page_size);
            }

            int sval0 = this.Level_to_intSliderValue(mval0);
            int svalN = this.Level_to_intSliderValue(mvalN);

            this.last_slider_value = svalN;

            if (sval0 != this.sbar.Value) {
                this.sbar.Value = this.last_slider_value;
                System.Windows.Forms.Application.DoEvents();
            }
        }

        public Boolean is_unknown(double val)
        {
            double diff = val - Globs.unknown;
            if (diff < 0.001 && diff > -0.001) {
                return true;
            }
            return false;
        }

        public void change_level(int new_level)
        {
            // adjust volume based on slider change

            if (false && this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
                new_level = this.slider_max_level - (new_level - this.slider_min_level);
            } //                                 ^^^^---- *** pageSize ????

            if (this.setting_level) {

                this.last_slider_value = new_level;

                this.d.ShowParameters(this);  // frmController::ShowParameterValues

                return;
            }

            // chg % = 100 * (new_level - last_slider_value) / (slider_max_level - marantz_min_level)

            string msg = String.Format("last_slider_value = {0}  new_level = {1}", this.last_slider_value, new_level);

            //double chg_percent = 100.0 * (double)(new_level - this.last_slider_value) / (double)(this.slider_max_level - this.slider_min_level);
            double chg_percent = this.Slider_Change_Percent(this.last_slider_value, new_level);

            msg += String.Format("  chg_percent = {0:0.00}", chg_percent);

            if (this.slider_max_level < 0.0 || this.slider_min_level < 0.0) {
                chg_percent = 100.0 * ((double)(new_level - this.last_slider_value) / (double)(100 - 0));
                msg += String.Format("  chg_percent = {0:0.00}", chg_percent);
            }

            this.last_slider_value = new_level;

            this.d.ShowParameters(this);  // frmController::ShowParameterValues

            double chg_amt = (chg_percent / 100.0) * (this.marantz_max_level - this.marantz_min_level);

            msg += String.Format("  chg_amt = {0:0.00}", chg_amt);

            if (this.marantz_max_level < 0.0 || this.marantz_min_level < 0.0) {
                chg_amt = (chg_percent / 100.0) * (100.0 - 0.0);
                msg += String.Format("  chg_amt = {0:0.00}", chg_amt);
            }

            Debug.WriteLine(msg);

            if ((chg_amt < 0.01) && (chg_amt > -0.01))
                return;

            send_currlev_to_marantz(chg_amt);
        }

        public void reset_control()
        {
            this.marantz_current_balance = marantz_start_level;
            this.lr_balance_value = 1.0;
            this.bf_balance_value = 1.0;
            this.balance_adjusted_level = marantz_current_balance;

            this.marantz_start_level = Globs.unknown;

            this.d.ShowParameters(this);  // frmController::ShowParameterValues

            QueueMessage msg = new QueueMessage();

            msg.Message = "<INIT>";

            set_gui_levels(Globs.unknown, Globs.unknown, this.marantz_current_balance, msg);

            send_currlev_to_marantz(0.0);
        }

        public void send_currlev_to_marantz(double chg_amt)
        {
            if (this.marantz_current_balance < 0.0) {
                return;
            }
            this.marantz_current_balance = Math.Min(100.0, Math.Max(0.0, this.marantz_current_balance + chg_amt));

            string msg = String.Format("^---> marantz_current_level = {0:0.00}", this.marantz_current_balance);

            Debug.WriteLine(msg);

            this.balance_adjusted_level = this.bf_balance_value * this.lr_balance_value * this.marantz_current_balance;

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
                cmd += String.Format(" {0:000}", 10 * int_lev + int_remain);
            else cmd += String.Format(" {0:00}", int_lev);

        //  string response = this.c.get_value(cmd, descr);  <<< forces this routine to hang waiting for a response !

            Boolean askForLevels = (cmd.StartsWith("MV") ? false : true);

            if (Globs.NO_SEND) {
            //  DON'T SEND ANYTHING
            } else {
                this.d.Send(cmd, descr, askForLevels);  // just append to cmdQueue, and display on GUI out-list
            }
        }

        public void set_gui_levels(double min_lev, double max_lev, double lev, QueueMessage msg)
        {
            //  this.gui_flags[fMin] = false;  -- let slider clear them after they are processed
            //  this.gui_flags[fMax] = false;
            //  this.gui_flags[fLev] = false;
            //  this.gui_flags[fTxt] = false;
            if (is_unknown(this.marantz_start_level) && !is_unknown(lev)) {
                this.marantz_start_level = lev;
                this.marantz_current_balance = lev;
                this.reported_level = lev;
                this.gui_flags[fLev] = true;
            }
            if (!is_unknown(min_lev)) {
                this.marantz_min_level = min_lev;
                this.gui_flags[fMin] = true;
            }
            if (!is_unknown(max_lev)) {
                this.marantz_max_level = max_lev;
                this.gui_flags[fMax] = true;
            }
            if (!is_unknown(lev)) {
                if (is_unknown(this.marantz_min_level)) {
                    this.marantz_min_level = 0.0;    // default min
                    this.gui_flags[fMin] = true;
                }
                if (is_unknown(this.marantz_max_level)) {
                    this.marantz_max_level = 100.0;  // default max
                    this.gui_flags[fMax] = true;
                }
                this.reported_level = lev;  // lev is what is received from Marantz
                this.gui_flags[fTxt] = true;
            }
            if (this.gui_flags[fMin] || this.gui_flags[fMax]) {
                this.sliderMarantzMapper.Adjust_B(this.marantz_min_level, this.marantz_max_level);
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
            } else if (msg.Message == "<INIT>") {
                our_message = false;  // initialize sliders
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

            if (msg.Message != "<INIT>") {
                this.balance_adjusted_level = this.reported_level;  // balance_adjusted_level is a calculated level
                                                                    // marantz_current_level is the desired level based on the slider
            }

            // tell ALL mutilevel controllers to recalculate their value from new levels

            if (this.balance_controlers != null) {
                foreach (string levctrl in this.balance_controlers.Keys) {
                    MultiLevelController mlc = this.balance_controlers[levctrl];
                    mlc.set_balance_from_levels();
                }
            }

            /**********************************************************
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
            **********************************************************/

            this.update_slider(this.marantz_current_balance);
        }

        public void resize_thumb()
        {
            this.gui_flags[fResize] = true;

            this.d.ResizeThumb(this);
        }

        public void update_slider(double marantz_lev)
        {
            // update slider to match current amplifier levels

            this.last_slider_value = this.Level_to_intSliderValue(marantz_lev);

            if (false && this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
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

    public class DualLevelController
    {
        public LevelController lb_lc;
        public LevelController rf_lc;
    }

    public class MultiLevelController : LevelController
    {
        string[] left_front_cmds;
        string[] right_front_cmds;
        string[] left_back_cmds;
        string[] right_back_cmds;

        public List<LevelController> left_front_lcs = new List<LevelController>();
        public List<LevelController> right_front_lcs = new List<LevelController>();
        public List<LevelController> left_back_lcs = new List<LevelController>();
        public List<LevelController> right_back_lcs = new List<LevelController>();

        public List<DualLevelController> left_right_lcs = new List<DualLevelController>();
        public List<DualLevelController> back_front_lcs = new List<DualLevelController>();

        public LevelController _right_back_lc;
        public LevelController _left_back_lc;

        private LevelMapper sliderToBalanceMapper;
        private LevelMapper balanceToMarantzMapper;
        private BalanceMapper LR_BF_BalanceMapper;

        //public Dictionary<string,int> marantz_start_levels;
        //public Dictionary<string,int> marantz_min_levels;
        //public Dictionary<string,int> marantz_max_levels;
        //public Dictionary<string,int> marantz_current_levels;
        public MultiLevelController (string _descr, string[] _left_front_cmds, string[] _right_front_cmds, string[] _left_back_cmds, string[] _right_back_cmds,
                                     LevelController.Orient _orientation, System.Windows.Forms.ScrollBar _sbar, System.Windows.Forms.TextBox _txt, Dispatcher _d) :
            base(_descr, "XX", _orientation, _sbar, _txt, _d)
        {
            int max_val = this.sbar.Maximum - (int)Math.Round(0.5 * (double)this.sbar.LargeChange); // this.sbar.Maximum - this.sbar.LargeChange
            int min_val = this.sbar.Minimum + (int)Math.Round(0.5 * (double)this.sbar.LargeChange); // (this.sbar.Minimum
            if (_orientation == Orient.vert) {
                sliderToBalanceMapper = new LevelMapper(max_val, min_val, -50.0, 50.0);
            } else {
                sliderToBalanceMapper = new LevelMapper(min_val, max_val, -50.0, 50.0);
            }
            balanceToMarantzMapper = new LevelMapper(-50.0, 50.0, -50.0, 50.0);  // NOTE: marantz doesn't have a balance control, so these are VIRTUAL values for our eyes only
            LR_BF_BalanceMapper = new BalanceMapper(-50.0, 50.0);

            LevelController.Orient noorient = Orient.horiz;
            left_front_cmds = _left_front_cmds;
            right_front_cmds = _right_front_cmds;
            left_back_cmds = _left_back_cmds;
            right_back_cmds = _right_back_cmds;

            foreach (string cmd in left_front_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                left_front_lcs.Add(lc);
            }
            foreach (string cmd in right_front_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                right_front_lcs.Add(lc);
            }
            foreach (string cmd in left_back_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                left_back_lcs.Add(lc);
            }
            foreach (string cmd in right_back_cmds) {
                LevelController lc = LevelControllerFactory.New(_descr, cmd, noorient, null, null, _d);
                right_back_lcs.Add(lc);
            }
            // *** and where is the center channel ???

            for (int i = left_front_cmds.GetLowerBound(0); i <= left_front_cmds.GetUpperBound(0); i++) {
                DualLevelController dlc = new DualLevelController();
                string lcmd = left_front_cmds[i];
                string rcmd = right_front_cmds[i];
                dlc.lb_lc = LevelControllerFactory.New(_descr, lcmd, noorient, null, null, _d);
                dlc.rf_lc = LevelControllerFactory.New(_descr, rcmd, noorient, null, null, _d);
                left_right_lcs.Add(dlc);

                DualLevelController dlc3 = new DualLevelController();
                string fcmd2 = left_front_cmds[i];
                dlc3.lb_lc = null;
                dlc3.rf_lc = LevelControllerFactory.New(_descr, fcmd2, noorient, null, null, _d);
                back_front_lcs.Add(dlc3);

                DualLevelController dlc2 = new DualLevelController();
                string fcmd = right_front_cmds[i];
                dlc2.lb_lc = null;
                dlc2.rf_lc = LevelControllerFactory.New(_descr, fcmd, noorient, null, null, _d);
                back_front_lcs.Add(dlc2);
            }
            for (int i = left_back_cmds.GetLowerBound(0); i <= left_back_cmds.GetUpperBound(0); i++) {
                DualLevelController dlc = new DualLevelController();
                string lcmd = left_back_cmds[i];
                string rcmd = right_back_cmds[i];
                dlc.lb_lc = LevelControllerFactory.New(_descr, lcmd, noorient, null, null, _d);
                dlc.rf_lc = LevelControllerFactory.New(_descr, rcmd, noorient, null, null, _d);
                left_right_lcs.Add(dlc);

                string bcmd = left_back_cmds[i];
                string bcmd2 = right_back_cmds[i];

                foreach (DualLevelController dlc2 in back_front_lcs) {
                    LevelController lc = dlc2.rf_lc;
                    if (lc.command == lcmd && dlc2.lb_lc == null) {
                        dlc2.lb_lc = LevelControllerFactory.New(_descr, bcmd, noorient, null, null, _d);
                    } else if (lc.command == rcmd && dlc2.lb_lc == null) {
                        dlc2.lb_lc = LevelControllerFactory.New(_descr, bcmd2, noorient, null, null, _d);
                    }
                }
            }

            QueueMessage msg = new QueueMessage();

            msg.Message = "<INIT>";

            base.set_gui_levels(-50.0,50.0,0.0, msg);  // there is no marantz balance control, so we report marantz levels as -50 to 50 with a start of "0"
        }

        public double SliderValue_to_Balance(double value)
        {
            // A = slider  B = balance
            return this.sliderToBalanceMapper.A_to_B(value);
        }
        public double Balance_to_SliderValue(double lev)
        {
            // A = slider  B = balance
            return this.sliderToBalanceMapper.B_to_A(lev);
        }
        public int Balance_to_intSliderValue(double lev)
        {
            // A = slider  B = balance
            return this.sliderToBalanceMapper.B_to_intA(lev);
        }

        public double Slider_Change_Percent(int oldA, int newA)
        {
            // A = slider  B = balance
            // convert old & new slider values to balance & calc change percent
            return this.sliderToBalanceMapper.A_change_percent(oldA, newA);
        }


        public void test_balance ()
        {
            LR_BF_BalanceMapper.test_L_R_to_Balance();
            LR_BF_BalanceMapper.test_Balance_w_Power();
        }

        public List<LevelController> level_controllers ()
        {
            List<LevelController> lcs = new List<LevelController>();

            foreach (LevelController lc in left_front_lcs) {
                lcs.Add(lc);
            }
            foreach (LevelController lc in right_front_lcs) {
                lcs.Add(lc);
            }
            foreach (LevelController lc in left_back_lcs) {
                lcs.Add(lc);
            }
            foreach (LevelController lc in right_back_lcs) {
                lcs.Add(lc);
            }
            return lcs;
        }

        public void set_balance_from_levels()
        {
            double f_lev = 0.0, b_lev = 0.0;
            double l_lev = 0.0, r_lev = 0.0;
            int num_t = 0, num_b = 0;
            int num_l = 0, num_r = 0;

            foreach (LevelController lc in this.left_back_lcs) {
                if (!is_unknown(lc.reported_level)) {
                    if (lc.reported_level > 0.01) {
                        b_lev += lc.reported_level;
                        l_lev += lc.reported_level;
                        num_b++;
                        num_l++;
                    }
                } else if (!is_unknown(lc.marantz_current_balance)) {
                    if (lc.marantz_current_balance > 0.01) {
                        b_lev += lc.marantz_current_balance;
                        l_lev += lc.marantz_current_balance;
                        num_b++;
                        num_l++;
                    }
                }
            }
            foreach (LevelController lc in this.right_back_lcs) {
                if (!is_unknown(lc.reported_level)) {
                    if (!is_unknown(lc.reported_level)) {
                        b_lev += lc.reported_level;
                        r_lev += lc.reported_level;
                        num_b++;
                        num_r++;
                    }
                } else if (!is_unknown(lc.marantz_current_balance)) {
                    if (lc.marantz_current_balance > 0.01) {
                        b_lev += lc.marantz_current_balance;
                        r_lev += lc.marantz_current_balance;
                        num_b++;
                        num_r++;
                    }
                }
            }
            foreach (LevelController lc in this.left_front_lcs) {
                if (!is_unknown(lc.reported_level)) {
                    if (!is_unknown(lc.reported_level)) {
                        f_lev += lc.reported_level;
                        l_lev += lc.reported_level;
                        num_t++;
                        num_l++;
                    }
                } else if (!is_unknown(lc.marantz_current_balance)) {
                    if (lc.marantz_current_balance > 0.01) {
                        f_lev += lc.marantz_current_balance;
                        l_lev += lc.marantz_current_balance;
                        num_t++;
                        num_l++;
                    }
                }
            }
            foreach (LevelController lc in this.right_front_lcs) {
                if (!is_unknown(lc.reported_level)) {
                    if (!is_unknown(lc.reported_level)) {
                        f_lev += lc.reported_level;
                        r_lev += lc.reported_level;
                        num_t++;
                        num_r++;
                    }
                } else if (!is_unknown(lc.marantz_current_balance)) {
                    if (lc.marantz_current_balance > 0.01) {
                        f_lev += lc.marantz_current_balance;
                        l_lev += lc.marantz_current_balance;
                        num_t++;
                        num_r++;
                    }
                }
            }
            double new_level;
            if (this.orientation == Orient.vert) {  // control pans between back & front:
                if (num_b == 0 || num_t == 0 || (f_lev == 0 && b_lev == 0)) {
                    return;
                } else {
                    b_lev = b_lev / (double)num_b; // --- pass total over all controls, adjusted by number of drivers <<< otherwise 5 drivers always wins over 4
                    f_lev = f_lev / (double)num_t;

                    new_level = this.LR_BF_BalanceMapper.BF_to_Balance(b_lev, f_lev);

                    Console.WriteLine("======================= VERTICAL");
                    Console.WriteLine("b_lev = {0}  f_lev = {1}  =>  curr_lev = {2}", b_lev, f_lev, new_level);
                }
            } else {                                // control pans between left & right:
                if (num_l == 0 || num_r == 0 || (f_lev == 0 && b_lev == 0)) {
                    return;
                } else {
                    l_lev = l_lev / (double)num_l; // --- pass total over all controls, adjusted by number of drivers <<< otherwise 5 drivers always wins over 4
                    r_lev = r_lev / (double)num_r;

                    new_level = this.LR_BF_BalanceMapper.LR_to_Balance(l_lev, r_lev);

                    Console.WriteLine("======================= HORIZONTAL");
                    Console.WriteLine("l_lev = {0}  r_lev = {1}  =>  curr_lev = {2}", l_lev, r_lev, new_level);
                }
            }
            //this.last_slider_value = this.sliderToBalanceMapper.B_to_intA(this.marantz_current_level);  // -50 .. 50 >>  sbar.min .. sbar.max

            //double new_level = this.sliderToBalanceMapper.B_to_A(this.marantz_current_level);  // -50 .. 50 >>  sbar.min .. sbar.max
            //if (this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
            //    new_level = this.slider_min_level + (this.slider_max_level - new_level);
            //}
            //Console.WriteLine("curr_lev = {0} => B_to_Aint => last_slider {1}  (sbar = {2} | {3})", this.marantz_current_level, this.last_slider_value, this.sbar.Minimum, this.sbar.Maximum);
            //Console.WriteLine("curr_lev = {0} => B_to_A => new_lev {1}", this.marantz_current_level, new_level);

            this.update_slider(new_level);
        }

        public void change_balance(int new_level)
        {
            // adjust balance based on slider change

            if (false && this.orientation == Orient.vert) {  // WinForms draws vertical control UPSIDEDOWN ^!($^*(^$(^$(*
                new_level = this.slider_max_level - (new_level - this.slider_min_level);
            }
            this.last_slider_value = new_level;

            double new_balance = this.SliderValue_to_Balance((double)new_level);  // sbar.min .. sbar.max  >>  -50 ... 50

            double old_current_balance = this.marantz_current_balance;
            this.marantz_current_balance = new_balance;

//          this.balance_adjusted_level = this.marantz_current_level;  // for balance controls, they are the same thing
//          this.reported_level = Globs.unknown; // <<< only set/change when something comes back

            this.d.ShowParameters(this);

            if (this.setting_level) {
                return;
            }

            double f_lev = 0.0, b_lev = 0.0;
            double l_lev = 0.0, r_lev = 0.0;
            int num_t = 0, num_b = 0;
            int num_l = 0, num_r = 0;
            double new_f_bal = 0.0, new_b_bal = 0.0;
            double new_l_bal = 0.0, new_r_bal = 0.0;
            double new_f_lev = 0.0, new_b_lev = 0.0;
            double new_l_lev = 0.0, new_r_lev = 0.0;
            Boolean bres;

            // l_lev = 

            foreach (LevelController lc in this.left_back_lcs) {
                if (lc.marantz_current_balance > 0.01) {
                    b_lev += lc.marantz_current_balance;
                    l_lev += lc.marantz_current_balance;
                    num_b++;
                    num_l++;
                }
            }
            foreach (LevelController lc in this.right_back_lcs) {
                if (lc.marantz_current_balance > 0.01) {
                    b_lev += lc.marantz_current_balance;
                    r_lev += lc.marantz_current_balance;
                    num_b++;
                    num_r++;
                }
            }
            foreach (LevelController lc in this.left_front_lcs) {
                if (lc.marantz_current_balance > 0.01) {
                    f_lev += lc.marantz_current_balance;
                    l_lev += lc.marantz_current_balance;
                    num_t++;
                    num_l++;
                }
            }
            foreach (LevelController lc in this.right_front_lcs) {
                if (lc.marantz_current_balance > 0.01) {
                    f_lev += lc.marantz_current_balance;
                    r_lev += lc.marantz_current_balance;
                    num_t++;
                    num_r++;
                }
            }
            if (this.orientation == Orient.vert) {  // control pans between back & front:
                double old_bf_balance = old_current_balance;
                double new_bf_balance = this.marantz_current_balance;
                //b_lev = b_lev / (double)num_b;  // --  pass total power, adjusted by number of drivers <<< otherwise 5 drivers always wins over 4
                //f_lev = f_lev / (double)num_t;
                foreach (DualLevelController dlc in this.back_front_lcs) {
                    b_lev = dlc.lb_lc.marantz_current_balance;
                    f_lev = dlc.rf_lc.marantz_current_balance;
                    bres = this.LR_BF_BalanceMapper.new_BF_Balance_w_Power(b_lev, f_lev, new_bf_balance,
                                                                           ref new_b_lev, ref new_f_lev);
                    new_b_bal = new_b_lev / b_lev; // could get huge or infinite
                    new_f_bal = new_f_lev / f_lev; // could get huge or infinite
                    dlc.lb_lc.bf_balance_value = new_b_bal;
                    dlc.rf_lc.bf_balance_value = new_f_bal;
                 // dlc.rf_lc.marantz_current_level = new_f_lev;
                 // dlc.lb_lc.marantz_current_level = new_b_lev;
                    dlc.lb_lc.send_currlev_to_marantz(0);
                    dlc.rf_lc.send_currlev_to_marantz(0);
                }
                //bres = this.LR_BF_BalanceMapper.new_LR_Balance_w_Power(b_lev, f_lev, new_bf_balance,
                //                                                      ref new_b_lev, ref new_f_lev);

                //foreach (LevelController lc in this.left_back_lcs) {
                //    lc.bf_balance_value = new_b_bal;
                //}
                //foreach (LevelController lc in this.right_back_lcs) {
                //    lc.bf_balance_value = new_b_bal;
                //}
                //foreach (LevelController lc in this.left_front_lcs) {
                //    lc.bf_balance_value = new_f_bal;
                //}
                //foreach (LevelController lc in this.right_front_lcs) {
                //    lc.bf_balance_value = new_f_bal;
                //}
            } else {                                // control pans between left & right:
                double old_lr_balance = old_current_balance;
                double new_lr_balance = this.marantz_current_balance;
                //l_lev = l_lev / (double)num_l;  // --  pass total power, adjusted by number of drivers <<< otherwise 5 drivers always wins over 4
                //r_lev = r_lev / (double)num_r;
                foreach (DualLevelController dlc in this.left_right_lcs) {
                    l_lev = dlc.lb_lc.marantz_current_balance;
                    r_lev = dlc.rf_lc.marantz_current_balance;
                    bres = this.LR_BF_BalanceMapper.new_LR_Balance_w_Power(l_lev, r_lev, new_lr_balance,
                                                                           ref new_l_lev, ref new_r_lev);
                    new_l_bal = new_l_lev / l_lev; // could get huge or infinite
                    new_r_bal = new_r_lev / r_lev; // could get huge or infinite
                    dlc.lb_lc.lr_balance_value = new_l_bal;
                    dlc.rf_lc.lr_balance_value = new_r_bal;
                    // dlc.rf_lc.marantz_current_level = new_f_lev;
                    // dlc.lb_lc.marantz_current_level = new_b_lev;
                    dlc.lb_lc.send_currlev_to_marantz(0);
                    dlc.rf_lc.send_currlev_to_marantz(0);
                }
                //bres = this.LR_BF_BalanceMapper.new_LR_Balance_w_Power(l_lev, r_lev, new_lr_balance, 
                //                                                      ref new_l_lev, ref new_r_lev);

                //foreach (LevelController lc in this.left_back_lcs) {
                //    lc.lr_balance_value = new_l_bal;
                //}
                //foreach (LevelController lc in this.left_front_lcs) {
                //    lc.lr_balance_value = new_l_bal;
                //}
                //foreach (LevelController lc in this.right_back_lcs) {
                //    lc.lr_balance_value = new_r_bal;
                //}
                //foreach (LevelController lc in this.right_front_lcs) {
                //    lc.lr_balance_value = new_r_bal;
                //}
            }

            //if (new_level <= 50.0) {
            //    l_lev = 1.0;
            //    r_lev = new_level / 50.0;
            //} else {
            //    l_lev = 1.0 - (new_level - 50.0) / 50.0;
            //    r_lev = 1.0;
            //}

            foreach (LevelController lc in this.left_back_lcs) {
                lc.send_currlev_to_marantz(0);
            }
            foreach (LevelController lc in this.right_back_lcs) {
                lc.send_currlev_to_marantz(0);
            }
            foreach (LevelController lc in this.left_front_lcs) {
                lc.send_currlev_to_marantz(0);
            }
            foreach (LevelController lc in this.right_front_lcs) {
                lc.send_currlev_to_marantz(0);
            }
        }
    }

    public class Resizer
    {
        Dispatcher d = null;

        public Resizer(Dispatcher _d)
        {
            this.d = _d;
        }

        public void HandleResize ()
        {
            this.d.ResizeAllThumbs();
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

        //  ProcessMarantzLevels(levctrls, msg);  <<< let Receiver thread handle this
        }

        public void ResizeAllThumbs(Dictionary<String, LevelController> levctrls)
        {
            foreach (string levctrl in levctrls.Keys) {
                LevelController lc = levctrls[levctrl];

                lc.resize_thumb();
            }
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

                                    lc.set_gui_levels(Globs.unknown, Globs.unknown, level, msg);

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
        public event EventHandler<MessageTranferEventArgs> ThumbResizer;
        public event EventHandler<MessageTranferEventArgs> SendClearer;
        public event EventHandler<MessageTranferEventArgs> SendRefresher;
        public event EventHandler<MessageTranferEventArgs> Expecter;
        public event EventHandler<MessageTranferEventArgs> Terminator;
        public event EventHandler<MessageTranferEventArgs> ConnectNotifier;
        public event EventHandler<MessageTranferEventArgs> SliderSetter;
        public event EventHandler<MessageTranferEventArgs> ResizeHandler;
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
        public void ResizeThumb(LevelController lc) // << resize_thumb
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            args.LC = lc;

            OnResizeThumb(args);  // reuse Form.SetSliderLevel to change thumb size
        }

        public void ResizeAllThumbs()
        {
            MessageTranferEventArgs args = new MessageTranferEventArgs();
            args.Connection = this.connection;
            args.Queue = this.queue;
            args.Form = this.ui;
            args.Dispatcher = this;
            OnResizeAllhumbs(args);  // Form.ResizeAllThumbs
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
            EventHandler<MessageTranferEventArgs> handler = this.SliderSetter;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnShowParameters (MessageTranferEventArgs e)  // Form::ShowParameterValues
        {
            EventHandler<MessageTranferEventArgs> handler = this.ParameterShower;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMessageRefreshSend (MessageTranferEventArgs e)  // Form::RefreshSendList
        {
            EventHandler<MessageTranferEventArgs> handler = this.SendRefresher;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMessageClear (MessageTranferEventArgs e)  // Form::ClearSendList
        {
            EventHandler<MessageTranferEventArgs> handler = this.SendClearer;
            if (handler != null) {
                handler(this, e);
            }
        }
                                                                          // two listeners...
        protected virtual void OnMessageSend (MessageTranferEventArgs e)  // Form::AddToSendList
        {                                                                 // AddToSendQueue
            EventHandler<MessageTranferEventArgs> handler = this.MessageSender;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnMessageReceive (MessageTranferEventArgs e)  // Form::AddToReceiveList
        {
            EventHandler<MessageTranferEventArgs> handler = this.MessageReceiver;
            if (handler != null) {
                handler(this, e);
            }
        }
        protected virtual void OnProcessReceived(MessageTranferEventArgs e)  // Form::ProcessReceivedMessages
        {
            EventHandler<MessageTranferEventArgs> handler = this.ReceiveProcessor;
            if (handler != null) {
                handler(this, e);
            }
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
        protected virtual void OnUpdateMarantzLevels(MessageTranferEventArgs e)  // Form::UpdateMarantzLevels
        {
            EventHandler<MessageTranferEventArgs> handler = this.MarantzLevelUpdater;
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
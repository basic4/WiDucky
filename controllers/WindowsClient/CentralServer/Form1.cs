using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;

namespace WiDuckClient
{
    public partial class Form1 : Form
    {
        String mapType = "UK";
        Boolean sendError = false;
        int controlMode = 0;
        System.Net.Sockets.TcpClient thisClient;
        System.Net.Sockets.NetworkStream networkStream;
        Boolean connected = false;
        String newData = "";
        String ScriptToExecute = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //Allow the form to expand and show the references
            if (this.Height < 500)
            {
                this.Height = 639;
                this.Refresh();
            }
            else
            {
                this.Height = 440;
                this.Refresh();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //close everything down before we leave.
            if (thisClient != null)
            {
                connected = false;
                try
                {
                    networkStream.Close();
                    thisClient.Close();
                    networkStream.Dispose();
                }
                catch (Exception ex)
                {
                    errDisp(ex);
                }
                lblStatus.Text = "OFF-LINE";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            this.Close();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            lblStatus.Text = "OFF-LINE";
        }


#region   "Stream IO methods"
    void sendCommandData(String inputx)
    {
        try {
            String msg = inputx;
            msg += "\n";

            foreach (byte b in msg.ToCharArray())
            {
                byte k = (byte)replaceKey((char)(b & 0xff));
                networkStream.WriteByte(k);
            }
            textBox3.Text = "";
        }
        catch (Exception ex)
        {
           errDisp(ex);
        }
    }

    private void sendData(String inpx) 
    {
        try 
        {
            foreach (byte b in inpx.ToCharArray())
            {
                char t = replaceKey((char) b);
                networkStream.WriteByte((byte)t);
            } 
        }
        catch (Exception ex)
        {
            errDisp(ex);
        }
    }

    private void sendData(byte inx)
    {
        try
        {
            waitTime(1);
            int y = (int)inx;
            networkStream.WriteByte((byte)(y & 0xff));
        }
        catch (Exception ex)
        {
            errDisp(ex);
        }
    }

    private void sendData(int ipx)
    {
        try
        {
            waitTime(1);
            ipx = ipx & 0xff;
            byte[] lol = new byte[] {0};
            lol[0]= (byte)ipx;
            networkStream.Write(lol, 0, 1);
        }
        catch (Exception ex)
        {
            errDisp(ex);
        }
    }
#endregion

#region "Command Processing"

    private void processLine(String comline)
        {
            String[] parts;
            
            if(comline.StartsWith("EXEC") && ScriptToExecute.Length > 0)
            {
                //execute loaded widucky script
                if(connected)
                {
                    executeScript(ScriptToExecute.ToString());
                }

            }
            if(comline.StartsWith("STRING"))
            {
                //Send the string chars one at a time
                String resultant = comline.Substring(7);
                sendData(resultant);
                return;
            }
            else if (comline.StartsWith("DELAY"))
            {
                //Wait given millsecs
                int delTime = Convert.ToInt32(comline.Substring(6));
                if(delTime > 0) 
                {
                    waitTime(delTime);
                }
                return;
            }
            else if(comline.StartsWith("ENTER"))
            {
                //Send a Return
                byte k = (byte)176;  //'B0'
                sendData(k);
                return;
            }
            else if (comline.StartsWith("COMD"))
            {
                //Send full command line plus return
                String resultant = comline.Substring(5);
                sendCommandData(resultant);
                return;
            }
            else if (comline.StartsWith("VER"))
            {
                sendData(250 & 0xff);
                return;
            }
            else if(comline.StartsWith("KEY"))
            {
                //Send a windows ALT key combo (eg. 'ALT + 0124')
                //Windows ALT keys(
                sendData(252 & 0xff);
                //numberpad keys inputs
                String nums = comline.Substring(4);
                nums = nums.Replace(" ","");
                nums = nums.Replace(System.Environment.NewLine, "");
                char[] bc = nums.ToCharArray();
                for(int x=0;x<nums.Length;x++)
                {
                    int ky = getNumericPad(bc[x]);
                    sendData(ky & 0xff);
                }
                //signal sequence end
                sendData(253 & 0xff);
                return;
            }
            else if(comline.StartsWith("MAP"))
            {
                String typx = comline.Substring(4);
                typx = typx.Replace(" ","");
                typx = typx.Replace(System.Environment.NewLine, "");
                mapType = typx;
                return;
            }
            else if(comline.StartsWith("RAW"))
            {
                //Raw hex data command
                comline = comline.Substring(4).ToString();
                comline = comline.Replace(System.Environment.NewLine, "");
                comline  = comline.Replace(" ", "");
                int y = Int32.Parse(comline, System.Globalization.NumberStyles.HexNumber);
                sendData(y & 0xff);
                return;
            }
            else if(comline.StartsWith("MODE"))
            {
                //Set comms mode: 1: ser/ser  0: key/ser
                comline = comline.Substring(5).ToString();
                comline = comline.Replace(System.Environment.NewLine, "");
                comline  = comline.Replace(" ", "");
                int y = Convert.ToInt32(comline);
                if(y > 0)
                {
                    //set mode 1: Ser / Ser
                    sendData(255 & 0xff);
                    controlMode = 1;
                }
                else
                {
                    //set mode 0: Key / Ser
                    sendData(249 & 0xff);
                    controlMode = 0;
                }
                return;

            }
            else
            {
                //Compound Command or Special Key
                String[] dif = new String[] {"+"};
                if(comline.IndexOf(dif[0]) > 0)
                {
                    //Multi-Key Special Character
                    comline = comline.Replace("\\r?\\n","");
                    parts = comline.Split(dif, StringSplitOptions.RemoveEmptyEntries);

                    sendData(251 & 0xff); //signal multi start
                    foreach(String part in  parts)
                    {
                        String mako = part.Replace(" ", "");
                        if(getKeyCode(mako) > 0)
                        {
                            sendData(getKeyCode(mako) & 0xff);
                        }
                        else
                        {
                            sendData(mako);
                        }
                        waitTime(100);
                    }
                    sendData(254 & 0xff); //signal multi end
                    waitTime(50);
                    sendData(254 & 0xff); //signal multi end
                    return;
                }
                else
                {
                    //Single Special Key
                    parts = new String[1];
                    parts[0] = comline.ToString();
                    String sect = parts[0].Replace(" ", "");
                    if(getKeyCode(sect) > 0)
                    {
                        sendData(getKeyCode(sect) & 0xff);
                    }
                    return;
                }

        }
     }
        private int getKeyCode(String subcom)
        {
            //Get the correct keycode
            int resultant = 0;
            int keyVal = -1;
            switch (subcom)
            {
                case "CTRL":
                    resultant = 128;
                    break;
                case "SHIFT":
                    //left shifttest
                    resultant = 129;
                    break;
                case "ALT":
                    resultant = 130;
                    break;
                case "TAB":
                    resultant = 179;
                    break;
                case "GUI":
                    //left GUI (windows)
                    resultant = 131;
                    break;
                case "MENU":
                    //MENU Key
                    resultant = 237;
                    break;
                case "GUI_R":
                    resultant = 135;
                    break;
                case "ESC":
                    resultant = 177;
                    break;
                case "BACKSPACE":
                    resultant = 178;
                    break;
                case "INS":
                    resultant = 209;
                    break;
                case "DEL":
                    resultant = 212;
                    break;
                case "HOME":
                    resultant = 210;
                    break;
                case "ALTGR":
                    resultant = 134;
                    break;
                case "CTRLR":
                    resultant = 132;
                    break;
                case "SHIFTR":
                    resultant = 133;
                    break;
                case "F1":
                    resultant = 194;
                    break;
                case "F2":
                    resultant = 195;
                    break;
                case "F3":
                    resultant = 196;
                    break;
                case "F4":
                    resultant = 197;
                    break;
                case "F5":
                    resultant = 198;
                    break;
                case "F6":
                    resultant = 199;
                    break;
                case "F7":
                    resultant = 200;
                    break;
                case "F8":
                    resultant = 201;
                    break;
                case "F9":
                    resultant = 202;
                    break;
                case "F10":
                    resultant = 203;
                    break;
                case "F11":
                    resultant = 204;
                    break;
                case "F12":
                    resultant = 205;
                    break;
                case "CAPS_LOCK":
                    resultant = 193;
                    break;
                case "PAGE_UP":
                    resultant = 211;
                    break;
                case "PAGE_DOWN":
                    resultant = 214;
                    break;
                case "UP":
                    resultant = 218;
                    break;
                case "DWN":
                    resultant = 217;
                    break;
                case "LFT":
                    resultant = 216;
                    break;
                case "RHT":
                    resultant = 215;
                    break;
                default:
                    resultant = keyVal;
                    break;
            }
            return (resultant);
        }
        private char replaceKey(char inp)
        {
            //Needed because of the keycode differences between
            //US and UK keyboards. Others are not supported
            char repKey = inp;
            switch (mapType)
            {
                case "UK":
                    switch ((int)inp)
                    {
                        case 64:
                            //@
                            repKey = (char)34;
                            break;
                        case 34:
                            // "
                            repKey = (char)64;
                            break;
                        case 35:
                            //#
                            repKey = (char)186;
                            break;
                        case 126:
                            //~
                            repKey = (char)124;
                            break;
                        case 47:
                            // Forward slash (/)
                            repKey = (char)192;
                            break;
                        case 92:
                            // Back slash (\)
                            repKey = (char)0xec;
                            break;
                        default:
                            repKey = inp;
                            break;
                    }

                    return (repKey);
            }
            return (repKey);
        }
        int getNumericPad(char inx)
        {
            //Ruturn the corresponding numeric pad
            //keycode
            int vx = (int)inx;
            if (vx > 0)
            {
                return (vx + 224);
            }
            else
            {
                return (234);
            }
        }
#endregion

        private void waitTime(int y)
        {
            //Used by the scripting engine to implement the 'DELAY 200'
            //command etc.
            DateTime x = DateTime.Now;
            x = x.AddMilliseconds(y);
            while (DateTime.Now < x)
            {
                Application.DoEvents();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //the Connect Button
            if(textBox2.Text.Length > 8)
            {
                try
                {
                    thisClient = new System.Net.Sockets.TcpClient();
                    thisClient.Connect(textBox2.Text, 6673);
                    networkStream = thisClient.GetStream();
                    Thread listenThread = new Thread(listenForData);
                    connected = true;
                    listenThread.Start();
                    this.timer1.Start();
                    lblStatus.Text = "ON-LINE";
                    lblStatus.ForeColor = System.Drawing.Color.Blue;
                }
                catch (Exception ex)
                {
                    errDisp(ex);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //The SEND button routine
            if(connected && textBox3.Text.Length > 0)
            {
                if (!textBox3.Text.StartsWith("EXEC"))
                {
                    textBox1.AppendText(textBox3.Text + System.Environment.NewLine);
                }
                processLine(textBox3.Text.ToString());
                textBox3.Text = "";

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Form timer used for updating the received data
            //window
            if(newData.Length > 0)
            {
                textBox1.AppendText(newData);
                newData = "";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Open the script load screen
            openFileDialog1.Multiselect = false;
            openFileDialog1.Title = "Load WiDucky Script File";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    System.IO.StreamReader sr = new
                    System.IO.StreamReader(openFileDialog1.FileName);
                    ScriptToExecute = sr.ReadToEnd();
                    sr.Close();
                    textBox1.AppendText("> SCRIPT LOADED > RUN WITH 'EXEC'");
                }
                catch (Exception ex)
                {
                    errDisp(ex);
                }

            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter)
            {
                this.button4_Click(sender, e);
            }
        }

        private void listenForData()
        {
            //The Data Received Thread
            while (connected && networkStream!=null)
            {
                
                if (networkStream.DataAvailable)
                {
                    int outx = networkStream.ReadByte();
                    newData += (char)outx;
                }
                
            }
        }

        private void executeScript(String file)
        {
            //execute a laoded script line by line
            try
            {
                String[] parts = file.Split(new string[1] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String part in parts)
                {
                    textBox1.AppendText((part + System.Environment.NewLine));
                    processLine(part);
                }
            }
            catch (Exception ex)
            {
                errDisp(ex);
            }
        }

        private void errDisp(Exception ex)
        {
            //a general error diaplay routine
            MessageBox.Show("Error: " + ex.Message.ToString());
            ex = null;
        }


   }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Threading;
using DXFImport;

namespace Paint
{

    public partial class MainForm : Form
    {
        public const int IMAGE_LENGTH = 999;//2199;
        public const int IMAGE_WIDTH = 799;//2499;

        //public const float IMAGE_LENGTH_SCALED = 2199;
        //public const float IMAGE_WIDTH_SCALED = 2499;

        public bool draw = false;
        ISerialSender _serialPort;
        private delegate void SetTextDeleg(string text);
        bool ready = true;

        private bool Brush = true;                      //Uses either Brush or Eraser. Default is Brush
        private Shapes DrawingShapes = new Shapes();    //Stores all the drawing data
        private bool IsPainting = false;                //Is the mouse currently down (PAINTING)
        private bool IsErasing = false;                 //Is the mouse currently down (ERASEING)
        private Point LastPos = new Point(0, 0);        //Last Position, used to cut down on repative data.
        private Color CurrentColour = Color.Black;      //Defaullt Colour
        private ColorCode CurrentColourCode;      //Defaullt Colour

        private float CurrentWidth = 3;                //Deafult Pen width
        private int ShapeNum = 0;                       //record the shapes so they can be drawn separately.
        private Point MouseLoc = new Point(0, 0);       //Record the mouse position
        private bool IsMouseing = false;                //Draw the mouse?
        
        private int baudRate = 9600; //9600 default
        private int dataBits = 8;

        decimal laserX = 0.0m;
        decimal laserY = 0.0m;

        decimal scale = 10.0m;

        bool startpoint = false;

        string[] lines = null;
        int totalsteps = 0;
        int counter = 0;
        bool rastering = false;
        bool vectoring = false;
        int loc = 25;
        //List<int> Power = new List<int>();
        StringBuilder strLog = new StringBuilder();
        List<ColorCode> Colors = new List<ColorCode>();
        int colorcount = 0;
        SerialSenderFactory m_serialSenderFactory;
        string[] gcode;
        int line = 0;

        CADImage M_FCADImage = null;
        public MainForm(SerialSenderFactory p_serialSenderFactory)
        {
            InitializeComponent();
            InitializeDrawingPanel();

            statusLabel.Text = "";

            //this.panelDrawPad.Size = new System.Drawing.Size(IMAGE_LENGTH + 1, IMAGE_WIDTH + 1);
            m_serialSenderFactory = p_serialSenderFactory;

            _serialPort = m_serialSenderFactory.GenerateSerialSender();
            tT = new ToolTip();
        }

        private void InitializeDrawingPanel()
        {
            //Set Double Buffering
            panelDrawPad.Controls.Clear();
            panelDrawPad.GetType().GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panelDrawPad, new object[] { System.Windows.Forms.ControlStyles.UserPaint | System.Windows.Forms.ControlStyles.AllPaintingInWmPaint | System.Windows.Forms.ControlStyles.DoubleBuffer, true });
            //combocom.Items.Add("");
            //combocom.Items.AddRange(SerialPort.GetPortNames());
            InitializeCOMCombo();
            //lblgcode.Text = "G90\nG21\n";
            AddColor(ColorCode.ColorType.Vector);
            CurrentColour = Color.Red;
            AddColor(ColorCode.ColorType.Raster);
            CurrentColour = Color.Black;
            
        }
        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(500);
            //string data = _serialPort.
            string data = _serialPort.ReadLine();
            this.BeginInvoke(new SetTextDeleg(si_DataReceived), new object[] { data });

        }

        private void si_DataReceived(string data)
        {
            //strLog.Append(string.Format("Recieved {0}\n", data));

            lblterminal.Text += data + Environment.NewLine;

            if (draw)
            {
                //Adjust mode.
                if (data == "Raster")
                {
                    //strLog.Append(string.Format("Inside Recieved Data\n"));
                    rastering = true;
                }
                if (data == "Vector")
                {
                    //strLog.Append(string.Format("Inside Recieved Vector\n"));
                    vectoring = true;
                    System.Threading.Thread.Sleep(10);
                    _serialPort.Write("\r\n\r\n");

                    _serialPort.Write(gcode[line] + "\n");
                }
            }
                if (data == "home")
                {
                    btnhome.BackColor = Color.LimeGreen;
                    btnhome.Enabled = true;
                    //strLog.Append(string.Format("Inside Recieved Home\n"));
                    Thread.Sleep(1000);
                    //Send vector code

                    if (CurrentColourCode.GCode != null && CurrentColourCode.GCode.Length > 1)
                        BeginSendVector();
                }
            

                if (rastering)
                {
                    //strLog.Append(string.Format("Rastering is true\n"));
                    //timer1.Enabled = true;
                    if (SendRaster(data)) //Ended sending raster code
                    {
                        //strLog.Append(string.Format("SendRaster() returns true\n"));
                        //Sleep 10 seconds to give time for Risha to home
                        //Thread.Sleep(20 * 1000);
                        //BeginSendVector();
                    }

                }



                if (vectoring)
                {
                    //strLog.Append(string.Format("Vectoring is true\n"));
                    SendVector(data);

                }
            
        }

        private void SendVector(string data)
        {
            if (data == "ok\r")
                ready = true;

            if (ready && gcode != null) //gcode
            {
                try
                {
                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    line++;
                    if (line != gcode.Length)
                    {
                        lblmc.Text = "Cut";
                        //strLog.Append(string.Format("normal line of SendVector \n"));
                        _serialPort.Write(gcode[line] + "\n");
                        lblterminal.Text += gcode[line] + Environment.NewLine;
                        ready = false;
                        lblstatus.Text = (line-1) + " of " + (gcode.Length - 1) + " (" + line * 100 / (gcode.Length - 1) + "%)";
                        progressbar.Visible = true;
                        progressbar.Value = line * 100 / (gcode.Length - 1);
                        btnhome.BackColor = Color.Red;
                        btnhome.Enabled = false;
                    }
                    else
                    {
                        //strLog.Append(string.Format("Last line of SendVector \n"));
                        Thread.Sleep(100);
                        _serialPort.Write("$H\n");
                        lblterminal.Text += "$H\n" + Environment.NewLine;
                        Thread.Sleep(100);
                        _serialPort.Write("raster\n");
                        lblterminal.Text += "raster\n" + Environment.NewLine;

                        //gcode = null;
                        vectoring = false;
                        line = 0;
                        //lblgcode.Text = "";
                        startpoint = false;
                        EnableDrawing();
                        progressbar.Visible = false;
                        lblstatus.Text = "Done";
                        draw = false;
                        btnhome.BackColor = Color.LimeGreen;
                        btnhome.Enabled = true;
                        lblmc.Text = "Neutral";

                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening/writing to serial port :: " + ex.Message, "Error!");
                }
            }
            else
                strLog.Append(string.Format("SendVector() not ready or gcode = null  \n"));
        }

        int linecount = 0;

        int RasterSleepTimeInMs = 10; //10 Default
        private bool SendRaster(string data)
        {   
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();

                if (data == "Establishing contact..")
                {
                    //strLog.Append(string.Format("Recieved Establishing contact..  \n"));
                    //System.Threading.Thread.Sleep(1000);
                    _serialPort.Write("A\n");
                    //strLog.Append(string.Format("Sent {0}\n", "A\n"));
                    lblstatus.Text = "Established contact..";
                    lblmc.Text = "Engrave";
                    linecount = lines.Count();
                }
                if (data == "A")
                {
                    //strLog.Append(string.Format("Recieved A..  \n"));
                    //System.Threading.Thread.Sleep(1000);
                    //_serialPort.Write("A\n");
                    lblstatus.Text = "Rastering...";
                    lblterminal.Text += lblstatus.Text + Environment.NewLine;

                }
                if (data == "1")
                {
                    counter++;
                    System.Threading.Thread.Sleep(RasterSleepTimeInMs);
                    _serialPort.Write((lines[counter - 1].Length + 10) + "\n");
                    //lblterminal.Text += (lines[counter - 1].Length + 10) + Environment.NewLine;

                    //strLog.Append(string.Format("Sent {0}\n", (lines[counter - 1].Length + 10) + "\n"));

                }
                if (data == "2")
                {
                    System.Threading.Thread.Sleep(RasterSleepTimeInMs);
                    _serialPort.Write(lines[counter - 1] + "\n");
                    //lblterminal.Text += lines[counter - 1] + Environment.NewLine;
                    //strLog.Append(string.Format("Sent {0}\n", lines[counter - 1] + "\n"));
                    //lblstatus.Text = (counter-1) + " of " + (totalsteps - 1) + " (" + (counter * 100) / (totalsteps) + "%)";
                    //progressbar.Visible = true;
                    //progressbar.Value = counter * 100 / (totalsteps );
                    //btnhome.BackColor = Color.Red;
                    //btnhome.Enabled = false;

                    if (counter == linecount)
                    {
                        timer1.Enabled = false;
                        lblstatus.Text = "Done";
                        lblterminal.Text += lblstatus.Text + Environment.NewLine;
                        //strLog.Append(string.Format("SendRaster() last line..  \n"));
                        //MessageBox.Show("Done");
                        rastering = false;
                        progressbar.Visible = false;
                        //_serialPort.Write("r\n");
                        //strLog.Append(string.Format("Sent {0}\n", "r\n"));

                        EnableDrawing();
                        counter = 0;
                        lines = null;
                        draw = false;
                        lblmc.Text = "Neutral";
                        return true; //Ended all raster
                    }
                }

                //if (lines[counter - 1] == "r")
                //{
                //    MessageBox.Show("Done");
                //    rastering = false;
                //    progressbar.Visible = false;
                //    _serialPort.Write("r\n");
                //    btndraw.Enabled = true;
                //    return true; //Ended all raster
                //}


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return false; //Awaiting reply
        }

        private void EnableDrawing()
        {
            strLog.Append(string.Format("EnableDrawing() & reset lblgcode ..  \n"));
            //lblgcode.Text = "";
            btndraw.Enabled = true;
            panelDrawPad.Enabled = true;
        }
        private void panel1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            tT.Show("X:" + e.Location.X + ",Y:" + e.Location.Y, panelDrawPad);
            //If we're painting...
            if (Brush)
            {
                laserX = (LastPos.X / scale);
                laserY = (/*IMAGE_WIDTH -*/ LastPos.Y) / scale;

                //lblgcode.Text += "M05\r\n";
                //label1.Text = "X: " + laserX + "\nY: " + laserY;
                //lblgcode.Text += "G0 X" + laserX + " Y" + laserY + " Z0\r\n";

                //set it to mouse down, illatrate the shape being drawn and reset the last position
                IsPainting = true;
                ShapeNum++;
                LastPos = new Point(0, 0);
            }
            //but if we're eraseing...
            else
            {
                IsErasing = true;
            }
        }


        protected void panel1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MouseLoc = e.Location;
            tT.Show("X:" + e.Location.X + ",Y:" + e.Location.Y, panelDrawPad);
            //PAINTING
            if (IsPainting)
            {
                //check its not at the same place it was last time, saves on recording more data.

                    if (LastPos != e.Location)
                    {
                        //set this position as the last positon
                        LastPos = e.Location;
                        laserX = (LastPos.X / scale);
                        laserY = (/*IMAGE_WIDTH -*/ LastPos.Y) / scale;

                        if (!startpoint)
                        {

                            //lblgcode.Text += "M05\r\n";
                            //label1.Text = "X: " + laserX + "\nY: " + laserY;

                            //Specify the speed after M03 (opening the laser)
                            if (CurrentColour == Color.Black)
                            {
                                //CurrentColourCode.GCode += string.Format("F{0}\n", 255);

                                CurrentColourCode.GCode += "G0 X" + laserX + " Y" + laserY + " Z0\n";

                                //OKE: Specify the power before M03 (opening the laser)
                                CurrentColourCode.GCode += string.Format("p{0}\n", CurrentColourCode.PowerString);


                                //Open the laser
                                CurrentColourCode.GCode += "M03\n";

                            }
                            startpoint = true;

                        }
                        else
                        {
                            //label1.Text = "X: " + laserX + "\nY: " + laserY;
                            if (CurrentColour == Color.Black)
                            {
                                CurrentColourCode.GCode += "G01 X" + laserX + " Y" + laserY + " Z0\n";

                            }
                        }

                    
                    //store the position, width, colour and shape relation data
                    DrawingShapes.NewShape(LastPos, CurrentWidth, CurrentColour, ShapeNum);
                }
            }
            if (IsErasing)
            {
                //Remove any point within a certain distance of the mouse
                DrawingShapes.RemoveShape(e.Location, 10);
            }
            //refresh the panel so it will be forced to re-draw.
            panelDrawPad.Refresh();
            
        }
 
        private void panel1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            tT.Show("X:" + e.Location.X + ",Y:" + e.Location.Y, panelDrawPad);
            if (IsPainting)
            {
                //Finished Painting.
                IsPainting = false;
                if (CurrentColour == Color.Black)
                {
                    CurrentColourCode.GCode += "M05\n";
                }
                    startpoint = false;

            }
            if (IsErasing)
            {
                //Finished Earsing.
                IsErasing = false;
            }
            //if (CurrentColourCode.Type == ColorCode.ColorType.Vector)
            //{
            //    string[] tempgcode;
            //    tempgcode = Regex.Split(CurrentColourCode.GCode, "\n");
            //    foreach (string g in tempgcode)
            //    {
            //        lblgcode.Text += g + Environment.NewLine;
            //    }
            //    lblgcode.Update();
            //}
        }

        //DRAWING FUNCTION
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            //No smoothing needed
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //IMORTANT: Nearest neighbor interpolation to prevent creating new pixels
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            //Draw the image if there is a chosen one
            if (chosenImage != null)
                e.Graphics.DrawImage(chosenImage, new Rectangle(new Point(20,20),new Size(chosenImage.Width,chosenImage.Height)));//panelDrawPad.ClientRectangle);

            //Draw the DXF Image if there is a chosen one
            if (M_FCADImage != null)
                M_FCADImage.Draw(e.Graphics);

            //DRAW THE LINES
            for (int i = 0; i < DrawingShapes.NumberOfShapes() - 1; i++)
            {
                Shape T = DrawingShapes.GetShape(i);
                Shape T1 = DrawingShapes.GetShape(i + 1);
                //make sure shape the two ajoining shape numbers are part of the same shape
                if (T.ShapeNumber == T1.ShapeNumber)
                {
                    //create a new pen with its width and colour
                    Pen p = new Pen(T.Colour, T.Width);
                    p.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    p.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    //draw a line between the two ajoining points
                    e.Graphics.DrawLine(p, T.Location, T1.Location);
                    //get rid of the pen when finished
                    p.Dispose();
                }
            }

            //If mouse is on the panel, draw the mouse
            if (IsMouseing)
            {
                e.Graphics.DrawEllipse(new Pen(Color.Blue, 0.5f), MouseLoc.X - (CurrentWidth / 2), MouseLoc.Y - (CurrentWidth / 2), CurrentWidth, CurrentWidth);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Go from the BRUSH to the ERASER
            Brush = !Brush;
        }



        public void AddColor(ColorCode.ColorType type)
        {
            if (panelColorCodes.Controls.Count == 0)
            {
                loc = 0;
            }

            int defaultPower = 80;

            ColorCode cc = new ColorCode();
            cc.Id = colorcount;
            cc.Ccolor = CurrentColour;
            cc.Type = type;
            cc.Power = defaultPower;
            cc.Speed = 0;

            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Name = colorcount + "c";
            c.Items.Add("Cutting");
            c.Items.Add("Engraving");
            if (type == ColorCode.ColorType.Vector)
            {
                c.SelectedIndex = 0;
                c.SelectedText = "Cutting";
            }
            else
            {
                c.SelectedIndex = 1;
                c.SelectedText = "Engraving";
            }
            c.Location = new Point(25, loc);
            c.SelectedIndexChanged += new EventHandler(c_SelectedIndexChanged);
            
            panelColorCodes.Controls.Add(c);
            loc = loc + 25;

            Label l = new Label();
            l.Name = CurrentColour.Name;
            l.Width = 20;
            l.Height = 20;
            l.BackColor = CurrentColour;
            l.DoubleClick += new EventHandler(l_DoubleClick);

            l.Location = new Point(0, loc);
            panelColorCodes.Controls.Add(l);

            Label p = new Label();
            p.Text = "P";
            p.AutoSize = true;
            p.Location = new Point(25, loc);
            panelColorCodes.Controls.Add(p);

            TrackBar pt = new TrackBar();
            pt.Name = colorcount.ToString();
            pt.Minimum = 0;
            pt.Maximum = 100;
            pt.Value = defaultPower;
            //pt.Width = 100;
            //pt.Height = 45;
            pt.TickFrequency = 1;
            pt.TickStyle = TickStyle.BottomRight;
            pt.SmallChange = 1;
            pt.LargeChange = 5;
            pt.Orientation = Orientation.Horizontal;
            pt.AutoSize = true;
            pt.Scroll += new EventHandler(pt_Scroll);
            pt.Location = new Point(35, loc);
            panelColorCodes.Controls.Add(pt);

            Label val = new Label();
            val.Name = colorcount + "val";
            val.AutoSize = true;
            val.Location = new Point(pt.Location.X + pt.Width, loc);
            val.Text = pt.Value.ToString();
            panelColorCodes.Controls.Add(val);

            loc = loc + 45;

            Label sep = new Label();
            sep.AutoSize = false;
            sep.Height = 2;
            sep.Width = panelColorCodes.Width;
            sep.BorderStyle = BorderStyle.Fixed3D;
            sep.Location = new Point(0, loc);
            panelColorCodes.Controls.Add(sep);

            loc = loc + 5;


            //Power.Add(pt.Value);
            Colors.Add(cc);
            colorcount++;

            c_SelectedIndexChanged(c, new EventArgs());
            pt_Scroll(pt, new EventArgs());

            CurrentColourCode = Colors[0];
            //Colors[0].Type = ColorCode.ColorType.Vector;
            //Colors[0].Power = 80;

            //loc = loc + 20;
        }

        void c_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox c = (ComboBox)sender;
            int id = Convert.ToInt32(c.Name.Substring(0, c.Name.Length - 1));
            if (c.Text == "Engraving")
            {
                Colors[id].Type = ColorCode.ColorType.Raster;
            }
            else
            {
                Colors[id].Type = ColorCode.ColorType.Vector;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {

        }

        private bool ColorExists(Color color)
        {
            foreach (ColorCode cc in Colors)
            {
                if (cc.Ccolor == color)
                    return true;
            }

            return false;
        }

        void l_DoubleClick(object sender, EventArgs e)
        {
            CurrentColour = Color.FromName(((Label)sender).Name);
            //if (CurrentColour == Color.Black)
            //{
            //    Colors[1] = CurrentColourCode;
            //    CurrentColourCode = Colors[0];
            //}
            //else
            //{
            //    Colors[0] = CurrentColourCode;
            //    CurrentColourCode = Colors[1];
            //}
            
        }

        void pt_Scroll(object sender, EventArgs e)
        {
            TrackBar b = (TrackBar)sender;
            int id = Convert.ToInt32(b.Name);
            Colors[id].Power = b.Value;
            Label l = (Label)panelColorCodes.Controls.Find(id + "val", false)[0];
            l.Text = b.Value.ToString();
            l.Update();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            //Change the width of the pen. Oh and convert it to a float
            CurrentWidth = Convert.ToSingle(numericStroke.Text);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Reset the list, removeing all shapes.
            DrawingShapes = new Shapes();
            panelDrawPad.Refresh();
            lblgcode.Text = "";
            lblgcode.Text = "G90\nG21\n";
            //label1.Text = "";
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            //Hide the mouse cursor and tell the re-drawing function to draw the mouse
            Cursor.Hide();
            IsMouseing = true;
            pnldraw.Focus();
            
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            //show the mouse, tell the re-drawing function to stop drawing it and force the panel to re-draw.
            Cursor.Show();
            IsMouseing = false;
            panelDrawPad.Refresh();
            //tT.Hide(panelDrawPad);
        }

        public string GenerateRasterCode(ColorCode p_cc = null)
        {
            //float scale = Math.Min(IMAGE_LENGTH_SCALED / IMAGE_LENGTH, IMAGE_WIDTH_SCALED / IMAGE_WIDTH);

            Bitmap b = new Bitmap(IMAGE_LENGTH + 1, IMAGE_WIDTH + 1);
            panelDrawPad.DrawToBitmap(b, new Rectangle(0, 0, IMAGE_LENGTH + 1, IMAGE_WIDTH + 1));
            //float newlength = (IMAGE_LENGTH * scale) + 1.0f;
            //float newwidth = (IMAGE_WIDTH * scale) + 1.0f;
            //Bitmap resized = new Bitmap(b, new Size((int)newlength,(int)newwidth));
            //Graphics gr = Graphics.FromImage(b);
            //gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            //gr.DrawImage(resized, new Rectangle(0, 0, (int)newlength, (int)newwidth));
            
            RasterInterpreter r = new RasterInterpreter(chkPowerGrayScale.Checked);
            List<ColorCode> Rasters = new List<ColorCode>();

            //Raster all if inputed ColorCode is null
            if (p_cc == null)
            {
                foreach (ColorCode cc in Colors)
                {
                    if (cc.Type == ColorCode.ColorType.Raster)
                    {
                        Rasters.Add(cc);
                    }
                }
            }
            else //Raster only the ColorCode inputed
            {
                Rasters.Add(p_cc);
            }

            return r.Convert(b, Rasters);
        }

        private void btnsave_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Starting to save code ...";
            System.IO.File.WriteAllText(@"Gcode.txt", lblgcode.Text);
            //MessageBox.Show("Saved Successfully", "Save GCode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            bool rasterexists = false;
            foreach (ColorCode c in Colors)
            {
                if (c.Type == ColorCode.ColorType.Raster)
                {
                    rasterexists = true;
                }
            }
            if (rasterexists)
            {
                statusLabel.Text = "Generating Raster Code ...";
                string outputraster = GenerateRasterCode();
                System.IO.File.WriteAllText(@"raster.txt", outputraster);
            }

            statusLabel.Text = "Saved Successfully";
            MessageBox.Show("Saved Successfully", "Save Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = string.Empty;
        }

        private void btnconnect_Click(object sender, EventArgs e)
        {
            if (btnconnect.Text != "Disconnect")
            {
                if (combocom.Text != "")//&& _serialPort.IsOpen == false)
                {
                    _serialPort = m_serialSenderFactory.GenerateSerialSender(combocom.Text, baudRate, Parity.None, dataBits, StopBits.One);
                    //_serialPort = new SerialPort("COM20", 115200, Parity.None, 8, StopBits.One);
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    _serialPort.Handshake = Handshake.None;
                    _serialPort.DtrEnable = true;
                    _serialPort.RtsEnable = true;

                    //_serialPort.ReadTimeout = 1000;
                    //_serialPort.WriteTimeout = 500;
                    _serialPort.Open();

                    btnconnect.Text = "Disconnect";
                    btnconnect.BackColor = Color.Red;

                    try
                    {
                        if (!_serialPort.IsOpen)
                            _serialPort.Open();

                        //_serialPort.Write("\r\n\r\n");
                        //System.Threading.Thread.Sleep(2);
                        btnhome.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error opening/writing to serial port :: " + ex.Message, "Error!");
                    }

                }
                else
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        btnconnect.Text = "Connect";
                        btnconnect.BackColor = Color.Lime;
                        btnhome.Enabled = false;
                    }
                    else
                    {
                        string[] coms = SerialPort.GetPortNames();
                        foreach (string com in coms)
                        {
                            if (com != "")
                            {
                                combocom.Text = com;
                                _serialPort = m_serialSenderFactory.GenerateSerialSender(combocom.Text, baudRate, Parity.None, dataBits , StopBits.One);
                                //_serialPort = new SerialPort("COM20", 115200, Parity.None, 8, StopBits.One);
                                _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                                _serialPort.Handshake = Handshake.None;
                                _serialPort.DtrEnable = true;
                                _serialPort.RtsEnable = true;

                                //_serialPort.ReadTimeout = 1000;
                                //_serialPort.WriteTimeout = 500;
                                _serialPort.Open();

                                btnconnect.Text = "Disconnect";
                                btnconnect.BackColor = Color.Red;

                                try
                                {
                                    if (!_serialPort.IsOpen)
                                        _serialPort.Open();

                                    //_serialPort.Write("\r\n\r\n");
                                    //System.Threading.Thread.Sleep(2);
                                    btnhome.Enabled = true;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Error opening/writing to serial port :: " + ex.Message, "Error!");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _serialPort.Close();
                btnconnect.Text = "Connect";
                btnconnect.BackColor = Color.Lime;
                EnableDrawing();
            }
        }

        private void btndraw_Click(object sender, EventArgs e)
        {
            //strLog.Append(string.Format("btndraw_Click()..  \n"));
            try
            {
                if (!_serialPort.IsOpen)
                {
                    btnconnect_Click(sender, e);
                }

                if (_serialPort.IsOpen)
                {
                    lblgcode.Text = "";
                    BeginSendRaster();
                }
                else
                {
                    //lblterminal.Text += "Please Connect 1st.." + Environment.NewLine;
                    string rastercode = GenerateRasterCode();
                }
           
            }
            catch (Exception m)
            {
                lblterminal.Text = m.Message;
            }

            
        }

        private void DisableDrawing()
        {
            strLog.Append(string.Format("DisableDrawing()..  \n"));
            btndraw.Enabled = false;
            panelDrawPad.Enabled = false;
        }

        private void BeginSendRaster()
        {
            strLog.Append(string.Format("BeginSendRaster() ..  \n"));
            List<ColorCode> t = new List<ColorCode>();
            foreach (ColorCode cc in Colors)
            {
                if (cc.Type == ColorCode.ColorType.Raster)
                {
                    t.Add(cc);
                }
            }
            if (t.Count != 0)
            {
                //strLog.Append(string.Format("GenerateRasterCode() called from BeginSendRaster() ..  \n"));
                if (checkengrave.Checked)
                {
                    draw = true;
                    DisableDrawing();
                    string rastercode = GenerateRasterCode();
                    _serialPort.Write("raster\n");
                    lines = rastercode.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    totalsteps = lines.Length;
                }
                else
                {
                    if (checkcut.Checked && CurrentColourCode.GCode != null && CurrentColourCode.GCode.Length > 0)
                    {
                        draw = true;
                        DisableDrawing();
                        BeginSendVector();
                    }
                    else
                    {
                        MessageBox.Show("Please Choose To Cut Or Engrave First Before Drawing","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                //strLog.Append(string.Format("BeginSendVector() called from BeginSendRaster() ..  \n"));
                BeginSendVector();
            }



        }
        private void BeginSendVector()
        {
            strLog.Append(string.Format("BeginSendVector()..  \n"));
            List<ColorCode> t = new List<ColorCode>();
            Colors[0] = CurrentColourCode;
            foreach (ColorCode cc in Colors)
            {
                
                if (cc.Type == ColorCode.ColorType.Vector)
                {
                    t.Add(cc);
                    string[] tempgcode = Regex.Split(CurrentColourCode.GCode, "\n");
                    lblgcode.Text = "$H\n" + Environment.NewLine + "G90\n" + Environment.NewLine + "G21\n" + Environment.NewLine;
                    foreach (string code in tempgcode)
                    {
                        lblgcode.Text += code + Environment.NewLine;
                    }
                }
            }
            lblgcode.Update();
            if (t.Count > 0)
            {
                if (checkcut.Checked)
                {
                    DisableDrawing();

                    gcode = Regex.Split(lblgcode.Text, "\n");

                    //strLog.Append(string.Format("Send vector in BeginSendVector()..  \n"));
                    //Thread.Sleep(1000);
                    vectoring = true;
                    _serialPort.Write("vector\n");
                    lblterminal.Text += "vector\n" + Environment.NewLine;
                    Thread.Sleep(100);
                    _serialPort.Write("\r\n\r\n");
                    lblterminal.Text += "\r\n\r\n" + Environment.NewLine;
                    //_serialPort.Write("$H\n");
                    //Send first line of gcode
                    Thread.Sleep(10);
                    _serialPort.Write(gcode[line] + "\n");
                    lblterminal.Text += gcode[line] + Environment.NewLine;

                    ready = false;
                }
                else
                {
                    //MessageBox.Show("Done");
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }


        Image chosenImage = null;
        private void imageBWToolStripMenuItem_Click(object sender, EventArgs e)
        {



        }

        private void ChooseImage(bool p_bBlackAndWhite)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Image Files (*.bmp, *.jpg)|*.bmp;*.jpg";
            dialog.Multiselect = false;
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string strFile = dialog.FileName;

                chosenImage = Bitmap.FromFile(strFile);

                Bitmap b = new Bitmap(chosenImage);

                b = GrayScale(b, p_bBlackAndWhite);

                chosenImage = (Image)b;
            }
        }

        public Bitmap GrayScale(Bitmap Bmp, bool p_pBlackAndWhite = false)
        {
            int rgb;
            Color c;

            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)((c.R + c.G + c.B) / 3);

                    if (p_pBlackAndWhite)
                    {


                        //if (rgb > 127)
                        if (rgb > 217)
                            Bmp.SetPixel(x, y, Color.White);
                        else
                            Bmp.SetPixel(x, y, Color.Red);
                    }
                    else
                        Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return Bmp;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chosenImage = null;
            Colors.Clear();
            DrawingShapes.Clear();
            panelDrawPad.Refresh();
            panelColorCodes.Controls.Clear();
            CurrentColour = Color.Black;
            counter = 0;
            ShapeNum = 0;
            colorcount = 0;
            InitializeDrawingPanel();
        }

        private void imageBlackWhiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChooseImage(true);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            chosenImage = null;
            Colors.Clear();
            DrawingShapes.Clear();
            panelDrawPad.Refresh();
            panelColorCodes.Controls.Clear();
            CurrentColour = Color.Black;
            counter = 0;
            ShapeNum = 0;
            colorcount = 0;
            InitializeDrawingPanel();
            //lblgcode.Text += "G0 X0 Y0 Z0\n";
            //try
            //{
            //    if (!_serialPort.IsOpen)
            //        _serialPort.Open();
            //    bool raster = false;
            //    bool vector = false;
            //    foreach (ColorCode c in Colors)
            //    {
            //        if (c.Type == ColorCode.ColorType.Raster)
            //        {
            //            raster = true;

            //        }
            //        if (c.Type == ColorCode.ColorType.Vector)
            //        {
            //            vector = true;
            //        }
            //    }

            //    //Sending raster part of image
            //    if (raster)
            //    {
            //        BeginSendRaster();

            //    }

            //    //Sending vector part of image, if there was no raster & there was vector
            //    if (!raster && vector)
            //    {
            //        BeginSendVector();

            //    }
            //    btndraw.Enabled = false;

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error opening/writing to serial port :: " + ex.Message, "Error!");
            //}
        }

        private void btnAddColor_Click(object sender, EventArgs e)
        {
            //Show and Get the result of the colour dialog
            DialogResult D = colorDialog1.ShowDialog();
            if (D == DialogResult.OK)
            {
                //Apply the new colour
                CurrentColour = colorDialog1.Color;

                if (!ColorExists(colorDialog1.Color))
                    AddColor(ColorCode.ColorType.Vector);
            }
        }

        private void imageGrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChooseImage(false);
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void InitializeCOMCombo()
        {
            combocom.Items.Clear();
            combocom.Items.Add("");
            combocom.Items.AddRange(SerialPort.GetPortNames());
        }
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            InitializeCOMCombo();
        }

        private void dXFFileRasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "(DXF Files)|*.dxf";

            if (openFileDialog1.ShowDialog(this) != DialogResult.OK) return;
            if (openFileDialog1.FileName != null)
            {
                M_FCADImage = new CADImage();
                M_FCADImage.Base.Y = Bottom - 100;
                M_FCADImage.Base.X = 100;
                M_FCADImage.LoadFromFile(openFileDialog1.FileName);

                EnableDXFUI();

            }
            this.Invalidate();
        }

        private void EnableDXFUI()
        {
            //Enable DXF Controls
            btnZoomInDXF.Enabled = true;
            btnZoomOutDXF.Enabled = true;
            btnMoveLeftDXF.Enabled = true;
            btnMoveRightDXF.Enabled = true;
            btnMoveUpDXF.Enabled = true;
            btnMoveDownDXF.Enabled = true;
        }
        private void btnZoomInDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.FScale = M_FCADImage.FScale * 1.2f;
            panelDrawPad.Invalidate();
        }

        private void btnZoomOutDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.FScale = M_FCADImage.FScale / 1.2f;
            M_FCADImage.Base.Y = Bottom - 100;
            M_FCADImage.Base.X = 100;
            panelDrawPad.Invalidate();
        }

        private void btnMoveLeftDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.Base.X -= 10;
            panelDrawPad.Invalidate();
        }

        private void btnMoveRightDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.Base.X += 10;
            panelDrawPad.Invalidate();
        }

        private void btnMoveUpDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.Base.Y -= 10;
            panelDrawPad.Invalidate();
        }

        private void btnMoveDownDXF_Click(object sender, EventArgs e)
        {
            M_FCADImage.Base.Y += 10;
            panelDrawPad.Invalidate();
        }

        private void toolStripContainer1_LeftToolStripPanel_Click(object sender, EventArgs e)
        {

        }

        private void txtterminal_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    string terminal = txtterminal.Text.Substring(0, txtterminal.Text.Length - 2);
                    _serialPort.Write(terminal + "\n");
                }
                catch (Exception m)
                {
                    lblterminal.Text += m.Message + Environment.NewLine;


                }
                txtterminal.Text = "";

            }

        }

        private void txtterminal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {


            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblstatus.Text = (counter - 1) + " of " + (totalsteps - 1) + " (" + (counter * 100) / (totalsteps) + "%)";
            progressbar.Visible = true;
            progressbar.Value = counter * 100 / (totalsteps);

        }

        private void numericStroke_TextChanged(object sender, EventArgs e)
        {
            CurrentWidth = Convert.ToSingle(numericStroke.Text);
        }
        public ToolTip tT { get; set; }
        
        private void panelDrawPad_MouseHover(object sender, EventArgs e)
        {
            //tT.Show("koko", panelDrawPad);
        }

        private void pnldraw_MouseEnter(object sender, EventArgs e)
        {
            pnldraw.Focus();
        }

        private void pnldraw_MouseLeave(object sender, EventArgs e)
        {
            //tT.Hide(panelDrawPad);

        }

        private void combozoom_TextUpdate(object sender, EventArgs e)
        {

        }

        private void combozoom_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnexecute_Click(object sender, EventArgs e)
        {
            lblmc.Text = "Vector";
            vectoring = true;
            line = 0;
            gcode = Regex.Split(lblgcode.Text, "\r\n");

            _serialPort.Write("vector\n");
            Thread.Sleep(50);
            _serialPort.Write("\r\n\r\n");
            //Send first line of gcode
            _serialPort.Write(gcode[line] + "\n");
            lblterminal.Text += gcode[line] + Environment.NewLine;
            ready = false;
            
        }

        private void btnhome_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                lblmc.Text = "Home";
                vectoring = true;
                line = 0;
                string HomeString = "$H\n";
                gcode = Regex.Split(HomeString, "\r\n");
                _serialPort.Write("vector\n");
                Thread.Sleep(50);
                _serialPort.Write("\r\n\r\n");
                _serialPort.Write(gcode[line] + "\n");
                lblterminal.Text += gcode[line] + Environment.NewLine;
                ready = false;
            }
            
            
        }

        private void lblterminal_TextChanged(object sender, EventArgs e)
        {
            lblterminal.SelectionStart = lblterminal.Text.Length;
            lblterminal.ScrollToCaret();
        }

        private void btnstop_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                string StopString = "!\n";
                Thread.Sleep(50);
                _serialPort.Write(StopString);
                lblterminal.Text += "Stopped" + Environment.NewLine;
                
            }
        }
    }





}

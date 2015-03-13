using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Paint;

namespace Paint
{
    public class RasterInterpreter
    {
        bool bLaserOff = false;

        const string RIGHT_FAST = "1";
        const string RIGHT_SLOW = "2";
        const string LEFT_FAST = "3";
        const string LEFT_SLOW = "4";
        const string UP_FAST = "5";
        const string UP_SLOW = "6";
        const string DOWN_FAST = "7";
        const string DOWN_SLOW = "8";
        const string LASER_ON = "9";
        const string LASER_OFF = "0";
        const string RETURN = "r";

        string Laser_Power = "";

        const int BUFFER_SIZE = 80;
        //fields
        private Bitmap _image = null;

        //Properties
        public Bitmap Image
        {
            get { return _image; }
            set { _image = value; }
        }

        bool m_bPowerDependsOnGrayScale;

        public RasterInterpreter(bool p_bPowerDependsOnGrayScale)
        {
            m_bPowerDependsOnGrayScale = p_bPowerDependsOnGrayScale;
        }

        //Functions
        public string Convert(Bitmap img,List<ColorCode> Colors)
        {
            string[,] burnpixels;
            List<int> starts;
            List<int> ends;
            StringBuilder outputarray;
            
            //
            GetImageArray(img, Colors, out burnpixels, out starts, out ends, out outputarray);

            System.IO.File.WriteAllText(@"imagearray.txt", outputarray.ToString());

            return ConvertToRasterCode(burnpixels, starts, ends);
        }

        //private void GetImageArray(Bitmap img, List<ColorCode> Colors, out string[,] burnpixels, out List<int> starts, out List<int> ends, out StringBuilder outputarray)
        //{
        //    int[,] imagarray = new int[(int)MainForm.IMAGE_LENGTH, (int)MainForm.IMAGE_WIDTH];
        //    burnpixels = new string[(int)MainForm.IMAGE_LENGTH, (int)MainForm.IMAGE_WIDTH];
        //    starts = new List<int>();
        //    ends = new List<int>();
        //    bool hasdata = false;
        //    bool detected = false;

        //    outputarray = new StringBuilder();


        //    for (int y = 1; y < img.Height - 1; y++)
        //    {
        //        int end = 0;
        //        for (int x = 1; x < img.Width - 1; x++)
        //        {
        //            Color p = img.GetPixel(x, y);
        //            //if (p.R != 255 || p.G != 255 || p.B != 255)
        //            foreach (ColorCode cc in Colors)
        //            {
        //                Laser_Power = cc.PowerString;
        //                if (p.ToArgb() == cc.Ccolor.ToArgb()
        //                    /*|| (m_bPowerDependsOnGrayScale && p.ToArgb() != Color.White.ToArgb())*/) //Consider any color except White
        //                {

        //                    imagarray[x, y] = new int();
        //                    imagarray[x, y] = 1;
        //                    //outputfile += "1";
        //                    outputarray.Append("1");
        //                    //burnpixels[x, y] = new int();

        //                    string power = "080";

        //                    if (m_bPowerDependsOnGrayScale)
        //                        power = p.PowerString_GrayScale();
        //                    else
        //                        power = cc.PowerString;

        //                    burnpixels[x, y] = power;
        //                    Laser_Power = power;
        //                    //Console.WriteLine(string.Format("1-burnpixels[{0},{1}] = {2}", x, y, cc.PowerString));
        //                    if (!hasdata)
        //                    {
        //                        hasdata = true;
        //                        starts.Add(x);
        //                    }

        //                    end = x;
        //                    detected = true;

        //                    //No need to iterate for other ColorCodes
        //                    //break;

        //                }


        //            }

        //            if (!detected)
        //            {
        //                imagarray[x, y] = new int();
        //                imagarray[x, y] = 0;
        //                //outputfile += "0";
        //                outputarray.Append("0");
        //                burnpixels[x, y] = null;
        //            }
        //            detected = false;
        //        }
        //        if (!hasdata)
        //        {
        //            starts.Add(0);
        //        }
        //        ends.Add(end);
        //        hasdata = false;
        //        //outputfile += System.Environment.NewLine;
        //        outputarray.AppendLine();
        //    }
        //}


        private void GetImageArray(Bitmap img, List<ColorCode> Colors, out string[,] burnpixels, out List<int> starts, out List<int> ends, out StringBuilder outputarray)
        {
            int[,] imagarray = new int[MainForm.IMAGE_LENGTH, MainForm.IMAGE_WIDTH];
            burnpixels = new string[MainForm.IMAGE_LENGTH, MainForm.IMAGE_WIDTH];
            starts = new List<int>();
            ends = new List<int>();
            bool hasdata = false;
            bool detected = false;

            outputarray = new StringBuilder();

            //DUMMY
            starts.Add(0);
            ends.Add(0);

            for (int y = 1; y < img.Height - 1; y++)
            {
                int end = 0;
                for (int x = 1; x < img.Width - 1; x++)
                {
                    
                    if (x == 25 && y == 25)
                    {
                        int l = 0;
                    }


                    Color p = img.GetPixel(x, y);
                    //if (p.R != 255 || p.G != 255 || p.B != 255)
                    foreach (ColorCode cc in Colors)
                    {
                        Laser_Power = cc.PowerString;
                        if (p.ToArgb() == cc.Ccolor.ToArgb()
                            || (m_bPowerDependsOnGrayScale && p.ToArgb() != Color.White.ToArgb())) //Consider any color except White
                        {

                            if (p.ToArgb() != Color.Black.ToArgb())
                            {

                                int j = 0;
                            }

                            imagarray[x, y] = new int();
                            imagarray[x, y] = 1;
                            //outputfile += "1";
                            outputarray.Append("1");
                            //burnpixels[x, y] = new int();

                            string power = "080";

                            if (m_bPowerDependsOnGrayScale)
                                power = p.PowerString_GrayScale();
                            else
                                power = cc.PowerString;

                            burnpixels[x, y] = power;
                            Laser_Power = power;
                            //Console.WriteLine(string.Format("1-burnpixels[{0},{1}] = {2}", x, y, cc.PowerString));
                            if (!hasdata)
                            {
                                hasdata = true;
                                starts.Add(x);
                            }

                            end = x;
                            detected = true;

                            //No need to iterate for other ColorCodes
                            break;

                        }

                    }

                    if (!detected)
                    {
                        imagarray[x, y] = new int();
                        imagarray[x, y] = 0;
                        //outputfile += "0";
                        outputarray.Append("0");
                        burnpixels[x, y] = null;
                    }
                    detected = false;
                }
                if (!hasdata)
                {
                    starts.Add(0);
                    //ends.Add(0);
                }
                //else
                    ends.Add(end);

                hasdata = false;
                //outputfile += System.Environment.NewLine;
                outputarray.AppendLine();
            }
        }


        string GetLaserStatus(string LASER_REQUIRED)
        {
            if (bLaserOff && LASER_REQUIRED == LASER_ON)
            {
                bLaserOff = false;
                return LASER_ON;
            }
            else if (!bLaserOff && LASER_REQUIRED == LASER_OFF)
            {
                bLaserOff = true;
                return LASER_OFF;
            }

            
            return string.Empty;
        }

        /// <summary>
        /// Converts image array (0,1) to raster code.
        /// </summary>
        /// <param name="burnpixels">The Image array pixels</param>
        /// <param name="starts">The start index of each line </param>
        /// <param name="ends">The end index of each line</param>
        /// <returns>String containing raster code</returns>
        private string ConvertToRasterCode(string[,] burnpixels, List<int> starts, List<int> ends)
        {
            string outputfile = "";
            
            StringBuilder buffer = new StringBuilder();
            int i = 1; //Position on x-axis
            int k = 1; //Current Buffer Length

            List<int> startLengths = new List<int>();
            List<int> endLengths = new List<int>();

            List<string> startDirs = new List<string>();
            List<string> endDirs = new List<string>();

            //OKE: Adjust the power
            buffer.Append("p"+Laser_Power+",");
            
            while (i < (MainForm.IMAGE_WIDTH - 1)) //-1: should not write on edge as it causes error
            {
                if (starts[i] != 0)
                {
                    //left to right
                    k = 1;
               
                        int length = Math.Abs(starts[i] - starts[i - 1]); //OKE: The distance to move further
                        string strDir = "";
                        if (starts[i] > starts[i - 1])
                            strDir = RIGHT_FAST;
                        else if (starts[i] < starts[i - 1])
                            strDir = LEFT_FAST;

                        startLengths.Add(length);
                        startDirs.Add(strDir);
                        if (!String.IsNullOrEmpty(strDir))
                        {
                            for (int u = 0; u < length; u++)
                            {
                                //buffer.Append("01");
                                string strAppend = GetLaserStatus(LASER_OFF) + strDir;
                                buffer.Append(strAppend);
                                k += strAppend.Length;
                                if (k >= BUFFER_SIZE)
                                {
                                    buffer.Append(",");

                                    k = 1;
                                }
                            }
                        }
                  


                    for (int j = starts[i]; j <= ends[i]; j++)
                    {

                        if (burnpixels[j, i] != null)
                        {
                            //buffer.Append("92");
                            //Console.WriteLine(string.Format("burnpixels[{0},{1}] = {2}",j,i,burnpixels[j, i].ToString()));
                            string strAppend = GetLaserStatus(LASER_ON) /*+ burnpixels[j, i]*/ + RIGHT_SLOW;
                            buffer.Append(strAppend);
                            k += strAppend.Length;
                        }
                        else
                        {
                            //buffer.Append("01");
                            string strAppend = GetLaserStatus(LASER_OFF) + RIGHT_FAST;
                            buffer.Append(strAppend);
                            k += strAppend.Length;
                        }

                        if (k >= BUFFER_SIZE)
                        {
                            buffer.Append(",");
                            k = 1;
                        }
                        //k++;
                    }

                    length = Math.Abs(ends[i + 1] - ends[i]);
                    strDir = "";
                    if (ends[i + 1] > ends[i])
                        strDir = RIGHT_FAST;
                    else if (ends[i + 1] < ends[i])
                        strDir = LEFT_FAST;

                    endLengths.Add(length);
                    endDirs.Add(strDir);

                    if (!String.IsNullOrEmpty(strDir))
                    {
                        for (int u = 0; u < length; u++)
                        {
                            //buffer.Append("01");
                            string strAppend = GetLaserStatus(LASER_OFF) + strDir;
                            buffer.Append(strAppend);
                            k += strAppend.Length;
                            if (k >= BUFFER_SIZE)
                            {
                                buffer.Append(",");
                                k = 1;
                            }
                        }
                    }

                    //OKE: Get to the next line.
                    i++;

                    //buffer.Append("0,5");
                    //Get down one line
                    buffer.Append(GetLaserStatus(LASER_OFF) + UP_FAST);


                    //right to left
                    k = 0;

                    //if (ends[i] < ends[i - 1])
                    //{
                    //    int length = ends[i - 1] - ends[i];
                     
                    //    for (int u = 0; u < length; u++)
                    //    {
                    //        //buffer.Append("03");
                    //        string strAppend = GetLaserStatus(LASER_OFF) + LEFT_FAST;
                    //        buffer.Append(strAppend);
                    //        k += strAppend.Length;
                    //        if (k >= BUFFER_SIZE)
                    //        {
                    //            buffer.Append(",");
                    //            k = 1;
                    //        }
                    //    }
                    //}

                    for (int j = ends[i]; j >= starts[i]; j--)
                    {
                        if (burnpixels[j, i] != null)
                        {
                            //buffer.Append("94");
                            //Console.WriteLine(string.Format("burnpixels[{0},{1}] = {2}", j, i, burnpixels[j, i].ToString()));
                            string strAppend = GetLaserStatus(LASER_ON) /*+ burnpixels[j, i]*/ + LEFT_SLOW;
                            buffer.Append(strAppend);
                            k += strAppend.Length;
                        }
                        else
                        {
                            //buffer.Append("03");
                            string strAppend = GetLaserStatus(LASER_OFF) + LEFT_FAST;
                            buffer.Append(strAppend);
                            k += strAppend.Length;
                        }
                        if (k >= BUFFER_SIZE)
                        {
                            buffer.Append(",");
                            k = 1;
                        }
                        //k++;
                    }

                    //if (starts[i] < starts[i + 1])
                    //{
                    //    int length = starts[i + 1] - starts[i];
                    //    for (int u = 0; u < length; u++)
                    //    {
                    //        //buffer.Append("03");
                    //        string strAppend = GetLaserStatus(LASER_OFF) + LEFT_FAST;
                    //        buffer.Append(strAppend);
                    //        k += strAppend.Length;
                    //        if (k >= BUFFER_SIZE)
                    //        {
                    //            buffer.Append(",");
                    //            k = 1;
                    //        }
                    //    }

                    //}

                    //Next Line
                    i++;
                    //buffer.Append("0,5,");
                    buffer.Append(GetLaserStatus(LASER_OFF) + UP_FAST + ",");
                    k = 0;
                }
                else
                {
                    //buffer.Append("5,");
                    buffer.Append(UP_FAST /*+ ","*/);
                    k++;
                    if (k >= BUFFER_SIZE)
                    {
                        buffer.Append(",");
                        k = 1;
                    }
                    i++;

                    startLengths.Add(0);
                    endLengths.Add(0);
                    startDirs.Add("");
                    endDirs.Add("");
                }
            }

            //r means go home.
            buffer.Append("," + RETURN);


            int oo = endLengths.Count;
            int ll = startLengths.Count;
            outputfile = buffer.ToString();
            return outputfile;
        }

    }
}


static class  ColorExtension
{
    public static string PowerString_GrayScale(this Color c)
    {
       
        float ratio = 100f / 256f;

        int grayscale = (c.R + c.G + c.B) / 3;

        //Normalize grayscale to get a power
        int power = 100 - (int)(grayscale * ratio);

        return ColorCode.GetPowerString(power);
        
    }


}

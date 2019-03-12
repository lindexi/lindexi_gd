using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trojan
{
    class laji
    {
        public laji()
        {

        }

        public void ce()
        {
            time = new DateTime(2005 , 1 , 1);
            ran = new Random();
            file_address = "Y:\\";           
            filestr = ".encry";
            int count = 100000;
            byte[] b;
            //StringBuilder str = new StringBuilder();
            for (int i = 0;i<count; i++)
            {
                b = new byte[ran.Next() % 1000000];
                for (int c = 0; c < b.Length; c++)
                {
                    b[c] = Convert.ToByte(ran.Next() % 255);
                }
                using (System.IO.FileStream file = new System.IO.FileStream(timex() , System.IO.FileMode.Create))
                {
                    file.Write(b , 0 , b.Length);
                }

                //if (time.Month == 2)
                //{
                //    break;
                //}
                //str.Append(timex() + "\r\n");

            }
            //b = Encoding.UTF8.GetBytes(str.ToString());
            //using (System.IO.FileStream file = new System.IO.FileStream(timex() , System.IO.FileMode.Create))
            //{
            //    file.Write(b , 0 , b.Length);
            //}
            //count = 27;
            //Console.WriteLine(a());
        }



        private DateTime time
        {
            set;
            get;
        }

        private string file_address
        {
            set;
            get;
        }

        private string filename
        {
            set;
            get;
        }

        private string filestr
        {
            set;
            get;
        }

        private Random ran
        {
            set;
            get;
        }

        private int count
        {
            set;
            get;
        }

        private string timex()
        {
            int i = 0;
            string str = string.Empty;
            if (time.Hour > 7 && time.Hour < 24)
            {
                i = ran.Next(2);
            }
            else
            {
                i = ran.Next(5);
            }

            if (i == 0)
            {
                str = a();
                count++;
            }
            else
            {
                time = time.AddHours(i);//.ToUniversalTime();
                
                count = 0;
            }

            if (!System.IO.Directory.Exists(file_address + time.Year.ToString() + "\\" + time.Month.ToString()))
            {
                System.IO.Directory.CreateDirectory(file_address + time.Year.ToString() + "\\" + time.Month.ToString());
            }

            return file_address + time.Year.ToString()+"\\" + time.Month.ToString() + "\\" + filename + time.Year.ToString() + time.Month.ToString() + time.Day.ToString() + time.Hour.ToString() + str + filestr;
        }
        private string a()
        {
            string str = "";
            int i;
            i = count;
            while (i >= 0)
            {
                str = str.Insert(0 , ( (char)( (int)'a' + i % 26 ) ).ToString());
                i /= 26;
                i--;
            }
            return str;
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddComma
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Users\afei\Desktop\XYGyroOut00.txt";
            string[] fileTemp = File.ReadAllLines(path);
            for (int i = 0; i < fileTemp.Length; i++)
            {
                fileTemp[i] = fileTemp[i].Replace(' ', ',');
            }

        }
    }
}

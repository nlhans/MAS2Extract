﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimTelemetry.Game.rFactor2;

namespace MAS2Extract
{
    class Program
    {
        static void Main(string[] args)
        {
            args = new string[2] { @"C:\Users\Hans\Documents\CoreShaders.mas", @"C:\Users\Hans\Documents\haha\" };
            if (args.Length == 0)
            {
                Console.WriteLine("mas2extract.exe filename.MAS [-s] [target_directory]");
                Console.WriteLine("MAS2 extractor tool for rFactor 2 MAS game files.");
                Console.WriteLine("");
                Console.WriteLine("Parameter -s will only show all files inside the archive, but not extract them");
                Console.WriteLine("If no extract directory is entered, the location of mas2extract.exe will be used.");
                return;
            }
            string f= args[0];
            if(File.Exists(f) == false)
            {
                Console.WriteLine("Cannot find target MAS file!");
                return;
            }

            string target = "";
            if (args.Length == 2)
                target = args[1];
            else
                target = "./";
            bool unpack = true;
            if (target == "-s")
            {
                unpack = false;
            }else
            {
                if (!Directory.Exists(target))
                {
                    Console.WriteLine("Target extract directory doesn't exist");
                    return;
                }
            }

            MAS2Reader reader;
            try
            {
                reader = new MAS2Reader(f);
                Console.WriteLine(reader.Count + " files in archive");

                Console.WriteLine(
                    "+-----------------------------------------------------------------------------+");
                Console.Write(PatchString("| Name", 38) + " | ");
                Console.Write(PatchString("Compressed", 10) + " | ");
                Console.Write(PatchString("Raw Size", 10) + " | ");
                Console.Write(PatchString("Ratio", 10) + " |");
                Console.WriteLine();
                Console.WriteLine(
                    "+-----------------------------------------------------------------------------+");

                foreach(MAS2File file in reader.Files)
                {
                    double ratio = 1 - file.CompressedSize/file.UncompressedSize;
                    Console.Write(PatchString("| "+file.Filename, 38) + " | ");
                    Console.Write(PatchString(file.CompressedSize.ToString(), 10) + " | ");
                    Console.Write(PatchString(file.UncompressedSize.ToString(), 10) + " | ");
                    Console.Write(PatchString(Math.Round(100*ratio,1).ToString(), 10) + " |");
                    Console.WriteLine();
                    if(unpack)
                        reader.ExtractFile(file, Path.Combine(target + "\\", file.Filename));
                }
                
                Console.WriteLine(
                    "+-----------------------------------------------------------------------------+");
                Console.WriteLine("Press ENTER to terminate");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open MAS archive. Is it a rFactor 2 archive?");
            }
        }

        private static string PatchString(string name, int max_width)
        {
            if (name.Length > max_width)
                return name.Substring(0, name.Length - 4) + "... ";
            else
            {
                string patch = "";
                for (int i = 0; i < max_width - name.Length; i++)
                    patch += " ";
                return name + patch;
            }
        }
    }
}

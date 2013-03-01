 /*   Builds an X-Ray file to be used on the Amazon Kindle
 *   Original xray builder by shinew, http://www.mobileread.com/forums/showthread.php?t=157770 , http://www.xunwang.me/xray/
 *
 *   Copyright (C) 2013 Ephemerality <Nick Niemi - ephemeral.vilification@gmail.com>
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.

 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.

 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XRayBuilder
{
    class Program
    {
        static void Exit(string err, bool wait = true)
        {
            Console.Error.WriteLine(err);
            if (wait)
            {
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
            Environment.Exit(1);
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: xraybuilder [-o path] [--offset N] [-p] [-r] [-s shelfariURL] [--spoilers] [-u path] mobiPath\n" +
                "-o path (--outdir)\tPath defines the output directory\n\t\t\tIf not specified, uses ./out\n" +
                "--offset N\tSpecifies an offset to be applied to every book location.\n\t\t\tN must be a number (usually negative)\n\t\t\tSee README for more info\n" +
                "-p path (--python)	Path must point to python.exe\n\t\t\tIf not specified, uses the command \"python\",\n\t\t\twhich requires the Python directory to be defined in\n\t\t\tthe PATH environment variable.\n" +
                "-r (--saveraw)\t\tSave raw book markup to the output directory\n" +
                "-s (--shelfari)\t\tShelfari URL\n\t\t\tIf not specified, there will be a prompt asking for it\n" +
                "--spoilers\t\tUse descriptions that contain spoilers\n\t\t\tDefault behaviour is to use spoiler-free descriptions.\n" +
                "-u path (--unpack)\tPath must point to mobi_unpack.py\n\t\t\tIf not specified, searches in the current directory\n\n" +
                "After used once, mobi_unpack path will be saved as default and is not necessary to include every time.\n" +
                "You can also drag and drop a number of mobi files onto the exe after those paths have been saved.\n\n" +
                "See README.txt for more information.");
            Exit("", true);
        }

        static void Main(string[] args)
        {
            string mobi_unpack = "";
            string python = "";
            string shelfariURL = "";
            string outDir = "";
            int offset = 0;
            bool saveRaw = true;
            bool spoilers = false;
            List<string> fileList = new List<string>();

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-m" || args[i] == "--mobi2mobi")
                    {
                        Console.WriteLine("Mobi2mobi is no longer required.");
                        i++;
                    }
                    else if (args[i] == "-o" || args[i] == "--outdir")
                        outDir = args[++i];
                    else if (args[i] == "--offset")
                        int.TryParse(args[++i], out offset);
                    else if (args[i] == "-p" || args[i] == "--python")
                        python = args[++i];
                    else if (args[i] == "-r" || args[i] == "--saveraw")
                        saveRaw = true;
                    else if (args[i] == "-s" || args[i] == "--shelfari")
                        shelfariURL = args[++i];
                    else if (args[i] == "--spoilers")
                        spoilers = true;
                    else if (args[i] == "-u" || args[i] == "--unpack")
                    {
                        mobi_unpack = args[++i];
                        if (!File.Exists(mobi_unpack)) Exit("Specified mobi_unpack.py script not found.");
                    }
                    else if (File.Exists(args[i]))
                        fileList.Add(args[i]);
                    else if (args[0] == "--usage" || args[0] == "--help" || args[0] == "-?")
                        ShowUsage();
                    else
                        Console.Error.WriteLine("Unknown arg {0}: {1}. Continuing anyway...", i, args[i]);
                }

                //if (fileList.Count == 0)
                //    Exit("No mobi files specified.");
                //else if (shelfariURL == "")
                //    Exit("Shelfari URL not found.");

                if (outDir != "" && !Directory.Exists(outDir))
                    Exit("Out directory does not exist. (" + outDir + ")");
                else if (outDir == "")
                {
                    outDir = Environment.CurrentDirectory + "\\out";
                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                }
            }
            else
                ShowUsage();

            Console.WriteLine("Using {0} as an output directory.", outDir);
            //TODO: Make a function to handle these instead of copy/pasting!
            if (mobi_unpack != "" && mobi_unpack != XRayBuilder.Properties.Settings.Default.mobi_unpack)
            {
                XRayBuilder.Properties.Settings.Default.mobi_unpack = mobi_unpack;
                XRayBuilder.Properties.Settings.Default.Save();
                Console.WriteLine("Saving mobi_unpack directory as default. If not specified in the future, this one will be used.");
            }
            else if (mobi_unpack == "" && XRayBuilder.Properties.Settings.Default.mobi_unpack != "")
            {
                mobi_unpack = XRayBuilder.Properties.Settings.Default.mobi_unpack;
                Console.WriteLine("Using saved mobi_unpack path ({0}).", mobi_unpack);
            }
            else if (mobi_unpack == "")
                mobi_unpack = "dist/mobi_unpack.exe";
            if(!File.Exists(mobi_unpack))
                Exit("Mobi_unpack not found.");

            if (python != "" && python != XRayBuilder.Properties.Settings.Default.python)
            {
                XRayBuilder.Properties.Settings.Default.python = python;
                XRayBuilder.Properties.Settings.Default.Save();
                Console.WriteLine("Saving python directory as default. If not specified in the future, this one will be used.");
            }
            else if (python == "" && XRayBuilder.Properties.Settings.Default.python != "")
            {
                python = XRayBuilder.Properties.Settings.Default.python;
                Console.WriteLine("Using saved python path ({0}).", python);
            }
            else if (python == "")
            {
                python = "python";
                if(Path.GetExtension(mobi_unpack) == ".py")
                    Console.WriteLine("Using default Python command. Ensure Python's directory is included in your PATH environment variable.");
            }

            foreach (string mobiFile in fileList)
            {
                Console.WriteLine("Processing {0}...", Path.GetFileName(mobiFile));
                if (shelfariURL == "")
                {
                    Console.WriteLine("Enter Shelfari URL for {0} (Enter to skip): ", Path.GetFileNameWithoutExtension(mobiFile));
                    shelfariURL = Console.ReadLine().Trim();
                    if (shelfariURL == "")
                    {
                        Console.WriteLine("No Shelfari URL specified! Skipping this book.");
                        continue;
                    }
                }
                Console.WriteLine("Running mobi_unpack to get book data...");
                //Create a temp folder and use mobi_unpack from command line to unpack mobi file to that folder
                ProcessStartInfo startInfo = new ProcessStartInfo();
                string randomFile = GetTempDirectory();
                if (Path.GetExtension(mobi_unpack) == ".py")
                {
                    startInfo.FileName = python;
                    startInfo.Arguments = mobi_unpack + " -r -d \"" + mobiFile + @""" """ + randomFile + @"""";
                }
                else
                {
                    startInfo.FileName = mobi_unpack;
                    startInfo.Arguments = "-r -d \"" + mobiFile + @""" """ + randomFile + @"""";
                }
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                string unpackInfo = "";
                try
                {
                    using (Process process = Process.Start(startInfo))
                    {
                        process.BeginErrorReadLine();
                        using (StreamReader reader1 = process.StandardOutput)
                        {
                            unpackInfo = reader1.ReadToEnd();
                            //Console.WriteLine(unpackInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error trying to launch mobi_unpack, skipping this book. ({0})", e.Message);
                    continue;
                }
                string rawML = Path.GetFileNameWithoutExtension(mobiFile) + ".rawml";
                //Was the unpack successful?
                if (unpackInfo.Contains("Write opf\r\nCompleted"))
                    rawML = randomFile + @"\mobi7\" + rawML;
                else if (File.Exists(randomFile + @"\mobi8\" + rawML))
                    rawML = randomFile + @"\mobi8\" + rawML;
                else
                    Exit("Error unpacking mobi file: " + unpackInfo);
                //Console.WriteLine(unpackInfo);
                Console.WriteLine("Mobi unpacked...");
                //Attempt to find the .rawml unpacked from the mobi
                if (!File.Exists(rawML))
                    Exit("Error finding unpacked rawml file. Path: " + rawML);
                Console.WriteLine("RawML found at {0}. Grabbing metadata...", rawML);
                if (saveRaw)
                {
                    Console.WriteLine("Saving rawML to output directory.");
                    File.Copy(rawML, Path.Combine(outDir, Path.GetFileName(rawML)), true);
                }

                string uniqid = "";
                string asin = "";
                Match match = Regex.Match(unpackInfo, @"'ASIN': \['([-|\w]*)']");
                if (match.Success && match.Groups.Count > 1)
                    asin = match.Groups[1].Value;
                match = Regex.Match(unpackInfo, @"'UniqueID': \['(\d*)']");
                if (match.Success && match.Groups.Count > 1)
                    uniqid = match.Groups[1].Value;
                //string test = unpackInfo.Substring(dsf + 
                //Attempt to get database name from the mobi file.
                //If mobi_unpack ran successfully, then hopefully this will always be valid?
                byte[] dbinput = new byte[32];
                FileStream stream = File.Open(mobiFile, FileMode.Open, FileAccess.Read);
                if (stream == null)
                {
                    Console.WriteLine("Error opening mobi file (stream error). Skipping book...");
                    continue;
                }
                int bytesRead = stream.Read(dbinput, 0, 32);
                if(bytesRead != 32)
                {
                    Console.WriteLine("Error reading from mobi file. Skipping book...");
                    continue;
                }
                string databaseName = Encoding.Default.GetString(dbinput).Trim('\0');
                
                if (databaseName == "" || uniqid == "" || asin == "")
                {
                    Console.WriteLine("Error getting metadata {0} - {1} - {3}. Skipping book...", databaseName, uniqid, asin);
                    continue;
                }

                Console.WriteLine("Got metadata!\nDatabase Name: {0}\nASIN: {1}\nUniqueID: {2}\nAttempting to build X-Ray...", databaseName, asin, uniqid);
                Console.WriteLine("Spoilers: {0}", spoilers ? "Enabled" : "Disabled");
                Console.WriteLine("Location Offset: {0}", offset);
                //Create X-Ray and attempt to create the base file (essentially the same as the site)
                XRay ss = new XRay(shelfariURL, databaseName, uniqid, asin, spoilers, offset);
                if (ss.createXRAY() > 0)
                {
                    Console.WriteLine("Error while processing. Skipping to next file.");
                    continue;
                }

                Console.WriteLine("Initial X-Ray built, adding locs and chapters...");
                //Expand the X-Ray file from the unpacked mobi
                if (ss.expandFromRawML(rawML) > 0)
                {
                    Console.WriteLine("Skipping to next file.");
                    continue;
                }

                using (StreamWriter streamWriter = new StreamWriter(outDir + "\\" + ss.getXRayName(), false, Encoding.Default))
                {
                    streamWriter.Write(ss.ToString());
                }
                Console.Error.WriteLine("XRay file created successfully!\nSaved to " + outDir + "\\" + ss.getXRayName());
                Console.WriteLine("*****************************************************");
                Directory.Delete(randomFile, true);
                shelfariURL = "";
            }
            Console.WriteLine("All files processed! Press Enter to exit.");
            Console.ReadLine();
        }

        public static string GetTempDirectory()
        {
            string path;
            do
            {
                path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); 
            } while (Directory.Exists(path));
            Directory.CreateDirectory(path); 
            return path; 
        }
    }
}

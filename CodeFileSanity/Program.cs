﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeFileSanity
{
    class Program
    {
        static string[] ignore_paths = {
            "bin",
            "obj",
            "packages"
        };

        static bool hasAppveyor;

        static void Main(string[] args)
        {
            hasAppveyor = runAppveyor("");
            checkDirectory(".");
        }

        private static string getLicenseHeader(string path)
        {
            string filename;

            while (true)
            {
                filename = Directory.GetFiles(path, "*.licenseheader").FirstOrDefault();
                if (filename != null) break;

                path = Directory.GetParent(path)?.FullName;
                if (path == null) return null;
            }

            bool started = false;
            string licenseHeader = string.Empty;

            foreach (string s in File.ReadAllLines(filename))
            {
                if (started)
                {
                    if (!s.StartsWith("//"))
                        break;
                    licenseHeader += s + "\r\n";
                }

                if (s == "extensions: .cs")
                {
                    started = true;
                }
            }

            return licenseHeader;
        }

        private static void checkDirectory(string path)
        {
            if (ignore_paths.Contains(path.Split(Path.DirectorySeparatorChar).Last()))
                return;

            foreach (var sub in Directory.GetDirectories(path))
                checkDirectory(sub);

            var license = getLicenseHeader(path);

            foreach (var file in Directory.GetFiles(path, "*.cs"))
                checkFile(file, license);
        }

        private static void checkFile(string file, string licenseHeader)
        {
            string text = File.ReadAllText(file);

            if (licenseHeader != null && !text.StartsWith(licenseHeader))
            {
                report(file, $"License header missing");
            }

            if (Regex.IsMatch(text, "\r[^\n].", RegexOptions.Multiline))
            {
                report(file, $"Incorrect line endings");
            }

        }

        private static void report(string filename, string message)
        {
            Console.WriteLine($"{filename}: {message}");

            //Add-AppveyorCompilationMessage "Unreachable code detected" -Category Warning -FileName "Program.cs" -Line 1 -Column 3

            if (hasAppveyor)
                runAppveyor($"{message} -Category Warning -FileName \"{filename}\"");
        }

        private static bool runAppveyor(string args)
        {
            try
            {
                Process.Start("appveyor", args);
                return true;
            }
            catch { }

            return false;
        }
    }
}

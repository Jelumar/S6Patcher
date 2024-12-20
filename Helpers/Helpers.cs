﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace S6Patcher
{
    internal class Helpers
    {
        public static bool IsSteamOV = false; // This does not actually refer to the OV from Steam
        public static bool IsSteamHE = false; // This however, does
        public static void WriteBytes(ref FileStream Stream, long Position, byte[] Bytes)
        {
            Stream.Position = ((!IsSteamOV) ?  Position :  Position - 0x3F0000);
            try
            {
                Stream.Write(Bytes, 0, Bytes.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("WriteToFileStream:\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        public static FileStream OpenFileStream(string Path, execID ID)
        {
            FileStream Stream;
            try
            {
                Stream = new FileStream(Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return null;
            }

            if (CheckExecVersion(ref Stream, ID) == false)
            {
                Stream.Close();
                Stream.Dispose();
                return null;
            }

            return Stream;
        }
        public static OpenFileDialog CreateOFDialog()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                CheckFileExists = true,
                ShowHelp = false,
                CheckPathExists = true,
                DereferenceLinks = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Multiselect = false,
                ShowReadOnly = false,
            };

            return ofd;
        }
        public static bool CreateBackup(string Filepath)
        {
            string FileName = Path.GetFileNameWithoutExtension(Filepath);
            string DirectoryPath = Path.GetDirectoryName(Filepath);
            string FinalPath = Path.Combine(DirectoryPath, FileName + "_BACKUP.exe");

            if (File.Exists(FinalPath) == false)
            {
                try
                {
                    File.Copy(Filepath, FinalPath, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }
            return true;
        }
        public static bool CheckExecVersion(ref FileStream Reader, execID Identifier, Int64 Offset = 0x0)
        {
            string ExpectedVersion = "1, 71, 4289, 0";
            UInt32[] Mapping = {0x6ECADC, 0xF531A4, 0x6D06A8};
            byte[] Result = new byte[30];

            Reader.Position = (Mapping[(char)Identifier] - Offset);
            Reader.Read(Result, 0, 30);

            string Version = Encoding.Unicode.GetString(Result).Substring(0, ExpectedVersion.Length);
            if (Version == ExpectedVersion)
            {
                IsSteamOV = false;
                IsSteamHE = false;
                return true;
            }
            else if (Offset == 0x0 && Identifier == execID.OV) // Try again with offset applied
            {
                if (CheckExecVersion(ref Reader, Identifier, 0x3F0000) == true)
                {
                    IsSteamOV = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (Offset == 0x0 && Identifier == execID.HE) // Check for Steam HE
            {
                if (CheckExecVersion(ref Reader, Identifier, -0x1400) == true)
                {
                    IsSteamHE = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Erwartet/Expected: " + ExpectedVersion + "\nGelesen/Read: " + Version.ToString(), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }
        }
        public static List<string> GetUserScriptDirectories()
        {
            // <Documents>/Settlers/Script/UserScriptGlobal.lua && UserScriptLocal.lua are always loaded by the game when a map is started!
            string DocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            List<string> Directories = new List<string>();
            foreach (string Element in Directory.GetDirectories(DocumentPath))
            {
                if (Element.Contains("Aufstieg eines") || Element.Contains("Rise of an"))
                {
                    Directories.Add(Path.Combine(DocumentPath, Element));
                }
            }

            return Directories;
        }
        public static void RestoreUserScriptFiles()
        {
            string[] ScriptFiles = {"UserScriptLocal.lua", "EMXBinData.s6patcher"};
            List<string> Directories = GetUserScriptDirectories();

            string ScriptPath = String.Empty;
            foreach (string Element in Directories)
            {
                ScriptPath = Path.Combine(Element, "Script");
                if (!Directory.Exists(ScriptPath))
                {
                    continue;
                }
                try
                {
                    File.Delete(Path.Combine(ScriptPath, ScriptFiles[0]));
                    File.Delete(Path.Combine(ScriptPath, ScriptFiles[1]));
                }
                catch (Exception) // Errors here do not matter
                {
                    continue;
                }
            }
        }
        public static bool RestoreBackup(string filePath)
        {
            RestoreUserScriptFiles(); // Delete Userscript from Folders

            string FileName = Path.GetFileNameWithoutExtension(filePath);
            string DirectoryPath = Path.GetDirectoryName(filePath);
            string FinalPath = Path.Combine(DirectoryPath, FileName + "_BACKUP.exe");

            if (File.Exists(FinalPath) == false)
            {
                return false;
            }

            try
            {
                File.Replace(FinalPath, filePath, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            return true;
        }
    }
}

﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace VivaldiModManager
{
    public class ModManager
    {
        public ObservableCollection<VivaldiPackage> vivaldiInstallations = new ObservableCollection<VivaldiPackage>();
        public VivaldiPackage selectedVersion
        {
            get
            {
                try
                {
                    return this.vivaldiInstallations.Where(f => f.isSelected).Single();
                }
                catch
                {
                    return null;
                }
            }
        }

        public class vivaldiMod
        {
            public string fileName { get; set; }
            public string filePath { get; set; }
            public string fileRealpath { get; set; }
            public bool isEnabled { get; set; }
            public Action ModRemoved;

            public void ToggleMod(bool parameter)
            {
                if (!parameter)
                {
                    string newPath = this.filePath + ".disabled";
                    string newRealpath = this.fileRealpath + ".disabled";
                    File.Move(this.filePath, newPath);
                    File.Move(this.fileRealpath, newRealpath);
                    this.filePath = newPath;
                    this.fileRealpath = newRealpath;
                    this.isEnabled = false;
                }
                else
                {
                    string newPath = this.filePath.Replace(".disabled", "");
                    string newRealpath = this.fileRealpath.Replace(".disabled", "");
                    File.Move(this.filePath, newPath);
                    File.Move(this.fileRealpath, newRealpath);
                    this.filePath = newPath;
                    this.fileRealpath = newRealpath;
                    this.isEnabled = false;
                }
            }

            public void RemoveMod()
            {
                File.Delete(this.filePath);
                File.Delete(this.fileRealpath);
                this.ModRemoved();
            }

            RelayCommand _toggleModCommand; public ICommand ToggleModCommand
            {
                get
                {
                    if (_toggleModCommand == null)
                    {
                        _toggleModCommand = new RelayCommand(param => this.ToggleMod((bool)param),
                            param => true);
                    }
                    return _toggleModCommand;
                }
            }

            RelayCommand _removeModCommand; public ICommand RemoveModCommand
            {
                get
                {
                    if (_removeModCommand == null)
                    {
                        _removeModCommand = new RelayCommand(param => this.RemoveMod(),
                            param => true);
                    }
                    return _removeModCommand;
                }
            }
        }

        public class VivaldiPackage
        {
            public string version { get; set; }
            public string installPath { get; set; }
            public string modsPersistentDir { get; set; }
            public string modsDir { get; set; }
            public string browserHtml { get; set; }
            public string modLoader { get; set; }
            public bool isModsEnabled { get; set; }
            public bool isSelected { get; set; }
            public bool requiresAdminRights { get; set; }
            public bool Enabled { get; set; }
            public ObservableCollection<vivaldiMod> installedStyles { get; set; }
            public ObservableCollection<vivaldiMod> installedScripts { get; set; }

            public VivaldiPackage(string version, string installPath, bool isSelected)
            {
                this.installedStyles = new ObservableCollection<vivaldiMod>();
                this.installedScripts = new ObservableCollection<vivaldiMod>();
                this.version = version;
                this.installPath = installPath;
                this.modsPersistentDir = installPath + "\\.vivaldimods\\" + version;
                this.modsDir = installPath + "\\" + version + "\\resources\\vivaldi\\user_mods";
                this.browserHtml = installPath + "\\" + version + "\\resources\\vivaldi\\browser.html";
                this.modLoader = installPath + "\\" + version + "\\resources\\vivaldi\\injectMods.js";
                this.isSelected = isSelected;
                this.requiresAdminRights = this.installPath.Contains("Program Files");

                if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    this.Enabled = false;
                } else
                {
                    this.Enabled = true;
                }

                this.isModsEnabled = File.ReadAllText(this.browserHtml).Contains("<script src=\"injectMods.js\"></script>");
                this.initModsEnabled();
            }

            public void Copy(string sourceDirectory, string targetDirectory)
            {
                DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
                DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
                CopyAll(diSource, diTarget);
            }

            public void CopyAll(DirectoryInfo source, DirectoryInfo target)
            {
                Directory.CreateDirectory(target.FullName);
                foreach (FileInfo fi in source.GetFiles())
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }

                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }

            public void initModsEnabled()
            {
                if (this.isModsEnabled && this.Enabled)
                {
                    if (!Directory.Exists(this.modsDir))
                    {
                        if (this.Enabled)
                        {
                            if (Directory.Exists(this.modsPersistentDir))
                            {
                                Directory.CreateDirectory(this.modsDir);
                                this.Copy(this.modsPersistentDir, this.modsDir);
                            }
                            else
                            {
                                Directory.CreateDirectory(this.modsDir);
                                Directory.CreateDirectory(this.modsDir + "\\css");
                                Directory.CreateDirectory(this.modsDir + "\\js");
                            }
                        }
                        else return;
                    }
                    if (!Directory.Exists(this.modsPersistentDir))
                    {
                        if (this.Enabled)
                        {
                            Directory.CreateDirectory(this.modsPersistentDir);
                            Directory.CreateDirectory(this.modsPersistentDir + "\\css");
                            Directory.CreateDirectory(this.modsPersistentDir + "\\js");
                        }
                        else return;
                    }
                    if (!File.Exists(this.modLoader))
                    {
                        if (this.Enabled) File.WriteAllText(this.modLoader, ModLoader.injectMods);
                        else
                        {
                            this.isModsEnabled = false;
                            return;
                        }
                    }
                    this.searchMods();
                }
            }

            public void installMod(string mod, bool refresh = false)
            {
                if (mod.EndsWith(".css"))
                {
                    File.Copy(mod, Path.Combine(this.modsPersistentDir + "\\css\\", Path.GetFileName(mod)));
                    File.Copy(mod, Path.Combine(this.modsDir + "\\css\\", Path.GetFileName(mod)));
                    if (refresh) this.searchMods();
                }
                if (mod.EndsWith(".js"))
                {
                    File.Copy(mod, Path.Combine(this.modsPersistentDir + "\\js\\", Path.GetFileName(mod)));
                    File.Copy(mod, Path.Combine(this.modsDir + "\\js\\", Path.GetFileName(mod)));
                    if (refresh) this.searchMods();
                }
            }

            public void searchMods()
            {
                this.installedStyles.Clear();
                this.installedScripts.Clear();

                if (!this.Enabled || !this.isModsEnabled) return;

                var css = Directory.EnumerateFiles(this.modsPersistentDir + "\\css");
                var js = Directory.EnumerateFiles(this.modsPersistentDir + "\\js");
                foreach(string item in css)
                {
                    var vMod = new vivaldiMod()
                    {
                        fileName = Path.GetFileName(item),
                        filePath = Path.Combine(this.modsPersistentDir + "\\css\\", Path.GetFileName(item)),
                        fileRealpath = Path.Combine(this.modsDir + "\\css\\", Path.GetFileName(item)),
                        isEnabled = true
                    };
                    vMod.ModRemoved = this.searchMods;
                    if (item.EndsWith(".disabled"))
                    {
                        vMod.isEnabled = false;
                    }
                    this.installedStyles.Add(vMod);
                }
                foreach (string item in js)
                {
                    var vMod = new vivaldiMod()
                    {
                        fileName = Path.GetFileName(item),
                        filePath = Path.Combine(this.modsPersistentDir + "\\js\\", Path.GetFileName(item)),
                        fileRealpath = Path.Combine(this.modsDir + "\\js\\", Path.GetFileName(item)),
                        isEnabled = true
                    };
                    vMod.ModRemoved = this.searchMods;
                    if (item.EndsWith(".disabled"))
                    {
                        vMod.isEnabled = false;
                    }
                    this.installedScripts.Add(vMod);
                }
            }

            public void migrateFrom(string modsPersistentDirFrom)
            {
                if(this.Enabled)
                {
                    if (!this.isModsEnabled) this.ToggleMods(true);
                    this.initModsEnabled();
                    this.Copy(modsPersistentDirFrom, this.modsPersistentDir);
                    this.Copy(modsPersistentDir, this.modsDir);
                    Directory.Delete(modsPersistentDirFrom, true);
                    this.searchMods();
                }
            }

            public void ToggleMods(bool parameter)
            {
                if(!parameter)
                {
                    string browserHtmlText = File.ReadAllText(this.browserHtml);
                    browserHtmlText = browserHtmlText.Replace("<script src=\"injectMods.js\"></script>", "");
                    File.WriteAllText(this.browserHtml, browserHtmlText);
                    this.isModsEnabled = false;
                    this.installedScripts.Clear();
                    this.installedStyles.Clear();
                }
                else
                {
                    string browserHtmlText = File.ReadAllText(this.browserHtml);
                    browserHtmlText = browserHtmlText.Replace("<script src=\"bundle.js\"></script>",
                        "<script src=\"bundle.js\"></script><script src=\"injectMods.js\"></script>");
                    File.WriteAllText(this.browserHtml, browserHtmlText);
                    this.initModsEnabled();
                    this.isModsEnabled = true;
                }
            }

            RelayCommand _toggleModsCommand; public ICommand ToggleModsCommand
            {
                get
                {
                    if (_toggleModsCommand == null)
                    {
                        _toggleModsCommand = new RelayCommand(param => this.ToggleMods((bool)param),
                            param => this.Enabled);
                    }
                    return _toggleModsCommand;
                }
            }
        }

        public ModManager()
        {
            this.searchVivaldiInstallations();
        }

        public void searchVivaldiInstallations()
        {
            this.vivaldiInstallations.Clear();
            string installPathLocal = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null);
            string installPathGlobal = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null);

            if (installPathLocal != null) this.addVivaldiVersion(installPathLocal);
            if (installPathGlobal != null) this.addVivaldiVersion(installPathGlobal);

            var firstInList = this.vivaldiInstallations.FirstOrDefault();
            if (firstInList != null) this.selectVivaldiVersion(firstInList.version);
        }

        public void addVivaldiVersion(string path, bool versionDirectory = false)
        {
            if(Directory.Exists(path))
            {
                Regex regm = new Regex(@"(\d\.[\d\.]+)$");
                if (versionDirectory)
                {
                    if(File.Exists(path + "\\vivaldi.dll"))
                    {
                        string version = regm.Match(path).Value;
                        string installPath = path.Replace(version, "");
                        if (this.vivaldiInstallations.Where(f => f.installPath.StartsWith(installPath)).Count() == 0)
                        {
                            this.vivaldiInstallations.Add(new VivaldiPackage(version, installPath, false));
                        }
                    }
                }
                else
                {
                    Regex reg = new Regex(@"Application\\(\d\.[\d\.]+)$");
                    var versionDirectories = Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories)
                        .Where(f => reg.IsMatch(f)).Distinct().ToList();
                    foreach (string vDirectory in versionDirectories)
                    {
                        if (Directory.Exists(vDirectory))
                        {
                            string version = regm.Match(vDirectory).Value;
                            this.vivaldiInstallations.Add(new VivaldiPackage(version, path, !(this.vivaldiInstallations.Count() > 0)));
                        }
                    }
                }
            }
        }

        public void selectVivaldiVersion(string version)
        {
            if (this.selectedVersion != null) this.selectedVersion.isSelected = false;
            this.vivaldiInstallations.Where(f => f.version == version).Single().isSelected = true;
        }
    }
}
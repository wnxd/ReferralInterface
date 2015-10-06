using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Text.RegularExpressions;
using System.IO;
using VSLangProj;
using System.Net;
using System.Windows;

namespace wnxd.ReferralInterface
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidReferralInterfacePkgString)]
    public sealed class ReferralInterfacePackage : Package
    {
        private Project cproject;
        public ReferralInterfacePackage()
        {
        }
        protected override void Initialize()
        {
            OleMenuCommandService mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                CommandID menuCommandID = new CommandID(GuidList.guidReferralInterfaceCmdSet, (int)PkgCmdIDList.cmdid1);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
                menuCommandID = new CommandID(GuidList.guidReferralInterfaceCmdSet, (int)PkgCmdIDList.cmdid2);
                menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                IVsMonitorSelection MonitorSelection = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));
                IntPtr hierarchyPtr, selectionContainerPtr;
                uint pitemid;
                IVsMultiItemSelect mis;
                MonitorSelection.GetCurrentSelection(out hierarchyPtr, out pitemid, out mis, out selectionContainerPtr);
                IVsHierarchy hierarchy = (IVsHierarchy)Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy));
                object obj;
                hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
                this.cproject = (Project)obj;
                string path = this.cproject.Properties.Item("FullPath").Value.ToString();
                string name = "Interface";
                if (Directory.Exists(path + name))
                {
                    int i = 1;
                    do name = "Interface" + (i++).ToString();
                    while (Directory.Exists(path + name));
                }
                new Add(this, this.cproject.Properties.Item("DefaultNamespace").Value.ToString(), name, path).ShowDialog();
            }
            catch
            {
                IVsUIShell uiShell = (IVsUIShell)this.GetService(typeof(SVsUIShell));
                int result;
                uiShell.ShowMessageBox(0, Guid.Empty, "提示", "本插件暂时只支持C#", string.Empty, 0, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out result);
            }
        }
        internal void AddFromDirectory(string dir)
        {
            VSProject VSProject = this.cproject.Object as VSProject;
            if (VSProject != null)
            {
                string path = this.cproject.Properties.Item("FullPath").Value.ToString();
                if (VSProject.References.Find("wnxd.Web") == null) this.AddReference(VSProject.References, "wnxd.web.dll", path);
                if (VSProject.References.Find("wnxd.javascript") == null) this.AddReference(VSProject.References, "wnxd.javascript.dll", path);
                if (VSProject.References.Find("Microsoft.CSharp") == null) VSProject.References.Add("Microsoft.CSharp");
            }
            this.cproject.ProjectItems.AddFromDirectory(dir);
        }
        private void AddReference(References References, string name, string dir)
        {
            if (!File.Exists(dir + name))
            {
                using (WebClient client = new WebClient())
                {
                xh:
                    try
                    {
                        client.DownloadFile("http://wnxd.me/dll/" + name, dir + name);
                    }
                    catch
                    {
                        goto xh;
                    }
                }
            }
            References.Add(dir + name);
        }
    }
}
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
                CommandID menuCommandID = new CommandID(GuidList.guidReferralInterfaceCmdSet, (int)PkgCmdIDList.cmdid);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }
        private void MenuItemCallback(object sender, EventArgs e)
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
        internal void AddFromDirectory(string dir)
        {
            VSProject VSProject = this.cproject.Object as VSProject;
            if (VSProject != null)
            {
                string path = this.cproject.Properties.Item("FullPath").Value.ToString();
                try
                {
                    Reference Reference = VSProject.References.Find("wnxd.Web");
                    if (Reference == null) throw new Exception();
                }
                catch
                {
                    this.AddReference(VSProject.References, "wnxd.web.dll", path);
                }
                try
                {
                    Reference Reference = VSProject.References.Find("wnxd.javascript");
                    if (Reference == null) throw new Exception();
                }
                catch
                {
                    this.AddReference(VSProject.References, "wnxd.javascript.dll", path);
                }
                try
                {
                    Reference Reference = VSProject.References.Find("Microsoft.CSharp");
                    if (Reference == null) throw new Exception();
                }
                catch
                {
                    VSProject.References.Add("Microsoft.CSharp");
                }
            }
            this.cproject.ProjectItems.AddFromDirectory(dir);
        }
        private void AddReference(References References, string name, string dir)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile("http://wnxd.me/dll/" + name, dir + name);
                References.Add(dir + name);
            }
        }
    }
}
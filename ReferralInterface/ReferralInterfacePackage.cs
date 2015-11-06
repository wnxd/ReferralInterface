using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using VSLangProj;

namespace wnxd.ReferralInterface
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidReferralInterfacePkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class ReferralInterfacePackage : Package
    {
        private Project cproject;
        private bool init = true;
        protected override void Initialize()
        {
            if (this.init)
            {
                this.init = false;
                OleMenuCommandService mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (mcs != null)
                {
                    CommandID menuCommandID = new CommandID(GuidList.guidReferralInterfaceCmdSet, PkgCmdIDList.add);
                    OleMenuCommand menuItem = new OleMenuCommand(this.cmd_AddReferences, menuCommandID);
                    menuItem.BeforeQueryStatus += menuItem_BeforeQueryStatus;
                    mcs.AddCommand(menuItem);
                    menuCommandID = new CommandID(GuidList.guidReferralInterfaceCmdSet, PkgCmdIDList.update);
                    menuItem = new OleMenuCommand(this.cmd_UpdateReferences, menuCommandID);
                    menuItem.BeforeQueryStatus += menuItem_BeforeQueryStatus;
                    mcs.AddCommand(menuItem);
                }
            }
        }
        private void cmd_AddReferences(object sender, EventArgs e)
        {
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
        private void cmd_UpdateReferences(object sender, EventArgs e)
        {
            uint pitemid;
            IVsHierarchy hierarchy = this.GetCurrentSelection(out pitemid);
            object obj;
            hierarchy.GetProperty(pitemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            if (obj is ProjectItem)
            {
                ProjectItem pi = (ProjectItem)obj;
                if (pi.FileCodeModel != null && pi.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp) this.UpdateInterface(pi.Properties.Item("FullPath").Value.ToString());
                this.UpdateProjectItems(pi.ProjectItems);
            }
            else if (obj is Project)
            {
                Project p = (Project)obj;
                this.UpdateProjectItems(p.ProjectItems);
            }
        }
        private void menuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                this.GetProject();
                menuCommand.Visible = this.cproject.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp;
            }
        }
        private IVsHierarchy GetCurrentSelection(out uint pitemid)
        {
            IVsMonitorSelection MonitorSelection = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));
            IntPtr hierarchyPtr, selectionContainerPtr;
            IVsMultiItemSelect mis;
            MonitorSelection.GetCurrentSelection(out hierarchyPtr, out pitemid, out mis, out selectionContainerPtr);
            return (IVsHierarchy)Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy));
        }
        private void GetProject()
        {
            uint pitemid;
            IVsHierarchy hierarchy = this.GetCurrentSelection(out pitemid);
            object obj;
            hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            this.cproject = (Project)obj;
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
            ProjectItem pi = this.cproject.ProjectItems.AddFromDirectory(dir);
            if (pi.ProjectItems.Count == 0)
            {
                string[] list = Directory.GetFiles(dir, "*.cs");
                for (int i = 0; i < list.Length; i++) pi.ProjectItems.AddFromFile(list[i]);
            }
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
        private void UpdateProjectItems(ProjectItems ProjectItems)
        {
            foreach (ProjectItem pi in ProjectItems)
            {
                if (pi.FileCodeModel != null && pi.FileCodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp) this.UpdateInterface(pi.Properties.Item("FullPath").Value.ToString());
                UpdateProjectItems(pi.ProjectItems);
            }
        }
        private void UpdateInterface(string path)
        {
            try
            {
                FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                StreamReader sr = new StreamReader(fs);
                if (sr.ReadLine() == "//Domain")
                {
                    string Domain = sr.ReadLine().Substring(2);
                    if (sr.ReadLine() == "//Namespace")
                    {
                        string Namespace = sr.ReadLine().Substring(2);
                        if (sr.ReadLine() == "//ClassName")
                        {
                            string ClassName = sr.ReadLine().Substring(2);
                            sr.Close();
                            fs.Close();
                            Add.CreateInterface(Path.GetDirectoryName(path) + "\\", Domain, Namespace, new string[] { ClassName }, (name) => Path.GetFileName(path));
                            goto rt;
                        }
                    }
                }
                sr.Close();
                fs.Close();
            rt:
                return;
            }
            catch
            {

            }
        }
    }
}
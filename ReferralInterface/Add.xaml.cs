using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Windows;
using wnxd.javascript;

namespace wnxd.ReferralInterface
{
    /// <summary>
    /// Add.xaml 的交互逻辑
    /// </summary>
    public partial class Add : Window
    {
        private const string Interface_Name_Key = "wnxd: interface_name";
        private const string Interface_Data_Key = "wnxd: interface_data";
        private static string query = HttpUtility.UrlEncode(EncryptString("$query$", Interface_Name_Key));
        private static string interface_name = EncryptString("{\"Name\":\"interface_name\"}", Interface_Data_Key);
        private ReferralInterfacePackage _Package;
        private string _name;
        private string _path;
        public Add(ReferralInterfacePackage Package, string pnp, string np, string path)
        {
            this.InitializeComponent();
            this._Package = Package;
            this._name = np;
            this._path = path;
            this.np.Text = pnp + "." + np;
        }

        private void query_Click(object sender, RoutedEventArgs e)
        {
            this.list.Items.Clear();
            if (string.IsNullOrEmpty(this.textbox.Text))
            {

            }
            else
            {
                if (this.textbox.Text.Substring(0, 4) != "http") this.textbox.Text = "http://" + this.textbox.Text;
                if (this.textbox.Text.Last() != '/') this.textbox.Text += "/";
                json arr = new json(Post(this.textbox.Text, interface_name));
                if (arr.GetType() == "array") for (int i = 0; i < arr.length; i++) this.list.Items.Add((string)arr[i]);
            }
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.np.Text)) MessageBox.Show("命名空间不能为空");
            else
            {
                if (this.list.Items.Count > 0)
                {
                    string dir = this._path + this._name + "\\";
                    Directory.CreateDirectory(dir);
                    if (this.textbox.Text.Substring(0, 4) != "http") this.textbox.Text = "http://" + this.textbox.Text;
                    if (this.textbox.Text.Last() != '/') this.textbox.Text += "/";
                    CreateInterface(dir, this.textbox.Text, this.np.Text, this.list.SelectedItems);
                    this._Package.AddFromDirectory(dir);
                }
                this.Close();
            }
        }
        internal static void CreateInterface(string Directory, string Domain, string Namespace, IList List, Func<string, string> WriteFunc = null)
        {
            json arr = new json(Post(Domain, EncryptString(new json(new { Name = "interface_info", List = List }).ToString(), Interface_Data_Key)));
            if (arr.GetType() == "array")
            {
                for (int i = 0; i < arr.length; i++)
                {
                    _ClassInfo info = (_ClassInfo)arr[i];
                    string sNamespace = info.Namespace;
                    string ClassName = info.ClassName;
                    string FileName = ClassName + ".cs";
                    IList<_MethodInfo> Methods = info.Methods;
                    if (WriteFunc != null) FileName = WriteFunc(FileName);
                    using (FileStream fs = File.Open(Directory + FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("//Domain");
                        sw.WriteLine("//" + Domain);
                        sw.WriteLine("//Namespace");
                        sw.WriteLine("//" + Namespace);
                        sw.WriteLine("//ClassName");
                        sw.WriteLine("//" + sNamespace + "." + ClassName);
                        sw.WriteLine("//请勿修改上述接口信息");
                        sw.WriteLine("namespace " + Namespace);
                        sw.WriteLine("{");
                        sw.WriteLine("    public class " + ClassName + " : wnxd.Web.InterfaceBase");
                        sw.WriteLine("    {");
                        sw.WriteLine("        public " + ClassName + "()");
                        sw.WriteLine("        {");
                        sw.WriteLine("            this.Domain = \"" + Domain + "\";");
                        sw.WriteLine("            this.Namespace = \"" + sNamespace + "\";");
                        sw.WriteLine("            this.ClassName = \"" + ClassName + "\";");
                        sw.WriteLine("        }");
                        for (int n = 0; n < Methods.Count; n++)
                        {
                            _MethodInfo MethodInfo = Methods[n];
                            int MethodToken = MethodInfo.MethodToken;
                            string MethodName = MethodInfo.MethodName;
                            string ReturnType = MethodInfo.ReturnType;
                            IList<_ParameterInfo> Parameters = MethodInfo.Parameters;
                            string summary = MethodInfo.Summary;
                            if (!string.IsNullOrEmpty(summary))
                            {
                                sw.WriteLine("        /// <summary>");
                                sw.WriteLine("        /// " + summary);
                                sw.WriteLine("        /// </summary>");
                            }
                            bool isvoid = false;
                            if (ReturnType == "System.Void") isvoid = true;
                            string args = string.Empty;
                            sw.Write("        public " + (isvoid ? "void" : ReturnType) + " " + MethodName + "(");
                            for (int x = 0; x < Parameters.Count; x++)
                            {
                                _ParameterInfo ParameterInfo = Parameters[x];
                                string ParameterName = ParameterInfo.ParameterName;
                                _ParameterType Type = ParameterInfo.Type;
                                bool IsOptional = ParameterInfo.IsOptional;
                                string ParameterType = ParameterInfo.ParameterType;
                                if (x > 0) sw.Write(", ");
                                switch (Type)
                                {
                                    case _ParameterType.In:
                                        break;
                                    case _ParameterType.Out:
                                        sw.Write("out ");
                                        break;
                                    case _ParameterType.Retval:
                                        sw.Write("ref ");
                                        break;
                                }
                                sw.Write(ParameterType + " " + ParameterName);
                                args += ", " + ParameterName;
                            }
                            sw.WriteLine(")");
                            sw.WriteLine("        {");
                            sw.Write("            wnxd.javascript.json r = this.Run(");
                            if (MethodToken == 0) sw.Write("\"" + MethodName + "\"");
                            else sw.Write(MethodToken);
                            sw.WriteLine(args + ");");
                            if (!isvoid)
                            {
                                if (ReturnType == "wnxd.javascript.json") sw.WriteLine("            return r;");
                                else sw.WriteLine("            return (" + ReturnType + ")r.TryConvert(typeof(" + ReturnType + "));");
                            }
                            sw.WriteLine("        }");
                        }
                        sw.WriteLine("    }");
                        sw.Write("}");
                        sw.Flush();
                    }
                }
            }
        }
        private static string Post(string url, string data)
        {
            try
            {
                WebRequest request = WebRequest.Create(url + "wnxd.aspx?wnxd_interface=" + query);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                request.ContentLength = bytes.Length;
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(bytes, 0, bytes.Length);
                    dataStream.Flush();
                }
                using (WebResponse response = request.GetResponse())
                using (Stream dataStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    string responseData = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(responseData)) responseData = DecryptString(responseData, Interface_Data_Key);
                    return responseData;
                }
            }
            catch
            {
                return null;
            }
        }
        private static string EncryptString(string sInputString, string sKey)
        {
            byte[] data = Encoding.UTF8.GetBytes(sInputString);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            DES.Key = ASCIIEncoding.ASCII.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            DES.IV = ASCIIEncoding.ASCII.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            ICryptoTransform desencrypt = DES.CreateEncryptor();
            byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
            return BitConverter.ToString(result);
        }
        private static string DecryptString(string sInputString, string sKey)
        {
            string[] sInput = sInputString.Split('-');
            byte[] data = new byte[sInput.Length];
            for (int i = 0; i < sInput.Length; i++) data[i] = byte.Parse(sInput[i], NumberStyles.HexNumber);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            DES.Key = ASCIIEncoding.ASCII.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            DES.IV = ASCIIEncoding.ASCII.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(sKey, "md5").Substring(0, 8));
            ICryptoTransform desencrypt = DES.CreateDecryptor();
            byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(result);
        }
    }
    class _ClassInfo
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public IList<_MethodInfo> Methods { get; set; }
    }
    class _MethodInfo
    {
        public int MethodToken { get; set; }
        public string MethodName { get; set; }
        public string ReturnType { get; set; }
        public IList<_ParameterInfo> Parameters { get; set; }
        public string Summary { get; set; }
    }
    class _ParameterInfo
    {
        public string ParameterName { get; set; }
        public _ParameterType Type { get; set; }
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; }
        public string ParameterType { get; set; }
    }
    enum _ParameterType
    {
        In,
        Out,
        Retval
    }
}

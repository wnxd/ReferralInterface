using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using wnxd.javascript;

namespace wnxd.ReferralInterface
{
    /// <summary>
    /// Add.xaml 的交互逻辑
    /// </summary>
    public partial class Add : Window
    {
        private static string query = HttpUtility.UrlEncode(EncryptString("$query$", "wnxd: interface_name"));
        private static string interface_name = EncryptString("{\"Name\":\"interface_name\"}", "wnxd: interface_data");
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
                    json arr = new json(Post(this.textbox.Text, EncryptString(new json(new { Name = "interface_info", List = this.list.SelectedItems }).ToString(), "wnxd: interface_data")));
                    if (arr.GetType() == "array")
                    {
                        for (int i = 0; i < arr.length; i++)
                        {
                            json info = arr[i];
                            string Namespace = info["Namespace"];
                            string ClassName = info["ClassName"];
                            json Methods = info["Methods"];
                            using (FileStream fs = File.OpenWrite(dir + ClassName + ".cs"))
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.WriteLine("namespace " + this.np.Text);
                                sw.WriteLine("{");
                                sw.WriteLine("    public class " + ClassName + " : wnxd.Web.InterfaceBase");
                                sw.WriteLine("    {");
                                sw.WriteLine("        public " + ClassName + "()");
                                sw.WriteLine("        {");
                                sw.WriteLine("            this.Domain = \"" + this.textbox.Text + "\";");
                                sw.WriteLine("            this.Namespace = \"" + Namespace + "\";");
                                sw.WriteLine("            this.ClassName = \"" + ClassName + "\";");
                                sw.WriteLine("        }");
                                for (int n = 0; n < Methods.length; n++)
                                {
                                    json MethodInfo = Methods[n];
                                    string MethodName = MethodInfo["MethodName"];
                                    string ReturnType = MethodInfo["ReturnType"];
                                    json Parameters = MethodInfo["Parameters"];
                                    string summary = MethodInfo["summary"];
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
                                    for (int x = 0; x < Parameters.length; x++)
                                    {
                                        json ParameterInfo = Parameters[x];
                                        string ParameterName = ParameterInfo["ParameterName"];
                                        int Type = ParameterInfo["Type"];
                                        bool IsOptional = ParameterInfo["IsOptional"];
                                        string ParameterType = ParameterInfo["ParameterType"];
                                        if (x > 0) sw.Write(", ");
                                        if (Type == 2) sw.Write("ref ");
                                        else if (Type == 1) sw.Write("out ");
                                        sw.Write(ParameterType + " " + ParameterName);
                                        args += ", " + ParameterName;
                                    }
                                    sw.WriteLine(")");
                                    sw.WriteLine("        {");
                                    sw.WriteLine("            wnxd.javascript.json r = this.Run(\"" + MethodName + "\"" + args + ");");
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
                    this._Package.AddFromDirectory(dir);
                }
                this.Close();
            }
        }
        private static string Post(string url, string data)
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
                if (!string.IsNullOrEmpty(responseData)) responseData = DecryptString(responseData, "wnxd: interface_data");
                return responseData;
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
}

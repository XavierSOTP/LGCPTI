using System.Diagnostics;
using System.Xml;

namespace LGC.PTI.App.Plugin.Server;

internal static class SdkCertificateHelpers
{
    public const string CertificateExtension = ".cert";
    public const string CertificateSubFolder = "certificates\\";
    private const string AlternateCertificateSubFolder = "clientcertificates\\";
    public static string GetApplicationId(string certificateFilename)
    {
        if (!System.IO.File.Exists(certificateFilename))
        {
            return null;
        }
        XmlDocument xmlDocument = new XmlDocument();
        try
        {
            xmlDocument.Load(certificateFilename);
            XmlNode xmlNode = xmlDocument.SelectSingleNode("/ClientCertificate/ApplicationId");
            if (xmlNode == null)
            {
                return string.Empty;
            }
            return xmlNode.InnerText;
        }
        catch (XmlException)
        {
            return string.Empty;
        }
    }
    public static string GetCertificatePath(Type type, bool alternate = false)
    {
        string subFolder = "certificates\\";
        if (alternate)
        {
            subFolder = "clientcertificates\\";
        }
        return GetBaseCertificatePath(type, subFolder);
    }
    public static string GetBaseCertificatePath(Type type, string subFolder)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }
        string path = string.Format("{0}.{1}{2}", type.Namespace, type.Name, ".cert");
        string path2 = Path.GetDirectoryName(GetFilePath(type.Assembly));
        if (!string.IsNullOrEmpty(subFolder))
        {
            path2 = Path.Combine(path2, subFolder);
        }
        return Path.Combine(path2, path);
    }
    public static string GetCertificatePath(Assembly callingAssembly)
    {
        string path = string.Format("{0}{1}", callingAssembly.FullName, ".cert");
        return Path.Combine(Path.Combine(GetFilePath(callingAssembly), "certificates\\"), path);
    }
    public static string GetCertificatePath()
    {
        FileInfo fileInfo = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
        string text = fileInfo.Name;
        if (Debugger.IsAttached && text.Contains(".vshost.exe"))
        {
            text = text.Replace(".vshost.exe", ".exe");
        }
        return Path.Combine(path2: string.Format("{0}{1}", text, ".cert"), path1: Path.Combine(fileInfo.DirectoryName, "certificates\\"));
    }


    public static string GetFilePath(Assembly assembly)
    {
        return Path.GetDirectoryName(assembly.Location);
    }
}

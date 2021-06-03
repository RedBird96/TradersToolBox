using System;
using System.Collections.Generic;
using System.Management;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Linq.Expressions;

static class Security
{
    public static string HardwareID {
        get
        {
            string s_cpu = string.Empty, s_motherboard = string.Empty;
            try { s_cpu = GetProcessorId(); } catch { }
            try { s_motherboard = GetMotherboardId(); }
            catch {
                if (string.IsNullOrEmpty(s_cpu))
                    return "Can't access hardware information!";
                else
                    s_cpu = "CPU_VIRTUAL";
            }
            return GetHash(s_motherboard + "$" + s_cpu);
        }
    }
    public static string ActivationCode {
        get
        {
            return ConstructCode(HardwareID);
        }
    }
    public static string GetMotherboardId()
    {
        string uuid = string.Empty;
        ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
        ManagementObjectCollection moc = mc.GetInstances();
        foreach (ManagementObject mo in moc)
        {
            uuid = mo.Properties["UUID"].Value.ToString();
            break;
        }
        return uuid;
    }
    public static string GetProcessorId()
    {
        ManagementClass mc = new ManagementClass("win32_processor");
        ManagementObjectCollection moc = mc.GetInstances();
        string Id = string.Empty;
        foreach (ManagementObject mo in moc)
        {
            Id = mo.Properties["processorID"].Value.ToString();
            break;
        }
        return Id;
    }
    public static string GetHash(string s)
    {
        //Initialize a new MD5 Crypto Service Provider in order to generate a hash
        MD5 sec = new MD5CryptoServiceProvider();
        //Grab the bytes of the variable 's'
        byte[] bt = Encoding.ASCII.GetBytes(s);
        //Grab the Hexadecimal value of the MD5 hash
        return GetHexString(sec.ComputeHash(bt));
    }
    public static string GetHexString(IList<byte> bt)
    {
        string s = string.Empty;
        for (int i = 0; i < bt.Count; i++)
        {
            byte b = bt[i];
            int n = b;
            int n1 = n & 15;
            int n2 = (n >> 4) & 15;
            if (n2 > 9)
                s += ((char)(n2 - 10 + 'A')).ToString(CultureInfo.InvariantCulture);
            else
                s += n2.ToString(CultureInfo.InvariantCulture);
            if (n1 > 9)
                s += ((char)(n1 - 10 + 'A')).ToString(CultureInfo.InvariantCulture);
            else
                s += n1.ToString(CultureInfo.InvariantCulture);
            if ((i + 1) != bt.Count && (i + 1) % 2 == 0) s += "-";
        }
        return s;
    }
    public static string ConstructCode(string hash)
    {
        StringBuilder s = new StringBuilder();
        string[] a = hash.Split('-');
        for (int i = 0, j = 1; i < 8; i++, j = (j + i) % 8)
            s.Append(a[j]);
        return GetHash(s.ToString()).Remove(29);
    }
    public static ulong ToUnixTime(DateTime date)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return Convert.ToUInt64((date.ToUniversalTime() - epoch).TotalSeconds);
    }
    public static ulong ToUnixTime(int date, int time)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dt = new DateTime(date / 10000, (date % 10000) / 100, date % 100, time / 10000, (time % 10000) / 100, time % 100, DateTimeKind.Utc);
        return Convert.ToUInt64((dt - epoch).TotalSeconds);
    }
    public static DateTime ToDateTime(long unixTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return epoch.AddSeconds(unixTime);
    }
    public static DateTime ToDateTime(int date, int time)
    {
        return new DateTime(date / 10000, (date % 10000) / 100, date % 100, time / 10000, (time % 10000) / 100, time % 100, DateTimeKind.Utc);
    }
    public static int TTdateToDecimal(ushort TTdate)
    {
        return ((TTdate >> 9) + 1970) * 10000 + (((TTdate & 0x1E0) >> 5) + 1) * 100 + (TTdate & 0x1F);
    }
    public static ushort DecimalToTTdate(int date)
    {
        return (ushort)(((date / 10000 - 1970) << 9) + (((date % 10000) / 100 - 1) << 5) + date % 100);
    }

    /*public static string tTest(long s)
    {
        int u1 = 35581;
        int u2 = 87397;

        string p1 = (u1 + s / 86400).ToString();
        string p2 = (u2 + int.Parse(ToDateTime(s).ToString("ddMMyyyy"))).ToString();
        string p = p1 + "TT" + p2;

        MD5 sec = new MD5CryptoServiceProvider();
        byte[] bt = Encoding.ASCII.GetBytes(p);
        string res = "39" + GetHexString(sec.ComputeHash(bt)).Replace("-", "") + "46";

        return res;
    }*/

    /// <summary>
    /// Get class's field or property name at runtime
    /// Usage:
    /// var fieldName = GetMemberName((MyClass c) => c.Field);
    /// var propertyName = GetMemberName((MyClass c) => c.Property);
    /// </summary>
    public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
    {
        return ((MemberExpression)memberAccess.Body).Member.Name;
    }
}
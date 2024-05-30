using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Management;

namespace dryice_programming {
    class Program {
        private static Process proc = new Process();
        private static bool debug = false;
        private static System.Threading.Timer close_program;
        static void Main(string[] args) {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            string head = "1";
            while (true)
            {
                try
                { head = File.ReadAllText("../../config/head.txt"); break; } catch { Thread.Sleep(50); }
            }
            File.Delete("../../config/head.txt");
            File.WriteAllText("call_exe_tric.txt", "");
            File.WriteAllText("dryice_programming_running_" + head + ".txt", "");
            string hex_file = "TTULTRA_MU_16K_64K_V710_20200319.hex";
            string stlink = "36FF6C064B46323646270643";
            int timeout = 20000;
            string checksum = "0x01742FC9";
            try
            { hex_file = File.ReadAllText("../../config/dryice_program_hex.txt"); } catch { }
            try
            { stlink = File.ReadAllText("../../config/dryice_program_stlink.txt"); } catch { }
            try
            { timeout = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_timeout.txt")); } catch { }
            try
            { debug = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt")); } catch { }
            try
            { checksum = File.ReadAllText("../../config/dryice_program_checksum.txt"); } catch { }
            close_program = new System.Threading.Timer(TimerCallback, null, 0, timeout + 10000);
            File.WriteAllText("dryice_program" + head + ".bat",
                              "\"C:\\Program Files (x86)\\STMicroelectronics\\STM32 ST-LINK Utility\\ST-LINK Utility\\ST-" +
                              "LINK_CLI.exe\" -c \"SN\"=\"" + stlink + "\" SWD UR -P \"D:\\svn\\2020_SST_Ultra_Dry_Ice_Automation\\1.Customer Documents\\7.Firmware\\" + hex_file + "\" " +
                              "-V -Cksum \"D:\\svn\\2020_SST_Ultra_Dry_Ice_Automation\\1.Customer Documents\\7.Firmware\\" + hex_file + "\" -Rst -HardRst > dryice_programming_" + head + ".log");
            //File.WriteAllText("dryice_program" + head + ".bat",//สลับไปมา
            //                  "\"C:\\Program Files (x86)\\STMicroelectronics\\STM32 ST-LINK Utility\\ST-LINK Utility\\ST-" +
            //                  "LINK_CLI.exe\" -c \"SN\"=\"" + stlink + "\" SWD UR -P \"D:\\svn\\2019_Sensitech_TT_Ultra_Dry_Ice\\1.Customer Documents\\7.Firmware\\" + hex_file + "\" " +
            //                  "-V -Cksum \"D:\\svn\\2019_Sensitech_TT_Ultra_Dry_Ice\\1.Customer Documents\\7.Firmware\\" + hex_file + "\" -Rst > dryice_programming_" + head + ".log");
            Console.WriteLine("head = " + head);
            Console.WriteLine("st_link = " + stlink);
            Console.WriteLine("hex_file = " + hex_file);
            Console.WriteLine("checksum = " + checksum);
            if (debug == true)
                Console.ReadKey();

            Stopwatch timeout_ = new Stopwatch();
            proc.StartInfo.WorkingDirectory = "";
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.FileName = "dryice_program" + head + ".bat";
            bool flag_e2 = false;
            bool flag_checksum = true;
            string checksum_sup = "";
            string data = "";
            for (int i = 1; i <= 4; i++)
            {
                File.Delete("dryice_programming_" + head + ".log");
                while (true)
                {
                    try
                    { proc.Start(); break; } catch { Thread.Sleep(50); }
                }
                Console.Write("wait log file");
                data = "";
                timeout_.Restart();
                while (timeout_.ElapsedMilliseconds < timeout)
                {
                    try
                    {
                        data = File.ReadAllText("dryice_programming_" + head + ".log");
                    } catch (Exception)
                    {
                        Console.Write(".");
                        Thread.Sleep(50);
                        continue;
                    }
                    timeout_.Stop();
                    break;
                }
                if (timeout_.IsRunning)
                { File.WriteAllText("test_head_" + head + "_result.txt", "timeout\r\nFAIL"); return; }
                bool flag_e2lite = true;
                if (!data.Contains("Verification...OK"))
                { flag_e2lite = false; Console.WriteLine(""); Console.WriteLine("not Verification...OK"); wait_discom(head); continue; }
                else
                { Console.WriteLine(""); Console.WriteLine("Verification...OK"); }
                if (!data.Contains("Programming Complete."))
                { flag_e2lite = false; Console.WriteLine("not Programming Complete."); wait_discom(head); continue; }
                else
                    Console.WriteLine("Programming Complete.");
                if (!data.Contains(checksum))
                {
                    flag_e2lite = false;
                    Console.WriteLine("checksum not equal");
                    string[] ss = data.Replace("Checksum:", "$").Split('$');
                    string[] vv = ss[1].Split('[');
                    checksum_sup = vv[0].Trim().Replace("\r", "").Replace("\n", "");
                    Console.WriteLine("checksum = " + checksum_sup);
                    flag_checksum = false;
                }
                else
                    Console.WriteLine("checksum equal");
                Console.WriteLine(hex_file);
                if (debug == true)
                    Console.ReadKey();
                if (flag_e2lite == true)
                { flag_e2 = true; break; }
            }
            File.Delete("dryice_program" + head + ".bat");
            if (flag_e2 == true)
                File.WriteAllText("test_head_" + head + "_result.txt", hex_file + "\r\nPASS");
            else
            {
                data = data.Replace("'", "").Replace(",", "").Replace("\n", "").Replace("\r", "");
                if (flag_checksum)
                    File.WriteAllText("test_head_" + head + "_result.txt", data + "\r\nFAIL");
                else
                    File.WriteAllText("test_head_" + head + "_result.txt", checksum_sup + "\r\nFAIL");
            }
            File.Delete("dryice_programming_running_" + head + ".txt");
            File.Delete("dryice_programming_discom_" + head + ".txt");
        }

        private static bool flag_close = false;
        private static void TimerCallback(Object o) {
            if (!flag_close)
            { flag_close = true; return; }
            if (debug)
                return;
            if (flag_close)
                Environment.Exit(0);
        }
        private static void wait_discom(string head) {
            return;//สลับไปมา
            File.WriteAllText("dryice_programming_discom_" + head + ".txt", "");
            List<string> head_all = new List<string>();
            for (int kj = 1; kj <= 4; kj++)
            {
                head_all.Add(kj.ToString());
            }
            head_all.Remove(head);
            int minminmin = 10;
            while (true)
            {
                bool flag_running = false;
                bool flag_discom = false;
                bool flag_discom_run = true;
                foreach (string xc in head_all)
                {
                    try
                    {
                        File.ReadAllText("dryice_programming_running_" + xc + ".txt");
                        flag_running = true;
                    } catch { }
                    try
                    {
                        File.ReadAllText("dryice_programming_discom_" + xc + ".txt");
                        if (minminmin > Convert.ToInt32(xc))
                            minminmin = Convert.ToInt32(xc);
                        flag_discom = true;
                    } catch { }
                    if (flag_running && !flag_discom)
                        flag_discom_run = false;
                }
                if (flag_discom_run)
                    break;
                Thread.Sleep(50);
            }
            if (Convert.ToInt32(head) < minminmin)
            {
                get_name_st_link();
                discom("disable");
                discom("enable");
            }
            else
                Thread.Sleep(2500);
        }

        private static void get_name_st_link() {
            ManagementObjectSearcher objOSDetails2 =
               new ManagementObjectSearcher(@"SELECT * FROM Win32_PnPEntity where DeviceID Like ""USB%""");
            ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
            foreach (ManagementObject usblist in osDetailsCollection2)
            {
                string arrport = usblist.GetPropertyValue("NAME").ToString();
                if (arrport.Contains("STM"))
                {
                    name_st = arrport;
                }
            }
        }
        private static string name_st = "STMicroelectronics STLink dongle";
        private static void discom(string cmd) {//enable disable//
            Process devManViewProc = new Process();
            devManViewProc.StartInfo.FileName = "DevManView.exe";
            devManViewProc.StartInfo.Arguments = "/" + cmd + " \"" + name_st + "\"";
            devManViewProc.Start();
            devManViewProc.WaitForExit();
        }

        /// <summary>
        /// Event Exception Catch Program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MyHandler(object sender, UnhandledExceptionEventArgs args) {
            Exception e = (Exception)args.ExceptionObject;
            LogProgramCatch(e.StackTrace);
        }

        /// <summary>
        /// Log program catch to csv file
        /// </summary>
        /// <param name="text"></param>
        private static void LogProgramCatch(string text) {
            string path = "D:\\LogError\\DryIceProgramCatch";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DateTime now = DateTime.Now;
            StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
            string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
            swOut.WriteLine(time + "," + text);
            swOut.Close();
        }
    }
}

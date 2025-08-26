using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DGUpdaterTest
{
    public partial class DGUpdaterTest : Form
    {
        public string hashlink = "http://5.42.223.21/DarkGame207_UPDT/hashes(2.1.1).json";
        const string batchname = "Update-Replacer.bat";
        public bool checkupdater;
        public bool downloadsfinished;
        public int finishedfiles;
        public string updatelinkzip;
        public List<string> missing = new List<string>();
        public List<string> needupdate = new List<string>();
        static Dictionary<string, string> linkfileplaces = new Dictionary<string, string>();

        

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     
            int nTopRect,     
            int nRightRect,   
            int nBottomRect,  
            int nWidthEllipse,
            int nHeightEllipse
        );

        public DGUpdaterTest()
        {
            if (!IsUserAdministrator())
            {
                MessageBox.Show("This application requires administrator privileges to run.", "Insufficient Privileges", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Process.GetCurrentProcess().Kill();
            }
            try
            {
                File.Delete(batchname);
            }
            catch { }
            InitializeComponent();
            GetLHashes();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        public static async Task<T> FetchJsonDataFromUrl<T>(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<T>(jsonResponse);
                    return data;
                }
                catch
                {

                    return default;
                }
            }
        }
        Dictionary<string, SubData> lhashes = new Dictionary<string, SubData>();
        Dictionary<string, SubData> dhashes = new Dictionary<string, SubData>();

        async Task GetdHashesAsync()
        {
            mobinsview.Text = "Geting Fetched!";
            var jsonData = await FetchJsonFromUrl(hashlink);
            dhashes = jsonData;
            mobinsview.Text = "Fetching finished!";
            CompareFiles();
        }
        void GetLHashes()
        {
            mobinsview.Text = "Hashing Our Files :)";
            var fileHashes = GetFilesWithHashes(AppDomain.CurrentDomain.BaseDirectory, "MD5");
            lhashes = fileHashes;
            mobinsview.Text = "Finished Hashing!";
            Task task = GetdHashesAsync();
        }
        bool changesFound = false;
        async void CompareFiles()
        {
            mobinsview.Text = "Comparing Data!";
        
            
        
            foreach (var kvp in dhashes)
            {
                if (!lhashes.ContainsKey(kvp.Key))
                {
                    missing.Add(kvp.Key);
                    changesFound = true;
                }
                else if (lhashes[kvp.Key].hash != kvp.Value.hash)
                {
                    missing.Add(kvp.Key);
                    changesFound = true;
                }
            }
            if (!changesFound)
            {
                mobinsview.Text = "Up To Date! Closing in 2 sec";
                await Task.Delay(2000);
                Application.Exit();
            }
            else
            {
                mobinsview.Text = "Updating.....";
                DownloadAndUpdate();
            }
        }
        
        class SubData
        {
            public string hash;
            public List<string> path;
        }
        static Dictionary<string, SubData> GetFilesWithHashes(string directory, string hashAlgorithm)
        {
            var fileHashes = new Dictionary<string, SubData>();
        
            try
            {
                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string directoryName = Path.GetDirectoryName(file);
                    if (file.Contains(".bundle") ||
                        file.Contains(".map") ||
                        file.Contains("Bundle") ||
                        file.Contains("UnityEngine") ||
                        file.Contains(".json") ||
                        file.Contains(".png") ||
                        file.Contains(".bmp") ||
                        file.Contains(".xml") ||
                        file.Contains(".ini") ||
                        file.Contains(".config") ||
                        file.Contains(".txt") ||
                        file.Contains(".browser") ||
                        file.Contains(".aspx") ||
                        file.Contains(".rar"))
                    {
                        continue;
                    }

                    try
                    {
                        string hash = ComputeFileHash(file, hashAlgorithm);
                        string currentDir = Directory.GetCurrentDirectory();
                        string[] fileName = GetRelativePath(currentDir, Path.GetDirectoryName(file)).Split('\\');
                        List<string> file12 = fileName.ToList();
                        file12.RemoveAll(item => item.Contains("."));
                        file12.RemoveAll(item => item.Contains(".."));
                        SubData subData = new SubData();
                        subData.hash = hash;
                        subData.path = file12;
                        fileHashes.Add(file.Split('\\').Last(), subData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error hashing file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory: {ex.Message}");
            }

            return fileHashes;
        }
        static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.TrimEnd('\\') + "\\");
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()).Replace('/', '\\');
        }
        static string ComputeFileHash(string filePath, string hashAlgorithm)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (HashAlgorithm algorithm = GetHashAlgorithm(hashAlgorithm))
                {
                    byte[] hashBytes = algorithm.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static HashAlgorithm GetHashAlgorithm(string hashAlgorithm)
        {
            switch (hashAlgorithm.ToUpperInvariant())
            {
                case "MD5": return MD5.Create();
                case "SHA1": return SHA1.Create();
                case "SHA256": return SHA256.Create();
                case "SHA512": return SHA512.Create();
                default: throw new ArgumentException("Unsupported hash algorithm");
            }
        }

        static async Task<Dictionary<string, SubData>> FetchJsonFromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string jsonString = await response.Content.ReadAsStringAsync();

                    var dict = JsonConvert.DeserializeObject<Dictionary<string, SubData>>(jsonString);

                    return dict ?? new Dictionary<string, SubData>();
                }
                catch (Exception)
                {
                    return new Dictionary<string, SubData>();
                }
            }
        }

        async Task StartBatUpdateAsync()
        {
            //await Task.Delay(3000);
            File.WriteAllText(batchname, $@"
@echo off
timeout /t 2 /nobreak >nul
del ""DGUpdater.exe""
rename ""DGUpdater1.exe"" ""DGUpdater.exe""
start """" ""DGUpdater.exe""
del ""{batchname}""
exit
");
            await Task.Delay(3000);
            Process.Start(new ProcessStartInfo
            {
                FileName = batchname,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
           
            Environment.Exit(0);
        }

        static bool IsUserAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        async Task DownloadAndUpdate()
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName == "DGLauncher" || process.ProcessName == "Rust207")
                {
                    process.Kill();
                }
            }

            using (HttpClient client = new HttpClient())
            {
                foreach (var missingfile in missing)
                {
                    string missingfilen = missingfile;
                    string filePath = Path.Combine(dhashes[missingfile].path.ToArray());

                    if (File.Exists(filePath) && !missingfile.Contains("DGUpdater.exe"))
                    {
                        File.Delete(filePath);
                    }
                    else if (missingfile.Contains("DGUpdater.exe"))
                    {
                        missingfilen = Path.Combine(Directory.GetCurrentDirectory(), "DGUpdater1.exe");
                        checkupdater = true;
                    }

                    string downloadUrl = $"http://5.42.223.21/DarkGame207_UPDT/{filePath}";

                    mobinsview.Text = $"Downloading {missingfile}...";

                    try
                    {
                        using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                            var canReportProgress = totalBytes != -1;

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                var buffer = new byte[81920]; 
                                long totalRead = 0;
                                int read;

                                progressBar1.Minimum = 0;
                                progressBar1.Maximum = 100;
                                progressBar1.Value = 0;

                                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fs.WriteAsync(buffer, 0, read);
                                    totalRead += read;

                                    if (canReportProgress)
                                    {
                                        int percent = (int)((totalRead * 100L) / totalBytes);
                                        progressBar1.Value = percent;

                                        mobinsview.Text = $"[{missingfile}] {percent}% " +
                                                          $"{Math.Round(totalBytes / 1024f / 1024f, 2)}MB";
                                    }
                                }

                            }
                        }

                        finishedfiles++;
                        mobinsview.Text = $"Downloaded {finishedfiles}/{missing.Count}";
                    }
                    catch (Exception ex)
                    {
                        mobinsview.Text = $"Error downloading {missingfile}: {ex.Message}";
                    }
                }
            }

            if (checkupdater)
            {
                Wait();
            }
            else
            {
                if(changesFound && File.Exists("DGLauncher.exe"))
                {
                    Process.Start("DGLauncher.exe");
                }
                Application.Exit();
            }
        }

        void Wait()
        {
            mobinsview.Text = "Waiting Updater To Updated";
            StartBatUpdateAsync();
        }
        
    }
}
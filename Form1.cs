using QRCoder;
using System.Diagnostics;
using System.IO.Compression;
using Emgu.CV;
using ZXing.Windows.Compatibility;

namespace YTQRStorage
{
    public partial class Form1 : Form
    {

        string filePath;
        string outputdir;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Form1()
        {
            InitializeComponent();
        }

        static void SplitFile(string filePath, int chunkSize, string outputdir)
        {
            byte[] buffer = new byte[chunkSize];

            using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead;
                int chunkNumber = 1;

                while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string chunkFileName = $"chunk_{chunkNumber}.bin";
                    string chunkFilePath = Path.Combine(outputdir, chunkFileName);

                    using (FileStream chunkFileStream = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write))
                    {
                        chunkFileStream.Write(buffer, 0, bytesRead);
                    }

                    chunkNumber++;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    textBox1.Text = filePath;
                }
            }
        }
        private async void button2_Click(object sender, EventArgs e)
        {
            int img = 0;
            SplitFile(filePath, 2953, outputdir);
            string[] files = Directory.GetFiles(outputdir, "chunk_*.bin");
            int maxDegreeOfParallelism = Environment.ProcessorCount - 1;
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            // Use Parallel.ForEach to process files in parallel
            Parallel.ForEach(files, options, (file) =>
            {
                byte[] buffer = File.ReadAllBytes(file);
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(buffer, QRCodeGenerator.ECCLevel.L);
                QRCode qrCode = new QRCode(qrCodeData);

                using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                {
                    // Use Interlocked to safely increment img in a multithreaded environment
                    int currentIndex = Interlocked.Increment(ref img);
                    qrCodeImage.Save(Path.Combine(outputdir, $"qr_{currentIndex}.png"));
                }
            });
            HttpClient client = new();
            string rsp = await client.GetStringAsync("https://www.gyan.dev/ffmpeg/builds/release-version");
            string pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
            string filepth = Path.Combine(pth, $"ffmpeg-{rsp}-full_build");
            string filepth2 = Path.Combine(filepth, "bin");
            string ffmpeg = Path.Combine(filepth2, "ffmpeg.exe");
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine($"{ffmpeg} -framerate 60 -start_number 1 -i {outputdir}\\qr_%d.png -c:v libx264 -pix_fmt yuv420p -fps_mode passthrough {outputdir}\\output.mp4");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            DirectoryInfo di = new DirectoryInfo(outputdir);
            foreach (FileInfo file in di.GetFiles("chunk_*.bin"))
            {
                file.Delete();
            }
            foreach (FileInfo file in di.GetFiles("qr_*.png"))
            {
                file.Delete();
            }
            label1.Visible = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox2.Text = fbd.SelectedPath;
                    outputdir = fbd.SelectedPath;
                    string[] files = Directory.GetFiles(outputdir, "chunk_*.bin");
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                    Debug.WriteLine(outputdir);
                }
            }
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            HttpClient client = new HttpClient();
            string rsp = await client.GetStringAsync("https://www.gyan.dev/ffmpeg/builds/release-version");
            string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
            string ffmpegdir = Path.Combine(appdata, $"ffmpeg-{rsp}-full_build");
            string ffmpegbin1 = Path.Combine(ffmpegdir, "bin");
            string ffmpegbin = Path.Combine(ffmpegbin1, "ffmpeg.exe");
            if (!Directory.Exists(appdata) || !Directory.Exists(ffmpegdir) || !File.Exists(ffmpegbin))
            {
                DirectoryInfo di = new DirectoryInfo(appdata);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
                label2.Text = "FFMPEG Not installed!";
                button4.Enabled = true;
            }
            else
            {
                label2.Text = "FFMPEG Installed!";
                button4.Enabled = false;
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            HttpClient client = new HttpClient();
            string rsp = await client.GetStringAsync("https://www.gyan.dev/ffmpeg/builds/release-version");
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS"));
                string pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
                string url = $"https://github.com/GyanD/codexffmpeg/releases/download/{rsp}/ffmpeg-{rsp}-full_build.zip";
                string filepth = Path.Combine(pth, $"ffmpeg-{rsp}-full_build.zip");
                Uri uri = new Uri(url);
                Stream filestream = await client.GetStreamAsync(uri);
                using (FileStream outputFileStream = new FileStream(filepth, FileMode.Create))
                {
                    await filestream.CopyToAsync(outputFileStream);
                }
                ZipFile.ExtractToDirectory(filepth, pth, true);
                File.Delete(filepth);
                label2.Text = "FFMPEG Installed.";
            }
            else
            {
                string pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
                string url = $"https://github.com/GyanD/codexffmpeg/releases/download/{rsp}/ffmpeg-{rsp}-full_build.zip";
                string filepth = Path.Combine(pth, $"ffmpeg-{rsp}-full_build.zip");
                Uri uri = new Uri(url);
                Stream filestream = await client.GetStreamAsync(uri);
                using (FileStream outputFileStream = new FileStream(filepth, FileMode.Create))
                {
                    await filestream.CopyToAsync(outputFileStream);
                }
                ZipFile.ExtractToDirectory(filepth, pth, true);
                label2.Text = "FFMPEG Installed.";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    textBox3.Text = filePath;
                }
            }
        }

        static byte[] ReadQRCode(string imagePath)
        {
            var barcodeReader = new BarcodeReader();
            try
            {
                using (FileStream stream = new FileStream(imagePath, FileMode.Open))
                {
                    using (Bitmap bitmap = new Bitmap(stream))
                    {
                        var result = barcodeReader.Decode(bitmap);

                        if (result != null)
                        {
                            return result.RawBytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading QR code: {ex.Message}");
            }

            return Array.Empty<byte>();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int i = 1;
            string file = textBox3.Text;
            FileInfo fi = new FileInfo(file);
            using (var video = new VideoCapture(file))
            {
                using (var img = new Mat())
                {
                    while (video.Grab())
                    {
                        video.Retrieve(img);
#pragma warning disable CS8604 // Possible null reference argument.
                        var filename = Path.Combine(fi.DirectoryName, $"qr_{i}.png");
                        CvInvoke.Imwrite(filename, img);
                        i++;
                    }
                }
            }
            i = 1;
            DirectoryInfo di = new DirectoryInfo(fi.DirectoryName);
            foreach (FileInfo filei in di.GetFiles("qr_*.png"))
            {
                FileStream fs = File.Create(Path.Combine(di.FullName, $"chunk_{i}.bin"));
                byte[] values = ReadQRCode(filei.FullName);
                fs.Write(values);
                fs.Close();
                i++;
            }
            FileStream finalfile = File.Create(Path.Combine(di.FullName, "final.png"));
            foreach (FileInfo filei in di.GetFiles("chunk_*.bin"))
            {
                byte[] buffer = Array.Empty<byte>();
                using (FileStream fs = filei.OpenRead())
                {
                    buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                }

                finalfile.Write(buffer);
                filei.Delete();
            }
            finalfile.Close();
            label3.Visible = true;
        }
    }
}
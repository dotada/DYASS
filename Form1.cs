using System.Diagnostics;
using System.IO.Compression;
using ZXing;
using IronBarCode;
using BarcodeReader = ZXing.Windows.Compatibility.BarcodeReader;

namespace YTQRStorage
{
    public partial class Form1 : Form
    {
        string rsp;
        string filePath;
        string outputdir;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Form1()
        {
            InitializeComponent();
        }

        static void CombineChunks(string inputDirectory)
        {
            using (FileStream outputStream = new(Path.Combine(inputDirectory, "final.png"), FileMode.Create, FileAccess.Write))
            {
                int index = 1;
                string chunkFileName = Path.Combine(inputDirectory, $"chunk_{index}.bin");
                while(File.Exists(chunkFileName))
                {
                    using (FileStream inputStream = new(chunkFileName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[inputStream.Length];
                        inputStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, buffer.Length);
                    }
                    index++;
                    chunkFileName = Path.Combine(inputDirectory, $"chunk_{index}.bin");
                }
            }
        }

        static void SplitFile(string inputFile, string outputDirectory, int chunkSize)
        {
            using (FileStream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                int index = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string chunkFileName = Path.Combine(outputDirectory, $"chunk_{index}.bin");
                    using (FileStream outputFile = new FileStream(chunkFileName, FileMode.Create, FileAccess.Write))
                    {
                        outputFile.Write(buffer, 0, bytesRead);
                    }

                    index++;
                }
            }

            Console.WriteLine("File has been split into chunks successfully.");
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
                    filePath = openFileDialog.FileName;
                    textBox1.Text = filePath;
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            int img = 1;
            SplitFile(filePath, outputdir, 1200);
            string[] files = Directory.GetFiles(outputdir, "chunk_*.bin");
            //int maxDegreeOfParallelism = Environment.ProcessorCount - 1;
            //ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };
            //Parallel.ForEach(files, options, (file) =>
            //{
                foreach (var file in files)
                {
                byte[] buffer = File.ReadAllBytes(file);
                QRCodeWriter.CreateQrCode(buffer, 500, QRCodeWriter.QrErrorCorrectionLevel.Highest, 0).SaveAsImage(Path.Combine(outputdir, $"qr_{img}.png"));
                img++;
                }
            //});
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
            cmd.StandardInput.WriteLine($"{ffmpeg} -framerate 60 -start_number 1 -i {outputdir}\\qr_%d.png -c copy -pix_fmt yuv420p -fps_mode passthrough {outputdir}\\output.mp4");
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
                }
            }
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            HttpClient client = new HttpClient();
            string extrsp = await client.GetStringAsync("https://www.gyan.dev/ffmpeg/builds/release-version");
            rsp = extrsp;
            string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
            if (!Directory.Exists(appdata))
            {
                Directory.CreateDirectory(appdata);
            }
            string ffmpegdir = Path.Combine(appdata, $"ffmpeg-{rsp}-full_build");
            if (!Directory.Exists(ffmpegdir))
            {
                Directory.CreateDirectory(ffmpegdir);
            }
            string ffmpegbin1 = Path.Combine(ffmpegdir, "bin");
            string ffmpegbin = Path.Combine(ffmpegbin1, "ffmpeg.exe");
            if (!File.Exists(ffmpegbin))
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
            label2.Text = "Downloading.";
            HttpClient client = new HttpClient();
            string pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DYASS");
            string url = $"https://github.com/GyanD/codexffmpeg/releases/download/{rsp}/ffmpeg-{rsp}-full_build.zip";
            string filepth = Path.Combine(pth, $"ffmpeg-{rsp}-full_build.zip");
            Uri uri = new Uri(url);
            Stream filestream = await client.GetStreamAsync(uri);
            using (FileStream outputFileStream = new FileStream(filepth, FileMode.Create))
            {
                await filestream.CopyToAsync(outputFileStream);
            }
            label2.Text = "Extracting.";
            ZipFile.ExtractToDirectory(filepth, pth, true);
            label2.Text = "FFMPEG Installed.";
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
                    filePath = openFileDialog.FileName;
                    textBox3.Text = filePath;
                }
            }
        }

        static byte[] ReadQRCode(string imagePath)
        {
            BarcodeReader barcodeReader = new();
            barcodeReader.Options.TryHarder = true;
            barcodeReader.Options.PureBarcode = true;
            barcodeReader.AutoRotate = true;
            barcodeReader.Options.PossibleFormats = new List<BarcodeFormat>
            {
                BarcodeFormat.QR_CODE
            };
            using FileStream stream = new(imagePath, FileMode.Open);
            using Bitmap bitmap = new(stream);
            var result = barcodeReader.Decode(bitmap);
            return result.RawBytes;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string file = textBox3.Text;
            FileInfo fi = new(file);
            int i = 1;
#pragma warning disable CS8604 // Possible null reference argument.
            DirectoryInfo di = new(fi.DirectoryName);
#pragma warning restore CS8604 // Possible null reference argument.
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
            cmd.StandardInput.WriteLine($"{ffmpeg} -i {file} -r 1/1 -c copy {Path.Combine(di.FullName, "qr_%08d.png")}");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            foreach (FileInfo filei in di.GetFiles("qr_*.png"))
            {
                FileStream fs = File.Create(Path.Combine(di.FullName, $"chunk_{i}.bin"));
                byte[] values = ReadQRCode(filei.FullName);
                fs.Write(values);
                fs.Close();
                i++;
            }
            CombineChunks(di.FullName);
            foreach(FileInfo fi2 in di.GetFiles("chunk_*.bin"))
            {
                fi2.Delete();
            }
            foreach(FileInfo fi2 in di.GetFiles("qr_*.png"))
            {
                fi2.Delete();
            }

            label3.Visible = true;
            byte[] data = File.ReadAllBytes(Path.Combine(di.FullName, "final.png"));
            data = data.Skip(4).ToArray();
            using FileStream outputStream = new(Path.Combine(di.FullName, "final.png"), FileMode.Create, FileAccess.Write);
            outputStream.Write(data, 0, data.Length);
        }
    }
}
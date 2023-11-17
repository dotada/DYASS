using QRCoder;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace YTQRStorage
{
    public partial class Form1 : Form
    {

        string filePath;
        string outputdir;
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
        /*
        private void button2_Click(object sender, EventArgs e)
        {
            int img = 0;
            SplitFile(filePath, 2000, outputdir);
            string[] files;
            files = Directory.GetFiles(outputdir, "chunk_*.bin");
            foreach (string file in files)
            {
                string contents = File.ReadAllText(file);
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(contents, QRCodeGenerator.ECCLevel.L);
                QRCode qrCode = new QRCode(qrCodeData);
                using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                {
                    qrCodeImage.Save(Path.Combine(outputdir, $"qr_{img}.png"));
                }
                img++;
            }
            label1.Visible = true;
        }
        */
        private void button2_Click(object sender, EventArgs e)
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
                foreach (DirectoryInfo dir  in di.GetDirectories())
                {
                    dir.Delete(true);
                }
                label2.Text = "FFMPEG Not installed!";
                button4.Enabled = true;
            } else
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
    }
}
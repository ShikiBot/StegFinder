using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Un4seen.Bass;
using Un4seen.Bass.Misc;
using System.Management;
using System.IO;
using System.Drawing.Imaging;

namespace stegfinder
{
    public partial class Form1 : Form
    {
        string fileName;
        Bitmap image = new Bitmap(10, 10);
        private Timer _scrollingTimer = null;

        public Form1()
        {
            InitializeComponent();
            Bass.BASS_Init(-1, 0, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
            ManagementObjectCollection collection = searcher.Get();
            string WinUser = ((string)collection.Cast<ManagementBaseObject>().First()["UserName"]).Split('\\')[1];
            openFileDialog1.InitialDirectory = $@"C:\Users\{WinUser}\Downloads";
        }

        private async void dataExtractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*string value = await Task.Run(() =>
            {
                byte[] data = new byte[10000];
                using (var fstream = File.OpenRead(fileName)) fstream.Read(data, 0, data.Length);
                return BitConverter.ToString(data);
            });*/
            MessageBox.Show("Пока не написано");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            openFileDialog1.ShowDialog();
        }

        private async void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            progressBar1.Visible = true;
            fileName = openFileDialog1.FileName;
            trackBar2.Value = 0;
            int x = trackBar1.Value;
            var progress = new Progress<int>(s => progressBar1.Value = s);            
            image = await Task.Run(() => DrawSpectrogram(fileName, pictureBox1.Height, x, progress));
            pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 60) : image.Size;
            pictureBox1.Image = image;
            progressBar1.Value = 0;
            progressBar1.Visible = false;
            fileFormatToolStripMenuItem.Enabled = true;
            dataExtractToolStripMenuItem.Enabled = true;
        }

        private Bitmap DrawSpectrogram(string fileName, int height, int stepsPerSecond, IProgress<int> progress)
        {            
            link1:
            //инициализация потока
            Bass.BASS_Init(-1, TagLib.File.Create(fileName).Properties.AudioSampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            int channel = Bass.BASS_StreamCreateFile(fileName, 0, 0, BASSFlag.BASS_DEFAULT);
            Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, 0);

            long len = Bass.BASS_ChannelGetLength(channel, BASSMode.BASS_POS_BYTES); // the length in bytes
            double time = Bass.BASS_ChannelBytes2Seconds(channel, len); // the length in seconds
            
            int steps = (int)Math.Floor(stepsPerSecond * time);
            if (steps < 1) goto link1; //TODO: пофиксить это

            Bitmap result = new Bitmap(steps, height);
            Graphics g = Graphics.FromImage(result);

            Visuals visuals = new Visuals();


            Bass.BASS_ChannelPlay(channel, false);
            

            for (int i = 0; i < steps; i++)
            {
                Bass.BASS_ChannelSetPosition(channel, 1.0 * i / stepsPerSecond);
                visuals.CreateSpectrum3DVoicePrint(channel, g, new Rectangle(0, 0, result.Width, result.Height), Color.Cyan, Color.Green, i, false, true);
                progress.Report(10000 * i / steps);
            }

            Bass.BASS_ChannelStop(channel);
            Bass.BASS_Stop();
            Bass.BASS_Free();

            return result;
        }

        private void fileFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var audioFile = TagLib.File.Create(fileName);            
            MessageBox.Show($"Альбом: {audioFile.Tag.Album}\n" +
                $"Исполнитель: {String.Join(", ", audioFile.Tag.Performers)}\n" +
                $"Название: {audioFile.Tag.Title}\n" +
                $"Год: {audioFile.Tag.Year}\n" +
                $"Длительность: {audioFile.Properties.Duration.ToString("mm\\:ss")}\n" +
                $"Аудио битрейт: {audioFile.Properties.AudioBitrate}\n" +
                $"Аудио каналы: {audioFile.Properties.AudioChannels}\n" +
                $"Аудио семпл рейт: {audioFile.Properties.AudioSampleRate}\n" +
                $"Бит в семпл: {audioFile.Properties.BitsPerSample}\n" +
                $"Описание: {audioFile.Properties.Description}\n" +
                $"Тип медиа: {audioFile.Properties.MediaTypes}");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 60) : new Size(image.Width, this.Height - 60);
            pictureBox1.Location = new Point(20 - trackBar2.Value * (pictureBox1.Width - this.Width + 50) / 1000, 25);
            trackBar1.Location = new Point(this.Width - 375, 0);
            pictureBox2.Location = new Point(trackBar1.Location.X, 24);
            progressBar1.Location = new Point(trackBar1.Location.X - 500, 0);
            trackBar2.Location = new Point(0, this.Height - 64);
            trackBar2.Width = this.Width - 16;
            trackBar2.Value = 0;
            if (fileName != null)
            {
                progressBar1.Visible = true;
                int x = trackBar1.Value;
                var progress = new Progress<int>(s => progressBar1.Value = s);
                image = await Task.Run(() => DrawSpectrogram(fileName, pictureBox1.Height, x, progress));
                pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 60) : image.Size;
                pictureBox1.Image = image;
                progressBar1.Value = 0;
                progressBar1.Visible = false;
            }
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            pictureBox1.Location = new Point(20 - trackBar2.Value * (pictureBox1.Width - this.Width + 50) / 1000, 25);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_scrollingTimer == null)
            {
                _scrollingTimer = new Timer() {Enabled = false, Interval = 100, Tag = (sender as TrackBar).Value}; // Will tick every 100ms (change as required)
                _scrollingTimer.Tick += async (send, ea) =>
                {                    
                    if (trackBar1.Value == (int)_scrollingTimer.Tag) // check to see if the value has changed since we last ticked
                    {                        
                        _scrollingTimer.Stop(); // scrolling has stopped so we are good to go ahead and do stuff
                        _scrollingTimer.Dispose();
                        _scrollingTimer = null;
                        if (fileName != null)
                        {
                            trackBar2.Value = 0;
                            progressBar1.Visible = true;
                            int x = trackBar1.Value;
                            var progress = new Progress<int>(s => progressBar1.Value = s);
                            image = await Task.Run(() => DrawSpectrogram(fileName, pictureBox1.Height, x, progress));
                            pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 60) : image.Size;
                            pictureBox1.Image = image;
                            progressBar1.Value = 0;
                            progressBar1.Visible = false;
                        }                          
                    }
                    else _scrollingTimer.Tag = trackBar1.Value; // record the last value seen                    
                };
                _scrollingTimer.Start();
            }            
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (image != null)
            {
                saveFileDialog1.ShowDialog();
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            image.Save(saveFileDialog1.FileName, ImageFormat.Png);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            toolTip1.Show($"{trackBar1.Value}", trackBar1, trackBar1.PointToClient(Cursor.Position), 1000);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Пока не написано");
        }
    }
}

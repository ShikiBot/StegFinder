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
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;

namespace stegfinder
{
    public partial class MainForm : Form
    {



/////////////////////////////////////////////////////////////////////////////ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ//////////////////////////////////////////////////////////////////////////////////////



        string fileName;
        Bitmap image = new Bitmap(10, 10);
        private Timer scrollingTimer = null;
        private Timer resizeTimer = null;
        List<Task<Bitmap>> tasksList = new List<Task<Bitmap>>();
        static System.Threading.CancellationTokenSource canselTocken;




/////////////////////////////////////////////////////////////////////////СТАНДАРТНЫЕ ФУНКЦИИ ЭЛЕМЕНТОВ//////////////////////////////////////////////////////////////////////////////////



        public MainForm()
        {
            InitializeComponent();
            Bass.BASS_Init(-1, 0, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            ManagementObjectSearcher userSearcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
            ManagementObjectCollection collection = userSearcher.Get();
            string WinUser = ((string)collection.Cast<ManagementBaseObject>().First()["UserName"]).Split('\\')[1];
            openFileDialog1.InitialDirectory = $@"C:\Users\{WinUser}\Downloads";
            Sizes();
            //--------------------------------------
            aboutToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// изменение положения элементов при изменении размеров формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            Sizes();
            //проверка чтобы не начинать новую отрисовку при изменении параметра каждый тик
            if (resizeTimer == null)
            {
                resizeTimer = new Timer() { Enabled = false, Interval = 500, Tag = Size.Width + Size.Height }; // новый таймер тикающий раз в 500мс
                resizeTimer.Tick += (send, ea) =>
                {
                    if (Size.Width + Size.Height == (int)resizeTimer.Tag) // проверка изменения значения с предыдущего тика
                    {
                        resizeTimer.Stop(); // остановка и удаление таймера если значения не изменились
                        resizeTimer.Dispose();
                        resizeTimer = null;
                        if (fileName != null) DrawSpec(); // вызов отрисовки 
                    }
                    else resizeTimer.Tag = Size.Width + Size.Height; // обновление значений в ином случае              
                };
                resizeTimer.Start();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                canselTocken.Cancel();
                tasksList.Clear();
            }
            catch { }
            openFileDialog1.ShowDialog();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (image != null) saveFileDialog1.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            progressBar1.Visible = true;
            fileName = openFileDialog1.FileName;
            fileFormatToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            //dataExtractToolStripMenuItem.Enabled = true;
            DrawSpec();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            image.Save(saveFileDialog1.FileName, ImageFormat.Png);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            toolTip1.Show($"{trackBar1.Value}", trackBar1, trackBar1.PointToClient(new Point(Cursor.Position.X, Cursor.Position.Y + 20)), 1000);
        }

        private void trackBar1_MouseMove(object sender, MouseEventArgs e)
        {
            toolTip1.Show($"{trackBar1.Value}", trackBar1, trackBar1.PointToClient(new Point(Cursor.Position.X, Cursor.Position.Y + 20)), 1000);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //проверка чтобы не начинать новую отрисовку при изменении параметра каждый тик
            if (scrollingTimer == null)
            {
                scrollingTimer = new Timer() { Enabled = false, Interval = 500, Tag = (sender as TrackBar).Value }; // новый таймер тикающий раз в 500мс
                scrollingTimer.Tick += (send, ea) =>
                {
                    if (trackBar1.Value == (int)scrollingTimer.Tag) // проверка изменения значения с предыдущего тика
                    {
                        scrollingTimer.Stop(); // остановка и удаление таймера если значения не изменились
                        scrollingTimer.Dispose();
                        scrollingTimer = null;
                        if (fileName != null) DrawSpec(); // вызов отрисовки 
                    }
                    else scrollingTimer.Tag = trackBar1.Value; // обновление значений в ином случае                
                };
                scrollingTimer.Start();
            }
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            pictureBox1.Location = new Point(20 - trackBar2.Value * (pictureBox1.Width - this.Width + 50) / 1000, 25);
        }



////////////////////////////////////////////////////////////////////////////САМОПАЛЬНЫЕ ФУНКЦИИ///////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// функция отрисовки 
        /// </summary>
        /// <param name="fileName">путь к открываемому файлу</param>
        /// <param name="height">высота pictureBox1</param>
        /// <param name="stepsPerSecond">частота отрисовки стереграммы (раз в секунду)</param>
        /// <param name="progress">ссылка на прогрессбар</param>
        /// <param name="cancellationToken">токен остановки потока</param>
        /// <returns></returns>
        private Bitmap DrawSpectrogram(string fileName, int height, int stepsPerSecond, IProgress<int> progress, System.Threading.CancellationToken cancellationToken)
        {
            //инициализация потока
            Bass.BASS_Init(-1, TagLib.File.Create(fileName).Properties.AudioSampleRate, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            int channel = Bass.BASS_StreamCreateFile(fileName, 0, 0, BASSFlag.BASS_DEFAULT); //номер потока
            long len = Bass.BASS_ChannelGetLength(channel, BASSMode.BASS_POS_BYTES); // длина файла в байтах
            double time = Bass.BASS_ChannelBytes2Seconds(channel, len); // длина файла в секундах         
            int steps = (int)Math.Floor(stepsPerSecond * time);
            Bass.BASS_ChannelSetAttribute(channel, BASSAttribute.BASS_ATTRIB_VOL, 0); //громкость на 0

            //инициализация объектов для рисования спектрограммы
            Bitmap result = new Bitmap(steps, height);
            Graphics g = Graphics.FromImage(result);
            Visuals visuals = new Visuals();

            Bass.BASS_ChannelPlay(channel, false); //запуск потока

            if (!cancellationToken.IsCancellationRequested) //если поток не остановлен, то можно рисовать
            {
                for (int i = 0; i < steps; i++)
                {
                
                    Bass.BASS_ChannelSetPosition(channel, 1.0 * i / stepsPerSecond); //переход на нужную позицию
                    visuals.CreateSpectrum3DVoicePrint(channel, g, new Rectangle(0, 0, result.Width, result.Height), Color.Blue, Color.Empty, i, false, true); //отрисовка спектрограммы
                    //удаление лишних цветов (вычленение зеленой компоненты и отрисовка в другом цвете)
                    for (int j = 0; j < height; j++)
                    {
                        UInt32 pixel = (UInt32)(result.GetPixel(i, j).ToArgb());
                        float G = (pixel & 0x0000FF00) >> 8;
                        UInt32 newPixel = 0xFF000000 | (2 * (UInt32)G << 16) | ((UInt32)G << 8) | (0);
                        result.SetPixel(i, j, Color.FromArgb((int)newPixel));
                    }
                    progress.Report(10000 * i / steps); //отправка текущего состояния отрисовки в прогрессбар
                }
            }

            //отключение потока
            Bass.BASS_ChannelStop(channel);
            Bass.BASS_Stop();
            Bass.BASS_Free();

            return result;
        } 

        /// <summary>
        /// правила перемещения элементов при изменении размеров формы
        /// </summary>
        private void Sizes()
        {
            pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 90) : new Size(image.Width, this.Height - 90);
            pictureBox1.Location = new Point(20 - trackBar2.Value * (pictureBox1.Width - this.Width + 50) / 1000, 25);
            trackBar1.Location = new Point(this.Width - 375, 0);
            pictureBox2.Location = new Point(trackBar1.Location.X, 24);
            progressBar1.Location = new Point(trackBar1.Location.X - 250, -1);
            trackBar2.Location = new Point(0, this.Height - 64);
            trackBar2.Width = this.Width - 16;
            trackBar2.Value = 0;
        }        

        /// <summary>
        /// вызов отрисовки стереограммы
        /// </summary>
        private async void DrawSpec()
        {
            progressBar1.Visible = true;
            int treckBarVal = trackBar1.Value;
            var progress = new Progress<int>(s => progressBar1.Value = s); //сылка на прогрессбар для потока
            if (Size.Height > 0) //если не свернуто
            {
                if (tasksList.Count == 0) //если потоков нет
                {
                    canselTocken = new System.Threading.CancellationTokenSource();
                    tasksList.Add(Task.Run(() => DrawSpectrogram(fileName, pictureBox1.Height, treckBarVal, progress, canselTocken.Token)));
                }
                else //если поток уже существует, его нужно остановить и запустить новый
                {
                    canselTocken.Cancel();
                    tasksList.Clear();
                    tasksList.Add(Task.Run(() => DrawSpectrogram(fileName, pictureBox1.Height, treckBarVal, progress, canselTocken.Token)));
                }
            }            
            image = await tasksList[0]; //ожидание отрисовки
            tasksList.Clear(); //очистка списка потоков
            pictureBox1.Size = image.Width < this.Width ? new Size(this.Width - 15, this.Height - 60) : image.Size; //подгонка пикчербокса под размер нарисованного изображения
            pictureBox1.Image = image;
            pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipY); //изображение почему-то перевернуто (???) переворачиваю обратно
            progressBar1.Value = 0;
            progressBar1.Visible = false;
        } 



////////////////////////////////////////////////////////////////////////////////ДРУГИЕ ФОРМЫ////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// открытие формы с информацией о тегах файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TagForm tagForm = new TagForm(fileName);
            tagForm.Show();
        }        

        /// <summary>
        /// открытие формы с HEX содержимым файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void dataExtractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*string value = await Task.Run(() =>
            {
                byte[] data = new byte[10000];
                using (var fstream = File.OpenRead(fileName)) fstream.ReadAsync(data, 0, data.Length);
                return BitConverter.ToString(data);
            });*/
        }

        /// <summary>
        /// открытие формы с информацией о программе
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Пока не написано");
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace stegfinder
{
    public partial class TagForm : Form
    {
        public TagForm(string fileName)
        {
            InitializeComponent();
            this.fileName = fileName;
        }

        readonly string fileName;

        private void TagForm_Load(object sender, EventArgs e)
        {
            var AudioFile = TagLib.File.Create(fileName);
            if (AudioFile.Tag.Pictures.Length > 0) pictureBox1.Image = Image.FromStream(new MemoryStream(AudioFile.Tag.Pictures[0].Data.Data));
            string answ = $"Название: {AudioFile.Tag.Title}\n" +
                $"Комментарий: {AudioFile.Tag.Comment}\n" +
                $"Исполнители: {StringFromArray(AudioFile.Tag.Performers)}\n" +
                $"Исполнители альбома: {StringFromArray(AudioFile.Tag.AlbumArtists)}\n" +
                $"Альбом: {AudioFile.Tag.Album}\n" +
                $"Год: {AudioFile.Tag.Year}\n" +
                $"Номер в плейлисте: {AudioFile.Tag.Track}\n" +
                $"Жанры: {StringFromArray(AudioFile.Tag.Genres)}\n" +
                $"Описание: {AudioFile.Properties.Description}\n" +
                $"Продолжительность: {AudioFile.Properties.Duration}\n" +
                $"Авторские права: {AudioFile.Tag.Copyright}\n" +
                $"Композиторы: {StringFromArray(AudioFile.Tag.Composers)}\n" +
                $"Дирижеры: {AudioFile.Tag.Conductor}\n" +
                $"Скорость потока: {AudioFile.Properties.AudioBitrate} Кбит/сек\n" +
                $"Количество каналов: {AudioFile.Properties.AudioChannels}\n" +
                $"Частота дискретизации: {AudioFile.Properties.AudioSampleRate}\n" +
                $"Битовая глубина: {AudioFile.Properties.BitsPerSample}\n" +
                $"Тип тега: {AudioFile.TagTypes}";
            richTextBox1.Text = answ;
            //применение полужирного стиля к названиям тегов
            foreach (string line in richTextBox1.Lines)
            {
                string paramName = line.Split(':')[0];
                int paramIndex = richTextBox1.Find(paramName);
                richTextBox1.Select(paramIndex, paramName.Length);
                richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
            }
            richTextBox1.Select(0,0);
        }

        /// <summary>
        /// приведение массивов в строку
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private string StringFromArray(string[] array)
        {
            string outString = "";
            foreach (string value in array)
                outString += value + ", ";
            outString = outString.Length > 0 ? outString.Substring(0, outString.Length - 2) : "";
            return outString;
        }
    }
}

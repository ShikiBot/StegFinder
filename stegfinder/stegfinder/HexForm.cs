using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stegfinder
{
    public partial class HexForm : Form
    {
        readonly string fileName;
        public string value;
        Thread thread;

        public HexForm(string fileName)
        {
            InitializeComponent();
            this.fileName = fileName;
        }

        private void hex_Load(object sender, EventArgs e)
        {
            var progress = new Progress<string>(s => richTextBox1.Text += s);
            thread = new Thread(() => ReadFile(fileName, progress, 0, 50));
            thread.Start();
        }

        /// <summary>
        /// Функция чтения файла по HEX строчкам по 16 символов
        /// </summary>
        /// <param name="fileName">путь к файлу</param>
        /// <param name="progress">объект класса Progress<string> со ссылкой на текстовый элемент формы</param>
        /// <param name="offset">индекс начальной строки</param>
        /// <param name="count">количество читаемых строк</param>
        public void ReadFile(string fileName, IProgress<string> progress, long offset, long count)
        {
            byte[] data = new byte[16];            
            FileStream FS = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            long Offset = count == 0 ? FS.Length : offset*16;
            while (Offset < count * 16 && Offset < FS.Length)
            {
                FS.Seek(Offset, SeekOrigin.Begin);
                FS.Read(data, 0, 16);
                Offset += 16;
                string s = BitConverter.ToString(data).Replace("-", " ") + "\n";
                progress.Report(s);
                Thread.Sleep(1);
            }
            FS.Close();
        }

        private void HexForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            thread.Abort();
        }

        //как-нибудь надо будет переделать
        private void button1_Click(object sender, EventArgs e)
        {
            byte[] data = new byte[10000];

            using (var fstream = File.OpenRead(fileName))
            {
                fstream.Read(data, 0, data.Length);
            }
            int stasSoSchetami = 0;
            label1.Text = "";
            string stroka = BitConverter.ToString(data);
            int result;

            string perenos = File.ReadAllText("keywords.txt");

            char[] b = perenos.ToArray();
            string petr = "";
            string[] danil = new string[b.Length];

            for (int i = 0; i < b.Length; i++)
            {
                danil[i] = Convert.ToString(b[i], 16);
                petr += danil[i];
            }

            string pattern = "da";
            string[] ilya = System.Text.RegularExpressions.Regex.Split(petr, pattern);
            string svetochka = "";

            for (int i = 0; i < ilya.Length; i++)
            {
                result = stroka.IndexOf(ilya[i]);
                if (result != -1)
                {
                    stasSoSchetami += 1;
                    svetochka = svetochka + " на позиции: " + result + " ";
                }
            }
            label1.Text = "Найдено " + stasSoSchetami + " Совпадений: " + svetochka;
        }
    }
}

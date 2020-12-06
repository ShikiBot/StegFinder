using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stegfinder
{
    public partial class HexForm : Form
    {
        string fileName;
        public string value;
        int y;
        private Timer scrollTimer = null;

        public HexForm(string fileName)
        {
            InitializeComponent();
            this.fileName = fileName;
            richTextBox1.MouseWheel += RichTextBox_MouseWheel;
            y = 1;
        }

        void RichTextBox_MouseWheel(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void hex_Load(object sender, EventArgs e)
        {
            int z = 0;
            for (int x = y; x <= y * 50; x++)
            {
                richTextBox1.Text += value.Substring(x * 48, 47) + "\n";
                z = x;
            }
            y = z + 1;
        }

        public async Task<string> aaa(string fileName)
        {
            FileInfo fileInf = new FileInfo(fileName);
            byte[] data = new byte[(int)fileInf.Length];
            using (var fstream = File.OpenRead(fileName)) await fstream.ReadAsync(data, 0, data.Length);
            string s = BitConverter.ToString(data).Replace("-", "\t"); 
            return s;
        }

        private void richTextBox1_VScroll(object sender, EventArgs e)
        {
            /*if (scrollTimer == null)
            {
                scrollTimer = new Timer() { Enabled = false, Interval = 100, Tag = MousePosition.Y }; // новый таймер тикающий раз в 500мс
                scrollTimer.Tick += (send, ea) =>
                {
                    if (MousePosition.Y == (int)scrollTimer.Tag) // проверка изменения значения с предыдущего тика
                    {
                        scrollTimer.Stop(); // остановка и удаление таймера если значения не изменились
                        scrollTimer.Dispose();
                        scrollTimer = null;
                        /*if (y * 48 < value.Length)
                        {
                            int z = 0;
                            for (int x = y; x <= y + 50; x++)
                            {
                                richTextBox1.AppendText(value.Substring(x * 48, 60) + "\n");
                                z = x;
                            }
                            y = z + 1;
                        }// вызов отрисовки 
                        else MessageBox.Show("asdas");
                    }
                    else scrollTimer.Tag = MousePosition.Y; // обновление значений в ином случае              
                };
                scrollTimer.Start();
            }*/
            
        }


    }
}

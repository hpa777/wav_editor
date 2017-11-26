using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Wave;
using System.Windows.Forms.DataVisualization.Charting;

namespace waveditor
{
    public partial class Form1 : Form
    {

        private WaveFileReader wave;
        private IWavePlayer player;
        private Timer timer;
        private string file;
        private int cof;

        public Form1()
        {
            InitializeComponent();

            fileNameLabel.Text = String.Empty;
            playButton.Enabled = false;

            chart1.Series.Add("wave");
            chart1.Series["wave"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series["wave"].ChartArea = "ChartArea1";
            chart1.ChartAreas["ChartArea1"].AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas["ChartArea1"].CursorX.IsUserEnabled = Enabled;
            chart1.ChartAreas["ChartArea1"].CursorX.IsUserSelectionEnabled = Enabled;


            player = new WaveOut();
            player.PlaybackStopped += new EventHandler<StoppedEventArgs>(player_PlaybackStopped);

            timer = new Timer();
            timer.Interval = 5;
            timer.Tick += new EventHandler(timer_Tick);

            initContextMenu();
            setChartImages();


        }


        ToolStripMenuItem motor1button;
        ToolStripMenuItem motor2button;
        ToolStripMenuItem deleteButton;

        private void initContextMenu()
        {
            MyContextMenuStrip menu = new MyContextMenuStrip();
            menu.Opening += new CancelEventHandler(menu_Opening);
            menu.AutoClose = false;
            motor1button = new ToolStripMenuItem("Мотор 1");
            motor1button.Name = "Motor1";
            motor1button.CheckOnClick = true;
            motor1button.Image = Properties.Resources.led1off;
            motor1button.CheckStateChanged += new EventHandler(menuItem_CheckStateChanged);

            motor2button = new ToolStripMenuItem("Мотор 2");
            motor2button.Name = "Motor2";
            motor2button.CheckOnClick = true;
            motor2button.Image = Properties.Resources.led2off;
            motor2button.CheckStateChanged += new EventHandler(menuItem_CheckStateChanged);

            ToolStripMenuItem menuItem3 = new ToolStripMenuItem("Сохранить");
            menuItem3.Click += new EventHandler(saveAnnotation);

            deleteButton = new ToolStripMenuItem("Удалить");
            deleteButton.Click += new EventHandler(deleteAnnotation);

            ToolStripMenuItem menuItem5 = new ToolStripMenuItem("Закрыть");
            menuItem5.Click += new EventHandler(exit_Click);


            menu.Items.Add(motor1button);
            menu.Items.Add(motor2button);
            menu.Items.Add(menuItem3);
            menu.Items.Add(deleteButton);
            menu.Items.Add(menuItem5);

            chart1.ContextMenuStrip = menu;
        }

        ImageAnnotation cur_annotation;

        private void chart1_AnnotationSelectionChanged(object sender, EventArgs e)
        {
            cur_annotation = sender as ImageAnnotation;
            char[] mm = cur_annotation.Image.ToCharArray();
            motor1button.Checked = mm[0].Equals('1');
            motor2button.Checked = mm[1].Equals('1');
            chart1.ContextMenuStrip.Show();
        }

        void menu_Opening(object sender, CancelEventArgs e)
        {
            if (wave == null)
            {
                e.Cancel = true;
            }
            else
            {
                deleteButton.Visible = cur_annotation != null;
            }
        }

        void deleteAnnotation(object sender, EventArgs e)
        {
            if (cur_annotation == null) return;
            chart1.Annotations.Remove(cur_annotation);
            cur_annotation = null;
            chart1.ContextMenuStrip.Close();
        }

        void saveAnnotation(object sender, EventArgs e)
        {
            chart1.ContextMenuStrip.Close();
            string i1 = motor1button.Checked ? "1" : "0";
            string i2 = motor2button.Checked ? "1" : "0";
            string s = i2 + i1;
            if (cur_annotation == null)
            {
                ImageAnnotation ann = new ImageAnnotation();
                ann.AxisX = chart1.ChartAreas["ChartArea1"].AxisX;
                ann.AxisY = chart1.ChartAreas["ChartArea1"].AxisY;
                ann.Image = s;
                ann.AnchorX = cur_pos;
                ann.AnchorY = 0;
                ann.AllowSelecting = true;
                chart1.Annotations.Add(ann);
            }
            else
            {
                cur_annotation.Image = s;
                cur_annotation = null;
            }
        }

        void menuItem_CheckStateChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item.Name.Equals("Motor1"))
            {
                item.Image = item.Checked ? Properties.Resources.led1on : Properties.Resources.led1off;
            }
            else if (item.Name.Equals("Motor2"))
            {
                item.Image = item.Checked ? Properties.Resources.led2on : Properties.Resources.led2off;
            }
        }

        void exit_Click(object sender, EventArgs e)
        {
            chart1.ContextMenuStrip.Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wave == null || chart1.Annotations.Count == 0)
            {
                MessageBox.Show(this, "Нечего сохранять", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            byte[] buf = new byte[wave.Length];
            wave.Read(buf, 0, (int)wave.Length);
            foreach (ImageAnnotation ann in chart1.Annotations)
            {
                int pos = (int)((ann.AnchorX * cof * batch) + (cof / 2));
                buf[pos] = Convert.ToByte(String.Format("000000{0}1", ann.Image), 2);
            }
            string nf = file.Replace(".wav", "_toROM.wav");
            using (WaveFileWriter fw = new WaveFileWriter(nf, wave.WaveFormat))
            {
                fw.Write(buf, 0, (int)wave.Length);
            }
            MessageBox.Show(this, "Файл успешно сохранен " + nf, "Сообщене", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private int batch;

        private void DrawChart(string fname)
        {

            var samples = wave.Length / (wave.WaveFormat.Channels * wave.WaveFormat.BitsPerSample / 8);
            batch = (int)Math.Max(40, samples / 4000);

            int f_count = 0;
            byte[] buf = new byte[cof];
            wave.Position = 0;
            while (wave.Position < wave.Length)
            {
                wave.Read(buf, 0, cof);
                int motor = cof == 2 ? buf[1] : (buf[2] + buf[3]);
                if (motor > 0)
                {
                    string fn = "";
                    switch (motor)
                    {
                        case 1: fn = "00"; break;
                        case 3: fn = "01"; break;
                        case 5: fn = "10"; break;
                        case 7: fn = "11"; break;
                    }
                    ImageAnnotation ann = new ImageAnnotation();
                    ann.AxisX = chart1.ChartAreas["ChartArea1"].AxisX;
                    ann.AxisY = chart1.ChartAreas["ChartArea1"].AxisY;
                    ann.Image = fn;
                    ann.AnchorX = wave.Position / cof / batch;
                    ann.AnchorY = 0;
                    ann.AllowSelecting = true;
                    chart1.Annotations.Add(ann);
                }
                if (f_count == 0)
                {
                    if (cof == 4)
                    {
                        chart1.Series["wave"].Points.Add(BitConverter.ToInt16(buf, 0));
                    }
                    else
                    {
                        chart1.Series["wave"].Points.Add(buf[0]);
                    }
                    f_count = batch;
                }
                else
                {
                    f_count--;
                }
            }
            wave.Position = 0;

        }

        private void openFile_Click(object sender, EventArgs e)
        {
            openFile(false);

        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile(true);
        }

        private void openFile(bool clear)
        {
            chart1.Series["wave"].Points.Clear();
            chart1.Annotations.Clear();
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Wave File (*.wav)|*.wav;";
            if (open.ShowDialog() != DialogResult.OK) return;
            file = open.FileName;

            wave = new WaveFileReader(file);

            if (wave.WaveFormat.Channels != 2 || !(wave.WaveFormat.BitsPerSample == 8 || wave.WaveFormat.BitsPerSample == 16))
            {
                MessageBox.Show(this, "Неподходящий формат файла", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                wave = null;
                return;
            }
            fileNameLabel.Text = file;
            cof = wave.WaveFormat.BitsPerSample / 8 * 2;
            if (clear)
            {
                byte[] buf = new byte[wave.Length];
                wave.Read(buf, 0, (int)wave.Length);
                for (int i = 0; i < buf.Length; i += cof)
                {
                    int i1 = i + cof / 2;
                    buf[i1] = 0;
                    if (wave.WaveFormat.BitsPerSample == 16)
                    {
                        buf[i1 + 1] = 0;
                    }

                }
                string nf = file.Replace(".wav", "_clr.wav");
                using (WaveFileWriter fw = new WaveFileWriter(nf, wave.WaveFormat))
                {
                    fw.Write(buf, 0, (int)wave.Length);
                }
                MessageBox.Show(this, "Файл успешно сохранен " + nf, "Сообщене", MessageBoxButtons.OK, MessageBoxIcon.Information);
                wave.Dispose();
                return;
            }
            DrawChart(file);
            player.Init(wave);
            playButton.Enabled = true;
        }

        bool isPlay = false;

        private void playButton_Click(object sender, EventArgs e)
        {
            if (!isPlay)
            {
                isPlay = true;
                timer.Start();
                player.Play();
            }
            else
            {
                player.Pause();
                timer.Stop();
                isPlay = false;
            }
        }

        private void chart1_DoubleClick(object sender, EventArgs e)
        {
            if (!isPlay && wave != null)
            {
                wave.Position = (int)chart1.ChartAreas["ChartArea1"].AxisX.PixelPositionToValue(((MouseEventArgs)e).X) * cof * batch;
                isPlay = true;
                timer.Start();
                player.Play();
            }

        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            timer.Stop();
            wave.Position = 0;
            isPlay = false;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            chart1.ChartAreas["ChartArea1"].CursorX.SetCursorPosition(wave.Position / cof / batch);
        }
        /*
       private void chart1_MouseMove(object sender, MouseEventArgs e)
       {
           
           Point mousePoint = new Point(e.X, e.Y);
           chart1.ChartAreas["ChartArea1"].CursorX.SetCursorPixelPosition(mousePoint, true);
           

       }
       */
        int cur_pos;

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            cur_pos = (int)chart1.ChartAreas["ChartArea1"].AxisX.PixelPositionToValue(e.X);
            if (e.Button == System.Windows.Forms.MouseButtons.Left && isPlay)
            {
                wave.Position = cur_pos * cof * batch;
            }

        }

        private void setChartImages()
        {
            NamedImage img1 = new NamedImage("00", Properties.Resources.alloff);
            NamedImage img2 = new NamedImage("11", Properties.Resources.allon);
            NamedImage img3 = new NamedImage("01", Properties.Resources._1on);
            NamedImage img4 = new NamedImage("10", Properties.Resources._2on);
            chart1.Images.Add(img1);
            chart1.Images.Add(img2);
            chart1.Images.Add(img3);
            chart1.Images.Add(img4);
        }

        

        private void chart1_KeyDown(object sender, KeyEventArgs e)
        {
            if (wave != null && e.KeyCode == Keys.Space)
            {
                playButton_Click(null, null);
            }
        }

        




    }


}



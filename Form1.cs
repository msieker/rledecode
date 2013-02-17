using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace rledecode
{
    public partial class Form1 : Form
    {
        private bool _closing;
        private const string FilePath = @"\\kyou\media\badapple";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(Worker);

        }

        private Image DecodeImage(string fileName)
        {
            var image = new Bitmap(220, 165, PixelFormat.Format24bppRgb);
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                var fileBuffer = new byte[file.Length];
                var fileOffset = 0;
                file.Read(fileBuffer, 0, (int)file.Length);
                var bd = image.LockBits(new Rectangle(0, 0, 220, 165), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var offset = 0;
                unsafe
                {
                    var imgStart = (byte*)bd.Scan0;
                    while (fileOffset < fileBuffer.Length)
                    {
                        var runLength = (int)fileBuffer[fileOffset++];

                        if (runLength > 127)
                        {
                            var dataByte = (int)fileBuffer[fileOffset++];
                            for (var i = 0; i < runLength - 128; i++)
                            {
                                imgStart[offset + 0] = (byte)dataByte;
                                imgStart[offset + 1] = (byte)dataByte;
                                imgStart[offset + 2] = (byte)dataByte;
                                offset += 3;
                            }
                        }
                        else
                        {
                            for (var i = 0; i < runLength; i++)
                            {
                                imgStart[offset + 0] = fileBuffer[fileOffset];
                                imgStart[offset + 1] = fileBuffer[fileOffset];
                                imgStart[offset + 2] = fileBuffer[fileOffset];
                                fileOffset++;
                                offset += 3;
                            }
                        }
                    }
                }
                image.UnlockBits(bd);
            }
            return image;
        }

        private void Worker(object state)
        {
            foreach (var fileName in Directory.GetFiles(FilePath).OrderBy(s => s))
            {
                var start = DateTime.Now;
                var thisName = fileName;
                BeginInvoke((Action)(() =>
                                          {
                                              label1.Text = thisName;
                                          }));

                var image = DecodeImage(fileName);
                BeginInvoke((Action)(() =>
                {
                    pictureBox1.Image = image;
                }));

                var elapsed = DateTime.Now.Subtract(start);
                if (elapsed.Milliseconds < 33)
                {
                    Thread.Sleep(33 - elapsed.Milliseconds);
                }
                if (_closing)
                {
                    return;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
        }
    }
}

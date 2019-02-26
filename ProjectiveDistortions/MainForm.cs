using System;
using System.Drawing;
using System.Windows.Forms;
using ProjectiveDistortions.ImageProcessing;

namespace ProjectiveDistortions
{
    public class MainForm : Form
    {
        public Bitmap CompressionBmp { get; set; }
        public Bitmap OriginalBmp { get; set; }
        public Bitmap ResImage { get; set; }
        public MainForm()
        {
            ClientSize = new Size(800, 600);
            Text = "Anya";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Button buttonOpen = new Button { FlatStyle = FlatStyle.System, Text = "Open" };
            Button buttonApply = new Button { FlatStyle = FlatStyle.System, Text = "Apply" };
            Button buttonSave = new Button { FlatStyle = FlatStyle.System, Text = "Save" };

            PictureBox pictureBox1 = new PictureBox { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            PictureBox pictureBox2 = new PictureBox { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            SizeChanged += (sender, args) => SizeSet(buttonSave, buttonOpen, buttonApply, pictureBox2, pictureBox1);
            Load += (sender, args) => OnSizeChanged(EventArgs.Empty);
            Controls.Add(pictureBox1);
            Controls.Add(pictureBox2);
            Controls.Add(buttonApply);
            Controls.Add(buttonOpen);
            Controls.Add(buttonSave);
            buttonOpen.Click += (sender, args) => OpenFile(pictureBox1);
            buttonApply.Click += (sender, args) => ImageTransformation(pictureBox2);
            buttonSave.Click += (sender, args) => SaveImage(pictureBox2);
        }

        private void SaveImage(PictureBox pictureBox2)
        {
            if (pictureBox2.Image != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Сохранить картинку как...";
                sfd.OverwritePrompt = true;
                sfd.CheckPathExists = true;
                sfd.Filter = "Image Files(*.BMP)|*.BMP|Image Files(*.JPG)|*.JPG|Image Files(*.GIF)|*.GIF|Image Files(*.PNG)|*.PNG|All files (*.*)|*.*";
                sfd.ShowHelp = true;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ResImage.Save(sfd.FileName);
                    }
                    catch
                    {
                        MessageBox.Show("Невозможно сохранить изображение", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ImageTransformation(PictureBox pictureBox2)
        {
            try
            {
                Bitmap newBmp = EliminationOfDistortions.RecognizeDocumentInImage(OriginalBmp);
                ResImage = newBmp;
                pictureBox2.Image = new Bitmap(newBmp, pictureBox2.Size);
            }
            catch
            {
                MessageBox.Show("Отсутствует объект", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFile(PictureBox pictureBox1)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    OriginalBmp = new Bitmap(ofd.FileName);
                    CompressionBmp = BitmapProcessing.ImageCompression(OriginalBmp);
                    pictureBox1.Image = new Bitmap(OriginalBmp, pictureBox1.ClientSize);
                }
                catch
                {
                    MessageBox.Show("Невозможно открыть выбранный файл", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SizeSet(Button buttonSave, Button buttonOpen, Button buttonApply, PictureBox pictureBox2, PictureBox pictureBox1)
        {
            pictureBox1.Location = new Point(ClientSize.Width / 4 - 150, ClientSize.Height / 4);
            pictureBox2.Location = new Point(3 * ClientSize.Width / 4 - 150, ClientSize.Height / 4);
            buttonApply.Location = new Point(ClientSize.Width / 2 - 50, ClientSize.Height / 3 - ClientSize.Height / 5);
            buttonOpen.Location = new Point(ClientSize.Width / 4 - 150, ClientSize.Height / 18);
            buttonSave.Location = new Point(3 * ClientSize.Width / 4 + 50, ClientSize.Height / 18);
            pictureBox1.Size = new Size(300, 400);
            pictureBox2.Size = new Size(300, 400);
            buttonApply.Size = new Size(100, 40);
            buttonOpen.Size = new Size(100, 40);
            buttonSave.Size = new Size(100, 40);
        }
    }
}

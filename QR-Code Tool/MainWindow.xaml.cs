using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

namespace QRCodeTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnGen_Click(object sender, RoutedEventArgs e)
        {
            if (System.String.IsNullOrEmpty(this.txtMessage.Text))
            {
                MessageBox.Show("编码用的内容字符串不能为空。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            // 生成 QR Code 位图
            var matrix = new MultiFormatWriter().encode(this.txtMessage.Text, BarcodeFormat.QR_CODE, 175, 175);
            var bitmap = new Bitmap(matrix.Width, matrix.Height, PixelFormat.Format32bppArgb);
            for (int x = 0; x < matrix.Width; x++)
                for (int y = 0; y < matrix.Height; y++)
                    bitmap.SetPixel(x, y, matrix[x, y] ? ColorTranslator.FromHtml("0xff000000") : ColorTranslator.FromHtml("0xffffffff"));

            // 转换成图片并显示出来
            var image = new BitmapImage();
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
            }
            this.imgQrcode.Source = image;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var image = this.imgQrcode.Source as BitmapImage;
            if (image == null)
            {
                MessageBox.Show("二维码图片空白，还没未生成图片。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var dlg = new  Microsoft.Win32.SaveFileDialog()
            {
                Filter = "PNG图片|*.png"
            };
            if (dlg.ShowDialog() == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (FileStream fs = File.Open(dlg.FileName, FileMode.OpenOrCreate))
                {
                    encoder.Save(fs);
                }

                MessageBox.Show("保存二维码图片成功。", "操作", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            var image = this.imgQrcode.Source as BitmapImage;
            if (image == null)
            {
                MessageBox.Show("二维码图片空白，还没未生成图片。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var dlg = new System.Windows.Controls.PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                dlg.PrintVisual(this.imgQrcode, "二维码");
            }
        }
    }
}

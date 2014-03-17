﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            if (String.IsNullOrEmpty(this.txtMessage.Text))
            {
                MessageBox.Show("编码用的内容字符串不能为空。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            try
            {
                // 生成 QR Code 位图
                var hints = new Dictionary<EncodeHintType, object>();
                hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
                var matrix = new MultiFormatWriter().encode(this.txtMessage.Text, BarcodeFormat.QR_CODE, (int)this.imgQrcode.Width, (int)this.imgQrcode.Height, hints);
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
            catch (Exception ex)
            {
                this.imgQrcode.Source = null;
                MessageBox.Show(String.Format("生成二维码图片时出错。/r/n错误类型：{0}/r/n错误信息：{1}", ex.GetType(), ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var image = this.imgQrcode.Source as BitmapImage;
            if (image == null)
            {
                MessageBox.Show("二维码图片空白，还未生成图片。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var dlg = new  SaveFileDialog()
            {
                Filter = "PNG图片|*.png"
            };
            if (dlg.ShowDialog() == true)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (FileStream fs = File.Open(dlg.FileName, FileMode.Create, FileAccess.ReadWrite))
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

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "PNG (*.png)|*.png|位图文件(*.bmp;*.dib)|*.bmp;*.dib|JPEG (*.jpg;*.jpeg;*.jpe;*.jfif)|*.jpg;*.jpeg;*.jpe;*.jfif|TIFF (*.tif;*.tiff)|*.tif;*.tiff|所有图片文件|*.png;*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jfif;*.tif;*.tiff|所有文件|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                // 采用先将图片文件内容读入字节数组然后再通过该数组创建图像实例是为了读取图片后图片文件不再会被文件访问锁锁定
                byte[] bytes = null;
                using (var stream = File.Open(dlg.FileName, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var fileInfo = new FileInfo(dlg.FileName);
                    bytes = reader.ReadBytes(unchecked((int)fileInfo.Length));
                    reader.Close();
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(bytes);
                image.EndInit();

                this.imgPhoto.Source = image;
            }
        }

        private void btnAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var image = this.imgPhoto.Source as BitmapImage;
            if (image == null)
            {
                MessageBox.Show("二维码图片空白，还没读取二维码图片。", "操作错误", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            this.txtShow.Text = String.Empty;
            this.txtStandard.Text = "（空）";

            Bitmap bitmap;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bitmap = new Bitmap(ms);
            }

            try
            {
                LuminanceSource source = new BitmapLuminanceSource(bitmap);
                /* 
                 * 在二值化方面，GlobalHistogramBinarizer 提供了比较基础的二值化方法，而 HybridBinarizer 则算是高级的算法，建议要机器性能比较好才使用。
                 * HybridBinarizer 在识别对比度比较低的图片就是比 GlobalHistogramBinarizer 要差；
                 * HybridBinarizer 在光照均匀的情况下，效果比 GlobalHistogramBinarizer 优。
                 */
                // var binarizer = new ZXing.Common.HybridBinarizer(luminance);
                var binarizer = new ZXing.Common.GlobalHistogramBinarizer(source);
                var binBitmap = new BinaryBitmap(binarizer);
                var hints = new Dictionary<DecodeHintType, object>();
                hints.Add(DecodeHintType.CHARACTER_SET, "UTF-8");
                var result = new MultiFormatReader().decode(binBitmap, hints);
                if (result == null)
                {
                    MessageBox.Show("无法正确解析图片。", "错误", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                this.txtShow.Text = result.Text;
                this.txtStandard.Text = result.BarcodeFormat.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("解析图片时出错。\r\n错误类型：{0}\r\n错误信息：{1}", ex.GetType(), ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
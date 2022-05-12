namespace ImageFFT.UI.Web.Pages;

using System.Numerics;
using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed partial class Index
{
  private Image<Rgba32> _img1;
  private Image<Rgba32> _img2;
  private string _img1FileName { get; set; } = "Upload image";
  private string _img1Url { get; set; } = GetDefaultImageString();
  private string _img2Url { get; set; } = GetDefaultImageString();
  private string _text { get; set; } = "Image must be grayscale";

  private async Task LoadFile1(InputFileChangeEventArgs e)
  {
    _img1 = await GetImage(e.File);
    _img1FileName = e.File.Name;
    _text = _img1 is null ? $"<b>{_img1FileName}</b> --> unknown format" : string.Empty;
    _img1Url = await GetImageString(e.File);

    var paddedImg = FFT2D.Padding(_img1);
    var fft = FFT2D.Forward(paddedImg);
    var fftImg = ConvertToImage(fft);
    _img2Url = await GetImageString(fftImg);
  }

  private static async Task<Image<Rgba32>> GetImage(IBrowserFile file)
  {
    var data = file.OpenReadStream();
    var ms = new MemoryStream();
    await data.CopyToAsync(ms);
    ms.Seek(0, SeekOrigin.Begin);

    var info = await Image.IdentifyAsync(ms);
    if (info is null)
    {
      return null;
    }

    ms.Seek(0, SeekOrigin.Begin);
    return Image.Load<Rgba32>(ms);
  }

  private static async Task<string> GetImageString(IBrowserFile file)
  {
    var buffers = new byte[file.Size];
    await file.OpenReadStream().ReadAsync(buffers);
    return $"data:{file.ContentType};base64,{Convert.ToBase64String(buffers)}";
  }

  private static async Task<string> GetImageString(Image<Rgba32> img)
  {
    using var ms = new MemoryStream();
    img.SaveAsPng(ms);
    var bytes = ms.ToArray();
    return $"data:img/png;base64,{Convert.ToBase64String(bytes)}";
  }

  private static string GetDefaultImageString(int width = 64, int height = 64)
  {
    var img = new Image<Rgba32>(Configuration.Default, width, height);
    using var ms = new MemoryStream();
    img.SaveAsPng(ms);
    var bytes = ms.ToArray();
    return $"data:img/png;base64,{Convert.ToBase64String(bytes)}";
  }

  private static Image<Rgba32> ConvertToImage(Complex[][] transform)
  {
    var size = transform.Length;

    var absTransform = new double[size, size];
    for (var i = 0; i < size; i++)
    {
      for (var j = 0; j < size; j++)
      {
        absTransform[i, j] = Complex.Abs(transform[i][j]);
      }
    }

    var max = 0.0;
    for (var i = 0; i < size; i++)
    {
      for (var j = 0; j < size; j++)
      {
        if (max < absTransform[i, j])
        {
          max = absTransform[i, j];
        }
      }
    }

    var image = new Image<Rgba32>(size, size);
    image.ProcessPixelRows(acc =>
    {
      for (var l = 0; l < acc.Height; l++)
      {
        var pxRow = acc.GetRowSpan(l);
        for (var k = 0; k < pxRow.Length; k++)
        {
          var pixelValue = Math.Log10(1 + absTransform[l, k]) * 255 / Math.Log10(1 + max);

          if (!double.IsNaN(pixelValue))
          {
            var gray = (byte)pixelValue;
            ref var px = ref pxRow[k];
            px.R = px.G = px.B = gray;
            px.A = Byte.MaxValue;
          }
        }
      }
    });
    return image;
  }
}

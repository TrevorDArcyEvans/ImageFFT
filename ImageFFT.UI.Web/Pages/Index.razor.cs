namespace ImageFFT.UI.Web.Pages;

using System.Numerics;
using Microsoft.AspNetCore.Components.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed partial class Index
{

  private static void SaveAsPng(Complex[][] transform, string path)
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
    image.SaveAsPng(path);
  }
}

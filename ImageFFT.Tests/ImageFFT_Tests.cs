namespace ImageFFT.Tests;

using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageFFT_Tests
{
    [Test]
    public void RoundTrip_Succeeds()
    {
        var baseImg = Image.Load<Rgba32>("images/lena.png");

        var fft = FFT2D.Forward(baseImg);

        var invImg = FFT2D.Inverse(fft);
        invImg.SaveAsPng("InvImg.png");
    }
}
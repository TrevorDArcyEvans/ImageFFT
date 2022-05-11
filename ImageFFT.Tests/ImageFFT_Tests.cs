namespace ImageFFT.Tests;

using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageFFT_Tests
{
  [Test]
  public void RoundTrip_Succeeds()
  {
    var baseImg = Image.Load<Rgba32>("images/lena.png");
    var baseBytes = new byte[baseImg.Width * baseImg.Height * Unsafe.SizeOf<Rgba32>()];
    baseImg.CopyPixelDataTo(baseBytes);

    var fft = FFT2D.Forward(baseImg);

    var invImg = FFT2D.Inverse(fft);
    var invBytes = new byte[invImg.Width * invImg.Height * Unsafe.SizeOf<Rgba32>()];
    invImg.CopyPixelDataTo(invBytes);

    invImg.Width.Should().Be(baseImg.Width);
    invImg.Height.Should().Be(baseImg.Height);
    invBytes.Length.Should().Be(baseBytes.Length);

    // looks like round trip alters alpha (?), so compare within a tolerance
    invBytes.Should().Equal(baseBytes, (left, right) => Math.Abs(left - right) <= 1);
  }
}

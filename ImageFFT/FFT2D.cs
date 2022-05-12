namespace ImageFFT;

using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// stolen from:
///     https://epochabuse.com/fourier-transform/
///     Copyright (c) 2020 Andraz Krzisnik
///
/// NOTE:   all images must be grayscale
/// </summary>
public static class FFT2D
{
    public static Complex[][] ToComplex(Image<Rgba32> image)
    {
        var w = image.Width;
        var h = image.Height;
        var result = new Complex[w][];
        foreach (var x in Enumerable.Range(0, w))
        {
            result[x] = new Complex[h];
        }

        image.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var pxRow = acc.GetRowSpan(y);
                for (var x = 0; x < pxRow.Length; x++)
                {
                    ref var px = ref pxRow[x];

                    // https://en.wikipedia.org/wiki/Grayscale
                    var gray = (29.9 * px.R + 58.7 * px.G + 11.4 * px.B) / 100;

                    result[x][y] = new Complex(gray, 0);
                }
            }
        });

        return result;
    }

    public static Complex[] Forward(Complex[] input, bool phaseShift = true)
    {
        var size = input.Length;
        var result = new Complex[input.Length];
        var omega = (float)(-2.0 * Math.PI / input.Length);

        if (input.Length == 1)
        {
            result[0] = input[0];

            if (Complex.IsNaN(result[0]))
            {
                return new[] { new Complex(0, 0) };
            }

            return result;
        }

        var evenInput = new Complex[input.Length / 2];
        var oddInput = new Complex[input.Length / 2];

        for (var i = 0; i < input.Length / 2; i++)
        {
            evenInput[i] = input[2 * i];
            oddInput[i] = input[2 * i + 1];
        }

        var even = Forward(evenInput, phaseShift);
        var odd = Forward(oddInput, phaseShift);

        for (var k = 0; k < input.Length / 2; k++)
        {
            int phase;

            if (phaseShift)
            {
                phase = k - size / 2;
            }
            else
            {
                phase = k;
            }

            odd[k] *= Complex.FromPolarCoordinates(1, omega * phase);
        }

        for (var k = 0; k < input.Length / 2; k++)
        {
            result[k] = even[k] + odd[k];
            result[k + input.Length / 2] = even[k] - odd[k];
        }

        return result;
    }

    public static Complex[][] Forward(Image<Rgba32> image)
    {
        if (image.Width != image.Height)
        {
            throw new ArgumentOutOfRangeException($"Image width ({image.Width}) must be same as image height ({image.Height})");
        }

        var size = image.Width;
        var p = new Complex[size][];
        var f = new Complex[size][];
        var t = new Complex[size][];

        var complexImage = ToComplex(image);

        for (var l = 0; l < size; l++)
        {
            p[l] = Forward(complexImage[l]);
        }

        for (var l = 0; l < size; l++)
        {
            t[l] = new Complex[size];
            for (var k = 0; k < size; k++)
            {
                t[l][k] = p[k][l];
            }

            f[l] = Forward(t[l]);
        }

        return f;
    }

    public static Image<Rgba32> Padding(this Image<Rgba32> image)
    {
        var size = 0;
        var w = image.Width;
        var h = image.Height;
        var n = 0;
        while (size <= Math.Max(w, h))
        {
            size = (int)Math.Pow(2, n);
            if (size == Math.Max(w, h))
            {
                break;
            }

            n++;
        }

        var padded_image = image.Clone(img => img.Pad(size, size, Color.White));

        return padded_image;
    }

    public static Complex[] Inverse(Complex[] input)
    {
        for (var i = 0; i < input.Length; i++)
        {
            input[i] = Complex.Conjugate(input[i]);
        }

        var transform = Forward(input, false);

        for (var i = 0; i < input.Length; i++)
        {
            transform[i] = Complex.Conjugate(transform[i]);
        }

        return transform;
    }

    public static Image<Rgba32> Inverse(Complex[][] frequencies)
    {
        var size = frequencies.Length;
        var p = new Complex[size][];
        var f = new Complex[size][];
        var t = new Complex[size][];

        for (var i = 0; i < size; i++)
        {
            p[i] = Inverse(frequencies[i]);
        }

        for (var i = 0; i < size; i++)
        {
            t[i] = new Complex[size];
            for (var j = 0; j < size; j++)
            {
                t[i][j] = p[j][i] / (size * size);
            }

            f[i] = Inverse(t[i]);
        }

        var image = new Image<Rgba32>(size, size);
        image.ProcessPixelRows(acc =>
        {
            for (var y = 0; y < acc.Height; y++)
            {
                var pxRow = acc.GetRowSpan(y);
                for (var x = 0; x < pxRow.Length; x++)
                {
                    ref var px = ref pxRow[x];
                    var gray = (byte)Complex.Abs(f[x][y]);
                    px.R = px.G = px.B = gray;
                    px.A = Byte.MaxValue;
                }
            }
        });

        return image;
    }
}
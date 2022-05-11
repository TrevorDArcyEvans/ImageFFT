namespace ImageFFT;

using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

/// <summary>
/// stolen from:
///     https://epochabuse.com/fourier-transform/
///     Copyright (c) 2020 Andraz Krzisnik
/// </summary>
public static class FFT2D
{
    public static Complex[][] ToComplex(Bitmap image)
    {
        var w = image.Width;
        var h = image.Height;

        var input_data = image.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var bytes = input_data.Stride * input_data.Height;

        var buffer = new byte[bytes];
        var result = new Complex[w][];

        Marshal.Copy(input_data.Scan0, buffer, 0, bytes);
        image.UnlockBits(input_data);

        int pixel_position;

        for (var x = 0; x < w; x++)
        {
            result[x] = new Complex[h];
            for (var y = 0; y < h; y++)
            {
                pixel_position = y * input_data.Stride + x * 4;
                result[x][y] = new Complex(buffer[pixel_position], 0);
            }
        }

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

    public static Complex[][] Forward(Bitmap image)
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

    public static Bitmap Padding(this Bitmap image)
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

        double horizontal_padding = size - w;
        double vertical_padding = size - h;
        var left_padding = (int)Math.Floor(horizontal_padding / 2);
        var top_padding = (int)Math.Floor(vertical_padding / 2);

        var image_data = image.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var bytes = image_data.Stride * image_data.Height;
        var buffer = new byte[bytes];
        Marshal.Copy(image_data.Scan0, buffer, 0, bytes);
        image.UnlockBits(image_data);

        var padded_image = new Bitmap(size, size);

        var padded_data = padded_image.LockBits(
            new Rectangle(0, 0, size, size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        var padded_bytes = padded_data.Stride * padded_data.Height;
        var result = new byte[padded_bytes];

        for (var i = 3; i < padded_bytes; i += 4)
        {
            result[i] = 255;
        }

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var image_position = y * image_data.Stride + x * 4;
                var padding_position = y * padded_data.Stride + x * 4;
                for (var i = 0; i < 3; i++)
                {
                    result[padded_data.Stride * top_padding + 4 * left_padding + padding_position + i] = buffer[image_position + i];
                }
            }
        }

        Marshal.Copy(result, 0, padded_data.Scan0, padded_bytes);
        padded_image.UnlockBits(padded_data);

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

    public static Bitmap Inverse(Complex[][] frequencies)
    {
        var size = frequencies.Length;
        var p = new Complex[size][];
        var f = new Complex[size][];
        var t = new Complex[size][];

        var image = new Bitmap(size, size);
        var image_data = image.LockBits(
            new Rectangle(0, 0, size, size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        var bytes = image_data.Stride * image_data.Height;
        var result = new byte[bytes];

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

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixel_position = y * image_data.Stride + x * 4;
                for (var i = 0; i < 3; i++)
                {
                    result[pixel_position + i] = (byte)Complex.Abs(f[x][y]);
                }

                result[pixel_position + 3] = 255;
            }
        }

        Marshal.Copy(result, 0, image_data.Scan0, bytes);
        image.UnlockBits(image_data);
        return image;
    }
}
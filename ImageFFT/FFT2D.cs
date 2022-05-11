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
        int w = image.Width;
        int h = image.Height;

        BitmapData input_data = image.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        int bytes = input_data.Stride * input_data.Height;

        byte[] buffer = new byte[bytes];
        Complex[][] result = new Complex[w][];

        Marshal.Copy(input_data.Scan0, buffer, 0, bytes);
        image.UnlockBits(input_data);

        int pixel_position;

        for (int x = 0; x < w; x++)
        {
            result[x] = new Complex[h];
            for (int y = 0; y < h; y++)
            {
                pixel_position = y * input_data.Stride + x * 4;
                result[x][y] = new Complex(buffer[pixel_position], 0);
            }
        }

        return result;
    }

    public static Complex[] Forward(Complex[] input, bool phaseShift = true)
    {
        var Size = input.Length;
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

        for (int i = 0; i < input.Length / 2; i++)
        {
            evenInput[i] = input[2 * i];
            oddInput[i] = input[2 * i + 1];
        }

        var even = Forward(evenInput, phaseShift);
        var odd = Forward(oddInput, phaseShift);

        for (int k = 0; k < input.Length / 2; k++)
        {
            int phase;

            if (phaseShift)
            {
                phase = k - Size / 2;
            }
            else
            {
                phase = k;
            }

            odd[k] *= Complex.FromPolarCoordinates(1, omega * phase);
        }

        for (int k = 0; k < input.Length / 2; k++)
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

        var Size = image.Width;
        var p = new Complex[Size][];
        var f = new Complex[Size][];
        var t = new Complex[Size][];

        var complexImage = ToComplex(image);

        for (int l = 0; l < Size; l++)
        {
            p[l] = Forward(complexImage[l]);
        }

        for (int l = 0; l < Size; l++)
        {
            t[l] = new Complex[Size];
            for (int k = 0; k < Size; k++)
            {
                t[l][k] = p[k][l];
            }

            f[l] = Forward(t[l]);
        }

        return f;
    }

    public static Bitmap Padding(this Bitmap image)
    {
        var Size = 0;
        int w = image.Width;
        int h = image.Height;
        int n = 0;
        while (Size <= Math.Max(w, h))
        {
            Size = (int)Math.Pow(2, n);
            if (Size == Math.Max(w, h))
            {
                break;
            }

            n++;
        }

        double horizontal_padding = Size - w;
        double vertical_padding = Size - h;
        int left_padding = (int)Math.Floor(horizontal_padding / 2);
        int top_padding = (int)Math.Floor(vertical_padding / 2);

        BitmapData image_data = image.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        int bytes = image_data.Stride * image_data.Height;
        byte[] buffer = new byte[bytes];
        Marshal.Copy(image_data.Scan0, buffer, 0, bytes);
        image.UnlockBits(image_data);

        Bitmap padded_image = new Bitmap(Size, Size);

        BitmapData padded_data = padded_image.LockBits(
            new Rectangle(0, 0, Size, Size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        int padded_bytes = padded_data.Stride * padded_data.Height;
        byte[] result = new byte[padded_bytes];

        for (int i = 3; i < padded_bytes; i += 4)
        {
            result[i] = 255;
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int image_position = y * image_data.Stride + x * 4;
                int padding_position = y * padded_data.Stride + x * 4;
                for (int i = 0; i < 3; i++)
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
        for (int i = 0; i < input.Length; i++)
        {
            input[i] = Complex.Conjugate(input[i]);
        }

        var transform = Forward(input, false);

        for (int i = 0; i < input.Length; i++)
        {
            transform[i] = Complex.Conjugate(transform[i]);
        }

        return transform;
    }

    public static Bitmap Inverse(Complex[][] frequencies)
    {
        var Size = frequencies.Length;
        var p = new Complex[Size][];
        var f = new Complex[Size][];
        var t = new Complex[Size][];

        Bitmap image = new Bitmap(Size, Size);
        BitmapData image_data = image.LockBits(
            new Rectangle(0, 0, Size, Size),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        int bytes = image_data.Stride * image_data.Height;
        byte[] result = new byte[bytes];

        for (int i = 0; i < Size; i++)
        {
            p[i] = Inverse(frequencies[i]);
        }

        for (int i = 0; i < Size; i++)
        {
            t[i] = new Complex[Size];
            for (int j = 0; j < Size; j++)
            {
                t[i][j] = p[j][i] / (Size * Size);
            }

            f[i] = Inverse(t[i]);
        }

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int pixel_position = y * image_data.Stride + x * 4;
                for (int i = 0; i < 3; i++)
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
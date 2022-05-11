# ImageFFT
Based on code by [Andraz Krzisnik](https://www.linkedin.com/in/andraz-krzisnik-517146128/):

[How To Use Fourier Transform On Images](https://epochabuse.com/fourier-transform/)

# Features
* runs on .NET Core
* supports Linux
* webassembly compatible

# Getting started

<details>

```bash
git clone https://github.com/TrevorDArcyEvans/ImageFFT.git
cd ImageFFT/
dotnet restore
dotnet build
dotnet test
```

</details>

# Discussion
This library provides functions to perform a forward and inverse
[Fast Fourier Transform (FFT)](https://en.wikipedia.org/wiki/Fast_Fourier_transform)
on an image file.

The general restriction on FFT of an image is that the image must be square
and its dimensions must be a power of 2.  If the image does not meet these
restrictions, the library has functions to pad it out so that it does.

# Further work
* ~~port to [ImageSharp](https://github.com/SixLabors/ImageSharp)~~
* ~~unit tests~~

# Further information
* [Articles by Andraz Krzisnik](https://epochabuse.com/author/andrson311/)
  * **lots** of **really** good articles (including code) on image processing 

# Acknowledgements
* [How To Use Fourier Transform On Images](https://epochabuse.com/fourier-transform/)

# PhotoSharp

An application with simple and clear controls for processing your images, based on [LeonidLodygin.ImageProcessing](https://github.com/LeonidLodygin/ImageProcessing)

## Features
* Apply filters and modifications to your image
* Observe immediately as the image changes
* Process a large number of images at once
* Utilize the power of your graphics card to speed up processing

![image](https://raw.githubusercontent.com/LeonidLodygin/PhotoSharp/dev/tmp/image.png)

## Quick start

Clone this repository or download ZIP:
```sh
> git clone https://github.com/LeonidLodygin/PhotoSharp.git
```

Go to the repository directory and download the package:
```sh
> wget https://github.com/LeonidLodygin/ImageProcessing/releases/download/v1.0.0/leonidlodygin.imageprocessing.1.0.0.nupkg
```

Add the project root to the Nuget package source:
```sh
> dotnet nuget add source $(pwd)
```

Run the app:
```sh
> dotnet run
```


## Contributors
- Leonid Lodygin (Github: [@LeonidLodygin](https://github.com/LeonidLodygin))

## License

This project licensed under MIT License. License text can be found in the
[license file](https://github.com/LeonidLodygin/PhotoSharp/blob/main/LICENSE.md).
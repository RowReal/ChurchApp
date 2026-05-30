namespace ChurchApp.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

public class ImageOptimizationService
{
    public async Task<string> CreateMobileVersion(string inputPath, string outputPath)
    {
        using var image = await Image.LoadAsync(inputPath);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(400, 400), // mobile friendly
            Mode = ResizeMode.Max
        }));

        await image.SaveAsWebpAsync(outputPath, new WebpEncoder
        {
            Quality = 75
        });

        return outputPath;
    }
}
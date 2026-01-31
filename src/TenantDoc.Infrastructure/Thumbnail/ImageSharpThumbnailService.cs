using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using TenantDoc.Core.Interfaces;

namespace TenantDoc.Infrastructure.Thumbnail;

public class ImageSharpThumbnailService : IThumbnailService
{
    public async Task<string> GenerateThumbnailAsync(string imagePath, int width, int height)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            throw new FileNotFoundException("Source image not found", imagePath);
        }

        using var image = await Image.LoadAsync(imagePath);

        // Resize maintaining aspect ratio
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));

        // Generate thumbnail path with -thumb suffix
        var directory = Path.GetDirectoryName(imagePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Unable to determine directory from image path");
        }

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(imagePath);
        var thumbnailPath = Path.Combine(directory, $"{fileNameWithoutExt}-thumb.jpg");

        // Save as JPEG with 80% quality
        await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 80 });

        return thumbnailPath;
    }
}

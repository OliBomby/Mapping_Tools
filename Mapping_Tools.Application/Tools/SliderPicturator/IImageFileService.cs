using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Core.Tools.SliderPicturator;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Provides decoded source images without leaking bitmap-library types into the application.</summary>
public interface IImageFileService
{
    /// <summary>Loads a local image file into a framework-neutral pixel buffer.</summary>
    /// <param name="path">The local image path.</param>
    /// <param name="cancellationToken">Cancels before or during decoding.</param>
    /// <returns>The decoded RGBA image.</returns>
    Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default);
}


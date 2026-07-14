using System.Runtime.InteropServices;
using Game.Shared.Host;
using Game.Shared.Host.Rendering;
using Game.Shared.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.Desktop.MonoGame;

/// <summary>
///     Presents the game's RGBA screen through MonoGame.
/// </summary>
/// <param name="graphicsDevice">Graphics device used for presentation.</param>
/// <param name="presentSurfaceIsRgba32">Whether the presented surface is already RGBA32.</param>
public sealed class MonoGameScreenPresenter(GraphicsDevice graphicsDevice, bool presentSurfaceIsRgba32) : IRenderBackend
{
    private const int VgaPresentationHeightNumerator = 6;
    private const int VgaPresentationHeightDenominator = 5;
    private readonly SpriteBatch _spriteBatch = new(graphicsDevice);
    private Color[]? _fullSurfaceUploadBuffer;
    private Texture2D? _presentTexture;
    private bool _presentTextureInvalidated = true;
    private Color[]? _regionUploadBuffer;

    /// <summary>
    ///     Releases the presentation resources owned by this backend.
    /// </summary>
    public void Dispose()
    {
        _spriteBatch.Dispose();
        _presentTexture?.Dispose();
    }

    /// <summary>
    ///     Presents the supplied screen.
    /// </summary>
    /// <param name="screen">The screen to present.</param>
    /// <param name="rect">Current host presentation rect.</param>
    public void Present(Screen screen, HostPresentationRect rect)
    {
        EnsurePresentTextureResources(screen);

        if (_presentTextureInvalidated || screen.DirtyRegions.IsFull)
        {
            UploadFullPresentSurface(screen);
            _presentTextureInvalidated = false;
        }
        else if (screen.DirtyRegions.HasAny)
        {
            foreach (var region in screen.DirtyRegions.Regions)
            {
                UploadPresentSurfaceRegion(screen, region);
            }
        }

        var destination = new Rectangle(rect.ContentX, rect.ContentY, rect.ContentWidth, rect.ContentHeight);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_presentTexture, destination, Color.White);
        _spriteBatch.End();
    }

    /// <summary>
    ///     Computes the presentation rect for one screen in the current viewport.
    /// </summary>
    /// <param name="viewport">Current viewport.</param>
    /// <param name="screen">Screen being presented.</param>
    public static HostPresentationRect ComputePresentationRect(Viewport viewport, Screen screen)
    {
        // The game's 320x200 VGA art was meant for a 4:3 display, so we apply the usual 6:5 vertical stretch before
        // fitting it into the current viewport.
        var vgaCorrectedHeight =
            checked(screen.Height * VgaPresentationHeightNumerator / VgaPresentationHeightDenominator);
        var scale = Math.Max(1, Math.Min(viewport.Width / screen.Width, viewport.Height / vgaCorrectedHeight));
        var destinationWidth = checked(screen.Width * scale);
        var destinationHeight = checked(vgaCorrectedHeight * scale);

        return new HostPresentationRect(
            (viewport.Width - destinationWidth) / 2,
            (viewport.Height - destinationHeight) / 2,
            destinationWidth,
            destinationHeight);
    }

    private void EnsurePresentTextureResources(Screen screen)
    {
        if (_presentTexture is not null && _presentTexture.Width == screen.Width &&
            _presentTexture.Height == screen.Height)
        {
            return;
        }

        _presentTexture?.Dispose();
        _presentTexture = new Texture2D(graphicsDevice, screen.Width, screen.Height, false, SurfaceFormat.Color);
        _fullSurfaceUploadBuffer = new Color[screen.Width * screen.Height];
        _presentTextureInvalidated = true;
    }

    private void EnsureRegionBuffer(int pixelCount)
    {
        if (_regionUploadBuffer is null || _regionUploadBuffer.Length < pixelCount)
        {
            _regionUploadBuffer = new Color[pixelCount];
        }
    }

    private void UploadFullPresentSurface(Screen screen)
    {
        var pixels = screen.PresentSurface.GetReadOnlyPixelSpan();
        var colorBuffer = _fullSurfaceUploadBuffer!;
        if (presentSurfaceIsRgba32)
        {
            pixels.CopyTo(MemoryMarshal.AsBytes(colorBuffer.AsSpan()));
        }
        else
        {
            CopyPixelsToColors(pixels, colorBuffer);
        }

        _presentTexture!.SetData(colorBuffer);
    }

    private void UploadPresentSurfaceRegion(Screen screen, IntRect region)
    {
        var pixelCount = region.Width * region.Height;
        EnsureRegionBuffer(pixelCount);
        var regionBuffer = _regionUploadBuffer!;

        var pixels = screen.PresentSurface.GetReadOnlyPixelSpan();

        if (presentSurfaceIsRgba32)
        {
            CopyRegionBytesToColors(pixels,
                screen.PresentSurface.Stride,
                screen.PresentSurface.BytesPerPixel,
                region,
                regionBuffer.AsSpan(0, pixelCount));
        }
        else
        {
            CopyRegionPixelsToColors(pixels,
                screen.PresentSurface.Stride,
                screen.PresentSurface.BytesPerPixel,
                region,
                regionBuffer);
        }

        _presentTexture!.SetData(0,
            new Rectangle(region.X, region.Y, region.Width, region.Height),
            regionBuffer,
            0,
            pixelCount);
    }

    private static void CopyPixelsToColors(ReadOnlySpan<byte> pixels, Span<Color> colors)
    {
        for (var i = 0; i < colors.Length; i++)
        {
            var offset = i * 4;
            colors[i] = new Color(pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);
        }
    }

    private static void CopyRegionBytesToColors(ReadOnlySpan<byte> pixels,
        int stride,
        int bytesPerPixel,
        IntRect region,
        Span<Color> colors)
    {
        var rowByteCount = checked(region.Width * bytesPerPixel);
        var colorBytes = MemoryMarshal.AsBytes(colors);
        var colorByteOffset = 0;

        for (var row = region.Y; row < region.Bottom; row++)
        {
            var rowStart = row * stride + region.X * bytesPerPixel;
            pixels.Slice(rowStart, rowByteCount).CopyTo(colorBytes.Slice(colorByteOffset, rowByteCount));
            colorByteOffset += rowByteCount;
        }
    }

    private static void CopyRegionPixelsToColors(ReadOnlySpan<byte> pixels,
        int stride,
        int bytesPerPixel,
        IntRect region,
        Span<Color> colors)
    {
        var bufferIndex = 0;
        for (var row = region.Y; row < region.Bottom; row++)
        {
            var rowStart = row * stride + region.X * bytesPerPixel;
            for (var column = 0; column < region.Width; column++)
            {
                var offset = rowStart + column * bytesPerPixel;
                colors[bufferIndex++] = new Color(pixels[offset],
                    pixels[offset + 1],
                    pixels[offset + 2],
                    pixels[offset + 3]);
            }
        }
    }
}

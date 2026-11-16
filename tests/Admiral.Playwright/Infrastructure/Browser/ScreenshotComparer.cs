namespace Admiral.Playwright.Infrastructure.Browser;

internal static class ScreenshotComparer
{
    public static Task<PixelDifference?> CompareAsync(IPage page, byte[] before, byte[] after) =>
        page.ReadSnapshotAsync<PixelDifference?>(
            """
        async (images) => {
          const bitmaps = await Promise.all(
            images.map((image) =>
              createImageBitmap(
                new Blob([Uint8Array.from(atob(image), (character) => character.charCodeAt(0))], {
                  type: 'image/png',
                }),
              ),
            ),
          );
          const [{ width, height }, second] = bitmaps;
          if (second.width !== width || second.height !== height) return null;
          const canvas = new OffscreenCanvas(width, height);
          const context = canvas.getContext('2d', { willReadFrequently: true });
          const [firstPixels, secondPixels] = bitmaps.map((bitmap) => {
            context.clearRect(0, 0, width, height);
            context.drawImage(bitmap, 0, 0);
            return context.getImageData(0, 0, width, height).data;
          });
          let changed = 0;
          for (let i = 0; i < firstPixels.length; i += 4) {
            if (
              [0, 1, 2].some(
                (channel) => Math.abs(firstPixels[i + channel] - secondPixels[i + channel]) > 32,
              )
            ) {
              changed++;
            }
          }
          return { changed, ratio: changed / (width * height) };
        }
        """,
            new[] { Convert.ToBase64String(before), Convert.ToBase64String(after) });
}

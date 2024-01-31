using Newtonsoft.Json.Linq;
using NetVips;


namespace SkylabStudio
{
    public partial class StudioClient
    {
        private async Task<List<Image>?> DownloadBgImages(dynamic profile)
        {
            var httpClient = new HttpClient();

            List<Image> tempBgs = new List<Image>();
            List<JToken> bgPhotos = ((JArray) profile!.photos).Where(photo => photo["jobId"] != null).ToList();

            foreach (dynamic bg in bgPhotos)
            {
                byte[] bgBuffer = await DownloadImageAsync(bg.originalUrl.Value);
                Image bgImage = Image.NewFromBuffer(bgBuffer);
                tempBgs.Add(bgImage);
            }

            return tempBgs;
            
        }

        private static async Task<byte[]?> DownloadImageAsync(string imageUrl)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Download the image into a byte array
                    byte[] imageBuffer = await httpClient.GetByteArrayAsync(imageUrl);

                    return imageBuffer;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> DownloadReplacedBackgroundImage(string fileName, Image inputImage, string outputPath, dynamic? profile = null, List<Image>? bgs = null)
        {
            try
            {
                string outputFileType = profile?.outputFileType?.Value ?? "png";

                if (bgs == null && profile?.photos?.Count > 0) {
                    bgs = await DownloadBgImages(profile);
                }

                Image alphaChannel = inputImage.ExtractBand(3);
                Image rgbChannel = inputImage.ExtractBand(0, 3);
                Image rgbCutout = rgbChannel.Bandjoin(alphaChannel);

                if (bgs != null && bgs.Count > 0){
                    for (int i = 0; i < bgs.Count; i++) {
                        string newFileName = i == 0 ? $"{Path.GetFileNameWithoutExtension(fileName)}.{outputFileType}": $"{Path.GetFileNameWithoutExtension(fileName)} ({i+1}).{outputFileType}";
                        Image resizedBgImage = bgs[i].ThumbnailImage(inputImage.Width, inputImage.Height, crop: Enums.Interesting.Centre);
                        Image resultImage = resizedBgImage.Composite2(rgbCutout, Enums.BlendMode.Over);
                        resultImage.WriteToFile(Path.Combine(outputPath, newFileName));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading background image: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DownloadAllPhotos(JArray photosList, dynamic profile, string outputPath)
        {
            try {
                profile = await GetProfile(profile.id.Value);
                List<Image> bgs = await DownloadBgImages(profile);

                var httpClient = new HttpClient();

                List<string> photoIds = photosList.Select(photo => photo?["id"]?.ToString() ?? "").ToList() ?? new List<string>();

                // Use a semaphore to control access to the download operation
                var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
                List<Task> downloadTasks = new List<Task>();
                foreach (string photoId in photoIds)
                {
                    downloadTasks.Add(DownloadPhoto(long.Parse(photoId), outputPath, profile, null, semaphore));
                }

                // Wait for all download tasks to complete
                await Task.WhenAll(downloadTasks);

                return true;
            } catch (Exception _e) {
                Console.WriteLine(_e);
                return false;
            }
        }
        public async Task<bool> DownloadPhoto(long photoId,  string outputPath, dynamic? profile = null, dynamic? options = null, SemaphoreSlim? semaphore = null)
        {
            try {
                if (semaphore != null) await semaphore.WaitAsync(); // Wait until a slot is available

                dynamic photo = await GetPhoto(photoId);
                long profileId = photo.job.profileId;

                string fileName = photo.name.Value;

                if (profile == null) {
                    profile = await GetProfile(profileId);
                }
                bool isExtract = Convert.ToBoolean(profile.enableExtract.Value);
                bool replaceBackground = Convert.ToBoolean(profile.enableExtract.Value);
                bool isDualFileOutput = Convert.ToBoolean(profile.dualFileOutput.Value);
                bool enableStripPngMetadata = Convert.ToBoolean(profile.enableStripPngMetadata.Value);
                List<Image>? bgs = options?.bgs;

                // Load output image 
                byte[] imageBuffer = await DownloadImageAsync(photo.retouchedUrl.Value);
                Image image = Image.NewFromBuffer(imageBuffer);
            
                if (isExtract) { // Output extract image
                    string pngFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.png";

                    // Dual File Output will provide an image in the format specified in the outputFileType field
                    // and an extracted image as a PNG.
                    if (isDualFileOutput) {
                        image.WriteToFile(Path.Combine(outputPath, pngFileName));
                    }

                    if (replaceBackground) {
                        await DownloadReplacedBackgroundImage(fileName, image, outputPath, profile, bgs);
                    }

                    // Regular Extract output
                    if (!isDualFileOutput) image.WriteToFile(Path.Combine(outputPath, pngFileName));
                } else { // Non-extracted regular image output
                    image.WriteToFile(Path.Combine(outputPath, fileName));
                }

                Console.WriteLine($"Successfully downloaded: {fileName}");
                return true;
            } catch (Exception _e)
            {
                Console.WriteLine($"Failed to download photo id: {photoId}");
                Console.WriteLine(_e);
                return false;
            }
        }
    }
}
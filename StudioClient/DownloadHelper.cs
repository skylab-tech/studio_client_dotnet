using Newtonsoft.Json.Linq;
using NetVips;
using RestSharp;


namespace SkylabStudio
{
    public class PhotoOptions
    {
        public List<Image>? Bgs { get; set; }

        // Default constructor (parameterless)
        public PhotoOptions()
        {
            Bgs = new List<Image>();
        }
    }

    public class DownloadAllPhotosResult
    {
        public List<string> SuccessPhotos { get; set; }
        public List<string> ErroredPhotos { get; set; }

        // Default constructor (parameterless)
        public DownloadAllPhotosResult()
        {
            SuccessPhotos = new List<string>();
            ErroredPhotos = new List<string>();
        }
    }
    public partial class StudioClient
    {
        private async Task<List<Image>?> DownloadBgImages(dynamic profile)
        {
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
            if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                throw new Exception($"Invalid retouchedUrl: \"{imageUrl}\" - Please ensure the job is complete");
            }

            try
            {
                using (RestClient httpClient = new RestClient())
                {
                    RestRequest request = new RestRequest(imageUrl, Method.Get);
                    // Download the image into a byte array
                    RestResponse response = await httpClient.ExecuteAsync(request);

                    byte[] imageBuffer = response.RawBytes ?? Array.Empty<byte>();

                    return imageBuffer;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error downloading image: {ex.Message}");
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
                Console.Error.WriteLine($"Error downloading background image: {ex.Message}");
                return false;
            }
        }

        public async Task<DownloadAllPhotosResult> DownloadAllPhotos(JArray photosList, dynamic profile, string outputPath)
        {
            if (!Directory.Exists(outputPath))
            {
                throw new Exception("Invalid output path");
            }

            List<string> successPhotos = new List<string>();
            List<string> erroredPhotos = new List<string>();
            List<Image> bgs = new List<Image>();

            try {
                profile = await GetProfile(profile.id.Value);
                if (profile?.photos?.Count > 0) {
                    bgs = await DownloadBgImages(profile);
                }

                List<string> photoIds = photosList.Select(photo => photo?["id"]?.ToString() ?? "").ToList() ?? new List<string>();

                // Use a semaphore to control access to the download operation
                var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
                List<Task<Tuple<string, bool>>> downloadTasks = new List<Task<Tuple<string, bool>>>();
                PhotoOptions photoOptions = new PhotoOptions
                {
                    Bgs = bgs
                };
                foreach (string photoId in photoIds)
                {
                    downloadTasks.Add(DownloadPhoto(long.Parse(photoId), outputPath, profile, photoOptions, semaphore));
                }

                // Wait for all download tasks to complete
                IEnumerable<Tuple<string, bool>> results = await Task.WhenAll(downloadTasks);

                foreach (var result in results)
                {
                    if (result.Item2) {
                        successPhotos.Add(result.Item1);
                    } else {
                        erroredPhotos.Add(result.Item1);
                    }
                }

                DownloadAllPhotosResult downloadResults = new DownloadAllPhotosResult {
                    SuccessPhotos = successPhotos,
                    ErroredPhotos = erroredPhotos
                };

                return downloadResults;
            } catch (Exception _e) {
                Console.Error.WriteLine(_e);

                DownloadAllPhotosResult downloadResults = new DownloadAllPhotosResult {
                    SuccessPhotos = successPhotos,
                    ErroredPhotos = erroredPhotos
                };

                return downloadResults;
            }
        }
        public async Task<Tuple<string,bool>> DownloadPhoto(long photoId,  string outputPath, dynamic? profile = null, PhotoOptions? options = null, SemaphoreSlim? semaphore = null)
        {
            string fileName = "";

            if (!Directory.Exists(outputPath))
            {
                // Must be a file path - separate outputPath and fileName
                fileName = Path.GetFileName(outputPath);
                outputPath = Path.GetDirectoryName(outputPath) ?? "";
            }

            if (semaphore != null) await semaphore.WaitAsync(); // Wait until a slot is available

            try {
                var photo = await GetPhoto(photoId);
                long profileId = photo.job.profileId;

                if (fileName.Length <= 0) fileName = photo.name.Value;

                if (profile == null) {
                    profile = await GetProfile(profileId);
                }
                bool isExtract = Convert.ToBoolean(profile.enableExtract.Value);
                bool replaceBackground = Convert.ToBoolean(profile.replaceBackground.Value);
                bool isDualFileOutput = Convert.ToBoolean(profile.dualFileOutput.Value);
                bool enableStripPngMetadata = Convert.ToBoolean(profile.enableStripPngMetadata.Value);
                List<Image>? bgs = options?.Bgs;

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
                    if (!isDualFileOutput && !replaceBackground) image.WriteToFile(Path.Combine(outputPath, pngFileName));
                } else { // Non-extracted regular image output
                    image.WriteToFile(Path.Combine(outputPath, fileName));
                }

                Console.WriteLine($"Successfully downloaded: {fileName}");
                return new (fileName, true);
            } catch (Exception _e)
            {
                Console.Error.WriteLine($"Failed to download photo id: {photoId}");
                Console.Error.WriteLine(_e);

                return new (fileName, false);
            } finally
            {
                if (semaphore != null) semaphore.Release(); // Release the semaphore
            }
        }
    }
}
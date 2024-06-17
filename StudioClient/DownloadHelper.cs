using Newtonsoft.Json.Linq;
using NetVips;
using RestSharp;


namespace SkylabStudio
{
    public class PhotoOptions
    {
        public List<BgImageResult>? Bgs { get; set; }
        public bool? ReturnOnError { get; set; }

        public PhotoOptions()
        {
            Bgs = new List<BgImageResult>();
            ReturnOnError = false;
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

    public class BgImageResult
    {
        public string BgName { get; set; }
        public Image BgImage { get; set; }

        public BgImageResult(string bgName, Image bgImage)
        {
            BgName = bgName;
            BgImage = bgImage;
        }
    }

    public partial class StudioClient
    {
        /// <summary>
        /// Downloads background images based on the provided profile.
        /// </summary>
        /// <param name="profile">The profile associated to the job.</param>
        /// <returns>List of downloaded background images.</returns>
        private async Task<List<BgImageResult>?> DownloadBgImages(dynamic profile)
        {
            List<BgImageResult> tempBgs = new List<BgImageResult>();
            List<JToken> bgPhotos = ((JArray) profile!.photos).Where(photo => photo["jobId"] != null).ToList();

            foreach (dynamic bg in bgPhotos)
            {
                byte[] bgBuffer = await DownloadImageAsync(bg.originalUrl.Value);
                Image bgImage = Image.NewFromBuffer(bgBuffer);
                tempBgs.Add(new BgImageResult(bg.name.Value, bgImage));
            }

            return tempBgs;
        }

        /// <summary>
        /// Downloads an image asynchronously from the specified URL.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download.</param>
        /// <returns>The downloaded image as a byte array.</returns>
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error downloading image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads and replaces the background image in the input image.
        /// Required: Profile should have a background uploaded and replace background toggled on
        /// </summary>
        /// <param name="fileName">The name of the file being processed.</param>
        /// <param name="inputImage">The input image to process.</param>
        /// <param name="outputPath">The path where the processed images will be saved.</param>
        /// <param name="profile">The profile associated to the job.</param>
        /// <param name="bgs">List of background images.</param>
        /// <returns>True if the operation is successful; otherwise, false.</returns>
        private async Task<bool> DownloadReplacedBackgroundImage(string fileName, Image inputImage, string outputPath, dynamic? profile = null, List<BgImageResult>? bgs = null)
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
                        string newFileName = $"{Path.GetFileNameWithoutExtension(fileName)} ({bgs[i].BgName}).{outputFileType}";
                        Image resizedBgImage = bgs[i].BgImage.ThumbnailImage(inputImage.Width, inputImage.Height, crop: Enums.Interesting.Centre);
                        Image resultImage = resizedBgImage.Composite2(rgbCutout, Enums.BlendMode.Over);
                        resultImage.WriteToFile(Path.Combine(outputPath, newFileName));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error downloading background image: {ex.Message}";
                Console.Error.WriteLine(errorMsg);
                
                throw new Exception(errorMsg);
            }
        }

        /// <summary>
        /// Downloads all photos based on a list of photo IDs.
        /// </summary>
        /// <param name="photosList">List of photo objects with IDs.</param>
        /// <param name="profile">The profile associated to the job.</param>
        /// <param name="outputPath">The path where photos will be downloaded.</param>
        /// <returns>Download result containing lists of success and errored photo names.</returns>
        public async Task<DownloadAllPhotosResult> DownloadAllPhotos(JArray photosList, dynamic profile, string outputPath)
        {
            if (!Directory.Exists(outputPath))
            {
                throw new Exception("Invalid output path");
            }

            List<string> successPhotos = new List<string>();
            List<string> erroredPhotos = new List<string>();
            List<BgImageResult> bgs = new List<BgImageResult>();

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
                    ReturnOnError = true,
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

        /// <summary>
        /// Downloads a photo based on the specified photo ID.
        /// </summary>
        /// <param name="photoId">The ID of the photo to download.</param>
        /// <param name="outputPath">The path where the downloaded photo will be saved. Could either be </param>
        /// <param name="profile">Optional: The profile containing photo processing options.</param>
        /// <param name="options">Optional: Additional options for photo processing.</param>
        /// <param name="semaphore">Optional - *Used Interally with DownloadAllPhotos* : SemaphoreSlim for controlling concurrent photo downloads.</param>
        /// <returns>
        /// A tuple containing the downloaded photo's filename and a boolean indicating
        /// whether the download was successful.
        /// </returns>
        /// <exception cref="Exception">Thrown on any download error when DownloadPhoto is called without ReturnOnError option.</exception>
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
                List<BgImageResult>? bgs = options?.Bgs;

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
                string errorMsg = $"Failed to download photo id: {photoId} - ${_e}";
                if (options?.ReturnOnError == null) {
                    throw new Exception(errorMsg);
                }

                return new (fileName, false);
            } finally
            {
                if (semaphore != null) semaphore.Release(); // Release the semaphore
            }
        }
    }
}
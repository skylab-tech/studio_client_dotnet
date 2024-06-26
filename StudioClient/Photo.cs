using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using RestSharp;


namespace SkylabStudio
{
    public partial class StudioClient
    {
        /// <summary>
        /// Array of valid file extensions for photos.
        /// </summary>
        public static readonly string[] VALID_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".webp" };

        /// <summary>
        /// Maximum allowed size for a photo in bytes.
        /// </summary>
        public const int MAX_PHOTO_SIZE = 27 * 1024 * 1024;

        /// <summary>
        /// Creates a new photo with the specified payload.
        /// </summary>
        /// <param name="payload">The photo payload to be sent in the request.</param>
        /// <returns>A dynamic object representing the created photo.</returns>
        public async Task<dynamic> CreatePhoto(object payload)
        {
            return await Request("photos", Method.Post, payload);
        }

        /// <summary>
        /// Retrieves information about a specific photo based on its ID.
        /// </summary>
        /// <param name="photoId">The ID of the photo to retrieve.</param>
        /// <returns>A dynamic object representing the requested photo.</returns>
        public async Task<dynamic> GetPhoto(long photoId)
        {
            return await Request($"photos/{photoId}", Method.Get);
        }

        /// <summary>
        /// Deletes a specific photo based on its ID.
        /// </summary>
        /// <param name="photoId">The ID of the photo to delete.</param>
        /// <returns>A dynamic object representing the result of the deletion.</returns>
        public async Task<dynamic> DeletePhoto(long photoId)
        {
            return await Request($"photos/{photoId}", Method.Delete);
        }

        /// <summary>
        /// Retrieves a presigned URL for uploading a photo.
        /// </summary>
        /// <param name="photoId">The ID of the photo for which to get the upload URL.</param>
        /// <param name="md5">The MD5 hash of the photo data.</param>
        /// <param name="useCacheUpload">Flag indicating whether to use cache upload.</param>
        /// <returns>A dynamic object representing the presigned upload URL.</returns>
        public async Task<dynamic> GetUploadUrl(long photoId, string md5 = "", bool useCacheUpload = false)
        {
            string queryParams = $"use_cache_upload={useCacheUpload.ToString().ToLower()}&photo_id={photoId}&content_md5={md5}";

            return await Request($"photos/upload_url?{queryParams}", Method.Get);
        }

        /// <summary>
        /// Uploads a photo associated with a job.
        /// </summary>
        /// <param name="photoPath">The path to the photo file.</param>
        /// <param name="jobId">The ID of the job associated with the photo.</param>
        /// <returns>A dynamic object representing the uploaded photo.</returns>
        public async Task<dynamic> UploadJobPhoto(string photoPath, long jobId)
        {
            return await UploadPhoto(photoPath, "job", jobId);
        }

        /// <summary>
        /// Uploads a photo associated with a profile (for profiles with replace bg enabled)
        /// </summary>
        /// <param name="photoPath">The path to the photo file.</param>
        /// <param name="profileId">The ID of the profile associated with the photo.</param>
        /// <returns>A dynamic object representing the uploaded photo.</returns>
        public async Task<dynamic> UploadProfilePhoto(string photoPath, long profileId)
        {
            return await UploadPhoto(photoPath, "profile", profileId);
        }

        /// <summary>
        /// Uploads a photo associated with a job or profile.
        /// </summary>
        /// <param name="photoPath">The path to the photo file.</param>
        /// <param name="modelName">The name of the model (job or profile).</param>
        /// <param name="modelId">The ID of the model associated with the photo.</param>
        /// <returns>A dynamic object representing the uploaded photo.</returns>
        private async Task<dynamic> UploadPhoto(string photoPath, string modelName, long modelId)
        {
            string[] availableModels = { "job", "profile" };
            if (!availableModels.Contains(modelName)) throw new Exception("Invalid model name. Must be 'job' or 'profile'");

            var photoBasename = Path.GetFileName(photoPath);
            string fileExtension = Path.GetExtension(photoBasename).ToLower();
            if (!VALID_EXTENSIONS.Contains(fileExtension))
            {
                throw new Exception("Photo has an invalid extension. Supported extensions (.jpg, .jpeg, .png, .webp)");
            }

            var photoObject = new JObject
            {
                { "name", $"{Guid.NewGuid()}{fileExtension}" },
                { "path", photoPath }
            };
            if (modelName == "job") photoObject["job_id"] = modelId; else photoObject["profile_id"] = modelId;

            dynamic photo = await CreatePhoto(photoObject);

            byte[] photoData = File.ReadAllBytes(photoPath);
            if (photoData.Length > 0 && ((photoData.Length / 1024 / 1024) > MAX_PHOTO_SIZE))
            {
                throw new Exception($"{photoPath} exceeds 27MB");
            }

            var md5 = MD5.Create();
            byte[] md5Hash = md5.ComputeHash(photoData);
            string md5Base64 = Convert.ToBase64String(md5Hash);

            dynamic uploadObj = await GetUploadUrl(photo.id.Value, System.Net.WebUtility.UrlEncode(md5Base64));
            string presignedUrl = uploadObj.url.Value;

            using (RestClient httpClient = new RestClient())
            {
                byte[] fileBytes = File.ReadAllBytes(photoPath);
                RestRequest request = new RestRequest(presignedUrl, Method.Put);

                request.AddParameter("application/octet-stream", fileBytes, ParameterType.RequestBody);
                request.AddHeader("Content-MD5", Convert.ToBase64String(md5.ComputeHash(fileBytes)));

                if (modelName == "job") request.AddHeader("X-Amz-Tagging", "job=photo&api=true");

                // Upload image via PUT request to presigned url
                RestResponse response = await httpClient.ExecuteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await DeletePhoto(photo.id.Value);
                    throw new Exception("Could not upload photo.");
                }

                return photo;
            }

            throw new Exception("An error has occurred uploading the photo.");
        }
    }
}
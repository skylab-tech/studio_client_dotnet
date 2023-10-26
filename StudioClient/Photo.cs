namespace StudioClient;

using System.Security.Cryptography;
using Newtonsoft.Json.Linq;


public partial class StudioClient
{
  public static readonly string[] VALID_EXTENSIONS = { ".png", ".jpg", ".jpeg", "webp" };
  public const int MAX_PHOTO_SIZE = 27 * 1024 * 1024;

  public async Task<dynamic> CreatePhoto(object payload)
  {
      return await Request("photos", HttpMethod.Post, payload);
  }

  public async Task<dynamic> GetPhoto(long photoId)
  {
      return await Request($"photos/{photoId}", HttpMethod.Get);
  }

  public async Task<dynamic> DeletePhoto(long photoId)
  {
      return await Request($"photos/{photoId}", HttpMethod.Delete);
  }

  public async Task<dynamic> GetUploadUrl(long photoId, string md5 = "", bool useCacheUpload = false)
  {
      string queryParams = $"use_cache_upload={useCacheUpload.ToString().ToLower()}&photo_id={photoId}&content_md5={md5}";

      return await Request($"photos/upload_url?{queryParams}", HttpMethod.Get);
  }

  public async Task<dynamic> UploadPhoto(string photoPath, string modelName, long modelId)
  {
      string[] availableModels = { "job", "profile" };
      if (!availableModels.Contains(modelName)) throw new Exception("Invalid model name. Must be 'job' or 'profile'");

      var photoBasename = Path.GetFileName(photoPath);
      string fileExtension = Path.GetExtension(photoBasename).ToLower();
      if (!VALID_EXTENSIONS.Contains(fileExtension))
      {
        throw new Exception("Photo has invalid extension. Supported extensions (.jpg, .jpeg, .png, .webp)");
      }

      var photoObject = new JObject
      {
          { "name", photoBasename },
      };
      if (modelName == "job") photoObject["job_id"] = modelId; else photoObject["profile_id"] = modelId;

      dynamic photo = await CreatePhoto(photoObject);

      byte[] photoData = await File.ReadAllBytesAsync(photoPath);
      if (photoData.Length > 0 && ((photoData.Length / 1024 / 1024) > MAX_PHOTO_SIZE))
      {
          throw new Exception($"{photoPath} exceeds 27MB");
      }

      var md5 = MD5.Create();
      byte[] md5Hash = md5.ComputeHash(photoData);
      string md5Base64 = Convert.ToBase64String(md5Hash);

      dynamic uploadObj = await GetUploadUrl(photo.id.Value, md5Base64);
      string presignedUrl = uploadObj.url.Value;

      using (var client = new HttpClient())
      {
          var content = new ByteArrayContent(photoData);
          var request = new HttpRequestMessage(HttpMethod.Put, presignedUrl);
          request.Content = content;
          request.Content.Headers.ContentMD5 = MD5.Create().ComputeHash(photoData);
          if (modelName == "job") request.Headers.Add("X-Amz-Tagging", "job=photo&api=true");            

          // Upload image via PUT request to presigned url
          HttpResponseMessage response = await client.SendAsync(request);

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
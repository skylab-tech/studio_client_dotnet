
using System.Security.Principal;
using NetVips;
using Newtonsoft.Json.Linq;

namespace SkylabStudio.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var studioOptions = new StudioOptions { MaxConcurrentDownloads = 5 };
            var apiClient = new StudioClient(Environment.GetEnvironmentVariable("SKYLAB_API_TOKEN"), studioOptions);

            try
            {
                Guid randomUuid = Guid.NewGuid();

                // CREATE PROFILE
                dynamic profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = false, enable_extract = true });

                // CREATE JOB
                var jobName = $"test-job-{randomUuid}";
                dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile.id.Value });

                // UPLOAD PHOTO
                string filePath = "/path/to/photo";
                dynamic res = await apiClient.UploadJobPhoto(filePath, job.id.Value);
 
                // QUEUE JOB
                dynamic queuedJob = await apiClient.QueueJob(job.id.Value, new { callback_url = "YOUR_CALLBACK_ENDPOINT" });

                // ...
                // !(wait until job status is completed by waiting for callback or by polling)!
                // FETCH COMPLETED JOB
                dynamic completedJob = await apiClient.GetJob(queuedJob.id.Value);

                // DOWNLOAD COMPLETED JOB PHOTOS
                JArray photosList = completedJob.photos;
                DownloadAllPhotosResult downloadResults = await apiClient.DownloadAllPhotos(photosList, completedJob.profile, "/output/folder/");
                Console.WriteLine($"Success photos: [{string.Join(", ", downloadResults.SuccessPhotos)}]");
                Console.WriteLine($"Erorred photos: [{string.Join(", ", downloadResults.ErroredPhotos)}]");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}

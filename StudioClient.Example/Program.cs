
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
                string filePath = "/Users/Paul/Downloads/dotnet_image/small_SENT18-009L171117-0701.jpg";
                dynamic res = await apiClient.UploadJobPhoto(filePath, job.id.Value);

                // QUEUE JOB
                dynamic queuedJob = await apiClient.QueueJob(job.id.Value, new { callback_url = "https://webhook.site/439248ba-db00-4088-bcc6-3b5ca3a4aeb2" });

                // ...
                // !(wait until job status is completed by waiting for callback or by polling)!
                // FETCH COMPLETED JOB
                dynamic completedJob = await apiClient.GetJob(queuedJob.id.Value);

                // DOWNLOAD COMPLETED JOB PHOTOS
                JArray photosList = completedJob.photos;
                DownloadAllPhotosResult downloadResults = await apiClient.DownloadAllPhotos(photosList, completedJob.profile, "/Users/Paul/Downloads/dotnet_image/results/");
                Console.WriteLine($"Success photos: [{string.Join(", ", downloadResults.SuccessPhotos)}]");
                Console.WriteLine($"Errored photos: [{string.Join(", ", downloadResults.ErroredPhotos)}]");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}

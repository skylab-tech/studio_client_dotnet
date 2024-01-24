
using System.Security.Principal;
using NetVips;
using Newtonsoft.Json.Linq;

namespace SkylabStudio.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var apiClient = new StudioClient(Environment.GetEnvironmentVariable("SKYLAB_API_TOKEN"));

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
                dynamic queuedJob = await apiClient.QueueJob(job.id.Value, new { callback_url = "http://127.0.0.1:3030" });

                // FETCH COMPLETED JOB (wait until job status is completed)
                dynamic completedJob = await apiClient.GetJob(queuedJob.id.Value);

                // DOWNLOAD COMPLETED JOB PHOTOS
                JArray photosList = completedJob.photos;
                await apiClient.DownloadAllPhotos(photosList, completedJob.profile, "/output/folder/");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}

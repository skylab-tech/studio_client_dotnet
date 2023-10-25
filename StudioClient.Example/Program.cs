namespace StudioClient.Example;


class Program
{
    static async Task Main(string[] args)
    {
        var apiClient = new StudioClient("YOUR_API_TOKEN");

        try
        {
            Guid randomUuid = Guid.NewGuid();

            // CREATE PROFILE
            dynamic profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });

            // CREATE JOB
            var jobName = $"test-job-{randomUuid}";
            dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile.id.Value });

            // UPLOAD PHOTO
            string filePath = "/Users/kevinle/Desktop/test photos/5png + jpg/CAM11165.JPG";
            dynamic res = await apiClient.UploadPhoto(filePath, "job", job.id.Value);
            Console.WriteLine(res);

            // QUEUE JOB
            dynamic queuedJob = await apiClient.QueueJob(job.id.Value, new { callback_url = "YOUR_CALLBACK_ENDPOINT" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}

namespace StudioClient.Tests;


public class JobTest : IAsyncLifetime
{
    Guid randomUuid = Guid.NewGuid();

    private readonly StudioClient apiClient = new StudioClient(Environment.GetEnvironmentVariable("SKYLAB_API_TOKEN"));
    private dynamic? profile;

    public async Task InitializeAsync()
    {
        profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Call_CreateJob_ReturnsJob()
    {
        Guid randomUuid = Guid.NewGuid();
        var jobName = $"test-job-{randomUuid}";
        dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile?.id.Value });

        Assert.NotNull(job.id);
    }

    [Fact]
    public async Task Call_ListJobs_ReturnsJobs()
    {
        dynamic jobs = await apiClient.ListJobs();

        Assert.True(jobs.Count >= 0);
    }

    [Fact]
    public async Task Call_GetJob_ReturnsJob()
    {
        Guid randomUuid = Guid.NewGuid();
        var jobName = $"test-job-{randomUuid}";
        dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile?.id.Value });

        Assert.NotNull(job.id);
    }

    [Fact]
    public async Task Call_UpdateJob_ReturnsJob()
    {
        Guid randomUuid = Guid.NewGuid();
        var jobName = $"test-job-{randomUuid}";
        dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile?.id.Value });
        dynamic updatedJob = await apiClient.UpdateJob(job.id.Value, new { name = $"Updated-Job-({randomUuid})" });

        Assert.NotNull(updatedJob.id);
    }
}
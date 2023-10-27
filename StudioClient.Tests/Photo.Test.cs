namespace SkylabStudio.Tests;


public class PhotoTest : IAsyncLifetime
{
    Guid randomUuid = Guid.NewGuid();

    private readonly StudioClient apiClient = new StudioClient(Environment.GetEnvironmentVariable("SKYLAB_API_TOKEN"));
    private dynamic? profile;
    private dynamic? job;

    public async Task InitializeAsync()
    {
        profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });
        job = await apiClient.CreateJob(new { name = $"Test Job ({randomUuid})", profile_id = profile?.id.Value });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Call_CreatePhoto_ReturnsPhoto()
    {
        Guid randomUuid = Guid.NewGuid();
        var photoName = $"test-name-{randomUuid}";
        dynamic photo = await apiClient.CreatePhoto(new { name = photoName, job_id = job?.id.Value });

        Assert.NotNull(photo.id);
    }

    [Fact]
    public async Task Call_GetPhoto_ReturnsPhoto()
    {
        Guid randomUuid = Guid.NewGuid();
        var photoName = $"test-name-{randomUuid}";
        dynamic createdPhoto = await apiClient.CreatePhoto(new { name = photoName, job_id = job?.id.Value });

        dynamic photo = await apiClient.GetPhoto(createdPhoto.id.Value);

        Assert.NotNull(photo.id);
    }

    [Fact]
    public async Task Call_DeletePhoto_ReturnsPhoto()
    {
        Guid randomUuid = Guid.NewGuid();
        var photoName = $"test-name-{randomUuid}";
        dynamic createdPhoto = await apiClient.CreatePhoto(new { name = photoName, job_id = job?.id.Value });

        dynamic deletedPhoto = await apiClient.DeletePhoto(createdPhoto.id.Value);

        Assert.NotNull(deletedPhoto.id);
    }
}
namespace SkylabStudio.Tests;


public class ProfileTest
{
    private readonly StudioClient apiClient = new StudioClient(Environment.GetEnvironmentVariable("SKYLAB_API_TOKEN"));

    [Fact]
    public async Task Call_CreateProfile_ReturnsProfile()
    {
        Guid randomUuid = Guid.NewGuid();
        dynamic profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });

        Assert.NotNull(profile.id);
    }

    [Fact]
    public async Task Call_ListProfiles_ReturnsProfiles()
    {
        dynamic profiles = await apiClient.ListProfiles();

        Assert.True(profiles.Count >= 0);
    }

    [Fact]
    public async Task Call_GetProfile_ReturnsProfile()
    {
        Guid randomUuid = Guid.NewGuid();
        dynamic createdProfile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });
        dynamic profile = await apiClient.GetProfile(createdProfile.id.Value);

        Assert.NotNull(profile.id);
    }

    [Fact]
    public async Task Call_UpdateProfile_ReturnsProfile()
    {
        Guid randomUuid = Guid.NewGuid();
        dynamic profile = await apiClient.CreateProfile(new { name = $"Test Profile ({randomUuid})", enable_crop = false, enable_color = true });
        dynamic updatedProfile = await apiClient.UpdateProfile(profile.id.Value, new { name = $"Updated Profile ({randomUuid})" });

        Assert.NotNull(updatedProfile.id);
    }
}
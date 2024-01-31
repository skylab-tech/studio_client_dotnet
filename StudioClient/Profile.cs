namespace SkylabStudio
{
    public partial class StudioClient
    { 
        public async Task<dynamic> CreateProfile(object payload)
        {
            return await Request("profiles", HttpMethod.Post, payload);
        }

        public async Task<dynamic> ListProfiles()
        {
            return await Request("profiles", HttpMethod.Get);
        }

        public async Task<dynamic> GetProfile(long profileId)
        {
            return await Request($"profiles/{profileId}", HttpMethod.Get);
        }

        public async Task<dynamic> UpdateProfile(long profileId, object payload)
        {
            return await Request($"profiles/{profileId}", HttpMethod.Put, payload);
        }

        public async Task<dynamic> GetProfileBgs(long profileId)
        {
            return await Request($"profiles/{profileId}/bg_photos", HttpMethod.Get);
        }
    }
}

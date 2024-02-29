namespace SkylabStudio
{
    public partial class StudioClient
    { 
        public async Task<dynamic> CreateProfile(object payload)
        {
            return await Request("profiles", RestSharp.Method.Post, payload);
        }

        public async Task<dynamic> ListProfiles()
        {
            return await Request("profiles", RestSharp.Method.Get);
        }

        public async Task<dynamic> GetProfile(long profileId)
        {
            return await Request($"profiles/{profileId}", RestSharp.Method.Get);
        }

        public async Task<dynamic> UpdateProfile(long profileId, object payload)
        {
            return await Request($"profiles/{profileId}", RestSharp.Method.Put, payload);
        }

        public async Task<dynamic> GetProfileBgs(long profileId)
        {
            return await Request($"profiles/{profileId}/bg_photos", RestSharp.Method.Get);
        }
    }
}

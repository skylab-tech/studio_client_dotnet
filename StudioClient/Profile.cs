namespace SkylabStudio
{
    public partial class StudioClient
    { 
        /// <summary>
        /// Creates a new profile with the specified payload.
        /// </summary>
        /// <param name="payload">The payload containing profile information.</param>
        /// <returns>A dynamic object representing the created profile.</returns>
        public async Task<dynamic> CreateProfile(object payload)
        {
            return await Request("profiles", RestSharp.Method.Post, payload);
        }

        /// <summary>
        /// Retrieves a list of profiles.
        /// </summary>
        /// <returns>A dynamic object representing a list of profiles.</returns>
        public async Task<dynamic> ListProfiles()
        {
            return await Request("profiles", RestSharp.Method.Get);
        }

        /// <summary>
        /// Retrieves the profile with the specified ID.
        /// </summary>
        /// <param name="profileId">The ID of the profile to retrieve.</param>
        /// <returns>A dynamic object representing the retrieved profile.</returns>
        public async Task<dynamic> GetProfile(long profileId)
        {
            return await Request($"profiles/{profileId}", RestSharp.Method.Get);
        }

        /// <summary>
        /// Updates the profile with the specified ID using the provided payload.
        /// </summary>
        /// <param name="profileId">The ID of the profile to update.</param>
        /// <param name="payload">The payload containing updated profile information.</param>
        /// <returns>A dynamic object representing the updated profile.</returns>
        public async Task<dynamic> UpdateProfile(long profileId, object payload)
        {
            return await Request($"profiles/{profileId}", RestSharp.Method.Put, payload);
        }

        /// <summary>
        /// Retrieves background photos associated with the profile identified by the given profile ID.
        /// </summary>
        /// <param name="profileId">The ID of the profile to retrieve background photos for.</param>
        /// <returns>A dynamic object representing background photos of the profile.</returns>
        public async Task<dynamic> GetProfileBgs(long profileId)
        {
            return await Request($"profiles/{profileId}/bg_photos", RestSharp.Method.Get);
        }
    }
}

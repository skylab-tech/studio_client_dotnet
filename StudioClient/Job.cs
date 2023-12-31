  
namespace SkylabStudio
{
    public partial class StudioClient
    { 
        public async Task<dynamic> ListJobs()
        {
            return await Request("jobs", HttpMethod.Get);
        }

        public async Task<dynamic> CreateJob(object payload)
        {
            return await Request("jobs", HttpMethod.Post, payload);
        }

        public async Task<dynamic> GetJob(long jobId)
        {
            return await Request($"jobs/{jobId}", HttpMethod.Get);
        }

        public async Task<dynamic> GetJobByName(string jobName)
        {
            return await Request($"jobs/find_by_name/?name={jobName}", HttpMethod.Get);
        }

        public async Task<dynamic> UpdateJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}", HttpMethod.Put, payload);
        }

        public async Task<dynamic> QueueJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}/queue", HttpMethod.Post, payload);
        }

        public async Task<dynamic> CancelJob(long jobId)
        {
            return await Request($"jobs/{jobId}/cancel", HttpMethod.Post);
        }

        public async Task<dynamic> JobsInFront(long jobId)
        {
            return await Request($"jobs/{jobId}/jobs_in_front", HttpMethod.Get);
        }
    }
}
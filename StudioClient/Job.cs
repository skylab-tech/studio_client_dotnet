  
namespace SkylabStudio
{
    public partial class StudioClient
    { 
        public async Task<dynamic> ListJobs()
        {
            return await Request("jobs", RestSharp.Method.Get);
        }

        public async Task<dynamic> CreateJob(object payload)
        {
            return await Request("jobs", RestSharp.Method.Post, payload);
        }

        public async Task<dynamic> GetJob(long jobId)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Get);
        }

        public async Task<dynamic> GetJobByName(string jobName)
        {
            return await Request($"jobs/find_by_name/?name={jobName}", RestSharp.Method.Get);
        }

        public async Task<dynamic> UpdateJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Put, payload);
        }

        public async Task<dynamic> QueueJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}/queue", RestSharp.Method.Post, payload);
        }

        public async Task<dynamic> CancelJob(long jobId)
        {
            return await Request($"jobs/{jobId}/cancel", RestSharp.Method.Post);
        }

        public async Task<dynamic> DeleteJob(long jobId)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Delete);
        }

        public async Task<dynamic> JobsInFront(long jobId)
        {
            return await Request($"jobs/{jobId}/jobs_in_front", RestSharp.Method.Get);
        }
    }
}
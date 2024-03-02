  
namespace SkylabStudio
{
    public partial class StudioClient
    { 
        /// <summary>
        /// Retrieves a list of all jobs.
        /// </summary>
        /// <returns>A dynamic object representing the list of jobs.</returns>
        public async Task<dynamic> ListJobs()
        {
            return await Request("jobs", RestSharp.Method.Get);
        }

        /// <summary>
        /// Creates a new job with the specified payload.
        /// </summary>
        /// <param name="payload">The job payload to be sent in the request.</param>
        /// <returns>A dynamic object representing the created job.</returns>
        public async Task<dynamic> CreateJob(object payload)
        {
            return await Request("jobs", RestSharp.Method.Post, payload);
        }

        /// <summary>
        /// Retrieves information about a specific job based on its ID.
        /// </summary>
        /// <param name="jobId">The ID of the job to retrieve.</param>
        /// <returns>A dynamic object representing the requested job.</returns>
        public async Task<dynamic> GetJob(long jobId)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Get);
        }

        /// <summary>
        /// Retrieves information about a job based on its name.
        /// </summary>
        /// <param name="jobName">The name of the job to retrieve.</param>
        /// <returns>A dynamic object representing the requested job.</returns>
        public async Task<dynamic> GetJobByName(string jobName)
        {
            return await Request($"jobs/find_by_name/?name={jobName}", RestSharp.Method.Get);
        }

        /// <summary>
        /// Updates a specific job with the provided payload.
        /// </summary>
        /// <param name="jobId">The ID of the job to update.</param>
        /// <param name="payload">The job payload to be sent in the request.</param>
        /// <returns>A dynamic object representing the updated job.</returns>
        public async Task<dynamic> UpdateJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Put, payload);
        }

        /// <summary>
        /// Queues a specific job with the provided payload.
        /// </summary>
        /// <param name="jobId">The ID of the job to queue.</param>
        /// <param name="payload">The job payload to be sent in the request.</param>
        /// <returns>A dynamic object representing the queued job.</returns>
        public async Task<dynamic> QueueJob(long jobId, object payload)
        {
            return await Request($"jobs/{jobId}/queue", RestSharp.Method.Post, payload);
        }

        /// <summary>
        /// Cancels a specific job based on its ID.
        /// </summary>
        /// <param name="jobId">The ID of the job to cancel.</param>
        /// <returns>A dynamic object representing the result of the cancellation.</returns>
        public async Task<dynamic> CancelJob(long jobId)
        {
            return await Request($"jobs/{jobId}/cancel", RestSharp.Method.Post);
        }

        /// <summary>
        /// Deletes a specific job based on its ID.
        /// </summary>
        /// <param name="jobId">The ID of the job to delete.</param>
        /// <returns>A dynamic object representing the result of the deletion.</returns>
        public async Task<dynamic> DeleteJob(long jobId)
        {
            return await Request($"jobs/{jobId}", RestSharp.Method.Delete);
        }

        /// <summary>
        /// Retrieves information about jobs that are in front of the specified job.
        /// </summary>
        /// <param name="jobId">The ID of the reference job.</param>
        /// <returns>A dynamic object representing jobs that are in front of the reference job.</returns>
        public async Task<dynamic> JobsInFront(long jobId)
        {
            return await Request($"jobs/{jobId}/jobs_in_front", RestSharp.Method.Get);
        }
    }
}
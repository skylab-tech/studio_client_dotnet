# Skylab Studio .NET Client

.NET client to interface with Skylab Studio [Public API](https://studio-docs.skylabtech.ai)

## Installation

```bash
$ nuget install SkylabStudio
```

## Example Usage

```dotnet
// CREATE PROFILE
dynamic profile = await apiClient.CreateProfile(new { name = $"Test Profile", enable_crop = false, enable_color = true });

// CREATE JOB
var jobName = $"test-job";
dynamic job = await apiClient.CreateJob(new { name = jobName, profile_id = profile.id.Value });

// UPLOAD PHOTO
string filePath = "/path/to/photo";
dynamic res = await apiClient.UploadPhoto(filePath, "job", job.id.Value);

// QUEUE JOB
dynamic queuedJob = await apiClient.QueueJob(job.id.Value, new { callback_url = "YOUR_CALLBACK_ENDPOINT" });

// NOTE: Once the job is queued, it will get processed then complete
// We will send a response to the specified callback_url with the output photo download urls
```

For all examples, assume:

```dotnet
using SkylabTech.SkylabStudio;

var api = new StudioClient("YOUR_SKYLAB_API_TOKEN");
```

### Error Handling

By default, the API calls return a JSON (JObject) response object no matter the type of response.

### Endpoints

#### List all jobs

```dotnet
api.ListJobs();
```

#### Create job

```dotnet
api.CreateJob(new { name = "Test Job", profileId = 123 });
```

For all payload options, consult the [API documentation](https://studio-docs.skylabtech.ai/#tag/job/operation/createJob).

#### Get job

```dotnet
api.GetJob(jobId);
```

#### Update job

```dotnet
api.UpdateJob(jobId, new { name = "Updated Job Name" });
```

For all payload options, consult the [API documentation](https://studio-docs.skylabtech.ai/#tag/job/operation/updateJobById).

#### Queue job

```dotnet
api.QueueJob(jobId, new { callback_url = "http://your.endpoint/"});
```

#### Delete job

```dotnet
api.DeleteJob(jobId);
```

#### Cancel job

```dotnet
api.CancelJob(jobId);
```

#### Jobs in front

Use after queueing job to check number of jobs ahead of yours

```dotnet
api.JobsInFront(jobId);
```

#### List all profiles

```dotnet
api.ListProfiles();
```

#### Create profile

```dotnet
api.CreateProfile(new { name = $"New Profile", enable_crop = false, enable_color = true });
```

For all payload options, consult the [API documentation](https://studio-docs.skylabtech.ai/#tag/profile/operation/createProfile).

#### Get profile

```dotnet
api.GetProfile(profileId);
```

#### Update profile

```dotnet
api.UpdateProfile(profileId, new { name = $"Test Profile", enable_crop = false, enable_color = true });
```

For all payload options, consult the [API documentation](https://studio-docs.skylabtech.ai/#tag/profile/operation/updateProfileById).

#### List all photos

```dotnet
api.ListPhotos();
```

#### Get photo

```dotnet
api.GetPhoto(photoId);
```

#### Upload photo

This function handles validating a photo, creating a photo object and uploading it to your job/profile's s3 bucket. If the bucket upload process fails, it retries 3 times and if failures persist, the photo object is deleted.

```dotnet
api.UploadPhoto(photoPath, model, modelId)
```

model can either be 'job' or 'profile'

modelId is the jobs/profiles respective id

```dotnet
api.UploadPhoto("/path/to/photo", "job", jobId)
```

OR

```dotnet
api.UploadPhoto("/path/to/photo", "profile", profileId);
```

If upload fails, the photo object is deleted for you. If upload succeeds and you later decide you no longer want to include that image, use api.DeletePhoto(photoId) to remove it.

#### Delete photo

This will remove the photo from the job/profile's bucket. Useful for when you've accidentally uploaded an image that you'd like removed.

```dotnet
api.DeletePhoto(photoId)
```

#### Validate hmac headers

Applicable if you utilize the job callback url. Use to validate the job payload integrity.

- secretKey (string): Obtain from Skylab

- jobJson (string): Stringified json object obtained from callback PATCH request

- requestTimestamp (string): Obtained from callback PATCH request header 'X-Skylab-Timestamp'

- signature (string): Signature generated by Skylab to compare. Obtained from callback PATCH request header 'X-Skylab-Signature'

Returns **True** or **False** based on whether or not the signatures match.

```dotnet
api.ValidateHmacHeaders(secretKey, jobJson, requestTimestamp, signature);
```

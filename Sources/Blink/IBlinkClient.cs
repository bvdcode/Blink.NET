using Blink.Models;
using Blink.Exceptions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Blink
{
    /// <summary>
    /// Blink client interface
    /// </summary>
    public interface IBlinkClient
    {
        /// <summary>
        /// Gets or sets the general sleep time in milliseconds.
        /// This delay is required to prevent the server from returning an empty response.
        /// Set to 0 to disable the delay.
        /// </summary>
        int GeneralSleepTime { get; set; }

        /// <summary>
        /// Authorize with email and password provided in constructor.
        /// </summary>
        /// <returns><see cref="LoginResult"/> object with authorization data</returns>
        /// <exception cref="BlinkClientException">Thrown when email or password is not provided in constructor</exception>
        /// <exception cref="BlinkClientException">Thrown when authorization fails</exception>
        /// <exception cref="BlinkClientException">Thrown when no content is returned</exception>
        Task<LoginResult> AuthorizeAsync(string email, string password, bool reauth = true);

        /// <summary>
        /// Deletes a specified video asynchronously.
        /// </summary>
        /// <param name="video">The video information to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        Task DeleteVideoAsync(BlinkVideoInfo video);

        /// <summary>
        /// Retrieves the dashboard information asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the dashboard information.</returns>
        Task<Dashboard> GetDashboardAsync();

        /// <summary>
        /// Retrieves a specified video asynchronously.
        /// </summary>
        /// <param name="video">The video information to retrieve.</param>
        /// <param name="tryCount">The number of attempts to retrieve the video. Default is 3.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the video data as a byte array.</returns>
        Task<byte[]> GetVideoAsync(BlinkVideoInfo video, int tryCount = 3);

        /// <summary>
        /// Retrieves a list of videos asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of video information.</returns>
        Task<IEnumerable<BlinkVideoInfo>> GetVideosAsync();

        /// <summary>
        /// Verifies a specified PIN code asynchronously.
        /// </summary>
        /// <param name="code">The PIN code to verify.</param>
        /// <returns>A task that represents the asynchronous verification operation.</returns>
        Task VerifyPinAsync(string code);
    }
}
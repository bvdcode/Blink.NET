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
        /// <param name="email">Email address</param>
        /// <param name="password">Password</param>
        /// <param name="reauth">Set to true if you already authorized with pin code and want to reauthorize. This is from Blink app behavior.</param>
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
        Task<byte[]> GetVideoBytesAsync(BlinkVideoInfo video, int tryCount = 3);

        /// <summary>
        /// Get videos from single module (the first one in the dashboard). Throws exception if more than one module found.
        /// </summary>
        /// <returns>Collection of <see cref="BlinkVideoInfo"/> objects with video data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get videos</exception>
        /// <exception cref="BlinkClientException">Thrown when more than one module found</exception>
        Task<IEnumerable<BlinkVideoInfo>> GetVideosFromSingleModuleAsync();

        /// <summary>
        /// Get videos from specified module. Get modules from <see cref="GetDashboardAsync"/> method - <see cref="Dashboard.SyncModules"/>.
        /// </summary>
        /// <returns>Collection of <see cref="BlinkVideoInfo"/> objects with video data</returns>
        /// <exception cref="BlinkClientException">Thrown when not authorized</exception>
        /// <exception cref="BlinkClientException">Thrown when failed to get videos</exception>
        Task<IEnumerable<BlinkVideoInfo>> GetVideosFromModuleAsync(SyncModule module);

        /// <summary>
        /// Verifies a specified PIN code asynchronously.
        /// </summary>
        /// <param name="code">The PIN code to verify.</param>
        /// <returns>A task that represents the asynchronous verification operation.</returns>
        Task VerifyPinAsync(string code);
    }
}
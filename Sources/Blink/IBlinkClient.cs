using Blink.Models;
using Blink.Exceptions;
using System;
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
        /// Occurs when a new refresh token is issued by the Blink API.
        /// Subscribe to this event to persist the latest token for future runs.
        /// Raised after a successful 2FA verification or refresh-token login/rotation.
        /// </summary>
        event Action<string>? OnTokenRefreshed;

        /// <summary>
        /// Gets or sets the general sleep time in milliseconds.
        /// This delay is required to prevent the server from returning an empty response.
        /// Set to 0 to disable the delay.
        /// </summary>
        int GeneralSleepTime { get; set; }

        /// <summary>
        /// Gets the refresh token associated with the last login result.
        /// Save this token to reuse it for future logins without needing to provide email and password again.
        /// Null if not logged in.
        /// </summary>
        string? RefreshToken { get; }

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
        /// Attempts to verify the provided PIN code for authentication.
        /// </summary>
        /// <remarks>Before calling this method, ensure that <see cref="TryLoginAsync"/> has been
        /// successfully called to initialize the email and password.</remarks>
        /// <param name="code">The PIN code to verify. This value cannot be null, empty, or consist only of whitespace.</param>
        /// <returns><see langword="true"/> if the PIN code is successfully verified; <see langword="false"/> if the PIN code is
        /// invalid or expired.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="code"/> is null, empty, or whitespace. Thrown if <see cref="TryLoginAsync"/> has
        /// not been called prior to this method. Thrown if the server responds with an error other than an invalid or
        /// expired PIN code.</exception>
        Task<bool> TryVerifyPinAsync(string code);

        /// <summary>
        /// Attempts to log in using the specified email and password.
        /// </summary>
        /// <remarks>This method sends a login request to the Blink API and evaluates the response to
        /// determine the outcome.  If the response indicates a precondition failure, the method returns <see
        /// langword="true"/>.  If the response indicates unauthorized access, the method returns <see
        /// langword="false"/>.  For all other response statuses, a <see cref="BlinkClientException"/> is
        /// thrown.</remarks>
        /// <param name="email">The email address associated with the account. Cannot be null, empty, or whitespace.</param>
        /// <param name="password">The password for the account. Cannot be null, empty, or whitespace.</param>
        /// <returns><see langword="true"/> if the login attempt requires additional preconditions to be met;  <see
        /// langword="false"/> if the login attempt fails due to invalid credentials.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="email"/> or <paramref name="password"/> is null, empty, or whitespace,  or if an
        /// unexpected error occurs during the login process.</exception>
        Task<bool> TryLoginAsync(string email, string password);

        /// <summary>
        /// Attempts to log in using the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token used to authenticate the user. Cannot be null, empty, or whitespace.</param>
        /// <returns><see langword="true"/> if the login attempt is successful and the refresh token is valid;  <see
        /// langword="false"/> if the refresh token is invalid or unauthorized.</returns>
        /// <exception cref="BlinkClientException">Thrown if <paramref name="refreshToken"/> is null, empty, or whitespace, or if the login attempt fails due
        /// to an unexpected error.</exception>
        Task<bool> TryLoginWithRefreshTokenAsync(string refreshToken);
    }
}
using HyPrism.Models;

namespace HyPrism.Services.Game.Auth;

/// <summary>
/// Handles authentication with the custom Hytale auth server.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates a game session and retrieves authentication tokens.
    /// </summary>
    Task<AuthTokenResult> GetGameSessionTokenAsync(string uuid, string playerName);

    /// <summary>
    /// Validates an existing token is still valid.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
}

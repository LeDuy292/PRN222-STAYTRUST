using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace STAYTRUST.Providers
{
    public class RevalidatingSessionProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IDistributedCache _cache;

        public RevalidatingSessionProvider(
            ILoggerFactory loggerFactory,
            IDistributedCache cache)
            : base(loggerFactory)
        {
            _cache = cache;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Extract UserId and SessionId from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionIdClaim = user.FindFirst("SessionId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(sessionIdClaim))
            {
                return false;
            }

            // Retrieve the latest active session ID from cache
            var activeSessionId = await _cache.GetStringAsync($"ActiveSession_{userIdClaim}", cancellationToken);

            // If no session is in cache, we assume the server restarted or it's a legacy session.
            // For strictness, you could return false here, but usually, we allow it to persist 
            // until a new login sets a session ID.
            if (string.IsNullOrEmpty(activeSessionId))
            {
                return true; 
            }

            // Compare current session ID with the one in cache
            if (sessionIdClaim != activeSessionId)
            {
                // A newer login session exists elsewhere
                return false;
            }

            return true;
        }
    }
}

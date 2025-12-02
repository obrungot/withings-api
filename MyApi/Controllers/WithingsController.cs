using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Http.Json;

namespace MyApi.Controllers
{
    public class WithingsOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string AccountUrl { get; set; } = string.Empty;
        public string WbsApiUrl { get; set; } = string.Empty;
        public string CallbackUri { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("withings")]
    public class WithingsController : ControllerBase
    {
        private readonly WithingsOptions _opts;
        private readonly IHttpClientFactory _httpClientFactory;

        public WithingsController(IOptions<WithingsOptions> opts, IHttpClientFactory httpClientFactory)
        {
            _opts = opts.Value;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize()
        {
            var query = new Dictionary<string, string>()
            {
                ["response_type"] = "code",
                ["client_id"] = _opts.ClientId,
                ["state"] = _opts.State,
                ["scope"] = "user.metrics",
                ["redirect_uri"] = _opts.CallbackUri
            };

            // Build query string without relying on QueryHelpers
            var content = new FormUrlEncodedContent(query);
            var qs = await content.ReadAsStringAsync();
            var url = $"{_opts.AccountUrl}/oauth2_user/authorize2?{qs}";

            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state)
        {

            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing code parameter");

            var client = _httpClientFactory.CreateClient();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonceParams = new Dictionary<string, string>
            {
                ["action"] = "getnonce",
                ["client_id"] = _opts.ClientId,
                ["timestamp"] = timestamp
            };

            //var signature = GenerateSignature(nonceParams, _opts.ClientSecret);
            var signature = GenerateHmacSignature(timestamp, _opts.ClientSecret, _opts.ClientId);

            var nonceQuery = new Dictionary<string, string>
            {
                ["action"] = "getnonce",
                ["client_id"] = _opts.ClientId,
                ["timestamp"] = timestamp,
                ["signature"] = signature
            };

            var nonceResp = await client.GetAsync($"{_opts.WbsApiUrl}/v2/signature?{await new FormUrlEncodedContent(nonceQuery).ReadAsStringAsync()}");
            var nonceText = await nonceResp.Content.ReadAsStringAsync();
            using var nonceDoc = JsonDocument.Parse(nonceText);
            var nonceRoot = nonceDoc.RootElement;
            string? nonce = null;
            if (nonceRoot.TryGetProperty("body", out var bodyElem) &&
                bodyElem.TryGetProperty("nonce", out var nonceElem))
            {
                nonce = nonceElem.GetString();
            }

            var tokenRequest = new Dictionary<string, string>
            {
                ["action"] = "requesttoken",
                ["client_id"] = _opts.ClientId,
                ["client_secret"] = _opts.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _opts.CallbackUri,
                ["nonce"] = nonce ?? string.Empty
            };

            var resp = await client.PostAsync($"{_opts.WbsApiUrl}/v2/oauth2", new FormUrlEncodedContent(tokenRequest));
            if (!resp.IsSuccessStatusCode)
            {
                var txt = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, txt);
            }

            var tokenText = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(tokenText);
            var root = doc.RootElement;
            string? accessToken = null;
            if (root.TryGetProperty("body", out var tokenBodyElem) &&
                tokenBodyElem.TryGetProperty("access_token", out var tokenElem))
            {
                accessToken = tokenElem.GetString();
            }

            if (string.IsNullOrEmpty(accessToken))
                return StatusCode(500, "No access token returned");

            var apiClient = _httpClientFactory.CreateClient();
            apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var deviceQuery = new Dictionary<string, string> { ["action"] = "getdevice" };
            var deviceContentPair = new FormUrlEncodedContent(deviceQuery);
            var deviceQs = await deviceContentPair.ReadAsStringAsync();
            var deviceUrl = $"{_opts.WbsApiUrl}/v2/user?{deviceQs}";

            var deviceResp = await apiClient.GetAsync(deviceUrl);
            var deviceContent = await deviceResp.Content.ReadAsStringAsync();

            return Content(deviceContent, "application/json");
        }

        private string GenerateHmacSignature(string timeStamp, string clientSecret, string clientId)
        {
            var data = "getnonce" + "," + clientId + "," + timeStamp;
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(clientSecret);
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            using (var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                // Return lowercase hex string to match CryptoJS.HmacSHA256(...).toString()
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

}

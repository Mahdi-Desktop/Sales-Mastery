using System.Text.Json;

public class ReCaptchaService
{
  private readonly string _secretKey;
  private readonly HttpClient _httpClient;

  public ReCaptchaService(IConfiguration configuration, HttpClient httpClient)
  {
    _secretKey = configuration["ReCaptcha:SecretKey"];
    _httpClient = httpClient;
  }

  public async Task<bool> VerifyReCaptcha(string token)
  {
    if (string.IsNullOrEmpty(token))
      return false;

    var parameters = new Dictionary<string, string>
        {
            {"secret", _secretKey},
            {"response", token}
        };

    var content = new FormUrlEncodedContent(parameters);
    var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
    var responseString = await response.Content.ReadAsStringAsync();

    // Parse the JSON response
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(responseString, options);

    return recaptchaResponse?.Success ?? false;
  }

  // Response model for recaptcha verification
  private class RecaptchaResponse
  {
    public bool Success { get; set; }
    public string[] ErrorCodes { get; set; }
    public double Score { get; set; }
    public string Action { get; set; }
  }
}

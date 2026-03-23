namespace STAYTRUST.Services
{
    public interface IImageValidationService
    {
        Task<ImageValidationResult> ValidateImageAsync(string imageUrl);
    }

    public class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal ConfidenceScore { get; set; }
        public ImageQualityIssues Issues { get; set; } = new();
    }

    public class ImageQualityIssues
    {
        public bool IsBlurry { get; set; }
        public bool ContainsSensitiveContent { get; set; }
        public bool IsFraudulent { get; set; }
        public List<string> Details { get; set; } = new();
    }
}

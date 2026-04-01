using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu đăng nhập (JWT cookie)
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // Các định dạng ảnh được phép
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Upload ảnh đại diện (avatar). Lưu vào wwwroot/uploads/avatars/ và trả về URL công khai.
        /// </summary>
        [HttpPost("avatar")]
        [RequestSizeLimit(6 * 1024 * 1024)] // cho phép tối đa 6MB để middleware không cắt trước validate
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            // 1. Kiểm tra file có được gửi lên không
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file ảnh." });

            // 2. Kiểm tra kích thước
            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { message = "Ảnh quá lớn. Vui lòng chọn ảnh nhỏ hơn 5MB." });

            // 3. Kiểm tra định dạng (MIME type)
            if (!AllowedContentTypes.Contains(file.ContentType))
                return BadRequest(new { message = "Chỉ chấp nhận file ảnh (JPG, PNG, GIF, WEBP)." });

            // 4. Xây dựng đường dẫn lưu file
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder); // tạo thư mục nếu chưa có

            // Tạo tên file ngẫu nhiên để tránh trùng lặp
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            // Đảm bảo extension hợp lệ
            if (string.IsNullOrEmpty(extension) || extension == ".")
                extension = ".jpg";

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // 5. Ghi file xuống disk
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // 6. Trả về URL công khai (tương đối, không cần domain)
            var publicUrl = $"/uploads/avatars/{fileName}";
            return Ok(new { url = publicUrl });
        }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace STAYTRUST.Models.DTOs
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Căn cước công dân")]
        public string? IdentityNumber { get; set; }

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Địa chỉ thường trú")]
        public string? Address { get; set; }

        public string? AvatarUrl { get; set; }
        
        public IBrowserFile? AvatarFile { get; set; }
    }
}

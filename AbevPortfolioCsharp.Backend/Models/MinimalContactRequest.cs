using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbevPortfolioCsharp.Backend.Models
{
    public class MinimalContactRequest
    {
        [Required, StringLength(80)] public string Name { get; set; } = "";
        [Required, EmailAddress, StringLength(120)] public string Email { get; set; } = "";
        [Required, Url, StringLength(300)] public string VerificationUrl { get; set; } = "";
        [StringLength(500)] public string? Message { get; set; }
        public string? Hp { get; set; }
        public int SecondsToSubmit { get; set; }
        public string? TurnstileToken { get; set; }
    }
}

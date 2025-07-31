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
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string VerificationUrl { get; set; } = "";
        public string? Message { get; set; }
        public string? Hp { get; set; }
        public int SecondsToSubmit { get; set; }
        public string? TurnstileToken { get; set; }
    }

}

using System.ComponentModel.DataAnnotations;

namespace CurlToSharp.Models
{
    public class ConvertModel
    {
        [Required]
        public string Curl { get; set; }
    }
}

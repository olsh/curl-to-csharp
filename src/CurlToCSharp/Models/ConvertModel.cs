using System.ComponentModel.DataAnnotations;

namespace CurlToCSharp.Models
{
    public class ConvertModel
    {
        [Required]
        [MaxLength(4000)]
        public string Curl { get; set; }
    }
}

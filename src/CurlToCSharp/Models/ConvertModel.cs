using System.ComponentModel.DataAnnotations;

namespace CurlToCSharp.Models
{
    public class ConvertModel
    {
        [Required]
        public string Curl { get; set; }
    }
}

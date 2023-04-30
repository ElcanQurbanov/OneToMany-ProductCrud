using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entity_Framework_Slider.Models
{
    public class Product: BaseEntity
    {
        [Required]
        public string Name { get; set; }
        //[Required]
        public decimal Price { get; set; }
        [Required]
        public int Count { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public ICollection<ProductImage> Images { get; set; }
        public Category Category { get; set; }
        [NotMapped]
        public IFormFile Photo { get; set; }
    }
}

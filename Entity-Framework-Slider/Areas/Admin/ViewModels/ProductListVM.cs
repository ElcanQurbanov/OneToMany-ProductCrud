﻿using Entity_Framework_Slider.Models;

namespace Entity_Framework_Slider.Areas.Admin.ViewModels
{
    public class ProductListVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string Description { get; set; }
        public string MainImage { get; set; }
        public string CategoryName { get; set; }
    }
}

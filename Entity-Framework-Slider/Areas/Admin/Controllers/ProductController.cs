using Entity_Framework_Slider.Areas.Admin.ViewModels;
using Entity_Framework_Slider.Data;
using Entity_Framework_Slider.Helpers;
using Entity_Framework_Slider.Models;
using Entity_Framework_Slider.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Entity_Framework_Slider.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public ProductController(IProductService productService,
                                ICategoryService categoryService,
                                IWebHostEnvironment env,
                                AppDbContext context)
        {
            _productService = productService;
            _categoryService = categoryService;
            _env = env;
            _context = context;
        }
        public async Task<IActionResult> Index(int page = 1, int take = 4)
        {
            ViewBag.Count = page * take - take;

            List<Product> products = await _productService.GetPaginatedDatas(page,take);

            List<ProductListVM> mappedDatas = GetMappedDatas(products);

            int pageCount = await GetPageCountAsync(take);

            Paginate<ProductListVM> paginatedDatas = new Paginate<ProductListVM>(mappedDatas, page, pageCount); 

            return View(paginatedDatas);

        }

        private async Task<int> GetPageCountAsync(int take)
        {
            var productCount = await _productService.GetCountAsync();
            return (int) Math.Ceiling((decimal)productCount / take);
        }

        private List<ProductListVM> GetMappedDatas(List<Product> products)
        {
            List<ProductListVM> mappedDatas = new();

            foreach (var product in products)
            {
                ProductListVM productVM = new()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Count = product.Count,
                    CategoryName = product.Category.Name,
                    MainImage = product.Images.Where(m => m.IsMain).FirstOrDefault()?.Image
                };
                mappedDatas.Add(productVM);
            }

            return mappedDatas;
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.categories = await GetCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateVM model)
        {
            try
            {
                ViewBag.categories = await GetCategoriesAsync();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                foreach (var photo in model.Photos)
                {
                    if (!photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View();
                    }

                    if (!photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 200kb");
                        return View();
                    }
                }

                List<ProductImage> productImages = new();

                foreach (var photo in model.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "-" + photo.FileName;

                    string path = FileHelper.GetFilePath(_env.WebRootPath, "img", fileName);

                    await FileHelper.SaveFileAsync(path, photo);

                    ProductImage productImage = new()
                    {
                        Image = fileName,
                    };

                    productImages.Add(productImage);
                }

                productImages.FirstOrDefault().IsMain = true;

                decimal convertedPrice = decimal.Parse(model.Price.Replace(".",","));

                Product newProduct = new()
                {
                    Name = model.Name,
                    Price = convertedPrice,
                    Count = model.Count,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    Images = productImages
                };

                await _context.ProductImages.AddRangeAsync(productImages);
                await _context.Products.AddAsync(newProduct);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));

            }
            catch (Exception)
            {

                throw;
            }
            
        }

        private async Task<SelectList> GetCategoriesAsync()
        {
            IEnumerable<Category> categories = await _categoryService.GetAll();
            return new SelectList(categories, "Id", "Name");
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest(); 

            Product product = await _productService.GetFullDataById((int)id);

            if (product == null) return NotFound();

            ViewBag.desc = Regex.Replace(product.Description, "<.*?>", String.Empty);

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteProduct(int? id)
        {
            Product product = await _productService.GetFullDataById((int)id);

            foreach (var item in product.Images)
            {
                string path = FileHelper.GetFilePath(_env.WebRootPath, "img", item.Image);

                FileHelper.DeleteFile(path);
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return BadRequest();

            Product product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);

            if (product is null) return NotFound();

            ViewBag.desc = Regex.Replace(product.Description, "<.*?>", String.Empty);

            ViewBag.categories = await GetCategoriesAsync();

            return View(product);
        }
       

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            Product dbProduct = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);

            if (dbProduct is null) return NotFound();

            ViewBag.desc = Regex.Replace(dbProduct.Description, "<.*?>", String.Empty);

            ViewBag.categories = await GetCategoriesAsync();

            return View(new ProductUpdateVM
            {
                Name = dbProduct.Name,
                Description = dbProduct.Description,
                Price = dbProduct.Price.ToString("0.#####").Replace(",", "."),
                Count = dbProduct.Count,
                CategoryId = dbProduct.CategoryId,
                Images = dbProduct.Images,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ProductUpdateVM product)
        {
            try
            {
                ViewBag.categories = await GetCategoriesAsync();


                if (!ModelState.IsValid)
                {
                    return View(product);
                }

                Product dbProduct = await _productService.GetFullDataById((int)id);


                foreach (var photo in product.Photos)
                {
                    if (!photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View();
                    }

                    if (!photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 200kb");
                        return View();
                    }
                }

                foreach (var photo in dbProduct.Images)
                {
                    string path = FileHelper.GetFilePath(_env.WebRootPath, "img", photo.Image);
                    FileHelper.DeleteFile(path);
                }

                List<ProductImage> productImages = new();

                foreach (var photo in product.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "-" + photo.FileName;

                    string path = FileHelper.GetFilePath(_env.WebRootPath, "img", fileName);

                    await FileHelper.SaveFileAsync(path, photo);

                    ProductImage productImage = new()
                    {
                        Image = fileName,
                    };

                    productImages.Add(productImage);
                }

                productImages.FirstOrDefault().IsMain = true;

                dbProduct.Images = productImages;

                decimal convertedPrice = decimal.Parse(product.Price.Replace("," , "."));

                dbProduct.Name = product.Name;
                dbProduct.Description = product.Description;
                dbProduct.Price = convertedPrice;
                dbProduct.CategoryId = product.CategoryId;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));


                //if (dbProduct.Photo == product.Photo)
                //{
                //    return RedirectToAction(nameof(Index));
                //}

                //string path = FileHelper.GetFilePath(_env.WebRootPath, "img", fileName);



                //product.Image = fileName;
                //_context.Products.Update(product);


            }
            catch (Exception ex)
            {
                throw;
            }
        }



    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using PortfolioBackend.DTOs;
using System.IO;

namespace PortfolioBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, IWebHostEnvironment env, ILogger<ProductsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string? domain = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                if (!string.IsNullOrEmpty(domain))
                {
                    query = query.Where(p => p.Domain.ToLower() == domain.ToLower());
                }

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting product with id {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Products/upload
        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Manager")]
        [RequestSizeLimit(10 * 1024 * 1024)] // Max 10 MB
        public async Task<ActionResult<Product>> UploadProduct([FromForm] ProductUploadDto dto)
        {
            try
            {
                if (dto.Image == null || dto.Image.Length == 0)
                    return BadRequest("Image is required.");

                var uploadsFolder = Path.Combine(_env.WebRootPath, "content");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                var product = new Product
                {
                    Title = dto.Title,
                    ShortDescription = dto.ShortDescription,
                    LongDescription = dto.LongDescription,
                    Domain = dto.Domain,
                    ImageUrl = $"/content/{fileName}"
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetProduct", new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateDto dto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Update fields
                product.Title = dto.Title;
                product.ShortDescription = dto.ShortDescription;
                product.LongDescription = dto.LongDescription;
                product.Domain = dto.Domain;
                product.UpdatedAt = DateTime.UtcNow;

                // Handle image update if provided
                if (dto.Image != null && dto.Image.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Upload new image
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "content");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    product.ImageUrl = $"/content/{fileName}";
                }

                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product with id {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Products/domains
        [HttpGet("domains")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductDomains()
        {
            try
            {
                var domains = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Domain))
                    .GroupBy(p => p.Domain)
                    .Select(g => new
                    {
                        domain = g.Key,
                        count = g.Count(),
                        products = g.Take(3).Select(p => new
                        {
                            id = p.Id,
                            title = p.Title,
                            imageUrl = p.ImageUrl
                        })
                    })
                    .OrderByDescending(d => d.count)
                    .ToListAsync();

                return Ok(domains);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product domains");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Delete associated image
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product with id {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
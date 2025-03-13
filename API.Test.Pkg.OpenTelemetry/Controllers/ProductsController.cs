using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Test.Pkg.OpenTelemetry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private static readonly List<string> Products = new()
        {
            "Laptop", "Smartphone", "Tablet"
        };

        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ILogger<ProductsController> logger)
        {
            _logger = logger;
        }

        // ✅ GET /api/products
        [HttpGet]
        public IActionResult GetProducts()
        {
            _logger.LogInformation("Fetching all products");
            return Ok(Products);
        }

        // ✅ GET /api/products/{id}
        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            if (id < 0 || id >= Products.Count)
            {
                _logger.LogWarning("Product ID {Id} not found", id);
                return NotFound($"Product with ID {id} not found.");
            }

            _logger.LogInformation("Fetched product {Product}", Products[id]);
            return Ok(Products[id]);
        }

        // ✅ POST /api/products
        [HttpPost]
        public IActionResult AddProduct([FromBody] string product)
        {
            if (string.IsNullOrEmpty(product))
            {
                _logger.LogWarning("Attempt to add empty product.");
                return BadRequest("Product name cannot be empty.");
            }

            Products.Add(product);
            _logger.LogInformation("Added new product: {Product}", product);
            return CreatedAtAction(nameof(GetProducts), new { product });
        }

        // ✅ PUT /api/products/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] string newProduct)
        {
            if (id < 0 || id >= Products.Count)
            {
                _logger.LogWarning("Attempt to update non-existing product ID {Id}", id);
                return NotFound($"Product with ID {id} not found.");
            }

            Products[id] = newProduct;
            _logger.LogInformation("Updated product ID {Id} to {NewProduct}", id, newProduct);
            return NoContent();
        }

        // ✅ DELETE /api/products/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            if (id < 0 || id >= Products.Count)
            {
                _logger.LogWarning("Attempt to delete non-existing product ID {Id}", id);
                return NotFound($"Product with ID {id} not found.");
            }

            var removedProduct = Products[id];
            Products.RemoveAt(id);
            _logger.LogInformation("Deleted product {Product}", removedProduct);
            return NoContent();
        }

        [HttpGet("test-exception")]
        public IActionResult TestException()
        {
            throw new Exception("This is a test exception for OpenTelemetry.");
        }

    }
}

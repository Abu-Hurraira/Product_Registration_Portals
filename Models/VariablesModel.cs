using System;
namespace Product_Registration_Portal.Models
{
    public class User
    {
        public string ProfileImage { get; set; }

        public int UserId { get; set; }   // PK (if needed)

        // Basic info
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Auth info
        public string Email { get; set; }
        public string Password { get; set; }

        // Optional details
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        // Role & Status
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }



// ===== Categories =====
public class CategoryModel
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Discription { get; set; }  // matches DB spelling
        public bool IsActive { get; set; }
    }

    // ===== Products =====
    public class ProductModel
    {
        public int ProductID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } // join helper
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImagePath { get; set; }
        public string HoverImagePath { get; set; }
        public string Brand { get; set; }
        public decimal? Rating { get; set; }
        public bool IsProductActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ===== Orders =====
    public class OrderModel
    {
        public int OrderID { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
    }
}

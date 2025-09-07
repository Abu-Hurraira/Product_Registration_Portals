using System;
using System.Collections.Generic;

namespace Product_Registration_Portal.Models
{
    // Used for displaying full order with customer + items (like receipt/invoice)
    public class OrderItemViewModel
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }

        // List of items inside this order
        public List<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
    }

    // Represents each product inside an order
    public class OrderItemModel
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public string ProductTitle { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Auto-calculated (not stored in DB)
        public decimal TotalAmount => Quantity * UnitPrice;
    }
}

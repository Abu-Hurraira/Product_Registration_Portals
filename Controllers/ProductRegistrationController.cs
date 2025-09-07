using Product_Registration_Portal.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.IO;                // For Path, Directory
using System.Linq;              // For Contains() on arrays
using System.Web;               // For HttpPostedFileBase
 // For Controller, ActionResult, etc.


namespace Product_Registration_Portal.Controllers
{
    public class ProductRegistrationController : Controller
    {
        string ConnectionString = "Server=HURRAIRA\\SQLEXPRESS;Database=ProductRegistrationPortalDB;Trusted_Connection=True;";

        [HttpGet]
        public ActionResult SignUp() => View();

        [HttpPost]
        public ActionResult SignUp(User user)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation Error";
                return View(user);
            }

            // ===== Password Validation =====
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                ViewBag.Error = "Password is required.";
                return View(user);
            }

            user.Password = user.Password.Trim();

            if (!Regex.IsMatch(user.Password, @"^[A-Za-z]{2}[0-9]{6}$"))
            {
                ViewBag.Error = "Password must be 2 letters followed by 6 digits (e.g., US098765).";
                return View(user);
            }
            // ===============================

            // Default role if not provided
            if (string.IsNullOrEmpty(user.Role))
                user.Role = "User";

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Users 
                        (FirstName, LastName, Email, Password, Phone, Address, City, Country, Role, IsActive) 
                        VALUES (@FirstName, @LastName, @Email, @Password, @Phone, @Address, @City, @Country, @Role, @IsActive)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", user.LastName);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);
                    cmd.Parameters.AddWithValue("@Phone", (object)user.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object)user.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@City", (object)user.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Country", (object)user.Country ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", user.Role);
                    cmd.Parameters.AddWithValue("@IsActive", user.IsActive);

                    cmd.ExecuteNonQuery();
                }

                TempData["Message"] = "Sign-up successful!";
                return RedirectToAction("Login");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    TempData["Error"] = "A user with this email already exists.";
                else
                    TempData["Error"] = "Database Error: " + ex.Message;

                return View(user);
            }
        }

        // ---------------- LOGIN ----------------
        [HttpGet]
        public ActionResult Login() => View();

        [HttpPost]
        public ActionResult Login(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Email and Password are required!";
                return View();
            }

            if (!Regex.IsMatch(Password, @"^[A-Za-z]{2}[0-9]{6}$"))
            {
                ViewBag.Error = "Password must be 2 letters followed by 6 digits (e.g., US098765).";
                return View();
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"SELECT UserId, Email, Role 
                 FROM Users 
                 WHERE Email = @Email COLLATE SQL_Latin1_General_CP1_CS_AS 
                   AND Password = @Password";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Session["UserID"] = reader["UserId"];   // ✅ FIXED
                    Session["UserEmail"] = reader["Email"].ToString();
                    Session["UserRole"] = reader["Role"].ToString();

                    TempData["LoginMessage"] = "Login Successful!";
                    return RedirectToAction("UserDashboard");
                }
            }

            ViewBag.Error = "Invalid Email or Password!";
            return View();
        }

        // ------------- DASHBOARD -------------
        [HttpGet]
        public ActionResult UserDashboard()
        {
            if (Session["UserRole"] == null)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "ProductRegistration");
            }

            string email = Session["UserEmail"].ToString();
            string role = Session["UserRole"].ToString();
            List<User> data = new List<User>();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = role.Equals("admin", StringComparison.OrdinalIgnoreCase)
                    ? "SELECT * FROM Users"
                    : "SELECT * FROM Users WHERE Email = @Email";

                SqlCommand cmd = new SqlCommand(query, con);
                if (!role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    cmd.Parameters.AddWithValue("@Email", email);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    data.Add(new User
                    {
                        UserId = Convert.ToInt32(dr["UserId"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        Email = dr["Email"].ToString(),
                        Phone = dr["Phone"].ToString(),
                        Address = dr["Address"].ToString(),
                        City = dr["City"].ToString(),
                        Country = dr["Country"].ToString(),
                        Role = dr["Role"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        // ✅ Profile image with fallback
                        ProfileImage = dr["ProfileImage"] == DBNull.Value
                            ? "~/Content/images/default-profile.png"
                            : dr["ProfileImage"].ToString()
                    });
                }
            }

            return View(data);
        }

        // ---------------- HOME (Default) ----------------
        [HttpGet]
        public ActionResult Index()
        {
            var products = new List<ProductModel>();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"SELECT p.ProductID, p.Title, p.Description, p.Price, p.ImagePath, p.HoverImagePath,
                                p.Brand, p.Rating, p.IsProductActive, c.CategoryName
                         FROM Products p
                         INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                         WHERE p.IsProductActive = 1
                         ORDER BY p.CreatedAt DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    products.Add(new ProductModel
                    {
                        ProductID = Convert.ToInt32(dr["ProductID"]),
                        Title = dr["Title"].ToString(),
                        Description = dr["Description"].ToString(),
                        Price = Convert.ToDecimal(dr["Price"]),
                        ImagePath = dr["ImagePath"].ToString(),
                        HoverImagePath = dr["HoverImagePath"].ToString(),
                        Brand = dr["Brand"].ToString(),
                        Rating = dr["Rating"] != DBNull.Value ? (decimal?)Convert.ToDecimal(dr["Rating"]) : null,
                        CategoryName = dr["CategoryName"].ToString(),
                        IsProductActive = Convert.ToBoolean(dr["IsProductActive"])
                    });
                }
            }

            return View(products);
        }

        // ---------------- Logout ----------------

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "ProductRegistration"); // ✅ go back to ProductRegistration Index
        }

        // ---------------- USER EDIT (GET) ----------------
        [HttpGet]
        public ActionResult EditUser(int id)
        {
            User user = null;
            using (var con = new SqlConnection(ConnectionString))
            {
                string query = "SELECT * FROM Users WHERE UserId=@UserId";
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", id);
                    con.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            user = new User
                            {
                                UserId = (int)dr["UserId"],
                                FirstName = dr["FirstName"].ToString(),
                                LastName = dr["LastName"].ToString(),
                                Email = dr["Email"].ToString(),
                                Password = dr["Password"].ToString(),
                                Phone = dr["Phone"]?.ToString(),
                                Address = dr["Address"]?.ToString(),
                                City = dr["City"]?.ToString(),
                                Country = dr["Country"]?.ToString(),
                                Role = dr["Role"].ToString(),
                                IsActive = (bool)dr["IsActive"],
                                ProfileImage = dr["ProfileImage"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("UserList");
            }

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(User user, HttpPostedFileBase ProfileImageFile)
        {
            string profilePath = user.ProfileImage;

            // Profile Image Upload
            if (ProfileImageFile != null && ProfileImageFile.ContentLength > 0)
            {
                var ext = Path.GetExtension(ProfileImageFile.FileName)?.ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Invalid profile image format.";
                    return View(user);
                }

                var uploadsDir = Server.MapPath("~/Uploads/Profiles");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var path = Path.Combine(uploadsDir, fileName);
                ProfileImageFile.SaveAs(path);
                profilePath = $"/Uploads/Profiles/{fileName}";
            }

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                {
                    string query = @"UPDATE Users SET 
                    FirstName=@FirstName, LastName=@LastName, Email=@Email, 
                    Phone=@Phone, Address=@Address, City=@City, 
                    Country=@Country, Role=@Role, IsActive=@IsActive, ProfileImage=@ProfileImage
                    WHERE UserId=@UserId";   // ❌ removed Password

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", user.UserId);
                        cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", user.LastName);
                        cmd.Parameters.AddWithValue("@Email", user.Email);
                        cmd.Parameters.AddWithValue("@Phone", (object)user.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object)user.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@City", (object)user.City ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Country", (object)user.Country ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Role", user.Role);
                        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                        
                        // ✅ Handle ProfileImage properly
                        if (ProfileImageFile != null && ProfileImageFile.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(ProfileImageFile.FileName);
                            string savePath = Path.Combine(Server.MapPath("~/Content/images/"), fileName);
                            ProfileImageFile.SaveAs(savePath);

                            cmd.Parameters.AddWithValue("@ProfileImage", "~/Content/images/" + fileName);
                        }
                        else
                        {
                            // Keep old image if not updated
                            cmd.Parameters.AddWithValue("@ProfileImage", (object)user.ProfileImage ?? DBNull.Value);
                        }

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                // ✅ make sure name matches what your view checks
                TempData["SuccessMessage"] = "✅ User updated successfully!";
                return RedirectToAction("UserDashboard");
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Database error: " + ex.Message;
                return View(user);
            }
        }


        // ---------------- PRODUCT REGISTER (GET) ----------------
        [HttpGet]
        public ActionResult ProductRegister()
        {
            if (Session["UserRole"] == null ||
                !string.Equals(Session["UserRole"].ToString(), "admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToAction("Dashboard");
            }

            LoadCategoriesAndProducts();
            return View();
        }


        // ---------------- PRODUCT REGISTER (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductRegister(ProductModel product, HttpPostedFileBase ImageFile, HttpPostedFileBase HoverImageFile)
        {
            if (Session["UserRole"] == null ||
                !Session["UserRole"].ToString().Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You are not authorized to register products.";
                return RedirectToAction("Dashboard");
            }

            if (string.IsNullOrWhiteSpace(product.Title) ||
                product.CategoryID <= 0 ||
                product.Price <= 0 ||
                product.StockQuantity < 0)
            {
                TempData["Error"] = "Title, Category, Price (>0), and Stock (≥0) are required.";
                LoadCategoriesAndProducts();
                return View(product);
            }

            string savedPath = null;
            string hoverSavedPath = null;

            try
            {
                var uploadsDir = Server.MapPath("~/Uploads/Products");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                // Main image
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var ext = Path.GetExtension(ImageFile.FileName)?.ToLower();
                    var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!ok.Contains(ext))
                        throw new Exception("Main image: invalid file type.");

                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsDir, fileName);
                    ImageFile.SaveAs(fullPath);
                    savedPath = $"/Uploads/Products/{fileName}";
                }

                // Hover image
                if (HoverImageFile != null && HoverImageFile.ContentLength > 0)
                {
                    var ext = Path.GetExtension(HoverImageFile.FileName)?.ToLower();
                    var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!ok.Contains(ext))
                        throw new Exception("Hover image: invalid file type.");

                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsDir, fileName);
                    HoverImageFile.SaveAs(fullPath);
                    hoverSavedPath = $"/Uploads/Products/{fileName}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Image upload failed: " + ex.Message;
                LoadCategoriesAndProducts();
                return View(product);
            }

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(@"
            INSERT INTO Products
                (Title, Description, CategoryID, Price, StockQuantity, ImagePath, HoverImagePath, Brand, Rating, IsActive, CreatedAt, UpdatedAt)
            VALUES
                (@Title, @Description, @CategoryID, @Price, @StockQuantity, @ImagePath, @HoverImagePath, @Brand, @Rating, @IsActive, GETDATE(), NULL)", con))
                {
                    cmd.Parameters.AddWithValue("@Title", product.Title.Trim());
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(product.Description) ? (object)DBNull.Value : product.Description);
                    cmd.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                    cmd.Parameters.AddWithValue("@Price", product.Price);
                    cmd.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
                    cmd.Parameters.AddWithValue("@ImagePath", (object)savedPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HoverImagePath", (object)hoverSavedPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Brand", string.IsNullOrEmpty(product.Brand) ? (object)DBNull.Value : product.Brand);
                    cmd.Parameters.AddWithValue("@Rating", product.Rating.HasValue ? (object)product.Rating.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", product.IsProductActive);

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    TempData["Message"] = rows > 0
                        ? "✅ Product registered successfully!"
                        : "⚠️ Insert failed. No rows affected.";
                }

                return RedirectToAction("ProductRegister");
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Database error: " + ex.Message;
                LoadCategoriesAndProducts();
                return View(product);
            }
        }
        // ---------------- EDIT PRODUCT (GET) ----------------
        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            // Load categories
            List<CategoryModel> categories = new List<CategoryModel>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string catQuery = "SELECT CategoryID, CategoryName FROM Categories WHERE IsActive=1";
                SqlCommand catCmd = new SqlCommand(catQuery, con);
                con.Open();
                SqlDataReader catReader = catCmd.ExecuteReader();
                while (catReader.Read())
                {
                    categories.Add(new CategoryModel
                    {
                        CategoryID = (int)catReader["CategoryID"],
                        CategoryName = catReader["CategoryName"].ToString()
                    });
                }
            }
            ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName");

            // Load product
            ProductModel product = null;
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = "SELECT * FROM Products WHERE ProductID=@ProductID";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ProductID", id);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    product = new ProductModel
                    {
                        ProductID = (int)dr["ProductID"],
                        Title = dr["Title"].ToString(),
                        Description = dr["Description"]?.ToString(),
                        CategoryID = (int)dr["CategoryID"],
                        Price = (decimal)dr["Price"],
                        StockQuantity = (int)dr["StockQuantity"],
                        ImagePath = dr["ImagePath"]?.ToString(),
                        HoverImagePath = dr["HoverImagePath"] == DBNull.Value ? null : dr["HoverImagePath"].ToString(),
                        Brand = dr["Brand"]?.ToString(),
                        Rating = dr["Rating"] != DBNull.Value ? (decimal?)dr["Rating"] : null,
                        IsProductActive = (bool)dr["IsActive"]
                    };
                }
            }

            if (product == null)
            {
                TempData["Error"] = "⚠️ Product not found.";
                return RedirectToAction("ProductRegister");
            }

            return View("ProductRegister", product);
        }


        // ---------------- UPDATE PRODUCT (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProduct(ProductModel product, HttpPostedFileBase ImageFile, HttpPostedFileBase HoverImageFile)
        {
            if (string.IsNullOrWhiteSpace(product.Title) ||
                product.CategoryID <= 0 ||
                product.Price <= 0 ||
                product.StockQuantity < 0)
            {
                TempData["Error"] = "Title, Category, Price (>0), and Stock (≥0) are required.";
                return RedirectToAction("EditProduct", new { id = product.ProductID });
            }

            string imagePath = product.ImagePath;
            string hoverImagePath = product.HoverImagePath;

            // Main image
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                var ext = Path.GetExtension(ImageFile.FileName)?.ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Only JPG, PNG, GIF, or WEBP images are allowed.";
                    return RedirectToAction("EditProduct", new { id = product.ProductID });
                }

                var uploadsDir = Server.MapPath("~/Uploads/Products");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var path = Path.Combine(uploadsDir, fileName);
                ImageFile.SaveAs(path);
                imagePath = $"/Uploads/Products/{fileName}";
            }

            // Hover image
            if (HoverImageFile != null && HoverImageFile.ContentLength > 0)
            {
                var ext = Path.GetExtension(HoverImageFile.FileName)?.ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Only JPG, PNG, GIF, or WEBP images are allowed.";
                    return RedirectToAction("EditProduct", new { id = product.ProductID });
                }

                var uploadsDir = Server.MapPath("~/Uploads/Products");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var path = Path.Combine(uploadsDir, fileName);
                HoverImageFile.SaveAs(path);
                hoverImagePath = $"/Uploads/Products/{fileName}";
            }

            try
            {
                using (var con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    string query = @"UPDATE Products SET 
                     Title = @Title,
                     Description = @Description,
                     CategoryID = @CategoryID,
                     StockQuantity = @StockQuantity,
                     Price = @Price,
                     ImagePath = @ImagePath,
                     HoverImagePath = @HoverImagePath,
                     Brand = @Brand,
                     Rating = @Rating,
                     IsActive = @IsActive,
                     UpdatedAt = GETDATE()
                  WHERE ProductID = @ProductID";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", product.ProductID);
                        cmd.Parameters.AddWithValue("@Title", product.Title.Trim());
                        cmd.Parameters.AddWithValue("@Description", (object)product.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryID", product.CategoryID);
                        cmd.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@ImagePath", (object)imagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HoverImagePath", (object)hoverImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Brand", (object)product.Brand ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Rating", product.Rating.HasValue ? (object)product.Rating.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsProductActive);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Message"] = "✅ Product updated successfully!";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Database error: " + ex.Message;
            }

            return RedirectToAction("ProductRegister");
        }


        // ---------------- DELETE PRODUCT ----------------
        [HttpGet]
        public ActionResult DeleteProduct(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = "DELETE FROM Products WHERE ProductID=@ProductID";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@ProductID", id);
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    TempData["Message"] = rows > 0
                        ? "✅ Product deleted successfully!"
                        : "⚠️ Product not found.";
                }
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Database error: " + ex.Message;
            }

            return RedirectToAction("ProductRegister");
        }


        // ---------------- LOW STOCK PRODUCTS ----------------
        public ActionResult LowStockProducts()
        {
            List<ProductModel> lowStockProducts = new List<ProductModel>();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();
                string query = "SELECT * FROM Products WHERE StockQuantity < 5";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lowStockProducts.Add(new ProductModel
                    {
                        ProductID = (int)reader["ProductID"],
                        Title = reader["Title"].ToString(),
                        Description = reader["Description"]?.ToString(),
                        StockQuantity = (int)reader["StockQuantity"],
                        Price = (decimal)reader["Price"],
                        ImagePath = reader["ImagePath"]?.ToString(),
                        HoverImagePath = reader["HoverImagePath"] == DBNull.Value ? null : reader["HoverImagePath"].ToString(),
                        Brand = reader["Brand"]?.ToString(),
                        Rating = reader["Rating"] != DBNull.Value ? (decimal?)reader["Rating"] : null,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? (DateTime?)reader["UpdatedAt"] : null,
                        IsProductActive = (bool)reader["IsActive"],
                        CategoryID = (int)reader["CategoryID"]
                    });
                }
            }

            return View(lowStockProducts);
        }

        private void LoadCategoriesAndProducts()
        {
            // Load categories for dropdown
            List<CategoryModel> categories = new List<CategoryModel>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = "SELECT CategoryID, CategoryName FROM Categories WHERE IsActive = 1";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    categories.Add(new CategoryModel
                    {
                        CategoryID = (int)reader["CategoryID"],
                        CategoryName = reader["CategoryName"].ToString()
                    });
                }
            }
            ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName");

            // Load products for listing (optional for your ProductRegister page)
            List<ProductModel> products = new List<ProductModel>();
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"SELECT p.ProductID, p.Title, p.Description, p.Price, p.StockQuantity,
                                p.ImagePath, p.HoverImagePath, p.Brand, p.Rating,
                                p.IsActive, p.CreatedAt, p.UpdatedAt, c.CategoryName
                         FROM Products p
                         INNER JOIN Categories c ON p.CategoryID = c.CategoryID
                         ORDER BY p.CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    products.Add(new ProductModel
                    {
                        ProductID = (int)dr["ProductID"],
                        Title = dr["Title"].ToString(),
                        Description = dr["Description"]?.ToString(),
                        Price = (decimal)dr["Price"],
                        StockQuantity = (int)dr["StockQuantity"],
                        ImagePath = dr["ImagePath"]?.ToString(),
                        HoverImagePath = dr["HoverImagePath"] == DBNull.Value ? null : dr["HoverImagePath"].ToString(),
                        Brand = dr["Brand"]?.ToString(),
                        Rating = dr["Rating"] != DBNull.Value ? (decimal?)dr["Rating"] : null,
                        IsProductActive = (bool)dr["IsActive"],
                        CreatedAt = (DateTime)dr["CreatedAt"],
                        UpdatedAt = dr["UpdatedAt"] == DBNull.Value ? (DateTime?)null : (DateTime?)dr["UpdatedAt"],
                        CategoryName = dr["CategoryName"].ToString()
                    });
                }
            }
            ViewBag.Products = products;
        }

    }

}

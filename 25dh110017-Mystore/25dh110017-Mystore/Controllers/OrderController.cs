
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using _25dh110017_Mystore.Models;
using _25dh110017_Mystore.Models.Cart;
using _25dh110017_Mystore.Models.ViewModel;

namespace _25dh110017_Mystore.Controllers
{
    public class OrderController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();

        // ==========================================
        // HÀM HỖ TRỢ
        // ==========================================

        private CartService GetCartService()
        {
            return new CartService(Session);
        }

        private Customer GetCurrentCustomer()
        {
            if (Session["Username"] == null)
                return null;

            string username = Session["Username"].ToString();

            return db.Customers.FirstOrDefault(
                c => c.Username == username
            );
        }

        // ==========================================
        // 1. INDEX: /Order/Index
        // ==========================================

        public ActionResult Index()
        {
            return RedirectToAction("MyOrder");
        }

        // ==========================================
        // 2. HISTORY: /Order/History
        // Khắc phục lỗi HTTP 404
        // ==========================================

        public ActionResult History()
        {
            return RedirectToAction("MyOrder");
        }

        // ==========================================
        // 3. TRANG THANH TOÁN
        // GET: /Order/Checkout
        // ==========================================

        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = GetCartService().GetCart();

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutVM
            {
                Cart = cart
            };

            if (Session["Username"] != null)
            {
                var customer = GetCurrentCustomer();

                if (customer != null)
                {
                    model.CustomerName = customer.CustomerName;
                    model.Phone = customer.CustomerPhone;
                    model.AddressDelivery = customer.CustomerAddress;
                }
            }

            return View("Checkout", model);
        }

        // ==========================================
        // 4. XỬ LÝ ĐẶT HÀNG
        // POST: /Order/Checkout
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = GetCartService().GetCart();

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            model.Cart = cart;

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int customerId = 0;

                    // Khách hàng đã đăng nhập
                    if (Session["Username"] != null)
                    {
                        var customer = GetCurrentCustomer();

                        if (customer != null)
                        {
                            customerId = customer.CustomerID;
                            customer.CustomerName = model.CustomerName;
                            customer.CustomerPhone = model.Phone;
                            customer.CustomerAddress = model.AddressDelivery;
                        }
                    }

                    // Khách hàng chưa đăng nhập
                    if (customerId == 0)
                    {
                        var existingCustomer = db.Customers.FirstOrDefault(
                            c => c.CustomerPhone == model.Phone
                        );

                        if (existingCustomer != null)
                        {
                            customerId = existingCustomer.CustomerID;

                            existingCustomer.CustomerName = model.CustomerName;
                            existingCustomer.CustomerAddress = model.AddressDelivery;
                        }
                        else
                        {
                            Customer newCustomer = new Customer
                            {
                                CustomerName = model.CustomerName,
                                CustomerPhone = model.Phone,
                                CustomerAddress = model.AddressDelivery
                            };

                            db.Customers.Add(newCustomer);
                            db.SaveChanges();

                            customerId = newCustomer.CustomerID;
                        }
                    }

                    // Tạo đơn hàng
                    Order order = new Order
                    {
                        CustomerID = customerId,
                        OrderDate = DateTime.Now,
                        TotalAmount = cart.TotalValue(),
                        AddressDelivery = model.AddressDelivery,
                        PaymentStatus = "Chờ thanh toán"
                    };

                    db.Orders.Add(order);
                    db.SaveChanges();

                    // Lưu chi tiết đơn hàng
                    foreach (var item in cart.Items)
                    {
                        OrderDetail detail = new OrderDetail
                        {
                            OrderID = order.OrderID,
                            ProductID = item.product.ProductID,
                            Quantity = item.quantity,
                            UnitPrice = item.product.ProductPrice
                        };

                        db.OrderDetails.Add(detail);
                    }

                    db.SaveChanges();

                    transaction.Commit();

                    // Xóa giỏ hàng sau khi đặt thành công
                    GetCartService().ClearCart();

                    return RedirectToAction(
                        "OrderSuccess",
                        new { id = order.OrderID }
                    );
                }
                catch (DbEntityValidationException ex)
                {
                    transaction.Rollback();

                    foreach (var entityErrors in ex.EntityValidationErrors)
                    {
                        foreach (var error in entityErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(
                                "",
                                error.PropertyName + ": " + error.ErrorMessage
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string message = ex.Message;

                    if (ex.InnerException != null)
                    {
                        message += " - " + ex.InnerException.Message;
                    }

                    ModelState.AddModelError(
                        "",
                        "Không thể tạo đơn hàng: " + message
                    );
                }
            }

            return View("Checkout", model);
        }

        // ==========================================
        // 5. ĐẶT HÀNG THÀNH CÔNG
        // GET: /Order/OrderSuccess/5
        // ==========================================

        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // ==========================================
        // 6. ĐƠN HÀNG CỦA TÔI
        // GET: /Order/MyOrder
        // ==========================================

        public ActionResult MyOrder(
            string status = "All",
            string searchString = "",
            bool viewAll = false)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                ViewBag.CurrentStatus = status;
                ViewBag.SearchString = searchString;
                ViewBag.ViewAll = viewAll;

                return View("MyOrder", new List<Order>());
            }

            // Lấy đơn hàng của khách hàng
            var ordersQuery = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Where(o => o.CustomerID == customer.CustomerID)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hàng hoặc tên sản phẩm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                int orderId;

                if (int.TryParse(searchString, out orderId))
                {
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderID == orderId ||
                        o.OrderDetails.Any(d =>
                            d.Product != null &&
                            d.Product.ProductName.Contains(searchString)
                        )
                    );
                }
                else
                {
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderDetails.Any(d =>
                            d.Product != null &&
                            d.Product.ProductName.Contains(searchString)
                        )
                    );
                }
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                ordersQuery = ordersQuery.Where(
                    o => o.PaymentStatus == status
                );
            }

            // Sắp xếp đơn hàng mới nhất
            var orders = ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Mặc định hiển thị 2 đơn hàng
            if (!viewAll &&
                status != "All" &&
                string.IsNullOrEmpty(searchString))
            {
                orders = orders.Take(2).ToList();
            }

            // Xem thêm tối đa 50 đơn hàng
            if (viewAll && status != "All")
            {
                orders = orders.Take(50).ToList();
            }

            ViewBag.CurrentStatus = status;
            ViewBag.SearchString = searchString;
            ViewBag.ViewAll = viewAll;

            return View("MyOrder", orders);
        }

        // ==========================================
        // 7. CHI TIẾT ĐƠN HÀNG
        // GET: /Order/Details/5
        // ==========================================

        public ActionResult Details(int id)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            var customer = GetCurrentCustomer();

            string role = Session["UserRole"]?.ToString();

            // Chỉ chủ đơn hàng hoặc quản trị viên được xem
            if ((customer == null ||
                order.CustomerID != customer.CustomerID) &&
                role != "A")
            {
                return new HttpUnauthorizedResult();
            }

            return View(order);
        }

        // ==========================================
        // 8. MUA LẠI ĐƠN HÀNG
        // GET: /Order/Reorder/5
        // ==========================================

        public ActionResult Reorder(int id)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra quyền sở hữu
            if (order.CustomerID != customer.CustomerID)
            {
                return new HttpUnauthorizedResult();
            }

            var cartService = GetCartService();
            var cart = cartService.GetCart();

            // Thêm sản phẩm vào giỏ hàng
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    cart.AddItem(
                        detail.Product,
                        detail.Quantity
                    );
                }
            }

            return RedirectToAction("Index", "Cart");
        }

        // ==========================================
        // 9. HỦY ĐƠN HÀNG
        // POST: /Order/CancelOrder
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int id)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();

            if (customer == null)
            {
                return new HttpUnauthorizedResult();
            }

            var order = db.Orders.Find(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra quyền sở hữu
            if (order.CustomerID != customer.CustomerID)
            {
                return new HttpUnauthorizedResult();
            }

            // Chỉ được hủy đơn chưa giao
            if (order.PaymentStatus == "Đang xử lý" ||
                order.PaymentStatus == "Chờ thanh toán" ||
                order.PaymentStatus == "Pending")
            {
                order.PaymentStatus = "Đã hủy";

                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
            }

            return RedirectToAction("MyOrder");
        }

        // ==========================================
        // 10. GIẢI PHÓNG DATABASE
        // ==========================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

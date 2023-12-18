using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS1.Data;
using QLNS1.Models;

namespace QLNS1.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly QLNS1Context _context;

        public InvoicesController(QLNS1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return _context.Invoice != null ?
                        View(await _context.Invoice.ToListAsync()) :
                        Problem("Entity set 'QLNS1Context.Invoice'  is null.");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("MaHoaDon,TenKhachHang,TenSach,TheLoai,SoLuong,Gia,Date,sdt,Debt")] Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                var book = _context.Sach.FirstOrDefault(s => s.Name == invoice.TenSach && s.Type == invoice.TheLoai);
                if (book != null)
                {
                    var remainingQuantity = book.Amount;
                    if (remainingQuantity - invoice.SoLuong >= 10)
                    {
                        invoice.Gia = book.Price * invoice.SoLuong;
                        invoice.Date = DateTime.Now;
                        book.Amount -= invoice.SoLuong;
                        if (invoice.Debt == 1)
                        {
                            //TODO: update user debt
                            var user = _context.Users.FirstOrDefault(u => u.PhoneNumber == invoice.sdt);
                            user.TienNo += invoice.Gia;
                            user.NgayNo = DateTime.Now.ToString();
                            _context.Update(user);
                        }
                        _context.Add(invoice);
                        _context.Update(book);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("SoLuong", "Số lượng sách không đủ");
                        return View(invoice);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Invoice == null)
                return NotFound();

            var invoice = await _context.Invoice
                .FirstOrDefaultAsync(m => m.MaHoaDon == id);
            if (invoice == null)
                return NotFound();

            return View(invoice);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Invoice == null)
                return NotFound();

            var invoice = await _context.Invoice
                .FirstOrDefaultAsync(m => m.MaHoaDon == id);
            if (invoice == null)
                return NotFound();

            return View(invoice);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaHoaDon,TenKhachHang,TenSach,TheLoai,SoLuong,Gia,Date,sdt,Debt")] Invoice invoice)
        {
            invoice.MaHoaDon = id;
            if (ModelState.IsValid)
            {
                try
                {
                    var sach = _context.Sach.FirstOrDefault(s => s.Name == invoice.TenSach && s.Type == invoice.TheLoai);
                    if (sach != null)
                    {
                        var originalInvoice = await _context.Invoice.AsNoTracking().FirstOrDefaultAsync(i => i.MaHoaDon == id);
                        var quantityDifference = originalInvoice.SoLuong - invoice.SoLuong;

                        if (sach.Amount + quantityDifference >= 0)
                        {
                            invoice.Gia = sach.Price * invoice.SoLuong;
                            invoice.Date = DateTime.Now;
                            sach.Amount += quantityDifference;

                            if (originalInvoice.Debt == 1 && invoice.Debt == 0)
                            {
                                // TODO: deduct debt from user
                                var user = _context.Users.FirstOrDefault(u => u.PhoneNumber == invoice.sdt);
                                user.TienNo -= originalInvoice.Gia;
                                _context.Update(user);
                            }
                            else if (originalInvoice.Debt == 0 && invoice.Debt == 1)
                            {
                                // TODO: add debt to user
                                var user = _context.Users.FirstOrDefault(u => u.PhoneNumber == invoice.sdt);
                                user.TienNo += invoice.Gia;
                                user.NgayNo = DateTime.Now.ToString();
                                _context.Update(user);
                            }

                            _context.Update(invoice);
                            _context.Update(sach);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            ModelState.AddModelError("SoLuong", "Số lượng sách không đủ");
                            return View(invoice);
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.MaHoaDon))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(invoice);
        }

        private bool InvoiceExists(int id)
        {
            return (_context.Invoice?.Any(e => e.MaHoaDon == id)).GetValueOrDefault();
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Invoice == null)
                return NotFound();

            var invoice = await _context.Invoice
                .FirstOrDefaultAsync(m => m.MaHoaDon == id);
            if (invoice == null)
                return NotFound();

            return View(invoice);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Invoice == null)
                return Problem("Entity set 'QLNS1Context.Invoice' is null.");
            var invoice = await _context.Invoice.FindAsync(id);
            if (invoice != null)
                _context.Invoice.Remove(invoice);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult DebtReport()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DebtReport(IFormCollection form)
        {
            var selectedMonth = form["Thang"];
            var users = _context.Users.ToList();
            var viewModelList = new List<HienTienNo>();
            foreach (var user in users)
            {
                var hoaDon = _context.Invoice.FirstOrDefault(s => s.sdt == user.PhoneNumber);

                if (hoaDon != null)
                {
                    var tonCuoiThang = user.TienNo;
                    var phatSinh = _context.Invoice
                        .Where(o => o.Date.Month == Int32.Parse(selectedMonth) && o.sdt == user.PhoneNumber && o.Debt == 1)
                        .Sum(o => o.Gia);
                    var tonDauThang = TinhTonDauThang(user.PhoneNumber, selectedMonth);

                    var viewModel = new HienTienNo
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        NoCuoiThang = tonCuoiThang,
                        PhatSinh = phatSinh,
                        NoDauThang = tonDauThang
                    };                    
                    viewModelList.Add(viewModel);
                }
            }
            return View("List", viewModelList);

        }
        private int TinhTonDauThang(string sdt, string month)
        {
            var invoices = _context.Invoice.Where(s => s.sdt == sdt && s.Date.Month == Int32.Parse(month)).ToList();
            return invoices.Sum(o => o.Gia);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLNS1.Data;
using QLNS1.Models;

namespace QLNS1.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class BooksController : Controller
    {
        private readonly QLNS1Context _context;
        public BooksController(QLNS1Context context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return _context.Sach != null ?
                        View(await _context.Sach.ToListAsync()) :
                        Problem("Entity set 'QLNS1Context.Sach'  is null.");
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Display()
        {
            var book = await _context.Sach.ToListAsync();

            if (book == null || book.Count == 0)
                return NotFound();

            return View(book);
        }

        [AllowAnonymous]
        public IActionResult LookUp(string searchString)
        {
            var books = string.IsNullOrWhiteSpace(searchString)
                            ? _context.Sach.ToList()
                            : _context.Sach.Where(s => s.Name.Contains(searchString) || s.Author.Contains(searchString)).ToList();

            return View(books);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Name,Author,Type,Amount,Price,Picture")] Sach book)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Sach == null)
                return NotFound();

            var book = await _context.Sach
                .FirstOrDefaultAsync(m => m.SachId == id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Sach == null)
                return NotFound();

            var book = await _context.Sach.FindAsync(id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SachId,Name,Author,Type,Amount,Price,Picture")] Sach book)
        {
            book.SachId = id;
            if (!ModelState.IsValid)
                return View(book);
            try
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SachExists(book.SachId))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Sach == null)
                return NotFound();

            var book = await _context.Sach
                .FirstOrDefaultAsync(m => m.SachId == id);
            if (book == null)
                return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Sach == null)
                return Problem("Entity set 'QLNS1Context.Sach'  is null.");
            var book = await _context.Sach.FindAsync(id);
            if (book != null)
                _context.Sach.Remove(book);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Inventory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Inventory(IFormCollection form)
        {
            var selectedMonth = form["Thang"];
            var sachList = _context.Sach.ToList();
            var viewModelList = new List<HienHoaDon>();

            foreach (var sach in sachList)
            {
                var nhapSach = _context.Nhap.FirstOrDefault(s => s.TenSach == sach.Name);

                if (nhapSach != null)
                {
                    var tonCuoiNam = sach.Amount;
                    var phatSinh = _context.Nhap
                        .Where(o => o.DateImport.Month == Int32.Parse(selectedMonth) && o.TenSach == sach.Name)
                        .Sum(o => o.AmountImport);
                    var tonDauNam = TinhTonDauThang(sach.Name, selectedMonth);

                    var viewModel = new HienHoaDon
                    {
                        TenSach = sach.Name,
                        TonCuoiNam = tonCuoiNam,
                        PhatSinh = phatSinh,
                        TonDauNam = tonDauNam
                    };

                    viewModelList.Add(viewModel);
                }
            }

            return View("List", viewModelList);
        }

        private bool SachExists(int id)
        {
            return (_context.Sach?.Any(e => e.SachId == id)).GetValueOrDefault();
        }
        
        private int TinhTonDauThang(string tenSach, string thang)
        {
            var nhapSachTheoThang = _context.Nhap
                .Where(s => s.TenSach == tenSach && s.DateImport.Month == Int32.Parse(thang))
                .ToList();

            var nhapSachCuoiCungTheoThang = nhapSachTheoThang
                .OrderByDescending(o => o.DateImport.Day)
                .FirstOrDefault();

            var hoaDonThang = _context.Nhap
                .Where(o => o.DateImport.Month == Int32.Parse(thang))
                .ToList();

            var hoaDonTuNgayNhapCuoiCung = hoaDonThang
                .Where(o => o.DateImport.Day >= nhapSachCuoiCungTheoThang.DateImport.Day)
                .ToList();

            var tongSoLuongBan = hoaDonTuNgayNhapCuoiCung.Sum(o => o.AmountImport);
            var tonDauThang = nhapSachCuoiCungTheoThang.SachSauNhap - tongSoLuongBan;

            return tonDauThang;
        }

    }
}
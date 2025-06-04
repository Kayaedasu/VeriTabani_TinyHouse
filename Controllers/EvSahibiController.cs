using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TinyHouse.Models;
using TinyHouse.Repositories;
using TinyHouse.Data;

namespace TinyHouse.Controllers
{
    public class EvSahibiController : Controller
    {
        private readonly TinyHouseRepository _tinyHouseRepository;
        private readonly KonumRepository _konumRepository;
        private readonly DurumRepository _durumRepository;
        private readonly RezervasyonRepository _rezervasyonRepository;
        private readonly YorumRepository _yorumRepository;
        private readonly BildirimRepository _bildirimRepository;
        private readonly SqlConnectionFactory _connectionFactory;

        public EvSahibiController(
            RezervasyonRepository rezervasyonRepository,
            TinyHouseRepository tinyHouseRepository,
            KonumRepository konumRepository,
            DurumRepository durumRepository,
            YorumRepository yorumRepository,
            BildirimRepository bildirimRepository,
            SqlConnectionFactory connectionFactory) // DI ile alıyoruz
        {
            _rezervasyonRepository = rezervasyonRepository;
            _tinyHouseRepository = tinyHouseRepository;
            _konumRepository = konumRepository;
            _durumRepository = durumRepository;
            _yorumRepository = yorumRepository;
            _bildirimRepository = bildirimRepository;
            _connectionFactory = connectionFactory;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("KullaniciID");
        }

        public async Task<IActionResult> Ilanlar()
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var ilanlar = (await _tinyHouseRepository.GetAllAsync())
                           .Where(x => x.EvSahibiID == evSahibiId.Value)
                           .ToList();

            foreach (var ilan in ilanlar)
            {
                ilan.Fotolar = (await _tinyHouseRepository.GetFotosByEvIdAsync(ilan.EvID)).ToList();
            }

            var ortalamaPuanlar = await _tinyHouseRepository.GetOrtalamaPuanlarAsync();

            foreach (var ilan in ilanlar)
            {
                ilan.OrtalamaPuan = ortalamaPuanlar.ContainsKey(ilan.EvID) ? ortalamaPuanlar[ilan.EvID] : 0.0;
            }

            return View(ilanlar);
        }

        public IActionResult IlanEkle()
        {
            ViewBag.Konumlar = new SelectList(_konumRepository.GetAll(), "KonumID", "Sehir");
            ViewBag.Durumlar = new SelectList(_durumRepository.GetAll(), "DurumID", "DurumAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IlanEkle(EvSahibiTinyHouse model, IFormFile[] fotoğraflar)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            if (!ModelState.IsValid)
            {
                ViewBag.Konumlar = new SelectList(_konumRepository.GetAll(), "KonumID", "Sehir", model.KonumID);
                ViewBag.Durumlar = new SelectList(_durumRepository.GetAll(), "DurumID", "DurumAdi", model.DurumID);
                return View(model);
            }

            model.EvSahibiID = evSahibiId.Value;
            model.EklenmeTarihi = DateTime.Now;

            int yeniEvID = await _tinyHouseRepository.AddAsync(model);

            if (fotoğraflar != null && fotoğraflar.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var dosya in fotoğraflar)
                {
                    if (dosya.Length > 0)
                    {
                        var dosyaAdi = Guid.NewGuid() + Path.GetExtension(dosya.FileName);
                        var fizikselYol = Path.Combine(uploadsFolder, dosyaAdi);

                        using (var stream = new FileStream(fizikselYol, FileMode.Create))
                        {
                            await dosya.CopyToAsync(stream);
                        }

                        await _tinyHouseRepository.EvFotoEkleAsync(new EvFoto
                        {
                            EvID = yeniEvID,
                            FotoUrl = "/uploads/" + dosyaAdi
                        });
                    }
                }
            }

            return RedirectToAction(nameof(Ilanlar));
        }

        public async Task<IActionResult> IlanGuncelle(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var ilan = await _tinyHouseRepository.GetByIdAsync(id);
            if (ilan == null)
                return NotFound();

            if (ilan.EvSahibiID != evSahibiId)
                return Unauthorized();

            ilan.Fotolar = (await _tinyHouseRepository.GetFotosByEvIdAsync(id)).ToList();

            ViewBag.Konumlar = new SelectList(_konumRepository.GetAll(), "KonumID", "Sehir", ilan.KonumID);
            ViewBag.Durumlar = new SelectList(_durumRepository.GetAll(), "DurumID", "DurumAdi", ilan.DurumID);

            return View(ilan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IlanGuncelle(int id, EvSahibiTinyHouse model, IFormFile[] yeniFotograflar)
        {
            if (id != model.EvID)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Konumlar = new SelectList(_konumRepository.GetAll(), "KonumID", "Sehir", model.KonumID);
                ViewBag.Durumlar = new SelectList(_durumRepository.GetAll(), "DurumID", "DurumAdi", model.DurumID);
                model.Fotolar = (await _tinyHouseRepository.GetFotosByEvIdAsync(id)).ToList();
                return View(model);
            }

            await _tinyHouseRepository.UpdateAsync(model);

            if (yeniFotograflar != null && yeniFotograflar.Length > 0)
            {
                var eskiFotolar = await _tinyHouseRepository.GetFotosByEvIdAsync(id);

                foreach (var eskiFoto in eskiFotolar)
                {
                    var fizikselYol = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", eskiFoto.FotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(fizikselYol))
                        System.IO.File.Delete(fizikselYol);
                }

                await _tinyHouseRepository.EvFotoSilAsync(id);

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var dosya in yeniFotograflar)
                {
                    if (dosya.Length > 0)
                    {
                        var dosyaAdi = Guid.NewGuid() + Path.GetExtension(dosya.FileName);
                        var fizikselYolYeni = Path.Combine(uploadsFolder, dosyaAdi);

                        using (var stream = new FileStream(fizikselYolYeni, FileMode.Create))
                        {
                            await dosya.CopyToAsync(stream);
                        }

                        await _tinyHouseRepository.EvFotoEkleAsync(new EvFoto
                        {
                            EvID = id,
                            FotoUrl = "/uploads/" + dosyaAdi
                        });
                    }
                }
            }

            return RedirectToAction(nameof(Ilanlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IlanSil(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var ilan = await _tinyHouseRepository.GetByIdAsync(id);

            if (ilan == null)
                return NotFound();

            if (ilan.EvSahibiID != evSahibiId.Value)
                return Unauthorized();

            await _tinyHouseRepository.EvFotoSilAsync(id);
            await _tinyHouseRepository.DeleteAsync(id);

            return RedirectToAction(nameof(Ilanlar));
        }

        public async Task<IActionResult> Rezervasyonlar()
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var rezervasyonlar = await _rezervasyonRepository.GetByEvSahibiIdAsync(evSahibiId.Value);
            return View(rezervasyonlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RezervasyonOnayla(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var rezervasyon = await _rezervasyonRepository.GetByIdAsync(id);

            if (rezervasyon == null || rezervasyon.EvSahibiID != evSahibiId.Value)
                return Unauthorized();

            await _rezervasyonRepository.GuncelleDurumAsync(id, "Onaylandı");

            await _bildirimRepository.AddAsync(new Bildirim
            {
                KullaniciID = rezervasyon.KiraciID,
                Baslik = "Rezervasyon Onaylandı",
                Mesaj = $"Rezervasyonunuz onaylandı: {rezervasyon.RezervasyonID}",
                Okundu = false,
                OlusturmaTarihi = DateTime.Now
            });

            return RedirectToAction(nameof(Rezervasyonlar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RezervasyonReddet(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var rezervasyon = await _rezervasyonRepository.GetByIdAsync(id);

            if (rezervasyon == null || rezervasyon.EvSahibiID != evSahibiId.Value)
                return Unauthorized();

            await _rezervasyonRepository.GuncelleDurumAsync(id, "İptal");

            await _bildirimRepository.AddAsync(new Bildirim
            {
                KullaniciID = rezervasyon.KiraciID,
                Baslik = "Rezervasyon İptal Edildi",
                Mesaj = $"Rezervasyonunuz iptal edildi: {rezervasyon.RezervasyonID}",
                Okundu = false,
                OlusturmaTarihi = DateTime.Now
            });

            return RedirectToAction(nameof(Rezervasyonlar));
        }

        public async Task<IActionResult> RezervasyonDetay(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var rezervasyon = await _rezervasyonRepository.GetByIdAsync(id);

            if (rezervasyon == null || rezervasyon.EvSahibiID != evSahibiId.Value)
                return Unauthorized();

            string kiraciAdi = "";
            string durumAdi = "";
            string evFotoUrl = "";

            using (var conn = _connectionFactory.CreateConnection())
            {
                conn.Open();

                using var cmdKiraci = new SqlCommand("SELECT Ad + ' ' + Soyad FROM Kullanici WHERE KullaniciID = @KiraciID", (SqlConnection)conn);
                cmdKiraci.Parameters.AddWithValue("@KiraciID", rezervasyon.KiraciID);
                kiraciAdi = (string?)cmdKiraci.ExecuteScalar() ?? "";

                using var cmdDurum = new SqlCommand("SELECT DurumAdi FROM RezervasyonDurum WHERE DurumID = @DurumID", (SqlConnection)conn);
                cmdDurum.Parameters.AddWithValue("@DurumID", rezervasyon.DurumID);
                durumAdi = (string?)cmdDurum.ExecuteScalar() ?? "";

                using var cmdFoto = new SqlCommand("SELECT TOP 1 FotoUrl FROM EvFoto WHERE EvID = @EvID", (SqlConnection)conn);
                cmdFoto.Parameters.AddWithValue("@EvID", rezervasyon.IlanID);
                var foto = (string?)cmdFoto.ExecuteScalar();
                if (!string.IsNullOrEmpty(foto))
                    evFotoUrl = foto.StartsWith("/uploads/") ? foto : "/uploads/" + foto;
            }

            var model = new EvSahibiRezervasyon
            {
                RezervasyonID = rezervasyon.RezervasyonID,
                KiraciID = rezervasyon.KiraciID,
                EvID = rezervasyon.IlanID,
                DurumID = rezervasyon.DurumID,
                BaslangicTarihi = rezervasyon.BaslangicTarihi,
                BitisTarihi = rezervasyon.BitisTarihi,
                OlusturmaTarihi = rezervasyon.OlusturmaTarihi != default ? rezervasyon.OlusturmaTarihi : DateTime.Now,
                Aciklama = rezervasyon.Aciklama ?? rezervasyon.Baslik,
                DurumAdi = durumAdi,
                KiraciAdi = kiraciAdi,
                EvSahibiID = rezervasyon.EvSahibiID,
                EvFotoUrl = evFotoUrl,
                OdemeDurumu = rezervasyon.OdemeDurumu
            };

            return View(model);
        }

        public async Task<IActionResult> Yorumlar()
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var yorumlar = await _yorumRepository.GetYorumlarByEvSahibiAsync(evSahibiId.Value);
            return View(yorumlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YorumCevapla(int yorumId, string cevapMetni)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var cevap = new YorumCevap
            {
                YorumID = yorumId,
                EvSahibiID = evSahibiId.Value,
                CevapMetni = cevapMetni,
                CevapTarihi = DateTime.Now // tarih eklenmeli
            };

            await _yorumRepository.YorumCevapEkleAsync(cevap);

            // Bildirim ekleme - yorumu yapan kiracıya
            var yorum = await _yorumRepository.GetByIdAsync(yorumId); // Yorum verisini al
            if (yorum != null)
            {
                string baslik = "Yorumunuza cevap geldi";
                string mesaj = $"Ev sahibinizden yeni bir cevap: {cevapMetni}";
                await _bildirimRepository.AddAsync(new Bildirim
                {
                    KullaniciID = yorum.KiraciID,
                    Baslik = baslik,
                    Mesaj = mesaj,
                    Okundu = false,
                    OlusturmaTarihi = DateTime.Now
                });

                // İstersen mail gönderimini burada yapabilirsin
            }

            return RedirectToAction(nameof(Yorumlar));
        }


        public async Task<IActionResult> IlanDetay(int id)
        {
            int? evSahibiId = GetCurrentUserId();
            if (evSahibiId == null)
                return RedirectToAction("Giris", "Kullanici");

            var ilan = await _tinyHouseRepository.GetByIdAsync(id);
            if (ilan == null || ilan.EvSahibiID != evSahibiId.Value)
                return NotFound();

            ilan.Fotolar = (await _tinyHouseRepository.GetFotosByEvIdAsync(id)).ToList();

            var rezervasyonlar = await _rezervasyonRepository.GetByEvSahibiIdAsync(evSahibiId.Value);
            var yorumlar = await _yorumRepository.GetByEvIdAsync(id);

            var model = new IlanDetayViewModel
            {
                Ilan = ilan,
                Rezervasyonlar = rezervasyonlar.ToList(),
                Yorumlar = yorumlar.ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Bildirimler()
        {
            int? kullaniciId = GetCurrentUserId();
            if (kullaniciId == null)
                return RedirectToAction("Giris", "Kullanici");

            var bildirimler = await _bildirimRepository.GetByKullaniciIdAsync(kullaniciId.Value);
            return View(bildirimler);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BildirimOkundu(int id)
        {
            await _bildirimRepository.MarkAsReadAsync(id);
            return RedirectToAction(nameof(Bildirimler));
        }
    }
}

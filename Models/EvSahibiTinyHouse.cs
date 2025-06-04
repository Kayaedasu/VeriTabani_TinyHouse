using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TinyHouse.Models
{
    public class EvSahibiTinyHouse
    {
        public int EvID { get; set; }

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        public string Baslik { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        public string Aciklama { get; set; }

        [Required(ErrorMessage = "Fiyat alanı zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Lütfen geçerli bir sayı giriniz.")]
        public decimal Fiyat { get; set; }

        public int EvSahibiID { get; set; }
        public int KonumID { get; set; }
        public int DurumID { get; set; }
        public DateTime EklenmeTarihi { get; set; }

        public List<EvFoto> Fotolar { get; set; } = new List<EvFoto>();
        public double OrtalamaPuan { get; set; } = 0.0;
    }
}

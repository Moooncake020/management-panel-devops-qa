namespace YonetimPaneli.Models.Enums
{
    /// <summary>
    /// Görevlerin iş sırasını ve dikkat seviyesini tek bir standartla ifade eder.
    /// Sayısal değerlerin aralıklı tutulması, ileride yeni öncelik seviyeleri
    /// eklenirken mevcut verileri bozmadan genişleme yapmayı kolaylaştırır.
    /// </summary>
    public enum GorevOnceligi
    {
        Dusuk = 10,
        Normal = 20,
        Yuksek = 30,
        Kritik = 40
    }
}

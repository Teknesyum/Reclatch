# Reclatch — plan

Windows için hafif ekran kaydedici. Ad üç turluk aday taramasının sonucu: web ve
GitHub'da hiçbir sonuç dönmüyor, marka sorgusu tamamen boş.

## Ne olduğu

OBS sahne sistemi kurmadan, Bandicam parası ödemeden ekranı kaydeden bir araç.
Kapsam bilerek dar: kaydet, durdur, dosyayı ver. Montaj, yayın, overlay yok.

## v1 listesi

### Yakalama
1. Tam ekran / belirli pencere / seçili bölge
2. Çoklu monitör, monitör seçici
3. İmleci göster-gizle, tıklama vurgusu
4. Kare hızı (30/60), çözünürlük ve ölçekleme

### Ses
5. Sistem sesi (WASAPI loopback) + mikrofon, ayrı ayrı açılıp kapanır
6. Kaynak seçimi ve seviye göstergesi
7. Ayrı ses kanalı olarak yazma (montaj için)

### Kodlama
8. Donanım hızlandırma (NVENC / QuickSync / AMF), yazılım geri düşüşü
9. MP4 (H.264) varsayılan, bitrate veya kalite hedefi
10. Kayıt klasörü + dosya adı şablonu

### Kontrol
11. Global kısayollar: başlat / durdur / duraklat-devam
12. Canlı durum: süre, boyut, düşen kare
13. Geri sayım ve tepsiye küçülme
14. Çökme sonrası dosya bozulmaz (parçalı mp4 / kurtarma)

### Temel
15. Duraklat/devam, tek dosyada
16. Disk alanı kontrolü ve uyarı
17. Kayıt bitince klasörü aç / dosyayı oynat
18. Ayarların kalıcı olması

## v1 dışı

Webcam overlay, sahne/kaynak sistemi, canlı yayın (RTMP), oyun içi overlay,
çizim/annotation, GIF çıktısı, zamanlanmış kayıt, replay buffer.

## Teknik omurga

- **Yakalama:** Windows.Graphics.Capture. Pencere ve monitör yakalamanın desteklenen
  yolu bu; DXGI Desktop Duplication pencere yakalayamıyor, GDI yavaş ve DWM ile
  uyumsuz. WGC ayrıca yakalama sınırını (sarı çerçeve) işletim sisteminden alıyor.
- **Ses:** WASAPI loopback sistem sesi için, WASAPI capture mikrofon için. İkisi ayrı
  saat üzerinde çalıştığı için karıştırmadan önce yeniden örnekleme (resample) gerekir —
  bu, senkron kaymasının en olası kaynağı.
- **Kodlama:** Media Foundation Sink Writer donanım kodlayıcıyı kendisi seçebiliyor;
  NVENC/QuickSync/AMF'yi ayrı ayrı sarmalamak yerine önce bu denenmeli.

## Açık kararlar

**1. Yığın.** WGC ve WASAPI'nin ikisi de WinRT/COM. Adaylar:
   - **C# / .NET 8 + WinUI 3** — CsWinRT ile WGC doğrudan çağrılır, Runly'de zaten
     .NET var. Dağıtım için self-contained yayın gerekir.
   - **C# / .NET 8 + WPF** — daha oturmuş, daha az sürpriz; WGC yine çalışır,
     kompozisyon katmanı biraz elle bağlanır.
   - **C++ / WinRT** — en az katman, en yüksek maliyet.

**2. Kayıt hattı.** Media Foundation Sink Writer mı, yoksa ffmpeg'i yanına koyup boru
   mu? Sink Writer bağımsız çalışır ama kodek esnekliği dar; ffmpeg her şeyi kodlar ama
   ~80 MB ikili taşır ve lisans anlatmayı gerektirir.

**3. Bölge seçimi.** Overlay penceresi mi, yoksa yakalanan kareyi kırpma mı? Kırpma
   basit ama seçim önizlemesi zayıf olur.

Bu üçü karara bağlanmadan `src/` açılmadı.

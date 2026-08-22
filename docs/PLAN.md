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

## Kararlar

**Kayıt hattı: ffmpeg, ayrı süreç olarak.** (fable görüşü, 22.08.2026)

Media Foundation elenmesinin sebebi tek bir v1 maddesi: sistem sesi ile mikrofonun
ayrı izler olarak yazılması. MF'nin MPEG-4 sink'i pratikte bir video + bir ses akışına
göre yazılmış; ikinci ses izi belgelenmemiş bölge. ffmpeg'de `-map` ile önemsiz bir iş.
Çökme dayanıklılığı da ffmpeg'de tek bayrak: `-movflags +frag_keyframe+empty_moov`.

Lisans engel değil — ffmpeg ayrı süreç olarak çalıştığı sürece uygulama GPL'e bulaşmaz,
ikilinin lisansını ve kaynağa bağlantıyı vermek yeter. Donanım kodlayıcıları
(`h264_nvenc`, `h264_qsv`, `h264_amf`) ffmpeg zaten sarıyor; yazılım geri düşüşü için
LGPL derlemede x264 yoksa `h264_mf` kullanılabilir.

Karma yol (MF ile başla, dar gelirse geç) reddedildi: MF baştan dar, sonradan değil.

**Asıl maliyet kodekte değil besleme katmanında.** stdin tek borudur; ham video + iki
ses akışını aynı ffmpeg sürecine vermek Windows'ta adlandırılmış boru (`\\.\pipe\...`)
gerektirir ve zaman damgası disiplinini biz kurarız. Senkron riski buradadır.

**Bölge seçimi: overlay arayüz + kare kırpma.** (fable görüşü, 22.08.2026)

Soru yanlış kurulmuştu — ikisi rakip değil. WGC yalnız monitör veya pencere
(`GraphicsCaptureItem`) yakalar, alt-bölge API'si yok; **kırpma zaten kaçınılmaz.**
Geriye yalnız seçim arayüzünün ne olacağı kalıyor, o da overlay.

Kırpma GPU tarafında `CopySubresourceRegion` ile yapılır, maliyeti fazladan bir tam boy
doku ve bir kopya — pratikte önemsiz. Sayısal alanla bölge seçtirmek v1'de bile
kullanılamaz hissettirir; overlay her ciddi kaydedicinin standardı.

Kayıt sırasında bölgeyi taşımak bu yolda bedava. Tek tuzak: kodlayıcı çözünürlüğü
sabit, yani taşımak serbest ama **boyut değiştirmek** ya ölçekleme ister ya v1'de kilitli
olmalı.

**Asıl risk çoklu monitör + karışık DPI.** Overlay per-monitor DPI aware olmalı, seçim
fiziksel piksele çevrilmeli. v1'de bölge tek monitörle sınırlı kalsın; iki WGC oturumu
dikip birleştirmeye kalkma.

**Yığın: C# / .NET 8 + WPF.** (fable görüşü, 22.08.2026)

C++'ın hız avantajı bu mimaride bir yere değmiyor, çünkü sıcak yol yönetilen kodda
değil: yakalama DWM/GPU'da, kırpma `CopySubresourceRegion` ile GPU'da, kodlama ffmpeg
sürecinde. C# tarafına kalan iş readback ve boruya yazma — bu memcpy ve DMA işi, dil
hızı işi değil. Aynı `Map`/`WriteFile` çağrıları her iki dilde de aynı.

Çöp toplayıcı gerçek ama çözülmüş bir tehdit. Kare arabellekleri önden ayrılır
(`Marshal.AllocHGlobal` veya pinned), sıcak yolda sıfır ayırma kurulur, gerekirse
`SustainedLowLatency` kipine geçilir. Kare başına yeni dizi ayrılmadığı sürece Gen0
duraklaması (<1 ms) 16.6 ms'lik bütçeyi yemez.

C++'ın bedeli somut: WPF yerine elle Win32/kompozisyon arayüz, tepsiden overlay'e kadar
her şey pahalı, hata ayıklama yavaş. Karma yol (C++ çekirdek + C# arayüz) çekirdekte
C++'ı gerektiren iş yokken iki dilin maliyetini birden ödetir.

**Sonraya not — readback'ten kaçış yolu.** Kareyi CPU'ya indirip ham olarak boruya
vermek 1080p60'ta kabaca 190 MB/s demek. Kareyi GPU'da tutup Media Foundation **encoder
MFT**'sine verip ffmpeg'e yalnız kodlanmış akışı göndermek bunu ~1 MB/s'ye indirir.
Dikkat: MF *sink writer* iki ses izi yüzünden elenmişti, ama *encoder MFT* ayrı bir
şeydir — o eleme bunu kapsamıyor.

Yine de v1 ham boruyla başlar: basit ve hata ayıklanabilir. Readback CPU'yu ya da
senkronu zorlarsa MFT yoluna geçilir. Bu ihtimal de C++'ı gerektirmez; MFT C#'tan
CsWinRT/COM ile sürülebilir.

## Durum

Açık karar kalmadı. `src/` iskeleti kuruldu ve yakalama zinciri bu makinede doğrulandı:
2560×1440, kare akıyor, büyütülmüş pencere çalışma alanını taşımıyor, TR/EN geçişi
kırpmadan çalışıyor.

Sırada v1 listesinin 5. maddesi var: WASAPI loopback ile sistem sesi.

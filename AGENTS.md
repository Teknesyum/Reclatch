# Reclatch

Windows için hafif ekran kaydedici. OBS'in sahne sistemi yok, Bandicam'in ücreti yok.
Tek iş: ekranı kaydet, dosyayı ver.

## Omurga

- Yakalama: Windows.Graphics.Capture (WGC)
- Ses: WASAPI loopback (sistem) + mikrofon, ayrı kanallar
- Kodlama: ffmpeg ayrı süreç, donanım (NVENC / QuickSync / AMF)
- Yığın: C# / .NET 8 + WPF

## Düzen

- `docs/PLAN.md` — kapsam, v1 listesi, açık kararlar
- `src/Reclatch.Core` — yakalama ve ses, arayüzsüz
- `src/Reclatch.App` — WPF arayüz
- `locale/` — arayüz metinleri, koda gömülmez

## Kurallar

- Kod yorumu yazma.
- Arayüz işi `teknesyum-ui` standardına uyar; renk ve ölçü uydurma.
- Depoya giden belge (README, CHANGELOG) İngilizce, iç belgeler Türkçe.

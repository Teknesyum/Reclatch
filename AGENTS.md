# Reclatch

Windows için hafif ekran kaydedici. OBS'in sahne sistemi yok, Bandicam'in ücreti yok.
Tek iş: ekranı kaydet, dosyayı ver.

## Omurga

- Yakalama: Windows.Graphics.Capture (WGC)
- Ses: WASAPI loopback (sistem) + mikrofon, ayrı kanallar
- Kodlama: donanım (NVENC / QuickSync / AMF), yazılım geri düşüşü

## Düzen

- `docs/PLAN.md` — kapsam, v1 listesi, açık kararlar
- `src/` — uygulama kaynağı (yığın seçilince açılır)

## Kurallar

- Kod yorumu yazma.
- Arayüz işi `teknesyum-ui` standardına uyar; renk ve ölçü uydurma.
- Depoya giden belge (README, CHANGELOG) İngilizce, iç belgeler Türkçe.

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Uploads;
using MiniWebsite.Application.Website.Dtos;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Website;

public partial class WebsiteService
{
    public async Task<ApiResult<CompanyDetailsDto>> UploadCompanyImageAsync(
        int cardId,
        string ownerEmail,
        string kind,
        Stream file,
        string fileName,
        string contentType,
        long length,
        CancellationToken ct = default)
    {
        if (!_storage.IsEnabled)
            return ApiResult<CompanyDetailsDto>.Fail("Cloudflare storage is not configured.");
        if (file == null || length <= 0)
            return ApiResult<CompanyDetailsDto>.Fail("No image selected.");
        if (length > 2_500_000)
            return ApiResult<CompanyDetailsDto>.Fail("Image is too large (max 2.5MB).");

        var slot = (kind ?? "").Trim().ToLowerInvariant();
        if (slot is not ("logo" or "hero"))
            return ApiResult<CompanyDetailsDto>.Fail("Unknown image.");

        var type = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (type is not ("image/jpeg" or "image/jpg" or "image/png" or "image/gif" or "image/webp")
            && ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
            return ApiResult<CompanyDetailsDto>.Fail("Use a JPG, PNG, GIF, or WebP image.");
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        {
            ext = type switch
            {
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg",
            };
        }
        if (!type.StartsWith("image/", StringComparison.Ordinal))
        {
            type = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg",
            };
        }

        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null || card.UserId is not int userId)
            return ApiResult<CompanyDetailsDto>.Fail("Card not found or access denied.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return ApiResult<CompanyDetailsDto>.Fail("User not found.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0 || bytes.Length > 2_500_000)
            return ApiResult<CompanyDetailsDto>.Fail("Image is too large (max 2.5MB).");

        var storedName = slot + "-" + Guid.NewGuid().ToString("N")[..12] + ext;
        string key;
        try
        {
            key = _storage.BuildUserKey(user.UserCode, _storage.WebsitePrefix + "/" + card.Id.ToString(CultureInfo.InvariantCulture), storedName);
        }
        catch (Exception ex)
        {
            return ApiResult<CompanyDetailsDto>.Fail(ex.Message);
        }

        try
        {
            await _storage.UploadAsync(key, bytes, type, ct);
        }
        catch (Exception ex)
        {
            return ApiResult<CompanyDetailsDto>.Fail("Unable to upload image: " + ex.Message);
        }

        var previous = (slot == "hero" ? card.DHeroImageLocation : card.DLogoLocation)?.Trim().Replace('\\', '/');
        if (slot == "hero") card.DHeroImageLocation = key;
        else card.DLogoLocation = key;

        var original = Path.GetFileName(fileName ?? "");
        await _files.TrackAsync(userId, new NewUploadedFile(
            UploadedFileFolder.Website,
            slot == "hero" ? UploadedFileDocument.WebsiteHero(card.Id) : UploadedFileDocument.WebsiteLogo(card.Id),
            string.IsNullOrWhiteSpace(original) ? storedName : original,
            storedName,
            key,
            type,
            bytes.LongLength), ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            if (!string.Equals(previous, key, StringComparison.Ordinal))
            {
                try { await _storage.DeleteAsync(key, ct); } catch { /* the database row was not saved */ }
            }
            return ApiResult<CompanyDetailsDto>.Fail("Unable to save image: " + ex.Message);
        }

        var otherKey = (slot == "hero" ? card.DLogoLocation : card.DHeroImageLocation)?.Trim().Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(previous)
            && !string.Equals(previous, key, StringComparison.Ordinal)
            && !string.Equals(previous, otherKey, StringComparison.OrdinalIgnoreCase)
            && _storage.IsUnder(previous, _storage.WebsitePrefix))
        {
            try { await _storage.DeleteAsync(previous, ct); } catch { /* the card now points at the new image */ }
        }

        return ApiResult<CompanyDetailsDto>.Ok(MapCompany(card), "Image uploaded.");
    }

    public async Task<ApiResult<CompanyDetailsDto>> DeleteCompanyImageAsync(
        int cardId, string ownerEmail, string kind, CancellationToken ct = default)
    {
        var slot = (kind ?? "").Trim().ToLowerInvariant();
        if (slot is not ("logo" or "hero"))
            return ApiResult<CompanyDetailsDto>.Fail("Unknown image.");

        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null || card.UserId is not int userId)
            return ApiResult<CompanyDetailsDto>.Fail("Card not found or access denied.");

        var key = (slot == "hero" ? card.DHeroImageLocation : card.DLogoLocation)?.Trim().Replace('\\', '/');
        if (slot == "hero") card.DHeroImageLocation = "";
        else card.DLogoLocation = "";

        if (!string.IsNullOrWhiteSpace(key))
        {
            var tracked = await _db.UploadedFiles
                .Where(f => f.UserId == userId && f.ObjectKey == key)
                .ToListAsync(ct);
            _db.UploadedFiles.RemoveRange(tracked);
        }

        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(key) && _storage.IsEnabled && _storage.IsUnder(key, _storage.WebsitePrefix))
        {
            try { await _storage.DeleteAsync(key, ct); } catch { /* the card no longer points at this file */ }
        }

        return ApiResult<CompanyDetailsDto>.Ok(MapCompany(card), "Image removed.");
    }

    public Task<ApiResult<WebsiteImageDto>> UploadServiceImageAsync(
        int cardId, string ownerEmail, Stream file, string fileName, string contentType, long length, CancellationToken ct = default)
        => UploadCardImageAsync(cardId, ownerEmail, "service", file, fileName, contentType, length, ct);

    public async Task<ApiResult<WebsiteImageDto>> UploadCardImageAsync(
        int cardId,
        string ownerEmail,
        string kind,
        Stream file,
        string fileName,
        string contentType,
        long length,
        CancellationToken ct = default)
    {
        var slot = (kind ?? "").Trim().ToLowerInvariant();
        var filePrefix = slot switch
        {
            "service" => "service",
            "product" => "product",
            "offer" => "offer",
            "gallery" => "gallery",
            "payment" => "payment",
            _ => "",
        };
        var documentLabel = slot switch
        {
            "service" => "Service",
            "product" => "Product",
            "offer" => "Offer",
            "gallery" => "Gallery",
            "payment" => "Payment",
            _ => "",
        };
        if (filePrefix.Length == 0)
            return ApiResult<WebsiteImageDto>.Fail("Unknown image.");
        if (!_storage.IsEnabled)
            return ApiResult<WebsiteImageDto>.Fail("Cloudflare storage is not configured.");
        if (file == null || length <= 0)
            return ApiResult<WebsiteImageDto>.Fail("No image selected.");
        if (length > 2_500_000)
            return ApiResult<WebsiteImageDto>.Fail("Image is too large (max 2.5MB).");

        var prepared = PrepareWebsiteImage(fileName, contentType);
        if (prepared.Error != null)
            return ApiResult<WebsiteImageDto>.Fail(prepared.Error);

        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null || card.UserId is not int userId)
            return ApiResult<WebsiteImageDto>.Fail("Card not found or access denied.");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return ApiResult<WebsiteImageDto>.Fail("User not found.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0 || bytes.Length > 2_500_000)
            return ApiResult<WebsiteImageDto>.Fail("Image is too large (max 2.5MB).");
        if (slot == "payment" && !IsSquareImage(bytes))
            return ApiResult<WebsiteImageDto>.Fail("QR image must be square (1:1).");

        var stamp = AppClock.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);
        var storedName = filePrefix + "_" + stamp + prepared.Extension;
        string key;
        try
        {
            key = _storage.BuildUserKey(user.UserCode, _storage.WebsitePrefix + "/" + card.Id.ToString(CultureInfo.InvariantCulture), storedName);
        }
        catch (Exception ex)
        {
            return ApiResult<WebsiteImageDto>.Fail(ex.Message);
        }

        try
        {
            await _storage.UploadAsync(key, bytes, prepared.ContentType, ct);
        }
        catch (Exception ex)
        {
            return ApiResult<WebsiteImageDto>.Fail("Unable to upload image: " + ex.Message);
        }

        var original = Path.GetFileName(fileName ?? "");
        await _files.TrackAsync(userId, new NewUploadedFile(
            UploadedFileFolder.Website,
            documentLabel + " " + stamp,
            string.IsNullOrWhiteSpace(original) ? storedName : original,
            storedName,
            key,
            prepared.ContentType,
            bytes.LongLength), ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            try { await _storage.DeleteAsync(key, ct); } catch { /* the database row was not saved */ }
            return ApiResult<WebsiteImageDto>.Fail("Unable to save image: " + ex.Message);
        }

        var url = _storage.PublicUrl(key) ?? "";
        return ApiResult<WebsiteImageDto>.Ok(new WebsiteImageDto(key, url), "Image uploaded.");
    }

    public async Task<ApiResult> DeleteServiceImageAsync(int cardId, string ownerEmail, string imageKey, CancellationToken ct = default)
    {
        var card = await LoadOwnedCardTrackedAsync(cardId, ownerEmail, ct);
        if (card == null || card.UserId is not int userId)
            return ApiResult.Fail("Card not found or access denied.");

        var key = (imageKey ?? "").Trim().Replace('\\', '/');
        if (!IsOwnedCardImageKey(card.Id, key))
            return ApiResult.Fail("Unknown image.");

        await RemoveTrackedWebsiteImageAsync(userId, key, ct);
        await _db.SaveChangesAsync(ct);
        await DeleteWebsiteObjectAsync(key, ct);
        return ApiResult.Ok("Image removed.");
    }

    public Task<ApiResult> DeleteCardImageAsync(int cardId, string ownerEmail, string imageKey, CancellationToken ct = default)
        => DeleteServiceImageAsync(cardId, ownerEmail, imageKey, ct);

    private async Task RemoveTrackedWebsiteImageAsync(int userId, string? stored, CancellationToken ct)
    {
        var key = stored?.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(key) || !_storage.IsUnder(key, _storage.WebsitePrefix)) return;
        var tracked = await _db.UploadedFiles
            .Where(f => f.UserId == userId && f.ObjectKey == key)
            .ToListAsync(ct);
        _db.UploadedFiles.RemoveRange(tracked);
    }

    private async Task DeleteWebsiteObjectAsync(string? stored, CancellationToken ct)
    {
        var key = stored?.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(key) || !_storage.IsEnabled || !_storage.IsUnder(key, _storage.WebsitePrefix))
            return;
        try { await _storage.DeleteAsync(key, ct); } catch { /* the record no longer points at this file */ }
    }

    private bool IsOwnedCardImageKey(int cardId, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !_storage.IsUnder(key, _storage.WebsitePrefix)) return false;
        var folder = "/" + _storage.WebsitePrefix + "/" + cardId.ToString(CultureInfo.InvariantCulture) + "/";
        var index = key.IndexOf(folder, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return false;
        var name = key[(index + folder.Length)..];
        return name.StartsWith("service_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("product_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("offer_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("gallery_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("payment_", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct PreparedWebsiteImage(string Extension, string ContentType, string? Error);

    private static PreparedWebsiteImage PrepareWebsiteImage(string? fileName, string? contentType)
    {
        var type = (contentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (type is not ("image/jpeg" or "image/jpg" or "image/png" or "image/gif" or "image/webp")
            && ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
            return new PreparedWebsiteImage("", "", "Use a JPG, PNG, GIF, or WebP image.");
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        {
            ext = type switch
            {
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg",
            };
        }
        if (!type.StartsWith("image/", StringComparison.Ordinal))
        {
            type = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg",
            };
        }
        return new PreparedWebsiteImage(ext, type, null);
    }

    private static bool IsSquareImage(byte[] data)
    {
        if (!TryImageSize(data, out var width, out var height)) return false;
        return width > 0 && width == height;
    }

    private static bool TryImageSize(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length >= 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
            height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
            return width > 0 && height > 0;
        }
        if (data.Length >= 10 && data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F')
        {
            width = data[6] | (data[7] << 8);
            height = data[8] | (data[9] << 8);
            return width > 0 && height > 0;
        }
        if (data.Length >= 30 && data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F'
            && data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P')
        {
            var chunk = System.Text.Encoding.ASCII.GetString(data, 12, 4);
            if (chunk == "VP8X")
            {
                width = 1 + (data[24] | (data[25] << 8) | (data[26] << 16));
                height = 1 + (data[27] | (data[28] << 8) | (data[29] << 16));
                return width > 0 && height > 0;
            }
            if (chunk == "VP8 " && data.Length >= 30 && (data[20] & 1) == 0 && data[23] == 0x9D && data[24] == 0x01 && data[25] == 0x2A)
            {
                width = (data[26] | (data[27] << 8)) & 0x3FFF;
                height = (data[28] | (data[29] << 8)) & 0x3FFF;
                return width > 0 && height > 0;
            }
            if (chunk == "VP8L" && data.Length >= 25 && data[20] == 0x2F)
            {
                var bits = data[21] | (data[22] << 8) | (data[23] << 16) | (data[24] << 24);
                width = (bits & 0x3FFF) + 1;
                height = ((bits >> 14) & 0x3FFF) + 1;
                return width > 0 && height > 0;
            }
            return false;
        }
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) return false;
        var index = 2;
        while (index + 8 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                index++;
                continue;
            }
            while (index < data.Length && data[index] == 0xFF) index++;
            if (index >= data.Length) return false;
            var marker = data[index++];
            if (marker is 0xD8 or 0xD9 or 0x01 || marker is >= 0xD0 and <= 0xD7) continue;
            if (index + 1 >= data.Length) return false;
            var length = (data[index] << 8) | data[index + 1];
            if (length < 2 || index + length > data.Length) return false;
            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                height = (data[index + 3] << 8) | data[index + 4];
                width = (data[index + 5] << 8) | data[index + 6];
                return width > 0 && height > 0;
            }
            index += length;
        }
        return false;
    }
}

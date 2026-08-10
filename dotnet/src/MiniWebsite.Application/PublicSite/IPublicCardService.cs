using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.PublicSite.Dtos;

namespace MiniWebsite.Application.PublicSite;

public interface IPublicCardService
{
    /// <summary>Resolve live public digi-card by slug or numeric id (n.php?n=…).</summary>
    Task<ApiResult<PublicCardViewerDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
}

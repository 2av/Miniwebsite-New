using MiniWebsite.Application.Admin.EmailTemplates.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.EmailTemplates;

public interface IAdminEmailTemplatesService
{
    Task<ApiResult<EmailTemplateMetaDto>> GetMetaAsync(CancellationToken ct = default);
    Task<ApiResult<EmailTemplatePageDto>> ListAsync(EmailTemplateQuery query, CancellationToken ct = default);
    Task<ApiResult<EmailTemplateRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<EmailTemplateRowDto>> GetByKeyAsync(string templateKey, CancellationToken ct = default);
    Task<ApiResult<EmailTemplateRowDto>> CreateAsync(UpsertEmailTemplateRequest request, CancellationToken ct = default);
    Task<ApiResult<EmailTemplateRowDto>> UpdateAsync(int id, UpsertEmailTemplateRequest request, CancellationToken ct = default);
    Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
    Task<ApiResult<PreviewEmailTemplateDto>> PreviewAsync(PreviewEmailTemplateRequest request, CancellationToken ct = default);
}

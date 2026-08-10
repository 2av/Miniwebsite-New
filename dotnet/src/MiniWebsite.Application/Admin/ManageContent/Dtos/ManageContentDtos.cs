namespace MiniWebsite.Application.Admin.ManageContent.Dtos;

public record ContentTypeOptionDto(string Value, string Label, string Badge);

public record ManageContentMetaDto(IReadOnlyList<ContentTypeOptionDto> ContentTypes);

public record ManageContentItemDto(
    string ContentType,
    string Title,
    string Content,
    string MetaDescription,
    string MetaKeywords,
    string? LastUpdated,
    string? LastUpdatedDisplay,
    string? UpdatedBy);

public record ManageContentListDto(IReadOnlyList<ManageContentItemDto> Items);

public record UpsertContentRequest(
    string ContentType,
    string Title,
    string Content,
    string? MetaDescription,
    string? MetaKeywords,
    string? UpdatedBy);

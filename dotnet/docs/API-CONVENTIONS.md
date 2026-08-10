# API Conventions

## Base URL

`/api/v1/{resource}`

## Response envelope

Success:

```json
{ "success": true, "message": optional, "data": { } }
```

Failure:

```json
{ "success": false, "message": "...", "errors": { "Field": ["..."] } }
```

## CRUD verbs

| Method | Route | Meaning |
|--------|-------|---------|
| GET | `/items` | Paged list |
| GET | `/items/{id}` | Detail |
| POST | `/items` | Create |
| PUT/PATCH | `/items/{id}` | Update |
| DELETE | `/items/{id}` | Soft delete preferred |

## Money

Amounts in **rupees** as `decimal` with 2 places in API contracts. Payment gateway may convert to paise internally.

## Auth (from Phase 1)

`Authorization: Bearer {access_token}`

Roles: `Customer`, `Franchisee`, `Team`, `Admin`

## Website media (PHP upload → .NET save filename)

API and PHP are on different domains. **Do not upload files to the .NET API.**

1. Frontend uploads file to **PHP** (later) → PHP saves under `/assets/upload/websites/{folder}/`
2. PHP returns `{ fileName }`
3. Frontend calls .NET with that `fileName` only (`logoLocation`, `productImage`, `galleryImage`, `offerImage`)
4. .NET responses include both filename and full PHP URL (`logoUrl`, `productImageUrl`, …)

Contract endpoint: `GET /api/v1/digi-cards/media/upload-contract`

Folder map:

| Type | Folder |
|------|--------|
| Logo / hero | `company_details` |
| Products | `product-pricing` |
| Services | `product-and-services` |
| Gallery | `image-gallery` |
| Offers | `special-offers` |


using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE2026.Plugin.Models.DTOs;
using OVCMOVE2026.Plugin.Repositories.Queries;

namespace OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionDetail;

public sealed record GetSecretMissionDetailQuery(Guid Id, Guid TeamId) : IRequest<SecretMissionDetailDto?>;

public class GetSecretMissionDetailQueryHandler : IRequestHandler<GetSecretMissionDetailQuery, SecretMissionDetailDto?>
{
    private readonly IDbExecutor _db;

    public GetSecretMissionDetailQueryHandler(IDbExecutor db)
    {
        _db = db;
    }

    // Class nội bộ (Raw) để hứng dữ liệu JSON dạng chuỗi từ Database trước khi Deserialize
    private class RawSecretMissionDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAssigned { get; set; } // Sửa Status thành IsAssigned
        public string? EvidenceImageUrlsJson { get; set; } // Hứng bí danh Alias từ SQL
        public string? EvidenceVideoUrlsJson { get; set; } // Hứng bí danh Alias từ SQL
        public DateTime? SubmittedTime { get; set; }
    }

    public async Task<SecretMissionDetailDto?> Handle(GetSecretMissionDetailQuery request, CancellationToken cancellationToken)
    {
        var raw = await _db.QueryFirstOrDefaultAsync<RawSecretMissionDetail>(
            SecretMissionQueries.GetDetailByIdAndTeamIdQuery(),
            new { request.Id, request.TeamId },
            cancellationToken: cancellationToken);

        if (raw == null) return null; // Trả về null để Controller biết là Not Found

        // Map từ Raw DB sang DTO chuẩn cho Frontend, Deserialize chuỗi JSON thành List<string>
        return new SecretMissionDetailDto
        {
            Id = raw.Id,
            Name = raw.Name,
            Description = raw.Description,
            IsAssigned = raw.IsAssigned, // Map giá trị IsAssigned
            SubmittedTime = raw.SubmittedTime,
            EvidenceImageUrls = string.IsNullOrWhiteSpace(raw.EvidenceImageUrlsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(raw.EvidenceImageUrlsJson),
            EvidenceVideoUrls = string.IsNullOrWhiteSpace(raw.EvidenceVideoUrlsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(raw.EvidenceVideoUrlsJson)
        };
    }
}
using System;
using System.Collections.Generic;
using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE2026.Plugin.CQRS.Commands.SubmitMissionEvidence;

/// <summary>
/// Command vận chuyển dữ liệu nộp bằng chứng từ Controller xuống Handler
/// </summary>
public sealed record SubmitMissionEvidenceCommand(
    Guid MissionId,
    Guid SubmittedBy,
    List<FileUploadModel>? Images,
    List<FileUploadModel>? Videos
) : IRequest<bool>;
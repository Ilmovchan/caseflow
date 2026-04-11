using CaseFlow.DAL.Enums;

namespace CaseFlow.BLL.Localization;

/// <summary>Ukrainian UI strings for enums stored as PostgreSQL enum labels.</summary>
public static class UkLabels
{
    public static string CaseStatusLabel(CaseStatus s) => s switch
    {
        CaseStatus.Opened => "Відкрито",
        CaseStatus.Closed => "Закрито",
        CaseStatus.Paused => "Призупинено",
        _ => s.ToString()
    };

    public static string DetectiveStatusLabel(DetectiveStatus s) => s switch
    {
        DetectiveStatus.Active => "Активний(а)",
        DetectiveStatus.OnVacation => "У відпустці",
        DetectiveStatus.Retired => "У відставці",
        DetectiveStatus.Fired => "Звільнений(а)",
        _ => s.ToString()
    };

    public static string ApprovalStatusLabel(ApprovalStatus s) => s switch
    {
        ApprovalStatus.Draft => "Чернетка",
        ApprovalStatus.Pending => "Надіслано",
        ApprovalStatus.Approved => "Схвалено",
        ApprovalStatus.Declined => "Відхилено",
        _ => s.ToString()
    };

    public static string EvidenceTypeLabel(EvidenceType t) => t switch
    {
        EvidenceType.Biometric => "Біометричний доказ",
        EvidenceType.Biological => "Біологічний доказ",
        EvidenceType.Video => "Відеодоказ",
        EvidenceType.Photo => "Фотодоказ",
        EvidenceType.Physical => "Матеріальний доказ",
        EvidenceType.Digital => "Цифровий доказ",
        EvidenceType.Document => "Документальний доказ",
        EvidenceType.Audio => "Аудіодоказ",
        EvidenceType.Object => "Фізичний доказ",
        EvidenceType.Electronic => "Електронний доказ",
        EvidenceType.Other => "Інше",
        _ => t.ToString()
    };
}

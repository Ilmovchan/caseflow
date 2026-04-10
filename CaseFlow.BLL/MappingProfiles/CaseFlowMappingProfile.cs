using AutoMapper;
using CaseFlow.BLL.Dto.Auth;
using CaseFlow.BLL.Dto.Case;
using CaseFlow.BLL.Dto.CaseType;
using CaseFlow.BLL.Dto.Client;
using CaseFlow.BLL.Dto.Detective;
using CaseFlow.BLL.Dto.Evidence;
using CaseFlow.BLL.Dto.Expense;
using CaseFlow.BLL.Dto.Report;
using CaseFlow.BLL.Dto.Suspect;
using CaseFlow.DAL.Models;

namespace CaseFlow.BLL.MappingProfiles;

public class CaseFlowMappingProfile : Profile
{
    public CaseFlowMappingProfile()
    {
        CreateMap<CreateCaseDto, Case>();
        
        CreateMap<UpdateCaseByAdminDto, Case>()
            .ForMember(dest => dest.CaseTypeId, opt => opt.Condition(src => src.CaseTypeId.HasValue))
            .ForMember(dest => dest.ClientId, opt => opt.Condition(src => src.ClientId.HasValue))
            .ForMember(dest => dest.DetectiveId, opt => opt.Condition(src => src.DetectiveId.HasValue))
            .ForMember(dest => dest.Title, opt => opt.Condition(src => src.Title != null))
            .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
            .ForMember(dest => dest.DeadlineDate, opt => opt.Condition(src => src.DeadlineDate.HasValue))
            .ForMember(dest => dest.CloseDate, opt => opt.Condition(src => src.CloseDate.HasValue))
            .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status.HasValue));

        CreateMap<UpdateCaseByDetectiveDto, Case>()
            .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
            .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status.HasValue));

        // Case to CaseDto mapping with navigation properties
        CreateMap<Case, CaseDto>()
            .ForMember(dest => dest.CaseTypeName, opt => opt.MapFrom(src => src.CaseType != null ? src.CaseType.Name : ""))
            .ForMember(dest => dest.ClientFullName, opt => opt.MapFrom(src => src.Client != null ? 
                $"{src.Client.LastName} {src.Client.FirstName}" + (src.Client.FatherName != null ? $" {src.Client.FatherName}" : "") : ""))
            .ForMember(dest => dest.DetectiveFullName, opt => opt.MapFrom(src => src.Detective != null ? 
                $"{src.Detective.LastName} {src.Detective.FirstName}" + (src.Detective.FatherName != null ? $" {src.Detective.FatherName}" : "") : null));
        
        CreateMap<CreateCaseTypeDto, CaseType>();

        CreateMap<UpdateCaseTypeDto, CaseType>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // CaseType to CaseTypeDto mapping
        CreateMap<CaseType, CaseTypeDto>();
        
        CreateMap<CreateClientDto, Client>();
        
        CreateMap<UpdateClientDto, Client>()
            .ForMember(dest => dest.ApartmentNumber, opt => opt.Condition(src => src.ApartmentNumber != null))
            .ForMember(dest => dest.BuildingNumber, opt => opt.Condition(src => src.BuildingNumber != null))
            .ForMember(dest => dest.City, opt => opt.Condition(src => src.City != null))
            .ForMember(dest => dest.DateOfBirth, opt => opt.Condition(src => src.DateOfBirth.HasValue))
            .ForMember(dest => dest.Email, opt => opt.Condition(src => src.Email != null))
            .ForMember(dest => dest.FatherName, opt => opt.Condition(src => src.FatherName != null))
            .ForMember(dest => dest.FirstName, opt => opt.Condition(src => src.FirstName != null))
            .ForMember(dest => dest.LastName, opt => opt.Condition(src => src.LastName != null))
            .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => src.PhoneNumber != null))
            .ForMember(dest => dest.Region, opt => opt.Condition(src => src.Region != null))
            .ForMember(dest => dest.Street, opt => opt.Condition(src => src.Street != null));

        // Client to ClientDto mapping
        CreateMap<Client, ClientDto>();

        CreateMap<CreateDetectiveDto, Detective>();

        CreateMap<UpdateDetectiveDto, Detective>()
            .ForMember(dest => dest.FirstName, opt => opt.Condition(src => src.FirstName != null))
            .ForMember(dest => dest.LastName, opt => opt.Condition(src => src.LastName != null))
            .ForMember(dest => dest.FatherName, opt => opt.Condition(src => src.FatherName != null))
            .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => src.PhoneNumber != null))
            .ForMember(dest => dest.Email, opt => opt.Condition(src => src.Email != null))
            .ForMember(dest => dest.DateOfBirth, opt => opt.Condition(src => src.DateOfBirth.HasValue))
            .ForMember(dest => dest.Region, opt => opt.Condition(src => src.Region != null))
            .ForMember(dest => dest.City, opt => opt.Condition(src => src.City != null))
            .ForMember(dest => dest.Street, opt => opt.Condition(src => src.Street != null))
            .ForMember(dest => dest.BuildingNumber, opt => opt.Condition(src => src.BuildingNumber != null))
            .ForMember(dest => dest.ApartmentNumber, opt => opt.Condition(src => src.ApartmentNumber.HasValue))
            .ForMember(dest => dest.Salary, opt => opt.Condition(src => src.Salary.HasValue))
            .ForMember(dest => dest.PersonalNotes, opt => opt.Condition(src => src.PersonalNotes != null));

        // Detective to DetectiveDto mapping
        CreateMap<Detective, DetectiveDto>();
        
        CreateMap<CreateSuspectDto, Suspect>();

        CreateMap<Suspect, SuspectDto>()
            .ForMember(dest => dest.FirstName, opt => opt.Condition(src => src.FirstName != null))
            .ForMember(dest => dest.LastName, opt => opt.Condition(src => src.LastName != null))
            .ForMember(dest => dest.FatherName, opt => opt.Condition(src => src.FatherName != null))
            .ForMember(dest => dest.Nickname, opt => opt.Condition(src => src.Nickname != null))
            .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => src.PhoneNumber != null))
            .ForMember(dest => dest.DateOfBirth, opt => opt.Condition(src => src.DateOfBirth.HasValue))
            .ForMember(dest => dest.Region, opt => opt.Condition(src => src.Region != null))
            .ForMember(dest => dest.City, opt => opt.Condition(src => src.City != null))
            .ForMember(dest => dest.Street, opt => opt.Condition(src => src.Street != null))
            .ForMember(dest => dest.BuildingNumber, opt => opt.Condition(src => src.BuildingNumber != null))
            .ForMember(dest => dest.ApartmentNumber, opt => opt.Condition(src => src.ApartmentNumber.HasValue))
            .ForMember(dest => dest.Height, opt => opt.Condition(src => src.Height.HasValue))
            .ForMember(dest => dest.Weight, opt => opt.Condition(src => src.Weight.HasValue))
            .ForMember(dest => dest.PhysicalDescription, opt => opt.Condition(src => src.PhysicalDescription != null))
            .ForMember(dest => dest.PriorConvictions, opt => opt.Condition(src => src.PriorConvictions != null));
            
            
        CreateMap<UpdateSuspectDto, Suspect>()
            .ForMember(dest => dest.FirstName, opt => opt.Condition(src => src.FirstName != null))
            .ForMember(dest => dest.LastName, opt => opt.Condition(src => src.LastName != null))
            .ForMember(dest => dest.FatherName, opt => opt.Condition(src => src.FatherName != null))
            .ForMember(dest => dest.Nickname, opt => opt.Condition(src => src.Nickname != null))
            .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => src.PhoneNumber != null))
            .ForMember(dest => dest.DateOfBirth, opt => opt.Condition(src => src.DateOfBirth.HasValue))
            .ForMember(dest => dest.Region, opt => opt.Condition(src => src.Region != null))
            .ForMember(dest => dest.City, opt => opt.Condition(src => src.City != null))
            .ForMember(dest => dest.Street, opt => opt.Condition(src => src.Street != null))
            .ForMember(dest => dest.BuildingNumber, opt => opt.Condition(src => src.BuildingNumber != null))
            .ForMember(dest => dest.ApartmentNumber, opt => opt.Condition(src => src.ApartmentNumber.HasValue))
            .ForMember(dest => dest.Height, opt => opt.Condition(src => src.Height.HasValue))
            .ForMember(dest => dest.Weight, opt => opt.Condition(src => src.Weight.HasValue))
            .ForMember(dest => dest.PhysicalDescription, opt => opt.Condition(src => src.PhysicalDescription != null))
            .ForMember(dest => dest.PriorConvictions, opt => opt.Condition(src => src.PriorConvictions != null));

        CreateMap<CreateReportDto, Report>();

        CreateMap<Report, ReportDto>()
            .ForMember(dest => dest.CaseTitle, opt => opt.MapFrom(src => src.Case != null ? src.Case.Title : ""))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        
        CreateMap<UpdateReportDto, Report>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateExpenseDto, Expense>();

        CreateMap<Expense, ExpenseDto>()
            .ForMember(dest => dest.DateTime, opt => opt.Condition(src => src.DateTime != default))
            .ForMember(dest => dest.Purpose, opt => opt.Condition(src => src.Purpose != null))
            .ForMember(dest => dest.Amount, opt => opt.Condition(src => src.Amount != default))
            .ForMember(dest => dest.Annotation, opt => opt.Condition(src => src.Annotation != null))
            .ForMember(dest => dest.CaseId, opt => opt.Condition(src => src.CaseId != default))
            .ForMember(dest => dest.CaseTitle, opt => opt.MapFrom(src => src.Case != null ? src.Case.Title : ""));
        
        CreateMap<UpdateExpenseDto, Expense>()
            .ForMember(dest => dest.DateTime, opt => opt.Condition(src => src.DateTime != default))
            .ForMember(dest => dest.Purpose, opt => opt.Condition(src => src.Purpose != null))
            .ForMember(dest => dest.Amount, opt => opt.Condition(src => src.Amount != default))
            .ForMember(dest => dest.Annotation, opt => opt.Condition(src => src.Annotation != null));
        
        CreateMap<CreateEvidenceDto, Evidence>();

        CreateMap<Evidence, EvidenceCaseDto>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Evidence, EvidenceDto>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Evidence?, Evidence>();
        
        CreateMap<UpdateEvidenceDto, Evidence>()
            .ForMember(dest => dest.Type, opt => opt.Condition(src => src.Type.HasValue))
            .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
            .ForMember(dest => dest.CollectionDate, opt => opt.Condition(src => src.CollectionDate.HasValue))
            .ForMember(dest => dest.Region, opt => opt.Condition(src => src.Region != null))
            .ForMember(dest => dest.Annotation, opt => opt.Condition(src => src.Annotation != null))
            .ForMember(dest => dest.Purpose, opt => opt.Condition(src => src.Purpose != null));
        
    }
}
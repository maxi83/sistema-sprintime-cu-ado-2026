using System.ComponentModel.DataAnnotations;
namespace lucho_sprintime.ViewModels;
public class CyclistAccountInput
{
 [Required,MaxLength(80)] public string FirstName {get;set;}="";
 [Required,MaxLength(80)] public string LastName {get;set;}="";
 [Required,MaxLength(30)] public string Dni {get;set;}="";
 [Required,EmailAddress,MaxLength(160)] public string Email {get;set;}="";
 [Required,MinLength(10),MaxLength(100),DataType(DataType.Password)] public string Password {get;set;}="";
 [Required,DataType(DataType.Date)] public DateTime? BirthDate {get;set;}
 [Required,MaxLength(40)] public string Phone {get;set;}="";
 [MaxLength(100)] public string City {get;set;}="";
 [MaxLength(120)] public string Team {get;set;}="";
 [Required,MaxLength(160)] public string EmergencyContact {get;set;}="";
 [Required,MaxLength(40)] public string EmergencyPhone {get;set;}="";
 [MaxLength(40)] public string? SecondEmergencyPhone {get;set;}
 [MaxLength(10)] public string? BloodType {get;set;}
 public bool HasInsurance {get;set;}
 [MaxLength(120)] public string? InsuranceNumber {get;set;}
 [MaxLength(1000)] public string? MedicalConditions {get;set;}
 [MaxLength(1000)] public string? MedicalNotes {get;set;}
 [Range(typeof(bool),"true","true",ErrorMessage="Necesitamos tu consentimiento para guardar los datos de emergencia y salud.")] public bool MedicalConsent {get;set;}
 public string? ReturnUrl {get;set;}
}

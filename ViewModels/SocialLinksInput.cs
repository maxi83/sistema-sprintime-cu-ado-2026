using System.ComponentModel.DataAnnotations;
namespace lucho_sprintime.ViewModels;
public class SocialLinksInput
{
 [Url,MaxLength(500),Display(Name="Facebook")] public string? FacebookUrl {get;set;}
 [Url,MaxLength(500),Display(Name="Instagram")] public string? InstagramUrl {get;set;}
 [Url,MaxLength(500),Display(Name="Grupo de WhatsApp")] public string? WhatsappGroupUrl {get;set;}
 public bool FacebookEnabled {get;set;}
 public bool InstagramEnabled {get;set;}
 public bool WhatsappGroupEnabled {get;set;}
}

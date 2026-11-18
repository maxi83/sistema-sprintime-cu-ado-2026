namespace lucho_sprintime.Models;
public class SocialLinks
{
 public int Id { get; set; }
 public string? FacebookUrl { get; set; }
 public string? InstagramUrl { get; set; }
 public string? WhatsappGroupUrl { get; set; }
 public bool RankingEnabled { get; set; }
 public bool FacebookEnabled { get; set; }
 public bool InstagramEnabled { get; set; }
 public bool WhatsappGroupEnabled { get; set; }
}

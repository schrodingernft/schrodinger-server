using System.ComponentModel.DataAnnotations;

namespace SchrodingerServer.Users.Dto;

public class GetPointsEarnedDetailInput
{
    [Required] public string DappName { get; set; }
    [Required] public string Domain { get; set; }
    [Required] public string Address { get; set; }
}
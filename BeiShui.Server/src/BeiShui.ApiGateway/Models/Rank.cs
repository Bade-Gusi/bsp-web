namespace BeiShui.ApiGateway.Models;

public class Rank
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MinMMR { get; set; }
    public int MaxMMR { get; set; }
    public string IconUrl { get; set; } = "";
}
